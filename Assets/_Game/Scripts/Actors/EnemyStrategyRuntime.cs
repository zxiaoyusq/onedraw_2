using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    public interface IEnemyAttackWorld
    {
        void ExecuteAttack(in EnemyAttackAction action, double timestamp);
    }

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

        public EnemyStrategyRuntime(
            EnemyController configuredController,
            IConfigProvider configProvider,
            IEnemyAttackWorld attackWorld,
            MovementStrategyRegistry configuredMovementRegistry = null,
            AttackStrategyRegistry configuredAttackRegistry = null)
        {
            controller = configuredController ??
                throw new ArgumentNullException(nameof(configuredController));
            if (!controller.IsSpawned)
            {
                throw new ArgumentException(
                    "Enemy controller must be spawned before strategy runtime creation.",
                    nameof(configuredController));
            }

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
                controller.Definition.EnemyId,
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

        public EnemyMovementSample SampleMovement(double movementElapsedSeconds)
        {
            ThrowIfDisposed();
            return movementRegistry.Sample(movement, movementElapsedSeconds);
        }

        public bool TryBeginAttack(
            in EnemyAttackTriggerContext context,
            double unitSelection,
            double timestamp)
        {
            ThrowIfDisposed();
            if (activeAttack.IsConfigured || controller.State.State != EnemyState.Move)
            {
                return false;
            }

            EnemyAttackDefinition selected = attackRegistry.Select(
                attacks,
                context,
                unitSelection);
            if (!selected.IsConfigured)
            {
                return false;
            }

            activeAttack = selected;
            activeAction = selected.CreateAction(context);
            actionExecuted = false;
            telegraph.Open(activeAction, activeAttack.Timeline, timestamp);
            bool began = controller.BeginAttack(selected.AttackId, timestamp);
            if (!began)
            {
                telegraph.Close(timestamp);
                ClearActiveAttack();
            }

            return began;
        }

        public int Tick(double timestamp)
        {
            ThrowIfDisposed();
            return controller.Tick(timestamp);
        }

        public void Dispose()
        {
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

        private void OnCombatEvent(EnemyCombatEvent combatEvent)
        {
            if (combatEvent.EventType == EnemyCombatEventType.StateChanged)
            {
                if (combatEvent.State == EnemyState.Attack)
                {
                    ExecuteAction(combatEvent.Timestamp);
                    telegraph.Close(combatEvent.Timestamp);
                }
                else if (combatEvent.State == EnemyState.Recovery ||
                         combatEvent.State == EnemyState.Stun ||
                         combatEvent.State == EnemyState.Dead ||
                         combatEvent.State == EnemyState.None)
                {
                    telegraph.Close(combatEvent.Timestamp);
                    ClearActiveAttack();
                }
            }
            else if (combatEvent.EventType == EnemyCombatEventType.Released)
            {
                telegraph.Close(combatEvent.Timestamp);
                ClearActiveAttack();
            }
        }

        private void ExecuteAction(double timestamp)
        {
            if (actionExecuted || !activeAction.IsConfigured)
            {
                return;
            }

            actionExecuted = true;
            world.ExecuteAttack(activeAction, timestamp);
        }

        private void ClearActiveAttack()
        {
            activeAttack = default;
            activeAction = default;
            actionExecuted = false;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(EnemyStrategyRuntime));
            }
        }
    }
}
