using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;

namespace OneStrokeDemon.Levels
{
    // 定义 BattleFlowAdvanceReport 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class BattleFlowAdvanceReport
    {
        // 初始化 BattleFlowAdvanceReport，并建立关卡流程所需的初始状态。
        internal BattleFlowAdvanceReport(
            in BattleTimeSlice time,
            LevelAdvanceReport level,
            bool settledThisAdvance,
            BattleFlowState state)
        {
            Time = time;
            Level = level ?? throw new ArgumentNullException(nameof(level));
            SettledThisAdvance = settledThisAdvance;
            State = state;
        }

        public BattleTimeSlice Time { get; }

        public LevelAdvanceReport Level { get; }

        public bool SettledThisAdvance { get; }

        public BattleFlowState State { get; }
    }

    // 定义 BattleFlowCoordinator 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class BattleFlowCoordinator
    {
        private static readonly IReadOnlyList<LevelRuntimeEvent> NoLevelEvents =
            Array.AsReadOnly(Array.Empty<LevelRuntimeEvent>());

        private readonly BattleFlowStateMachine flow;
        private readonly LevelRunner level;

        // 初始化 BattleFlowCoordinator，并建立关卡流程所需的初始状态。
        public BattleFlowCoordinator(
            IConfigProvider configProvider,
            string playerId,
            string levelId,
            ILevelSpawnWorld spawnWorld)
            : this(
                new BattleFlowStateMachine(
                    BattleFlowSettingsFactory.Create(configProvider, playerId)),
                new LevelRunner(configProvider, levelId, spawnWorld))
        {
        }

        // 初始化 BattleFlowCoordinator，并建立关卡流程所需的初始状态。
        public BattleFlowCoordinator(
            BattleFlowStateMachine stateMachine,
            LevelRunner levelRunner)
        {
            flow = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            level = levelRunner ?? throw new ArgumentNullException(nameof(levelRunner));
            SynchronizeLevelPause();
        }

        public BattleFlowStateMachine Flow => flow;

        public LevelRunner Level => level;

        // 推进 Advance 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public BattleFlowAdvanceReport Advance(
            double unscaledDeltaSeconds,
            bool playerDied = false)
        {
            BattleTimeSlice time = flow.Advance(unscaledDeltaSeconds);
            LevelAdvanceReport levelReport = new LevelAdvanceReport(NoLevelEvents);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (IsGameplayActive(flow.State))
            {
                level.SetPaused(false);
                levelReport = level.Advance(time.GameplayDeltaSeconds);
            }

            bool settled = flow.ResolveOutcome(new BattleOutcomeFacts(
                playerDied,
                level.State == LevelRunnerState.Completed,
                level.DurationLimitReached));
            SynchronizeLevelPause();
            return new BattleFlowAdvanceReport(
                time,
                levelReport,
                settled,
                flow.State);
        }

        // 处理 ConfirmPlayerAction 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool ConfirmPlayerAction()
        {
            return flow.State == BattleFlowState.Playing &&
                   level.ConfirmPlayerAction();
        }

        // 处理 NotifyEnemyDefeated 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool NotifyEnemyDefeated(long entityId)
        {
            return IsGameplayActive(flow.State) &&
                   level.NotifyEnemyDefeated(entityId);
        }

        // 尝试执行 TryBeginUltimateDrawing 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool TryBeginUltimateDrawing()
        {
            return flow.TryBeginUltimateDrawing();
        }

        // 判断是否允许 CanAcceptUltimateGestureEvent 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool CanAcceptUltimateGestureEvent(ulong gestureEventId)
        {
            return flow.CanAcceptUltimateGestureEvent(gestureEventId);
        }

        // 解析 ResolveUltimate 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool ResolveUltimate(
            ulong gestureEventId,
            in SkillActivationResult result)
        {
            return flow.ResolveUltimate(gestureEventId, result);
        }

        // 判断是否允许 CancelUltimateDrawing 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool CancelUltimateDrawing()
        {
            return flow.CancelUltimateDrawing();
        }

        // 设置 SetPlayerPaused 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetPlayerPaused(bool paused)
        {
            bool changed = flow.SetPlayerPaused(paused);
            SynchronizeLevelPause();
            return changed;
        }

        // 设置 SetApplicationFocus 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetApplicationFocus(bool hasFocus)
        {
            bool changed = flow.SetApplicationFocus(hasFocus);
            SynchronizeLevelPause();
            return changed;
        }

        // 设置 SetApplicationPaused 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetApplicationPaused(bool paused)
        {
            bool changed = flow.SetApplicationPaused(paused);
            SynchronizeLevelPause();
            return changed;
        }

        // 应用 ApplyGameplayScale 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public void ApplyGameplayScale(double scale, double durationSeconds)
        {
            flow.Time.ApplyGameplayScale(scale, durationSeconds);
        }

        // 处理 SynchronizeLevelPause 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void SynchronizeLevelPause()
        {
            level.SetPaused(!IsGameplayActive(flow.State));
        }

        // 判断是否 IsGameplayActive 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool IsGameplayActive(BattleFlowState state)
        {
            return state == BattleFlowState.Playing ||
                   state == BattleFlowState.UltimateDrawing;
        }
    }
}
