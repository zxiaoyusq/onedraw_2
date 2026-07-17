using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;

namespace OneStrokeDemon.Levels
{
    // 定义 TutorialLevelCoordinator 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class TutorialLevelCoordinator
    {
        private readonly BattleFlowCoordinator battle;
        private readonly TutorialSequence tutorial;
        private bool battleReadyPublished;
        private bool tutorialCompletionGatePending;

        // 初始化 TutorialLevelCoordinator，并建立关卡流程所需的初始状态。
        public TutorialLevelCoordinator(
            IConfigProvider configProvider,
            string playerId,
            string levelId,
            ILevelSpawnWorld spawnWorld)
            : this(
                new BattleFlowCoordinator(
                    configProvider,
                    playerId,
                    levelId,
                    spawnWorld),
                new TutorialSequence(
                    TutorialDefinitionFactory.Create(configProvider, levelId)))
        {
        }

        // 初始化 TutorialLevelCoordinator，并建立关卡流程所需的初始状态。
        public TutorialLevelCoordinator(
            BattleFlowCoordinator battleFlow,
            TutorialSequence tutorialSequence)
        {
            battle = battleFlow ?? throw new ArgumentNullException(nameof(battleFlow));
            tutorial = tutorialSequence ??
                throw new ArgumentNullException(nameof(tutorialSequence));
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!string.Equals(
                    battle.Level.Definition.LevelId,
                    tutorial.Definition.LevelId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Battle level and tutorial definition must have the same level id.",
                    nameof(tutorialSequence));
            }

            SynchronizeLevelProgressGate();
        }

        public BattleFlowCoordinator Battle => battle;

        public TutorialSequence Tutorial => tutorial;

        // 推进 Advance 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public BattleFlowAdvanceReport Advance(
            double unscaledDeltaSeconds,
            bool playerDied = false)
        {
            BattleFlowAdvanceReport report = battle.Advance(
                unscaledDeltaSeconds,
                playerDied);

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!battleReadyPublished &&
                IsGameplayActive(report.State) &&
                tutorial.State != TutorialSequenceState.Completed)
            {
                battleReadyPublished = true;
                ApplyTutorialUpdate(tutorial.Notify(new TutorialGameplayEvent(
                    TutorialEventType.BattleReady)));
            }

            ApplyTutorialUpdate(tutorial.Advance(
                report.Time.GameplayUnscaledDeltaSeconds));
            SynchronizeLevelProgressGate();
            TryConfirmCompletedTutorialGate();
            return report;
        }

        // 处理 SkipTutorial 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public TutorialUpdateReport SkipTutorial()
        {
            TutorialUpdateReport report = tutorial.Skip();
            ApplyTutorialUpdate(report);
            SynchronizeLevelProgressGate();
            TryConfirmCompletedTutorialGate();
            return report;
        }

        // 处理 NotifyGameplayEvent 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public TutorialUpdateReport NotifyGameplayEvent(
            in TutorialGameplayEvent gameplayEvent)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!gameplayEvent.IsValid)
            {
                throw new ArgumentException(
                    "Tutorial gameplay event must be initialized.",
                    nameof(gameplayEvent));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!IsGameplayActive(battle.Flow.State) ||
                tutorial.State == TutorialSequenceState.Completed)
            {
                return default;
            }

            TutorialUpdateReport report = tutorial.Notify(gameplayEvent);
            ApplyTutorialUpdate(report);
            SynchronizeLevelProgressGate();
            return report;
        }

        // 处理 NotifyEnemyDefeated 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool NotifyEnemyDefeated(long entityId)
        {
            bool accepted = battle.NotifyEnemyDefeated(entityId);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (accepted)
            {
                TryConfirmCompletedTutorialGate();
            }

            return accepted;
        }

        // 尝试执行 TryBeginUltimateDrawing 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool TryBeginUltimateDrawing()
        {
            return battle.TryBeginUltimateDrawing();
        }

        // 判断是否允许 CanAcceptUltimateGestureEvent 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool CanAcceptUltimateGestureEvent(ulong gestureEventId)
        {
            return battle.CanAcceptUltimateGestureEvent(gestureEventId);
        }

        // 解析 ResolveUltimate 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool ResolveUltimate(
            ulong gestureEventId,
            in SkillActivationResult result)
        {
            bool resolved = battle.ResolveUltimate(gestureEventId, result);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (resolved && result.Succeeded)
            {
                NotifyGameplayEvent(new TutorialGameplayEvent(
                    TutorialEventType.UltimateSucceeded,
                    value: 1L,
                    gestureType: TutorialProtocol.ParseGesture(
                        battle.Flow.Settings.UltimateGestureType,
                        tutorial.Definition.TutorialId,
                        tutorial.CurrentStep == null
                            ? tutorial.Definition.Steps.Count
                            : tutorial.CurrentStep.Order)));
            }

            return resolved;
        }

        // 判断是否允许 CancelUltimateDrawing 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool CancelUltimateDrawing()
        {
            return battle.CancelUltimateDrawing();
        }

        // 设置 SetPlayerPaused 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetPlayerPaused(bool paused)
        {
            bool changed = battle.SetPlayerPaused(paused);
            SynchronizeLevelProgressGate();
            return changed;
        }

        // 设置 SetApplicationFocus 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetApplicationFocus(bool hasFocus)
        {
            bool changed = battle.SetApplicationFocus(hasFocus);
            SynchronizeLevelProgressGate();
            return changed;
        }

        // 设置 SetApplicationPaused 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetApplicationPaused(bool paused)
        {
            bool changed = battle.SetApplicationPaused(paused);
            SynchronizeLevelProgressGate();
            return changed;
        }

        // 应用 ApplyGameplayScale 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public void ApplyGameplayScale(double scale, double durationSeconds)
        {
            battle.ApplyGameplayScale(scale, durationSeconds);
        }

        // 应用 ApplyTutorialUpdate 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void ApplyTutorialUpdate(in TutorialUpdateReport report)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!report.TutorialCompleted)
            {
                return;
            }

            tutorialCompletionGatePending = true;
            TryConfirmCompletedTutorialGate();
        }

        // 尝试执行 TryConfirmCompletedTutorialGate 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void TryConfirmCompletedTutorialGate()
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!tutorialCompletionGatePending)
            {
                return;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (battle.Level.State == LevelRunnerState.Completed)
            {
                tutorialCompletionGatePending = false;
                return;
            }

            WaveRunner wave = battle.Level.CurrentWave;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (wave.Definition.EndCondition != WaveEndCondition.PlayerConfirmed ||
                !wave.Scheduler.IsComplete ||
                wave.ActiveCount != 0)
            {
                return;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (battle.ConfirmPlayerAction())
            {
                tutorialCompletionGatePending = false;
            }
        }

        // 处理 SynchronizeLevelProgressGate 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void SynchronizeLevelProgressGate()
        {
            bool blocked = false;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (battle.Level.State != LevelRunnerState.Completed &&
                tutorial.State == TutorialSequenceState.Active)
            {
                TutorialStepDefinition current = tutorial.CurrentStep;
                blocked = current.BlockProgress;
            }

            battle.Level.SetProgressBlocked(blocked);
        }

        // 判断是否 IsGameplayActive 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool IsGameplayActive(BattleFlowState state)
        {
            return state == BattleFlowState.Playing ||
                   state == BattleFlowState.UltimateDrawing;
        }
    }
}
