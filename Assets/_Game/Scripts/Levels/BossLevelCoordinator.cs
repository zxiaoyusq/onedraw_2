using System;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;

namespace OneStrokeDemon.Levels
{
    // 定义 IBossLevelWorld 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public interface IBossLevelWorld :
        ILevelSpawnWorld,
        IEnemyAttackWorld,
        ISkillEffectWorld
    {
        bool TryGetEnemyController(long entityId, out EnemyController controller);
    }

    // 定义 BossLevelCoordinator 的关卡领域契约，用于描述时间线、流程或持久化边界。
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

        // 初始化 BossLevelCoordinator，并建立关卡流程所需的初始状态。
        public BossLevelCoordinator(
            IConfigProvider configProvider,
            string playerId,
            string levelId,
            PlayerCombatController playerController,
            IBossLevelWorld bossWorld)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

        // 推进 Advance 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public BattleFlowAdvanceReport Advance(double unscaledDeltaSeconds)
        {
            ThrowIfDisposed();
            BattleFlowAdvanceReport report = battle.Advance(
                unscaledDeltaSeconds,
                player.Current.IsDead);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

        // 处理 NotifyEnemyDefeated 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool NotifyEnemyDefeated(long entityId)
        {
            ThrowIfDisposed();
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (entityId == bossEntityId &&
                bossPhases != null &&
                !bossPhases.HasEnded)
            {
                return false;
            }

            return battle.NotifyEnemyDefeated(entityId);
        }

        // 释放 Dispose 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public void Dispose()
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (disposed)
            {
                return;
            }

            StopBossPhases();
            BossPhaseChanged = null;
            disposed = true;
        }

        // 处理 AttachSpawnedBoss 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void AttachSpawnedBoss(LevelAdvanceReport report)
        {
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < report.Events.Count; index++)
            {
                LevelRuntimeEvent runtimeEvent = report.Events[index];
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (runtimeEvent.Kind != LevelRuntimeEventKind.EnemySpawned ||
                    !runtimeEvent.SpawnRequest.IsBoss)
                {
                    continue;
                }

                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (bossPhases != null)
                {
                    throw new InvalidOperationException(
                        "A boss level cannot attach more than one active boss.");
                }

                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!world.TryGetEnemyController(
                        runtimeEvent.EntityId,
                        out EnemyController controller) ||
                    controller == null)
                {
                    throw new InvalidOperationException(
                        $"Boss world did not expose spawned entity '{runtimeEvent.EntityId}'.");
                }

                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

        // 处理 StopBossPhases 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void StopBossPhases()
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (bossPhases == null)
            {
                return;
            }

            bossPhases.PhaseChanged -= OnBossPhaseChanged;
            bossPhases.Dispose();
            bossPhases = null;
        }

        // 响应 OnBossPhaseChanged 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void OnBossPhaseChanged(BossPhaseChangedEvent phaseEvent)
        {
            BossPhaseChanged?.Invoke(phaseEvent);
        }

        // 判断是否 IsGameplayActive 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool IsGameplayActive(BattleFlowState state)
        {
            return state == BattleFlowState.Playing ||
                   state == BattleFlowState.UltimateDrawing;
        }

        // 处理 ThrowIfDisposed 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void ThrowIfDisposed()
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BossLevelCoordinator));
            }
        }
    }
}
