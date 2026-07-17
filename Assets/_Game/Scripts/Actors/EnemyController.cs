using System;
using System.Collections.Generic;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using UnityEngine;

namespace OneStrokeDemon.Actors
{
    // 定义 EnemyCombatEventType 的角色领域数据与行为边界，供上层流程以明确契约使用。
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

    // 定义 EnemyReleaseReason 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum EnemyReleaseReason
    {
        None = 0,
        Manual = 1,
        Disabled = 2,
        Cleared = 3
    }

    // 定义 EnemyCombatEvent 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyCombatEvent
    {
        // 初始化 EnemyCombatEvent，并建立角色运行时所需的初始状态。
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

    // 定义 EnemyHitResolution 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyHitResolution
    {
        // 初始化 EnemyHitResolution，并建立角色运行时所需的初始状态。
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

    // 定义 EnemyReleaseSnapshot 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyReleaseSnapshot
    {
        // 初始化 EnemyReleaseSnapshot，并建立角色运行时所需的初始状态。
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
    // 定义 EnemyController 的角色领域数据与行为边界，供上层流程以明确契约使用。
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

        // 处理 Awake 对应的角色逻辑，并返回或发布一致的状态结果。
        private void Awake()
        {
            EnsureComponents();
            EnsureStateEvents();
        }

        // 更新 Update 对应的角色逻辑，并返回或发布一致的状态结果。
        private void Update()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (isSpawned)
            {
                Tick(Time.timeAsDouble);
            }
        }

        // 响应 OnDisable 对应的角色逻辑，并返回或发布一致的状态结果。
        private void OnDisable()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (isSpawned)
            {
                double timestamp = stateMachine.Current.HasClock
                    ? stateMachine.Current.LastTimestamp
                    : 0d;
                Release(EnemyReleaseReason.Disabled, timestamp);
            }
        }

        // 生成 Spawn 对应的角色逻辑，并返回或发布一致的状态结果。
        public void Spawn(
            IConfigProvider configuredProvider,
            string enemyId,
            int hitTargetId,
            double timestamp,
            WeakpointController configuredWeakpoint = null)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configuredProvider == null)
            {
                throw new ArgumentNullException(nameof(configuredProvider));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (weakpoint != null)
            {
                weakpoint.Configure(definition.Weakpoint, damageable);
            }

            isSpawned = true;
            stateMachine.Spawn(timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        // 完成 CompleteSpawn 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool CompleteSpawn(double timestamp)
        {
            RequireSpawned();
            bool completed = stateMachine.CompleteSpawn(timestamp);
            UpdateWeakpoint(timestamp);
            return completed;
        }

        // 应用 ApplyBossPhaseProfile 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyPhaseProfileResult ApplyBossPhaseProfile(
            in EnemyDefinition phaseDefinition,
            string bossPhaseId,
            double timestamp)
        {
            RequireAliveCombatState();
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(bossPhaseId))
            {
                throw new ArgumentException(
                    "Boss phase id must be non-empty.",
                    nameof(bossPhaseId));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (phaseDefinition.Weakpoint.HasHitbox && weakpoint == null)
            {
                throw new InvalidOperationException(
                    $"Boss phase '{bossPhaseId}' requires a WeakpointController child.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!stateMachine.ChangePhase(timestamp))
            {
                throw new InvalidOperationException(
                    $"Boss phase '{bossPhaseId}' cannot be applied in state '{State.State}'.");
            }

            EnemyPhaseProfileResult result =
                damageable.ApplyPhaseProfile(phaseDefinition);
            definition = phaseDefinition;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (weakpoint != null)
            {
                weakpoint.Configure(definition.Weakpoint, damageable);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 开始 BeginAttack 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 按时间推进 Tick 对应的角色逻辑，并返回或发布一致的状态结果。
        public int Tick(double timestamp)
        {
            RequireSpawned();
            int transitions = stateMachine.Tick(timestamp);
            buffContainer.Tick(timestamp);
            UpdateWeakpoint(timestamp);
            return transitions;
        }

        // 处理 RecoverFromStun 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool RecoverFromStun(double timestamp)
        {
            RequireSpawned();
            bool recovered = stateMachine.RecoverFromStun(timestamp);
            UpdateWeakpoint(timestamp);
            return recovered;
        }

        // 应用 ApplyStrokeDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyHitResolution ApplyStrokeDamage(
            in DamageResult resolvedDamage,
            string gestureType,
            double timestamp,
            string sourceId)
        {
            RequireSpawned();
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!resolvedDamage.IsResolved)
            {
                throw new ArgumentException(
                    "Resolved T360 damage is required.",
                    nameof(resolvedDamage));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (resolvedDamage.TargetId != damageable.HitTargetId)
            {
                throw new ArgumentException(
                    $"Damage target '{resolvedDamage.TargetId}' does not match enemy target '{damageable.HitTargetId}'.",
                    nameof(resolvedDamage));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (damage.DeathTriggered)
            {
                stateMachine.TryKill(timestamp);
            }
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            else if (damage.Changed && resolvedDamage.ShouldInterruptAttack)
            {
                interrupt = stateMachine.TryInterrupt(gestureType, timestamp);
            }

            UpdateWeakpoint(timestamp);
            return new EnemyHitResolution(damage, interrupt);
        }

        // 应用 ApplyDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDamageResult ApplyDamage(
            long amount,
            string sourceId,
            double timestamp)
        {
            RequireDamageableState();
            Tick(timestamp);
            EnemyDamageResult result = damageable.ApplyDamage(ScaleIncomingDamage(amount));
            PublishDamage(result, sourceId, timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (result.DeathTriggered)
            {
                stateMachine.TryKill(timestamp);
                UpdateWeakpoint(timestamp);
            }

            return result;
        }

        // 应用 ApplyProjectileDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDamageResult ApplyProjectileDamage(
            in ProjectileDamageSource source,
            double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!source.IsValid)
            {
                throw new ArgumentException(
                    "Projectile damage source must be initialized.",
                    nameof(source));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 恢复 Heal 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyHealingResult Heal(long amount, string sourceId, double timestamp)
        {
            RequireSpawned();
            Tick(timestamp);
            EnemyHealingResult result = damageable.Heal(amount);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 移除 RemoveArmor 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 尝试执行 TryExecute 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyExecuteResult TryExecute(
            double threshold,
            string sourceId,
            double timestamp)
        {
            RequireDamageableState();
            Tick(timestamp);
            EnemyExecuteResult result = damageable.TryExecute(threshold);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (result.DeathTriggered)
            {
                stateMachine.TryKill(timestamp);
                UpdateWeakpoint(timestamp);
            }

            return result;
        }

        // 应用 ApplyBuff 对应的角色逻辑，并返回或发布一致的状态结果。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (string.Equals(result.Buff.Type, "Stun", StringComparison.Ordinal))
                {
                    stateMachine.ApplyTimedStun(durationSeconds, timestamp);
                    UpdateWeakpoint(timestamp);
                }
            }

            return result;
        }

        // 处理 RequestKnockback 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 处理 IncrementCounter 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool IncrementCounter(
            string counterId,
            double amount,
            double limit,
            string sourceId,
            double timestamp)
        {
            RequireAliveCombatState();
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 尝试执行 TryGetCounter 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool TryGetCounter(string counterId, out double value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (counterId != null && counters.TryGetValue(counterId, out value))
            {
                return true;
            }

            value = 0d;
            return false;
        }

        // 释放 Release 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyReleaseSnapshot Release(EnemyReleaseReason reason, double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (reason == EnemyReleaseReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason),
                    "A concrete enemy release reason is required.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            CombatEventPublished = null;
            nextEventSequence = 1UL;

            return snapshot;
        }

        // 处理 AcquireFromPool 对应的角色逻辑，并返回或发布一致的状态结果。
        public void AcquireFromPool(in PoolLease lease)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!lease.IsValid)
            {
                throw new ArgumentException("A valid pool lease is required.", nameof(lease));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        // 释放 ReleaseToPool 对应的角色逻辑，并返回或发布一致的状态结果。
        public void ReleaseToPool(in PoolReleaseContext context)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!hasPoolParent)
            {
                poolParent = transform.parent;
                hasPoolParent = true;
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (context.Lease.IsValid && poolLease.IsValid && context.Lease != poolLease)
            {
                throw new InvalidOperationException("Enemy pool release used a stale lease.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        // 重置 ResetPoolTransform 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ResetPoolTransform()
        {
            transform.SetParent(hasPoolParent ? poolParent : null, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        // 处理 EnsureComponents 对应的角色逻辑，并返回或发布一致的状态结果。
        private void EnsureComponents()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (damageable == null)
            {
                damageable = GetComponent<Damageable>();
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (damageable == null)
            {
                throw new InvalidOperationException(
                    "EnemyController requires a Damageable component.");
            }
        }

        // 处理 EnsureStateEvents 对应的角色逻辑，并返回或发布一致的状态结果。
        private void EnsureStateEvents()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!stateEventsAttached)
            {
                stateMachine.Transitioned += OnStateTransitioned;
                stateEventsAttached = true;
            }
        }

        // 响应 OnStateTransitioned 对应的角色逻辑，并返回或发布一致的状态结果。
        private void OnStateTransitioned(EnemyStateTransition transition)
        {
            bool acceptsHits = transition.CurrentState == EnemyState.Move ||
                               transition.CurrentState == EnemyState.Windup ||
                               transition.CurrentState == EnemyState.Attack ||
                               transition.CurrentState == EnemyState.Recovery ||
                               transition.CurrentState == EnemyState.Stun;
            damageable?.SetStrokeHitEnabled(acceptsHits);

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (weakpoint != null)
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (transition.CurrentState == EnemyState.Windup &&
                    transition.Reason == EnemyTransitionReason.AttackStarted)
                {
                    weakpoint.BeginAttack(transition.Timestamp);
                }
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 处理 PublishDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        private void PublishDamage(
            in EnemyDamageResult result,
            string sourceId,
            double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 处理 Publish 对应的角色逻辑，并返回或发布一致的状态结果。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 更新 UpdateWeakpoint 对应的角色逻辑，并返回或发布一致的状态结果。
        private void UpdateWeakpoint(double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (weakpoint == null || !definition.IsConfigured)
            {
                return;
            }

            EnemyState currentState = stateMachine.Current.State;
            bool attackMayExpose =
                currentState == EnemyState.Windup || currentState == EnemyState.Attack;
            weakpoint.Tick(timestamp, attackMayExpose);
        }

        // 处理 ScaleIncomingDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        private long ScaleIncomingDamage(long configuredAmount)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configuredAmount < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredAmount),
                    "Enemy incoming damage must be non-negative.");
            }

            double scaled = configuredAmount * buffContainer.GetIncomingDamageMultiplier();
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 处理 RequireSpawned 对应的角色逻辑，并返回或发布一致的状态结果。
        private void RequireSpawned()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isSpawned)
            {
                throw new InvalidOperationException("EnemyController is not spawned.");
            }
        }

        // 处理 RequireDamageableState 对应的角色逻辑，并返回或发布一致的状态结果。
        private void RequireDamageableState()
        {
            RequireSpawned();
            EnemyState state = stateMachine.Current.State;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (state == EnemyState.Spawn || state == EnemyState.None)
            {
                throw new InvalidOperationException(
                    $"Enemy state '{state}' cannot receive combat effects.");
            }
        }

        // 处理 RequireAliveCombatState 对应的角色逻辑，并返回或发布一致的状态结果。
        private void RequireAliveCombatState()
        {
            RequireDamageableState();
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (stateMachine.Current.State == EnemyState.Dead)
            {
                throw new InvalidOperationException(
                    "Dead enemies cannot receive live combat effects.");
            }
        }

        // 校验 ValidateFiniteNonNegative 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateFiniteNonNegative(double value, string parameterName)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
