using System;
using System.Collections.Generic;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    public readonly struct BossPhaseChangedEvent
    {
        internal BossPhaseChangedEvent(
            in BossPhaseTransition transition,
            in EnemyPhaseProfileResult profileResult,
            double timestamp,
            IReadOnlyList<SkillEffectStepResult> entryEffectSteps)
        {
            Transition = transition;
            ProfileResult = profileResult;
            Timestamp = timestamp;
            EntryEffectSteps = entryEffectSteps ?? Array.Empty<SkillEffectStepResult>();
            IsValid = true;
        }

        public BossPhaseTransition Transition { get; }

        public EnemyPhaseProfileResult ProfileResult { get; }

        public double Timestamp { get; }

        public IReadOnlyList<SkillEffectStepResult> EntryEffectSteps { get; }

        public bool IsValid { get; }
    }

    public sealed class BossPhaseController : IDisposable
    {
        private static readonly IReadOnlyList<BossPhaseTransition> NoTransitions =
            Array.Empty<BossPhaseTransition>();

        private readonly IConfigProvider configProvider;
        private readonly EnemyController boss;
        private readonly IEnemyAttackWorld attackWorld;
        private readonly SkillService skillService;
        private readonly BossPhaseEffectWorld effectWorld;
        private readonly IReadOnlyList<BossPhaseDefinition> phases;
        private readonly BossPhaseStateMachine stateMachine;
        private EnemyStrategyRuntime strategy;
        private double lastTimestamp;
        private double pendingTimestamp;
        private bool hasTimestamp;
        private bool subscribed;
        private bool applyingPhase;
        private bool pendingHpObservation;
        private bool ended;
        private bool disposed;

        public BossPhaseController(
            IConfigProvider configuredProvider,
            EnemyController bossController,
            IEnemyAttackWorld configuredAttackWorld,
            SkillService configuredSkillService,
            ISkillEffectWorld configuredEffectWorld)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            boss = bossController ??
                throw new ArgumentNullException(nameof(bossController));
            attackWorld = configuredAttackWorld ??
                throw new ArgumentNullException(nameof(configuredAttackWorld));
            skillService = configuredSkillService ??
                throw new ArgumentNullException(nameof(configuredSkillService));
            if (!boss.IsAlive || boss.Definition.Tier != EnemyTier.Boss)
            {
                throw new ArgumentException(
                    "Boss controller must be spawned, alive, and tier Boss.",
                    nameof(bossController));
            }

            if (boss.State.State != EnemyState.Move)
            {
                throw new ArgumentException(
                    "Boss controller must complete Spawn before phases start.",
                    nameof(bossController));
            }

            var bossTarget = new EnemySkillEffectTarget(boss);
            effectWorld = new BossPhaseEffectWorld(
                configuredEffectWorld ??
                    throw new ArgumentNullException(nameof(configuredEffectWorld)),
                bossTarget);
            phases = BossPhaseCatalog.Create(configProvider, boss.Definition.EnemyId);
            stateMachine = new BossPhaseStateMachine(phases);
        }

        public event Action<BossPhaseChangedEvent> PhaseChanged;

        public bool IsStarted => stateMachine.IsStarted;

        public bool HasEnded => ended;

        public IReadOnlyList<BossPhaseDefinition> Phases => phases;

        public BossPhaseDefinition CurrentPhase => stateMachine.Current;

        public EnemyStrategyRuntime Strategy => strategy ??
            throw new InvalidOperationException(
                "Boss phase strategy is not active.");

        public BossPhaseChangedEvent Start(double timestamp)
        {
            ThrowIfDisposed();
            ObserveTimestamp(timestamp);
            if (IsStarted)
            {
                throw new InvalidOperationException(
                    "Boss phase controller can only start once.");
            }

            boss.CombatEventPublished += OnBossCombatEvent;
            subscribed = true;
            try
            {
                BossPhaseTransition transition =
                    stateMachine.Start(boss.Damage.HpRatio);
                BossPhaseChangedEvent phaseEvent =
                    ApplyTransition(transition, timestamp);
                ProcessPendingObservation(timestamp);
                return phaseEvent;
            }
            catch
            {
                Unsubscribe();
                strategy?.Dispose();
                strategy = null;
                throw;
            }
        }

        public IReadOnlyList<BossPhaseTransition> ObserveCurrentHp(double timestamp)
        {
            ThrowIfDisposed();
            RequireStarted();
            ObserveTimestamp(timestamp);
            if (ended || !boss.IsAlive || boss.Damage.IsDead)
            {
                return NoTransitions;
            }

            if (applyingPhase)
            {
                pendingHpObservation = true;
                pendingTimestamp = Math.Max(pendingTimestamp, timestamp);
                return NoTransitions;
            }

            IReadOnlyList<BossPhaseTransition> transitions =
                stateMachine.Advance(boss.Damage.HpRatio);
            ApplyTransitions(transitions, timestamp);
            ProcessPendingObservation(timestamp);
            return transitions;
        }

        public EnemyMovementSample SampleMovement(double phaseElapsedSeconds)
        {
            ThrowIfDisposed();
            RequireActive();
            return Strategy.SampleMovement(phaseElapsedSeconds);
        }

        public bool TryBeginAttack(
            in EnemyAttackTriggerContext context,
            double unitSelection,
            double timestamp)
        {
            ThrowIfDisposed();
            RequireActive();
            ObserveTimestamp(timestamp);
            return Strategy.TryBeginAttack(context, unitSelection, timestamp);
        }

        public int Tick(double timestamp)
        {
            ThrowIfDisposed();
            RequireActive();
            ObserveTimestamp(timestamp);
            return Strategy.Tick(timestamp);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            Unsubscribe();
            strategy?.Dispose();
            strategy = null;
            PhaseChanged = null;
            disposed = true;
        }

        private void ApplyTransitions(
            IReadOnlyList<BossPhaseTransition> transitions,
            double timestamp)
        {
            for (int index = 0; index < transitions.Count; index++)
            {
                ApplyTransition(transitions[index], timestamp);
                if (ended || !boss.IsAlive)
                {
                    break;
                }
            }
        }

        private BossPhaseChangedEvent ApplyTransition(
            in BossPhaseTransition transition,
            double timestamp)
        {
            applyingPhase = true;
            try
            {
                strategy?.Dispose();
                strategy = null;
                EnemyPhaseProfileResult profile = boss.ApplyBossPhaseProfile(
                    transition.CurrentPhase.CombatProfile,
                    transition.CurrentPhase.BossPhaseId,
                    timestamp);
                strategy = new EnemyStrategyRuntime(
                    boss,
                    configProvider,
                    attackWorld);
                IReadOnlyList<SkillEffectStepResult> steps =
                    skillService.ExecuteEffectGroup(
                        transition.CurrentPhase.OnEnterEffectGroupId,
                        transition.CurrentPhase.BossPhaseId,
                        new SkillEffectContext(effectWorld, timestamp));
                var phaseEvent = new BossPhaseChangedEvent(
                    transition,
                    profile,
                    timestamp,
                    steps);
                PhaseChanged?.Invoke(phaseEvent);
                return phaseEvent;
            }
            finally
            {
                applyingPhase = false;
            }
        }

        private void ProcessPendingObservation(double fallbackTimestamp)
        {
            while (pendingHpObservation && !ended && boss.IsAlive)
            {
                double timestamp = Math.Max(fallbackTimestamp, pendingTimestamp);
                pendingHpObservation = false;
                pendingTimestamp = 0d;
                IReadOnlyList<BossPhaseTransition> transitions =
                    stateMachine.Advance(boss.Damage.HpRatio);
                ApplyTransitions(transitions, timestamp);
                fallbackTimestamp = timestamp;
            }
        }

        private void OnBossCombatEvent(EnemyCombatEvent combatEvent)
        {
            if (disposed)
            {
                return;
            }

            if (combatEvent.EventType == EnemyCombatEventType.HpChanged)
            {
                ObserveCurrentHp(combatEvent.Timestamp);
            }
            else if (combatEvent.EventType == EnemyCombatEventType.Died)
            {
                ended = true;
                strategy?.Dispose();
                strategy = null;
            }
            else if (combatEvent.EventType == EnemyCombatEventType.Released)
            {
                Dispose();
            }
        }

        private void ObserveTimestamp(double timestamp)
        {
            if (double.IsNaN(timestamp) ||
                double.IsInfinity(timestamp) ||
                timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    "Boss phase timestamp must be finite and non-negative.");
            }

            if (hasTimestamp && timestamp < lastTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    $"Boss phase timestamp cannot move backwards from {lastTimestamp}.");
            }

            lastTimestamp = timestamp;
            hasTimestamp = true;
        }

        private void RequireStarted()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException(
                    "Boss phase controller must start before use.");
            }
        }

        private void RequireActive()
        {
            RequireStarted();
            if (ended || strategy == null)
            {
                throw new InvalidOperationException(
                    "Boss phase controller has no active combat strategy.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BossPhaseController));
            }
        }

        private void Unsubscribe()
        {
            if (subscribed)
            {
                boss.CombatEventPublished -= OnBossCombatEvent;
                subscribed = false;
            }
        }

        private sealed class BossPhaseEffectWorld : ISkillEffectWorld
        {
            private readonly ISkillEffectWorld inner;
            private readonly EnemySkillEffectTarget bossTarget;

            public BossPhaseEffectWorld(
                ISkillEffectWorld configuredInner,
                EnemySkillEffectTarget configuredBossTarget)
            {
                inner = configuredInner;
                bossTarget = configuredBossTarget;
            }

            public IReadOnlyList<ISkillEffectTarget> Targets => inner.Targets;

            public ISkillEffectTarget PrimaryTarget => bossTarget;

            public int RepeatLastStroke(
                float damageMultiplier,
                float delaySeconds,
                string sourceId,
                double timestamp)
            {
                return inner.RepeatLastStroke(
                    damageMultiplier,
                    delaySeconds,
                    sourceId,
                    timestamp);
            }

            public int SetTimeScale(
                float scale,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                return inner.SetTimeScale(scale, durationSeconds, sourceId, timestamp);
            }

            public int SetNextStrokeDamageMultiplier(
                float multiplier,
                string sourceId,
                double timestamp)
            {
                return inner.SetNextStrokeDamageMultiplier(
                    multiplier,
                    sourceId,
                    timestamp);
            }

            public int ClearHostileProjectiles(string sourceId, double timestamp)
            {
                return inner.ClearHostileProjectiles(sourceId, timestamp);
            }

            public void PlayVfx(
                string vfxKey,
                IReadOnlyList<ISkillEffectTarget> targets,
                string sourceId,
                double timestamp)
            {
                inner.PlayVfx(vfxKey, targets, sourceId, timestamp);
            }

            public void PlayAudio(string audioKey, string sourceId, double timestamp)
            {
                inner.PlayAudio(audioKey, sourceId, timestamp);
            }
        }
    }
}
