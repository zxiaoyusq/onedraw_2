using System;
using OneStrokeDemon.Combat;
using UnityEngine;

namespace OneStrokeDemon.Actors
{
    // 定义 EnemyDamageStatus 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum EnemyDamageStatus
    {
        None = 0,
        Applied = 1,
        NoDamage = 2,
        Inactive = 3,
        AlreadyDead = 4,
        Killed = 5
    }

    // 定义 EnemyHealingStatus 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum EnemyHealingStatus
    {
        None = 0,
        Applied = 1,
        NoHealing = 2,
        AtMaximum = 3,
        Inactive = 4,
        AlreadyDead = 5
    }

    // 定义 EnemyExecuteStatus 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum EnemyExecuteStatus
    {
        None = 0,
        Executed = 1,
        AboveThreshold = 2,
        Inactive = 3,
        AlreadyDead = 4
    }

    // 定义 EnemyDamageSnapshot 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyDamageSnapshot
    {
        // 初始化 EnemyDamageSnapshot，并建立角色运行时所需的初始状态。
        internal EnemyDamageSnapshot(
            string enemyId,
            int hitTargetId,
            long maximumHp,
            long currentHp,
            long maximumArmor,
            long currentArmor,
            string breakEffectGroupId,
            bool isActive)
        {
            EnemyId = enemyId ?? string.Empty;
            HitTargetId = hitTargetId;
            MaximumHp = maximumHp;
            CurrentHp = currentHp;
            MaximumArmor = maximumArmor;
            CurrentArmor = currentArmor;
            BreakEffectGroupId = breakEffectGroupId ?? string.Empty;
            IsActive = isActive;
            IsValid = true;
        }

        public string EnemyId { get; }

        public int HitTargetId { get; }

        public long MaximumHp { get; }

        public long CurrentHp { get; }

        public long MaximumArmor { get; }

        public long CurrentArmor { get; }

        public string BreakEffectGroupId { get; }

        public bool IsActive { get; }

        public bool IsValid { get; }

        public bool IsDead => IsActive && CurrentHp == 0L;

        public double HpRatio => MaximumHp > 0L
            ? (double)CurrentHp / MaximumHp
            : 0d;
    }

    // 定义 EnemyDamageResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyDamageResult
    {
        // 初始化 EnemyDamageResult，并建立角色运行时所需的初始状态。
        internal EnemyDamageResult(
            EnemyDamageStatus status,
            long requestedDamage,
            long appliedArmorDamage,
            long appliedHpDamage,
            bool armorBroken,
            bool deathTriggered,
            EnemyDamageSnapshot state)
        {
            Status = status;
            RequestedDamage = requestedDamage;
            AppliedArmorDamage = appliedArmorDamage;
            AppliedHpDamage = appliedHpDamage;
            ArmorBroken = armorBroken;
            DeathTriggered = deathTriggered;
            State = state;
            IsValid = true;
        }

        public EnemyDamageStatus Status { get; }

        public long RequestedDamage { get; }

        public long AppliedArmorDamage { get; }

        public long AppliedHpDamage { get; }

        public long AppliedTotalDamage => AppliedArmorDamage + AppliedHpDamage;

        public bool ArmorBroken { get; }

        public bool DeathTriggered { get; }

        public EnemyDamageSnapshot State { get; }

        public bool IsValid { get; }

        public bool Changed => AppliedArmorDamage > 0L || AppliedHpDamage > 0L;
    }

    // 定义 EnemyHealingResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyHealingResult
    {
        // 初始化 EnemyHealingResult，并建立角色运行时所需的初始状态。
        internal EnemyHealingResult(
            EnemyHealingStatus status,
            long requestedHealing,
            long appliedHealing,
            EnemyDamageSnapshot state)
        {
            Status = status;
            RequestedHealing = requestedHealing;
            AppliedHealing = appliedHealing;
            State = state;
            IsValid = true;
        }

        public EnemyHealingStatus Status { get; }

        public long RequestedHealing { get; }

        public long AppliedHealing { get; }

        public EnemyDamageSnapshot State { get; }

        public bool IsValid { get; }

        public bool Changed => AppliedHealing > 0L;
    }

    // 定义 EnemyExecuteResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyExecuteResult
    {
        // 初始化 EnemyExecuteResult，并建立角色运行时所需的初始状态。
        internal EnemyExecuteResult(
            EnemyExecuteStatus status,
            double threshold,
            long appliedHpDamage,
            EnemyDamageSnapshot state)
        {
            Status = status;
            Threshold = threshold;
            AppliedHpDamage = appliedHpDamage;
            State = state;
            IsValid = true;
        }

        public EnemyExecuteStatus Status { get; }

        public double Threshold { get; }

        public long AppliedHpDamage { get; }

        public EnemyDamageSnapshot State { get; }

        public bool IsValid { get; }

        public bool DeathTriggered => Status == EnemyExecuteStatus.Executed;
    }

    // 定义 EnemyPhaseProfileResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyPhaseProfileResult
    {
        // 初始化 EnemyPhaseProfileResult，并建立角色运行时所需的初始状态。
        internal EnemyPhaseProfileResult(
            long previousArmor,
            long currentArmor,
            EnemyDamageSnapshot state)
        {
            PreviousArmor = previousArmor;
            CurrentArmor = currentArmor;
            State = state;
            IsValid = true;
        }

        public long PreviousArmor { get; }

        public long CurrentArmor { get; }

        public long ArmorDelta => CurrentArmor - PreviousArmor;

        public EnemyDamageSnapshot State { get; }

        public bool IsValid { get; }

        public bool ArmorChanged => PreviousArmor != CurrentArmor;
    }

    // 定义 EnemyDamageModel 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class EnemyDamageModel
    {
        private EnemyDefinition definition;
        private int hitTargetId;
        private long currentHp;
        private long currentArmor;
        private bool isActive;

        public EnemyDamageSnapshot Current => new EnemyDamageSnapshot(
            definition.IsConfigured ? definition.EnemyId : string.Empty,
            hitTargetId,
            definition.IsConfigured ? definition.MaximumHp : 0L,
            currentHp,
            definition.IsConfigured ? definition.Defense.MaximumArmor : 0L,
            currentArmor,
            definition.IsConfigured
                ? definition.Defense.BreakEffectGroupId
                : string.Empty,
            isActive);

        // 生成 Spawn 对应的角色逻辑，并返回或发布一致的状态结果。
        public void Spawn(in EnemyDefinition configuredDefinition, int configuredHitTargetId)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!configuredDefinition.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy definition must be configured.",
                    nameof(configuredDefinition));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configuredHitTargetId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredHitTargetId),
                    "Enemy hit target id must be non-zero.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (isActive)
            {
                throw new InvalidOperationException(
                    "Active enemy damage state must be released before reuse.");
            }

            definition = configuredDefinition;
            hitTargetId = configuredHitTargetId;
            currentHp = definition.MaximumHp;
            currentArmor = definition.Defense.MaximumArmor;
            isActive = true;
        }

        // 应用 ApplyPhaseProfile 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyPhaseProfileResult ApplyPhaseProfile(
            in EnemyDefinition configuredDefinition)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!configuredDefinition.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy phase definition must be configured.",
                    nameof(configuredDefinition));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isActive || currentHp == 0L)
            {
                throw new InvalidOperationException(
                    "Only an active living enemy can apply a phase profile.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!string.Equals(
                    definition.EnemyId,
                    configuredDefinition.EnemyId,
                    StringComparison.Ordinal) ||
                definition.MaximumHp != configuredDefinition.MaximumHp ||
                definition.Tier != configuredDefinition.Tier)
            {
                throw new ArgumentException(
                    "A phase profile cannot replace enemy identity, tier, or maximum HP.",
                    nameof(configuredDefinition));
            }

            long previousArmor = currentArmor;
            definition = configuredDefinition;
            currentArmor = definition.Defense.MaximumArmor;
            return new EnemyPhaseProfileResult(previousArmor, currentArmor, Current);
        }

        // 应用 ApplyDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDamageResult ApplyDamage(long amount)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Enemy damage must be non-negative.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isActive)
            {
                return DamageResult(EnemyDamageStatus.Inactive, amount, 0L, 0L, false, false);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentHp == 0L)
            {
                return DamageResult(
                    EnemyDamageStatus.AlreadyDead,
                    amount,
                    0L,
                    0L,
                    false,
                    false);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount == 0L)
            {
                return DamageResult(
                    EnemyDamageStatus.NoDamage,
                    amount,
                    0L,
                    0L,
                    false,
                    false);
            }

            long armorBefore = currentArmor;
            long armorDamage = Math.Min(amount, currentArmor);
            currentArmor -= armorDamage;
            long remaining = amount - armorDamage;
            long hpDamage = Math.Min(remaining, currentHp);
            currentHp -= hpDamage;
            bool armorBroken = armorBefore > 0L && currentArmor == 0L;
            bool deathTriggered = currentHp == 0L;
            return DamageResult(
                deathTriggered ? EnemyDamageStatus.Killed : EnemyDamageStatus.Applied,
                amount,
                armorDamage,
                hpDamage,
                armorBroken,
                deathTriggered);
        }

        // 移除 RemoveArmor 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDamageResult RemoveArmor(long amount)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Enemy armor removal must be non-negative.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isActive)
            {
                return DamageResult(EnemyDamageStatus.Inactive, amount, 0L, 0L, false, false);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentHp == 0L)
            {
                return DamageResult(
                    EnemyDamageStatus.AlreadyDead,
                    amount,
                    0L,
                    0L,
                    false,
                    false);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount == 0L || currentArmor == 0L)
            {
                return DamageResult(
                    EnemyDamageStatus.NoDamage,
                    amount,
                    0L,
                    0L,
                    false,
                    false);
            }

            long armorBefore = currentArmor;
            long armorDamage = Math.Min(amount, currentArmor);
            currentArmor -= armorDamage;
            return DamageResult(
                EnemyDamageStatus.Applied,
                amount,
                armorDamage,
                0L,
                armorBefore > 0L && currentArmor == 0L,
                false);
        }

        // 恢复 Heal 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyHealingResult Heal(long amount)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Enemy healing must be non-negative.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isActive)
            {
                return HealingResult(EnemyHealingStatus.Inactive, amount, 0L);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentHp == 0L)
            {
                return HealingResult(EnemyHealingStatus.AlreadyDead, amount, 0L);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount == 0L)
            {
                return HealingResult(EnemyHealingStatus.NoHealing, amount, 0L);
            }

            long missingHp = definition.MaximumHp - currentHp;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (missingHp == 0L)
            {
                return HealingResult(EnemyHealingStatus.AtMaximum, amount, 0L);
            }

            long applied = Math.Min(amount, missingHp);
            currentHp += applied;
            return HealingResult(EnemyHealingStatus.Applied, amount, applied);
        }

        // 尝试执行 TryExecute 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyExecuteResult TryExecute(double threshold)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(threshold) ||
                double.IsInfinity(threshold) ||
                threshold < 0d ||
                threshold > 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(threshold),
                    "Enemy execute threshold must be in [0, 1].");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isActive)
            {
                return ExecuteResult(EnemyExecuteStatus.Inactive, threshold, 0L);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentHp == 0L)
            {
                return ExecuteResult(EnemyExecuteStatus.AlreadyDead, threshold, 0L);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if ((double)currentHp / definition.MaximumHp > threshold)
            {
                return ExecuteResult(EnemyExecuteStatus.AboveThreshold, threshold, 0L);
            }

            long applied = currentHp;
            currentHp = 0L;
            return ExecuteResult(EnemyExecuteStatus.Executed, threshold, applied);
        }

        // 释放 Release 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool Release()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isActive)
            {
                return false;
            }

            definition = default;
            hitTargetId = 0;
            currentHp = 0L;
            currentArmor = 0L;
            isActive = false;
            return true;
        }

        // 处理 DamageResult 对应的角色逻辑，并返回或发布一致的状态结果。
        private EnemyDamageResult DamageResult(
            EnemyDamageStatus status,
            long requested,
            long armorDamage,
            long hpDamage,
            bool armorBroken,
            bool deathTriggered)
        {
            return new EnemyDamageResult(
                status,
                requested,
                armorDamage,
                hpDamage,
                armorBroken,
                deathTriggered,
                Current);
        }

        // 恢复 HealingResult 对应的角色逻辑，并返回或发布一致的状态结果。
        private EnemyHealingResult HealingResult(
            EnemyHealingStatus status,
            long requested,
            long applied)
        {
            return new EnemyHealingResult(status, requested, applied, Current);
        }

        // 处理 ExecuteResult 对应的角色逻辑，并返回或发布一致的状态结果。
        private EnemyExecuteResult ExecuteResult(
            EnemyExecuteStatus status,
            double threshold,
            long applied)
        {
            return new EnemyExecuteResult(status, threshold, applied, Current);
        }
    }

    [DisallowMultipleComponent]
    // 定义 Damageable 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class Damageable : MonoBehaviour, IHittable
    {
        private readonly EnemyDamageModel model = new EnemyDamageModel();
        private bool strokeHitEnabled;

        public EnemyDamageSnapshot Current => model.Current;

        public int HitTargetId => Current.HitTargetId;

        public bool CanReceiveStrokeHit =>
            strokeHitEnabled && Current.IsActive && !Current.IsDead;

        // 生成 Spawn 对应的角色逻辑，并返回或发布一致的状态结果。
        internal void Spawn(in EnemyDefinition definition, int hitTargetId)
        {
            model.Spawn(definition, hitTargetId);
            strokeHitEnabled = false;
        }

        // 设置 SetStrokeHitEnabled 对应的角色逻辑，并返回或发布一致的状态结果。
        internal void SetStrokeHitEnabled(bool enabled)
        {
            strokeHitEnabled = enabled && Current.IsActive && !Current.IsDead;
        }

        // 应用 ApplyPhaseProfile 对应的角色逻辑，并返回或发布一致的状态结果。
        internal EnemyPhaseProfileResult ApplyPhaseProfile(
            in EnemyDefinition definition)
        {
            return model.ApplyPhaseProfile(definition);
        }

        // 应用 ApplyResolvedDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDamageResult ApplyResolvedDamage(in DamageResult damage)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!damage.IsResolved)
            {
                throw new ArgumentException(
                    "Resolved T360 damage is required.",
                    nameof(damage));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (damage.TargetId != HitTargetId)
            {
                throw new ArgumentException(
                    $"Damage target '{damage.TargetId}' does not match enemy target '{HitTargetId}'.",
                    nameof(damage));
            }

            return model.ApplyDamage(damage.Damage);
        }

        // 应用 ApplyDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDamageResult ApplyDamage(long amount)
        {
            return model.ApplyDamage(amount);
        }

        // 应用 ApplyProjectileDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDamageResult ApplyProjectileDamage(in ProjectileDamageSource source)
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

            return model.ApplyDamage(source.Damage);
        }

        // 移除 RemoveArmor 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDamageResult RemoveArmor(long amount)
        {
            return model.RemoveArmor(amount);
        }

        // 恢复 Heal 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyHealingResult Heal(long amount)
        {
            return model.Heal(amount);
        }

        // 尝试执行 TryExecute 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyExecuteResult TryExecute(double threshold)
        {
            return model.TryExecute(threshold);
        }

        // 释放 Release 对应的角色逻辑，并返回或发布一致的状态结果。
        internal bool Release()
        {
            strokeHitEnabled = false;
            return model.Release();
        }
    }
}
