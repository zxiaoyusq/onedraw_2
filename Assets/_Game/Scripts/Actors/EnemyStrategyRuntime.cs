using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    // 定义 IEnemyAttackWorld 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public interface IEnemyAttackWorld
    {
        void ExecuteAttack(in EnemyAttackAction action, double timestamp);
    }

    // 定义 EnemyStrategyRuntime 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class EnemyStrategyRuntime : IDisposable
    {
        private readonly EnemyController controller;
        private readonly IEnemyAttackWorld world;
        private readonly AttackStrategyRegistry attackRegistry;
        private readonly MovementStrategyRegistry movementRegistry;
        private readonly IReadOnlyList<EnemyAttackDefinition> attacks;
        private readonly EnemyMovementDefinition movement;
        private readonly EnemyAttackTelegraph telegraph = new EnemyAttackTelegraph();
        private EnemyAttackDefinition activeAttack;
        private EnemyAttackAction activeAction;
        private bool actionExecuted;
        private bool disposed;

        // 初始化 EnemyStrategyRuntime，并建立角色运行时所需的初始状态。
        public EnemyStrategyRuntime(
            EnemyController configuredController,
            IConfigProvider configProvider,
            IEnemyAttackWorld attackWorld,
            MovementStrategyRegistry configuredMovementRegistry = null,
            AttackStrategyRegistry configuredAttackRegistry = null)
        {
            controller = configuredController ??
                throw new ArgumentNullException(nameof(configuredController));
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!controller.IsSpawned)
            {
                throw new ArgumentException(
                    "Enemy controller must be spawned before strategy runtime creation.",
                    nameof(configuredController));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            world = attackWorld ?? throw new ArgumentNullException(nameof(attackWorld));
            movementRegistry = configuredMovementRegistry ??
                MovementStrategyRegistry.CreateDefault();
            attackRegistry = configuredAttackRegistry ??
                AttackStrategyRegistry.CreateDefault();
            movement = EnemyMovementDefinitionFactory.Create(
                configProvider,
                controller.Definition,
                movementRegistry);
            attacks = EnemyAttackDefinitionFactory.Create(
                configProvider,
                controller.Definition.AttackSetId,
                attackRegistry);
            controller.CombatEventPublished += OnCombatEvent;
        }

        public EnemyAttackTelegraphSnapshot Telegraph => telegraph.Current;

        public EnemyAttackAction ActiveAction => activeAction;

        public IReadOnlyList<EnemyAttackDefinition> Attacks => attacks;

        public EnemyMovementDefinition Movement => movement;

        // 处理 SampleMovement 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyMovementSample SampleMovement(double movementElapsedSeconds)
        {
            ThrowIfDisposed();
            return movementRegistry.Sample(movement, movementElapsedSeconds);
        }

        // 尝试执行 TryBeginAttack 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool TryBeginAttack(
            in EnemyAttackTriggerContext context,
            double unitSelection,
            double timestamp)
        {
            ThrowIfDisposed();
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (activeAttack.IsConfigured || controller.State.State != EnemyState.Move)
            {
                return false;
            }

            EnemyAttackDefinition selected = attackRegistry.Select(
                attacks,
                context,
                unitSelection);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!selected.IsConfigured)
            {
                return false;
            }

            activeAttack = selected;
            activeAction = selected.CreateAction(context);
            actionExecuted = false;
            telegraph.Open(activeAction, activeAttack.Timeline, timestamp);
            bool began = controller.BeginAttack(selected.AttackId, timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!began)
            {
                telegraph.Close(timestamp);
                ClearActiveAttack();
            }

            return began;
        }

        // 按时间推进 Tick 对应的角色逻辑，并返回或发布一致的状态结果。
        public int Tick(double timestamp)
        {
            ThrowIfDisposed();
            return controller.Tick(timestamp);
        }

        // 释放 Dispose 对应的角色逻辑，并返回或发布一致的状态结果。
        public void Dispose()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (disposed)
            {
                return;
            }

            controller.CombatEventPublished -= OnCombatEvent;
            double timestamp = controller.State.HasClock
                ? controller.State.LastTimestamp
                : 0d;
            telegraph.Close(timestamp);
            ClearActiveAttack();
            disposed = true;
        }

        // 响应 OnCombatEvent 对应的角色逻辑，并返回或发布一致的状态结果。
        private void OnCombatEvent(EnemyCombatEvent combatEvent)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (combatEvent.EventType == EnemyCombatEventType.StateChanged)
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (combatEvent.State == EnemyState.Attack)
                {
                    ExecuteAction(combatEvent.Timestamp);
                    telegraph.Close(combatEvent.Timestamp);
                }
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                else if (combatEvent.State == EnemyState.Recovery ||
                         combatEvent.State == EnemyState.Stun ||
                         combatEvent.State == EnemyState.Dead ||
                         combatEvent.State == EnemyState.None)
                {
                    telegraph.Close(combatEvent.Timestamp);
                    ClearActiveAttack();
                }
            }
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            else if (combatEvent.EventType == EnemyCombatEventType.Released)
            {
                telegraph.Close(combatEvent.Timestamp);
                ClearActiveAttack();
            }
        }

        // 处理 ExecuteAction 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ExecuteAction(double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (actionExecuted || !activeAction.IsConfigured)
            {
                return;
            }

            actionExecuted = true;
            world.ExecuteAttack(activeAction, timestamp);
        }

        // 清理 ClearActiveAttack 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ClearActiveAttack()
        {
            activeAttack = default;
            activeAction = default;
            actionExecuted = false;
        }

        // 处理 ThrowIfDisposed 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ThrowIfDisposed()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(EnemyStrategyRuntime));
            }
        }
    }
}
