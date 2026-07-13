using System;
using System.Collections.Generic;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using UnityEngine;

namespace OneStrokeDemon.Actors
{
    public enum EnemyCombatEventType
    {
        None = 0,
        StateChanged = 1,
        ArmorChanged = 2,
        HpChanged = 3,
        ArmorBroken = 4,
        Interrupted = 5,
        BuffApplied = 6,
        KnockbackRequested = 7,
        CounterChanged = 8,
        Died = 9,
        Released = 10,
        PhaseChanged = 11
    }

    public enum EnemyReleaseReason
    {
        None = 0,
        Manual = 1,
        Disabled = 2,
        Cleared = 3
    }

    public readonly struct EnemyCombatEvent
    {
        internal EnemyCombatEvent(
            ulong sequence,
            EnemyCombatEventType eventType,
            string enemyId,
            int hitTargetId,
            EnemyState state,
            string sourceId,
            long signedAmount,
            double value,
            double durationSeconds,
            string effectGroupId,
            string buffId,
            double timestamp)
        {
            Sequence = sequence;
            EventType = eventType;
            EnemyId = enemyId ?? string.Empty;
            HitTargetId = hitTargetId;
            State = state;
            SourceId = sourceId ?? string.Empty;
            SignedAmount = signedAmount;
            Value = value;
            DurationSeconds = durationSeconds;
            EffectGroupId = effectGroupId ?? string.Empty;
            BuffId = buffId ?? string.Empty;
            Timestamp = timestamp;
            IsValid = true;
        }

        public ulong Sequence { get; }

        public EnemyCombatEventType EventType { get; }

        public string EnemyId { get; }

        public int HitTargetId { get; }

        public EnemyState State { get; }

        public string SourceId { get; }

        public long SignedAmount { get; }

        public double Value { get; }

        public double DurationSeconds { get; }

        public string EffectGroupId { get; }

        public string BuffId { get; }

        public double Timestamp { get; }

        public bool IsValid { get; }
    }

    public readonly struct EnemyHitResolution
    {
        internal EnemyHitResolution(
            EnemyDamageResult damage,
            EnemyInterruptResult interrupt)
        {
            Damage = damage;
            Interrupt = interrupt;
            IsValid = true;
        }

        public EnemyDamageResult Damage { get; }

        public EnemyInterruptResult Interrupt { get; }

        public bool IsValid { get; }
    }

    public readonly struct EnemyReleaseSnapshot
    {
        internal EnemyReleaseSnapshot(
            EnemyReleaseReason reason,
            string enemyId,
            int hitTargetId,
            EnemyState stateBeforeRelease,
            long hpBeforeRelease,
            long armorBeforeRelease,
            double timestamp)
        {
            Reason = reason;
            EnemyId = enemyId ?? string.Empty;
            HitTargetId = hitTargetId;
            StateBeforeRelease = stateBeforeRelease;
            HpBeforeRelease = hpBeforeRelease;
            ArmorBeforeRelease = armorBeforeRelease;
            Timestamp = timestamp;
            IsValid = true;
        }

        public EnemyReleaseReason Reason { get; }

        public string EnemyId { get; }

        public int HitTargetId { get; }

        public EnemyState StateBeforeRelease { get; }

        public long HpBeforeRelease { get; }

        public long ArmorBeforeRelease { get; }

        public double Timestamp { get; }

        public bool IsValid { get; }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable))]
    public sealed class EnemyController : MonoBehaviour, IPoolable
    {
        private readonly EnemyStateMachine stateMachine = new EnemyStateMachine();
        private readonly EnemyBuffContainer buffContainer = new EnemyBuffContainer();
        private readonly Dictionary<string, double> counters =
            new Dictionary<string, double>(StringComparer.Ordinal);
        private Damageable damageable;
        private WeakpointController weakpoint;
        private IConfigProvider configProvider;
        private EnemyDefinition definition;
        private Transform poolParent;
        private PoolLease poolLease;
        private ulong nextEventSequence = 1UL;
        private bool hasPoolParent;
        private bool stateEventsAttached;
        private bool isSpawned;

        public event Action<EnemyCombatEvent> CombatEventPublished;

        public bool IsSpawned => isSpawned;

        public bool IsPoolActive => poolLease.IsValid;

        public bool IsAlive => isSpawned && stateMachine.Current.IsAlive;

        public EnemyDefinition Definition => definition;

        public EnemyStateSnapshot State => stateMachine.Current;

        public EnemyDamageSnapshot Damage => Damageable.Current;

        public Damageable Damageable
        {
            get
            {
                EnsureComponents();
                return damageable;
            }
        }

        public WeakpointController Weakpoint => weakpoint;

        public EnemyBuffContainer Buffs => buffContainer;

        public double MovementMultiplier => buffContainer.GetMovementMultiplier();

        public bool IsRooted => buffContainer.HasType("Root");

        private void Awake()
        {
            EnsureComponents();
            EnsureStateEvents();
        }

        private void Update()
        {
            if (isSpawned)
            {
                Tick(Time.timeAsDouble);
            }
        }

        private void OnDisable()
        {
            if (isSpawned)
            {
                double timestamp = stateMachine.Current.HasClock
                    ? stateMachine.Current.LastTimestamp
                    : 0d;
                Release(EnemyReleaseReason.Disabled, timestamp);
            }
        }

        public void Spawn(
            IConfigProvider configuredProvider,
            string enemyId,
            int hitTargetId,
            double timestamp,
            WeakpointController configuredWeakpoint = null)
        {
            if (configuredProvider == null)
            {
                throw new ArgumentNullException(nameof(configuredProvider));
            }

            if (isSpawned)
            {
                throw new InvalidOperationException(
                    "Active enemy controller must be released before reuse.");
            }

            EnsureComponents();
            EnsureStateEvents();
            EnemyDefinition configuredDefinition =
                EnemyDefinitionFactory.Create(configuredProvider, enemyId);
            WeakpointController resolvedWeakpoint = configuredWeakpoint ??
                GetComponentInChildren<WeakpointController>(true);
            if (configuredDefinition.Weakpoint.HasHitbox && resolvedWeakpoint == null)
            {
                throw new InvalidOperationException(
                    $"Enemy '{configuredDefinition.EnemyId}' requires a WeakpointController child.");
            }

            configProvider = configuredProvider;
            definition = configuredDefinition;
            weakpoint = resolvedWeakpoint;
            counters.Clear();
            damageable.Spawn(definition, hitTargetId);
            buffContainer.Spawn(timestamp);
            if (weakpoint != null)
            {
                weakpoint.Configure(definition.Weakpoint, damageable);
            }

            isSpawned = true;
            stateMachine.Spawn(timestamp);
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public bool CompleteSpawn(double timestamp)
        {
            RequireSpawned();
            bool completed = stateMachine.CompleteSpawn(timestamp);
            UpdateWeakpoint(timestamp);
            return completed;
        }

        public EnemyPhaseProfileResult ApplyBossPhaseProfile(
            in EnemyDefinition phaseDefinition,
            string bossPhaseId,
            double timestamp)
        {
            RequireAliveCombatState();
            if (string.IsNullOrWhiteSpace(bossPhaseId))
            {
                throw new ArgumentException(
                    "Boss phase id must be non-empty.",
                    nameof(bossPhaseId));
            }

            if (!phaseDefinition.IsConfigured ||
                phaseDefinition.Tier != EnemyTier.Boss ||
                definition.Tier != EnemyTier.Boss ||
                !string.Equals(
                    definition.EnemyId,
                    phaseDefinition.EnemyId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Boss phase profile must preserve the active Boss identity.",
                    nameof(phaseDefinition));
            }

            if (phaseDefinition.Weakpoint.HasHitbox && weakpoint == null)
            {
                throw new InvalidOperationException(
                    $"Boss phase '{bossPhaseId}' requires a WeakpointController child.");
            }

            if (!stateMachine.ChangePhase(timestamp))
            {
                throw new InvalidOperationException(
                    $"Boss phase '{bossPhaseId}' cannot be applied in state '{State.State}'.");
            }

            EnemyPhaseProfileResult result =
                damageable.ApplyPhaseProfile(phaseDefinition);
            definition = phaseDefinition;
            if (weakpoint != null)
            {
                weakpoint.Configure(definition.Weakpoint, damageable);
            }

            if (result.ArmorChanged)
            {
                Publish(
                    EnemyCombatEventType.ArmorChanged,
                    bossPhaseId,
                    result.ArmorDelta,
                    0d,
                    0d,
                    string.Empty,
                    string.Empty,
                    timestamp);
            }

            Publish(
                EnemyCombatEventType.PhaseChanged,
                bossPhaseId,
                0L,
                result.State.HpRatio,
                0d,
                string.Empty,
                string.Empty,
                timestamp);
            UpdateWeakpoint(timestamp);
            return result;
        }

        public bool BeginAttack(string attackId, double timestamp)
        {
            RequireSpawned();
            EnemyAttackTimeline attack = EnemyAttackTimelineFactory.Create(
                configProvider,
                definition.AttackSetId,
                attackId);
            bool began = stateMachine.BeginAttack(attack, timestamp);
            UpdateWeakpoint(timestamp);
            return began;
        }

        public int Tick(double timestamp)
        {
            RequireSpawned();
            int transitions = stateMachine.Tick(timestamp);
            buffContainer.Tick(timestamp);
            UpdateWeakpoint(timestamp);
            return transitions;
        }

        public bool RecoverFromStun(double timestamp)
        {
            RequireSpawned();
            bool recovered = stateMachine.RecoverFromStun(timestamp);
            UpdateWeakpoint(timestamp);
            return recovered;
        }

        public EnemyHitResolution ApplyStrokeDamage(
            in DamageResult resolvedDamage,
            string gestureType,
            double timestamp,
            string sourceId)
        {
            RequireSpawned();
            if (!resolvedDamage.IsResolved)
            {
                throw new ArgumentException(
                    "Resolved T360 damage is required.",
                    nameof(resolvedDamage));
            }

            if (resolvedDamage.TargetId != damageable.HitTargetId)
            {
                throw new ArgumentException(
                    $"Damage target '{resolvedDamage.TargetId}' does not match enemy target '{damageable.HitTargetId}'.",
                    nameof(resolvedDamage));
            }

            if (!damageable.CanReceiveStrokeHit)
            {
                throw new InvalidOperationException(
                    $"Enemy state '{State.State}' cannot receive a stroke hit.");
            }

            Tick(timestamp);
            long amount = ScaleIncomingDamage(resolvedDamage.Damage);
            EnemyDamageResult damage = damageable.ApplyDamage(amount);
            PublishDamage(damage, sourceId, timestamp);
            EnemyInterruptResult interrupt = default;
            if (damage.DeathTriggered)
            {
                stateMachine.TryKill(timestamp);
            }
            else if (damage.Changed && resolvedDamage.ShouldInterruptAttack)
            {
                interrupt = stateMachine.TryInterrupt(gestureType, timestamp);
            }

            UpdateWeakpoint(timestamp);
            return new EnemyHitResolution(damage, interrupt);
        }

        public EnemyDamageResult ApplyDamage(
            long amount,
            string sourceId,
            double timestamp)
        {
            RequireDamageableState();
            Tick(timestamp);
            EnemyDamageResult result = damageable.ApplyDamage(ScaleIncomingDamage(amount));
            PublishDamage(result, sourceId, timestamp);
            if (result.DeathTriggered)
            {
                stateMachine.TryKill(timestamp);
                UpdateWeakpoint(timestamp);
            }

            return result;
        }

        public EnemyDamageResult ApplyProjectileDamage(
            in ProjectileDamageSource source,
            double timestamp)
        {
            if (!source.IsValid)
            {
                throw new ArgumentException(
                    "Projectile damage source must be initialized.",
                    nameof(source));
            }

            if (source.CurrentOwner.Faction != ProjectileFaction.Player)
            {
                throw new ArgumentException(
                    "Only player-owned reflected projectiles may damage an enemy.",
                    nameof(source));
            }

            return ApplyDamage(
                source.Damage,
                source.ProjectileId,
                timestamp);
        }

        public EnemyHealingResult Heal(long amount, string sourceId, double timestamp)
        {
            RequireSpawned();
            Tick(timestamp);
            EnemyHealingResult result = damageable.Heal(amount);
            if (result.Changed)
            {
                Publish(
                    EnemyCombatEventType.HpChanged,
                    sourceId,
                    result.AppliedHealing,
                    0d,
                    0d,
                    string.Empty,
                    string.Empty,
                    timestamp);
            }

            return result;
        }

        public EnemyDamageResult RemoveArmor(
            long amount,
            string sourceId,
            double timestamp)
        {
            RequireDamageableState();
            Tick(timestamp);
            EnemyDamageResult result = damageable.RemoveArmor(amount);
            PublishDamage(result, sourceId, timestamp);
            return result;
        }

        public EnemyExecuteResult TryExecute(
            double threshold,
            string sourceId,
            double timestamp)
        {
            RequireDamageableState();
            Tick(timestamp);
            EnemyExecuteResult result = damageable.TryExecute(threshold);
            if (result.AppliedHpDamage > 0L)
            {
                Publish(
                    EnemyCombatEventType.HpChanged,
                    sourceId,
                    -result.AppliedHpDamage,
                    0d,
                    0d,
                    string.Empty,
                    string.Empty,
                    timestamp);
            }

            if (result.DeathTriggered)
            {
                stateMachine.TryKill(timestamp);
                UpdateWeakpoint(timestamp);
            }

            return result;
        }

        public EnemyBuffApplyResult ApplyBuff(
            BuffConfig buff,
            double durationSeconds,
            string sourceId,
            double timestamp)
        {
            RequireAliveCombatState();
            Tick(timestamp);
            EnemyBuffApplyResult result = buffContainer.Apply(
                buff,
                durationSeconds,
                sourceId,
                timestamp);
            if (result.Changed)
            {
                Publish(
                    EnemyCombatEventType.BuffApplied,
                    sourceId,
                    result.Buff.Stacks,
                    result.Buff.Magnitude,
                    durationSeconds,
                    string.Empty,
                    result.Buff.BuffId,
                    timestamp);
                if (string.Equals(result.Buff.Type, "Stun", StringComparison.Ordinal))
                {
                    stateMachine.ApplyTimedStun(durationSeconds, timestamp);
                    UpdateWeakpoint(timestamp);
                }
            }

            return result;
        }

        public bool RequestKnockback(
            double distanceReferencePixels,
            double durationSeconds,
            string sourceId,
            double timestamp)
        {
            RequireAliveCombatState();
            ValidateFiniteNonNegative(
                distanceReferencePixels,
                nameof(distanceReferencePixels));
            ValidateFiniteNonNegative(durationSeconds, nameof(durationSeconds));
            Tick(timestamp);
            Publish(
                EnemyCombatEventType.KnockbackRequested,
                sourceId,
                0L,
                distanceReferencePixels,
                durationSeconds,
                string.Empty,
                string.Empty,
                timestamp);
            return true;
        }

        public bool IncrementCounter(
            string counterId,
            double amount,
            double limit,
            string sourceId,
            double timestamp)
        {
            RequireAliveCombatState();
            if (string.IsNullOrWhiteSpace(counterId))
            {
                throw new ArgumentException(
                    "Enemy counter id must be non-empty.",
                    nameof(counterId));
            }

            ValidateFiniteNonNegative(amount, nameof(amount));
            ValidateFiniteNonNegative(limit, nameof(limit));
            Tick(timestamp);
            counters.TryGetValue(counterId, out double current);
            double next = Math.Min(limit, current + amount);
            if (next == current)
            {
                return false;
            }

            counters[counterId] = next;
            Publish(
                EnemyCombatEventType.CounterChanged,
                sourceId,
                0L,
                next,
                0d,
                string.Empty,
                string.Empty,
                timestamp);
            return true;
        }

        public bool TryGetCounter(string counterId, out double value)
        {
            if (counterId != null && counters.TryGetValue(counterId, out value))
            {
                return true;
            }

            value = 0d;
            return false;
        }

        public EnemyReleaseSnapshot Release(EnemyReleaseReason reason, double timestamp)
        {
            if (reason == EnemyReleaseReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason),
                    "A concrete enemy release reason is required.");
            }

            if (!isSpawned)
            {
                return default;
            }

            EnemyDamageSnapshot before = damageable.Current;
            EnemyState stateBefore = stateMachine.Current.State;
            string enemyId = definition.EnemyId;
            int targetId = before.HitTargetId;
            stateMachine.Release(timestamp);
            weakpoint?.Release();
            damageable.Release();
            buffContainer.Release();
            counters.Clear();
            configProvider = null;
            definition = default;
            weakpoint = null;
            isSpawned = false;

            var snapshot = new EnemyReleaseSnapshot(
                reason,
                enemyId,
                targetId,
                stateBefore,
                before.CurrentHp,
                before.CurrentArmor,
                timestamp);
            Publish(
                EnemyCombatEventType.Released,
                reason.ToString(),
                0L,
                0d,
                0d,
                string.Empty,
                string.Empty,
                timestamp,
                enemyId,
                targetId,
                EnemyState.None);
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            CombatEventPublished = null;
            nextEventSequence = 1UL;

            return snapshot;
        }

        public void AcquireFromPool(in PoolLease lease)
        {
            if (!lease.IsValid)
            {
                throw new ArgumentException("A valid pool lease is required.", nameof(lease));
            }

            if (poolLease.IsValid || isSpawned)
            {
                throw new InvalidOperationException(
                    "Enemy must be fully released before acquiring another pool lease.");
            }

            poolParent = transform.parent;
            hasPoolParent = true;
            poolLease = lease;
            CombatEventPublished = null;
            nextEventSequence = 1UL;
            ResetPoolTransform();
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void ReleaseToPool(in PoolReleaseContext context)
        {
            if (!hasPoolParent)
            {
                poolParent = transform.parent;
                hasPoolParent = true;
            }

            if (context.Lease.IsValid && poolLease.IsValid && context.Lease != poolLease)
            {
                throw new InvalidOperationException("Enemy pool release used a stale lease.");
            }

            if (isSpawned)
            {
                double timestamp = stateMachine.Current.HasClock
                    ? stateMachine.Current.LastTimestamp
                    : 0d;
                EnemyReleaseReason reason = context.Reason == PoolReleaseReason.Manual
                    ? EnemyReleaseReason.Manual
                    : EnemyReleaseReason.Cleared;
                Release(reason, timestamp);
            }

            weakpoint?.Release();
            damageable?.Release();
            buffContainer.Release();
            counters.Clear();
            configProvider = null;
            definition = default;
            weakpoint = null;
            isSpawned = false;
            CombatEventPublished = null;
            nextEventSequence = 1UL;
            poolLease = default;
            ResetPoolTransform();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void ResetPoolTransform()
        {
            transform.SetParent(hasPoolParent ? poolParent : null, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private void EnsureComponents()
        {
            if (damageable == null)
            {
                damageable = GetComponent<Damageable>();
            }

            if (damageable == null)
            {
                throw new InvalidOperationException(
                    "EnemyController requires a Damageable component.");
            }
        }

        private void EnsureStateEvents()
        {
            if (!stateEventsAttached)
            {
                stateMachine.Transitioned += OnStateTransitioned;
                stateEventsAttached = true;
            }
        }

        private void OnStateTransitioned(EnemyStateTransition transition)
        {
            bool acceptsHits = transition.CurrentState == EnemyState.Move ||
                               transition.CurrentState == EnemyState.Windup ||
                               transition.CurrentState == EnemyState.Attack ||
                               transition.CurrentState == EnemyState.Recovery ||
                               transition.CurrentState == EnemyState.Stun;
            damageable?.SetStrokeHitEnabled(acceptsHits);

            if (weakpoint != null)
            {
                if (transition.CurrentState == EnemyState.Windup &&
                    transition.Reason == EnemyTransitionReason.AttackStarted)
                {
                    weakpoint.BeginAttack(transition.Timestamp);
                }
                else if (transition.CurrentState == EnemyState.Recovery ||
                         transition.CurrentState == EnemyState.Move ||
                         transition.CurrentState == EnemyState.Stun ||
                         transition.CurrentState == EnemyState.Dead ||
                         transition.CurrentState == EnemyState.None)
                {
                    weakpoint.EndAttack();
                }
            }

            Publish(
                EnemyCombatEventType.StateChanged,
                transition.Reason.ToString(),
                0L,
                0d,
                0d,
                string.Empty,
                string.Empty,
                transition.Timestamp,
                definition.IsConfigured ? definition.EnemyId : string.Empty,
                damageable != null ? damageable.HitTargetId : 0,
                transition.CurrentState);

            if (transition.Reason == EnemyTransitionReason.Interrupted)
            {
                Publish(
                    EnemyCombatEventType.Interrupted,
                    transition.AttackId,
                    0L,
                    0d,
                    0d,
                    string.Empty,
                    string.Empty,
                    transition.Timestamp);
            }
            else if (transition.Reason == EnemyTransitionReason.Killed)
            {
                Publish(
                    EnemyCombatEventType.Died,
                    transition.AttackId,
                    0L,
                    0d,
                    0d,
                    string.Empty,
                    string.Empty,
                    transition.Timestamp);
            }
        }

        private void PublishDamage(
            in EnemyDamageResult result,
            string sourceId,
            double timestamp)
        {
            if (result.AppliedArmorDamage > 0L)
            {
                Publish(
                    EnemyCombatEventType.ArmorChanged,
                    sourceId,
                    -result.AppliedArmorDamage,
                    0d,
                    0d,
                    string.Empty,
                    string.Empty,
                    timestamp);
            }

            if (result.AppliedHpDamage > 0L)
            {
                Publish(
                    EnemyCombatEventType.HpChanged,
                    sourceId,
                    -result.AppliedHpDamage,
                    0d,
                    0d,
                    string.Empty,
                    string.Empty,
                    timestamp);
            }

            if (result.ArmorBroken)
            {
                Publish(
                    EnemyCombatEventType.ArmorBroken,
                    sourceId,
                    0L,
                    0d,
                    0d,
                    result.State.BreakEffectGroupId,
                    string.Empty,
                    timestamp);
            }
        }

        private void Publish(
            EnemyCombatEventType eventType,
            string sourceId,
            long signedAmount,
            double value,
            double durationSeconds,
            string effectGroupId,
            string buffId,
            double timestamp,
            string enemyIdOverride = null,
            int? targetIdOverride = null,
            EnemyState? stateOverride = null)
        {
            ulong sequence = nextEventSequence;
            if (sequence == 0UL || sequence == ulong.MaxValue)
            {
                throw new OverflowException("Enemy combat event sequence is exhausted.");
            }

            nextEventSequence = sequence + 1UL;
            string enemyId = enemyIdOverride ??
                (definition.IsConfigured ? definition.EnemyId : string.Empty);
            int targetId = targetIdOverride ??
                (damageable != null ? damageable.HitTargetId : 0);
            EnemyState state = stateOverride ?? stateMachine.Current.State;
            CombatEventPublished?.Invoke(new EnemyCombatEvent(
                sequence,
                eventType,
                enemyId,
                targetId,
                state,
                sourceId,
                signedAmount,
                value,
                durationSeconds,
                effectGroupId,
                buffId,
                timestamp));
        }

        private void UpdateWeakpoint(double timestamp)
        {
            if (weakpoint == null || !definition.IsConfigured)
            {
                return;
            }

            EnemyState currentState = stateMachine.Current.State;
            bool attackMayExpose =
                currentState == EnemyState.Windup || currentState == EnemyState.Attack;
            weakpoint.Tick(timestamp, attackMayExpose);
        }

        private long ScaleIncomingDamage(long configuredAmount)
        {
            if (configuredAmount < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredAmount),
                    "Enemy incoming damage must be non-negative.");
            }

            double scaled = configuredAmount * buffContainer.GetIncomingDamageMultiplier();
            if (double.IsNaN(scaled) ||
                double.IsInfinity(scaled) ||
                scaled > long.MaxValue)
            {
                throw new OverflowException(
                    "Configured enemy incoming damage exceeds Int64 capacity.");
            }

            return checked((long)Math.Round(
                scaled,
                0,
                MidpointRounding.AwayFromZero));
        }

        private void RequireSpawned()
        {
            if (!isSpawned)
            {
                throw new InvalidOperationException("EnemyController is not spawned.");
            }
        }

        private void RequireDamageableState()
        {
            RequireSpawned();
            EnemyState state = stateMachine.Current.State;
            if (state == EnemyState.Spawn || state == EnemyState.None)
            {
                throw new InvalidOperationException(
                    $"Enemy state '{state}' cannot receive combat effects.");
            }
        }

        private void RequireAliveCombatState()
        {
            RequireDamageableState();
            if (stateMachine.Current.State == EnemyState.Dead)
            {
                throw new InvalidOperationException(
                    "Dead enemies cannot receive live combat effects.");
            }
        }

        private static void ValidateFiniteNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Enemy effect value must be finite and non-negative.");
            }
        }
    }
}
