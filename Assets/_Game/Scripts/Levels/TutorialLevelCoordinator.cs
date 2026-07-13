using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;

namespace OneStrokeDemon.Levels
{
    public sealed class TutorialLevelCoordinator
    {
        private readonly BattleFlowCoordinator battle;
        private readonly TutorialSequence tutorial;
        private bool battleReadyPublished;

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

        public TutorialLevelCoordinator(
            BattleFlowCoordinator battleFlow,
            TutorialSequence tutorialSequence)
        {
            battle = battleFlow ?? throw new ArgumentNullException(nameof(battleFlow));
            tutorial = tutorialSequence ??
                throw new ArgumentNullException(nameof(tutorialSequence));
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

        public BattleFlowAdvanceReport Advance(
            double unscaledDeltaSeconds,
            bool playerDied = false)
        {
            BattleFlowAdvanceReport report = battle.Advance(
                unscaledDeltaSeconds,
                playerDied);

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
            return report;
        }

        public TutorialUpdateReport NotifyGameplayEvent(
            in TutorialGameplayEvent gameplayEvent)
        {
            if (!gameplayEvent.IsValid)
            {
                throw new ArgumentException(
                    "Tutorial gameplay event must be initialized.",
                    nameof(gameplayEvent));
            }

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

        public bool NotifyEnemyDefeated(long entityId)
        {
            return battle.NotifyEnemyDefeated(entityId);
        }

        public bool TryBeginUltimateDrawing()
        {
            return battle.TryBeginUltimateDrawing();
        }

        public bool CanAcceptUltimateGestureEvent(ulong gestureEventId)
        {
            return battle.CanAcceptUltimateGestureEvent(gestureEventId);
        }

        public bool ResolveUltimate(
            ulong gestureEventId,
            in SkillActivationResult result)
        {
            bool resolved = battle.ResolveUltimate(gestureEventId, result);
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

        public bool CancelUltimateDrawing()
        {
            return battle.CancelUltimateDrawing();
        }

        public bool SetPlayerPaused(bool paused)
        {
            bool changed = battle.SetPlayerPaused(paused);
            SynchronizeLevelProgressGate();
            return changed;
        }

        public bool SetApplicationFocus(bool hasFocus)
        {
            bool changed = battle.SetApplicationFocus(hasFocus);
            SynchronizeLevelProgressGate();
            return changed;
        }

        public bool SetApplicationPaused(bool paused)
        {
            bool changed = battle.SetApplicationPaused(paused);
            SynchronizeLevelProgressGate();
            return changed;
        }

        public void ApplyGameplayScale(double scale, double durationSeconds)
        {
            battle.ApplyGameplayScale(scale, durationSeconds);
        }

        private void ApplyTutorialUpdate(in TutorialUpdateReport report)
        {
            if (!report.StepCompleted ||
                !report.TutorialCompleted ||
                battle.Level.State == LevelRunnerState.Completed ||
                battle.Level.CurrentWave.Definition.EndCondition !=
                    WaveEndCondition.PlayerConfirmed)
            {
                return;
            }

            if (!battle.ConfirmPlayerAction())
            {
                throw new InvalidOperationException(
                    $"Tutorial step {report.CompletedStepOrder} completed but its " +
                    "configured PlayerConfirmed wave gate rejected the event.");
            }
        }

        private void SynchronizeLevelProgressGate()
        {
            bool blocked = false;
            if (battle.Level.State != LevelRunnerState.Completed &&
                tutorial.State == TutorialSequenceState.Active)
            {
                TutorialStepDefinition current = tutorial.CurrentStep;
                blocked = current.BlockProgress;
            }

            battle.Level.SetProgressBlocked(blocked);
        }

        private static bool IsGameplayActive(BattleFlowState state)
        {
            return state == BattleFlowState.Playing ||
                   state == BattleFlowState.UltimateDrawing;
        }
    }
}
