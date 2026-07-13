using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;

namespace OneStrokeDemon.Levels
{
    public sealed class BattleFlowAdvanceReport
    {
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

    public sealed class BattleFlowCoordinator
    {
        private static readonly IReadOnlyList<LevelRuntimeEvent> NoLevelEvents =
            Array.AsReadOnly(Array.Empty<LevelRuntimeEvent>());

        private readonly BattleFlowStateMachine flow;
        private readonly LevelRunner level;

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

        public BattleFlowAdvanceReport Advance(
            double unscaledDeltaSeconds,
            bool playerDied = false)
        {
            BattleTimeSlice time = flow.Advance(unscaledDeltaSeconds);
            LevelAdvanceReport levelReport = new LevelAdvanceReport(NoLevelEvents);
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

        public bool ConfirmPlayerAction()
        {
            return flow.State == BattleFlowState.Playing &&
                   level.ConfirmPlayerAction();
        }

        public bool NotifyEnemyDefeated(long entityId)
        {
            return IsGameplayActive(flow.State) &&
                   level.NotifyEnemyDefeated(entityId);
        }

        public bool TryBeginUltimateDrawing()
        {
            return flow.TryBeginUltimateDrawing();
        }

        public bool CanAcceptUltimateGestureEvent(ulong gestureEventId)
        {
            return flow.CanAcceptUltimateGestureEvent(gestureEventId);
        }

        public bool ResolveUltimate(
            ulong gestureEventId,
            in SkillActivationResult result)
        {
            return flow.ResolveUltimate(gestureEventId, result);
        }

        public bool CancelUltimateDrawing()
        {
            return flow.CancelUltimateDrawing();
        }

        public bool SetPlayerPaused(bool paused)
        {
            bool changed = flow.SetPlayerPaused(paused);
            SynchronizeLevelPause();
            return changed;
        }

        public bool SetApplicationFocus(bool hasFocus)
        {
            bool changed = flow.SetApplicationFocus(hasFocus);
            SynchronizeLevelPause();
            return changed;
        }

        public bool SetApplicationPaused(bool paused)
        {
            bool changed = flow.SetApplicationPaused(paused);
            SynchronizeLevelPause();
            return changed;
        }

        public void ApplyGameplayScale(double scale, double durationSeconds)
        {
            flow.Time.ApplyGameplayScale(scale, durationSeconds);
        }

        private void SynchronizeLevelPause()
        {
            level.SetPaused(!IsGameplayActive(flow.State));
        }

        private static bool IsGameplayActive(BattleFlowState state)
        {
            return state == BattleFlowState.Playing ||
                   state == BattleFlowState.UltimateDrawing;
        }
    }
}
