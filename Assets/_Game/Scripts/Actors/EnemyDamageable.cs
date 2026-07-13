using System;
using OneStrokeDemon.Combat;
using UnityEngine;

namespace OneStrokeDemon.Actors
{
    public enum EnemyDamageStatus
    {
        None = 0,
        Applied = 1,
        NoDamage = 2,
        Inactive = 3,
        AlreadyDead = 4,
        Killed = 5
    }

    public enum EnemyHealingStatus
    {
        None = 0,
        Applied = 1,
        NoHealing = 2,
        AtMaximum = 3,
        Inactive = 4,
        AlreadyDead = 5
    }

    public enum EnemyExecuteStatus
    {
        None = 0,
        Executed = 1,
        AboveThreshold = 2,
        Inactive = 3,
        AlreadyDead = 4
    }

    public readonly struct EnemyDamageSnapshot
    {
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

    public readonly struct EnemyDamageResult
    {
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

    public readonly struct EnemyHealingResult
    {
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

    public readonly struct EnemyExecuteResult
    {
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

        public void Spawn(in EnemyDefinition configuredDefinition, int configuredHitTargetId)
        {
            if (!configuredDefinition.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy definition must be configured.",
                    nameof(configuredDefinition));
            }

            if (configuredHitTargetId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredHitTargetId),
                    "Enemy hit target id must be non-zero.");
            }

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

        public EnemyDamageResult ApplyDamage(long amount)
        {
            if (amount < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Enemy damage must be non-negative.");
            }

            if (!isActive)
            {
                return DamageResult(EnemyDamageStatus.Inactive, amount, 0L, 0L, false, false);
            }

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

        public EnemyDamageResult RemoveArmor(long amount)
        {
            if (amount < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Enemy armor removal must be non-negative.");
            }

            if (!isActive)
            {
                return DamageResult(EnemyDamageStatus.Inactive, amount, 0L, 0L, false, false);
            }

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

        public EnemyHealingResult Heal(long amount)
        {
            if (amount < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Enemy healing must be non-negative.");
            }

            if (!isActive)
            {
                return HealingResult(EnemyHealingStatus.Inactive, amount, 0L);
            }

            if (currentHp == 0L)
            {
                return HealingResult(EnemyHealingStatus.AlreadyDead, amount, 0L);
            }

            if (amount == 0L)
            {
                return HealingResult(EnemyHealingStatus.NoHealing, amount, 0L);
            }

            long missingHp = definition.MaximumHp - currentHp;
            if (missingHp == 0L)
            {
                return HealingResult(EnemyHealingStatus.AtMaximum, amount, 0L);
            }

            long applied = Math.Min(amount, missingHp);
            currentHp += applied;
            return HealingResult(EnemyHealingStatus.Applied, amount, applied);
        }

        public EnemyExecuteResult TryExecute(double threshold)
        {
            if (double.IsNaN(threshold) ||
                double.IsInfinity(threshold) ||
                threshold < 0d ||
                threshold > 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(threshold),
                    "Enemy execute threshold must be in [0, 1].");
            }

            if (!isActive)
            {
                return ExecuteResult(EnemyExecuteStatus.Inactive, threshold, 0L);
            }

            if (currentHp == 0L)
            {
                return ExecuteResult(EnemyExecuteStatus.AlreadyDead, threshold, 0L);
            }

            if ((double)currentHp / definition.MaximumHp > threshold)
            {
                return ExecuteResult(EnemyExecuteStatus.AboveThreshold, threshold, 0L);
            }

            long applied = currentHp;
            currentHp = 0L;
            return ExecuteResult(EnemyExecuteStatus.Executed, threshold, applied);
        }

        public bool Release()
        {
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

        private EnemyHealingResult HealingResult(
            EnemyHealingStatus status,
            long requested,
            long applied)
        {
            return new EnemyHealingResult(status, requested, applied, Current);
        }

        private EnemyExecuteResult ExecuteResult(
            EnemyExecuteStatus status,
            double threshold,
            long applied)
        {
            return new EnemyExecuteResult(status, threshold, applied, Current);
        }
    }

    [DisallowMultipleComponent]
    public sealed class Damageable : MonoBehaviour, IHittable
    {
        private readonly EnemyDamageModel model = new EnemyDamageModel();
        private bool strokeHitEnabled;

        public EnemyDamageSnapshot Current => model.Current;

        public int HitTargetId => Current.HitTargetId;

        public bool CanReceiveStrokeHit =>
            strokeHitEnabled && Current.IsActive && !Current.IsDead;

        internal void Spawn(in EnemyDefinition definition, int hitTargetId)
        {
            model.Spawn(definition, hitTargetId);
            strokeHitEnabled = false;
        }

        internal void SetStrokeHitEnabled(bool enabled)
        {
            strokeHitEnabled = enabled && Current.IsActive && !Current.IsDead;
        }

        public EnemyDamageResult ApplyResolvedDamage(in DamageResult damage)
        {
            if (!damage.IsResolved)
            {
                throw new ArgumentException(
                    "Resolved T360 damage is required.",
                    nameof(damage));
            }

            if (damage.TargetId != HitTargetId)
            {
                throw new ArgumentException(
                    $"Damage target '{damage.TargetId}' does not match enemy target '{HitTargetId}'.",
                    nameof(damage));
            }

            return model.ApplyDamage(damage.Damage);
        }

        public EnemyDamageResult ApplyDamage(long amount)
        {
            return model.ApplyDamage(amount);
        }

        public EnemyDamageResult ApplyProjectileDamage(in ProjectileDamageSource source)
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

            return model.ApplyDamage(source.Damage);
        }

        public EnemyDamageResult RemoveArmor(long amount)
        {
            return model.RemoveArmor(amount);
        }

        public EnemyHealingResult Heal(long amount)
        {
            return model.Heal(amount);
        }

        public EnemyExecuteResult TryExecute(double threshold)
        {
            return model.TryExecute(threshold);
        }

        internal bool Release()
        {
            strokeHitEnabled = false;
            return model.Release();
        }
    }
}
