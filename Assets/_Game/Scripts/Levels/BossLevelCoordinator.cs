using System;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;

namespace OneStrokeDemon.Levels
{
    public interface IBossLevelWorld :
        ILevelSpawnWorld,
        IEnemyAttackWorld,
        ISkillEffectWorld
    {
        bool TryGetEnemyController(long entityId, out EnemyController controller);
    }

    public sealed class BossLevelCoordinator : IDisposable
    {
        private readonly BattleFlowCoordinator battle;
        private readonly IConfigProvider config;
        private readonly PlayerCombatController player;
        private readonly IBossLevelWorld world;
        private readonly SkillService skills;
        private BossPhaseController bossPhases;
        private long bossEntityId;
        private bool disposed;

        public BossLevelCoordinator(
            IConfigProvider configProvider,
            string playerId,
            string levelId,
            PlayerCombatController playerController,
            IBossLevelWorld bossWorld)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            config = configProvider;
            player = playerController ??
                throw new ArgumentNullException(nameof(playerController));
            world = bossWorld ?? throw new ArgumentNullException(nameof(bossWorld));
            battle = new BattleFlowCoordinator(
                configProvider,
                playerId,
                levelId,
                world);
            if (string.IsNullOrWhiteSpace(battle.Level.Definition.BossEnemyId))
            {
                throw new ArgumentException(
                    $"Level '{levelId}' must configure a bossEnemyId.",
                    nameof(levelId));
            }

            skills = new SkillService(configProvider, player);
        }

        public event Action<BossPhaseChangedEvent> BossPhaseChanged;

        public BattleFlowCoordinator Battle => battle;

        public BossPhaseController BossPhases => bossPhases;

        public long BossEntityId => bossEntityId;

        public bool HasActiveBoss =>
            bossPhases != null && !bossPhases.HasEnded;

        public BattleFlowAdvanceReport Advance(double unscaledDeltaSeconds)
        {
            ThrowIfDisposed();
            BattleFlowAdvanceReport report = battle.Advance(
                unscaledDeltaSeconds,
                player.Current.IsDead);
            if (IsGameplayActive(report.State))
            {
                AttachSpawnedBoss(report.Level);
            }
            else
            {
                StopBossPhases();
            }

            return report;
        }

        public bool NotifyEnemyDefeated(long entityId)
        {
            ThrowIfDisposed();
            if (entityId == bossEntityId &&
                bossPhases != null &&
                !bossPhases.HasEnded)
            {
                return false;
            }

            return battle.NotifyEnemyDefeated(entityId);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            StopBossPhases();
            BossPhaseChanged = null;
            disposed = true;
        }

        private void AttachSpawnedBoss(LevelAdvanceReport report)
        {
            for (int index = 0; index < report.Events.Count; index++)
            {
                LevelRuntimeEvent runtimeEvent = report.Events[index];
                if (runtimeEvent.Kind != LevelRuntimeEventKind.EnemySpawned ||
                    !runtimeEvent.SpawnRequest.IsBoss)
                {
                    continue;
                }

                if (bossPhases != null)
                {
                    throw new InvalidOperationException(
                        "A boss level cannot attach more than one active boss.");
                }

                if (!world.TryGetEnemyController(
                        runtimeEvent.EntityId,
                        out EnemyController controller) ||
                    controller == null)
                {
                    throw new InvalidOperationException(
                        $"Boss world did not expose spawned entity '{runtimeEvent.EntityId}'.");
                }

                if (!string.Equals(
                        controller.Definition.EnemyId,
                        runtimeEvent.SpawnRequest.EnemyId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Boss entity '{runtimeEvent.EntityId}' does not match its spawn request.");
                }

                bossEntityId = runtimeEvent.EntityId;
                bossPhases = new BossPhaseController(
                    config,
                    controller,
                    world,
                    skills,
                    world);
                bossPhases.PhaseChanged += OnBossPhaseChanged;
                double timestamp = Math.Max(
                    runtimeEvent.LevelSeconds,
                    controller.State.LastTimestamp);
                bossPhases.Start(timestamp);
            }
        }

        private void StopBossPhases()
        {
            if (bossPhases == null)
            {
                return;
            }

            bossPhases.PhaseChanged -= OnBossPhaseChanged;
            bossPhases.Dispose();
            bossPhases = null;
        }

        private void OnBossPhaseChanged(BossPhaseChangedEvent phaseEvent)
        {
            BossPhaseChanged?.Invoke(phaseEvent);
        }

        private static bool IsGameplayActive(BattleFlowState state)
        {
            return state == BattleFlowState.Playing ||
                   state == BattleFlowState.UltimateDrawing;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BossLevelCoordinator));
            }
        }
    }
}
