using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    public enum EnemyTier
    {
        None = 0,
        Normal = 1,
        Elite = 2,
        Boss = 3
    }

    public readonly struct EnemyDefenseDefinition
    {
        internal EnemyDefenseDefinition(
            string defenseRuleId,
            long maximumArmor,
            string breakEffectGroupId)
        {
            DefenseRuleId = defenseRuleId;
            MaximumArmor = maximumArmor;
            BreakEffectGroupId = breakEffectGroupId;
            IsConfigured = true;
        }

        public string DefenseRuleId { get; }

        public long MaximumArmor { get; }

        public string BreakEffectGroupId { get; }

        public bool IsConfigured { get; }
    }

    public readonly struct EnemyWeakpointDefinition
    {
        internal EnemyWeakpointDefinition(
            string weakpointRuleId,
            double windowStartSeconds,
            double windowEndSeconds,
            float radiusReferencePixels,
            double damageMultiplier,
            bool interruptsAttack,
            long energyBonus,
            long scoreBonus,
            string vfxKey)
        {
            WeakpointRuleId = weakpointRuleId;
            WindowStartSeconds = windowStartSeconds;
            WindowEndSeconds = windowEndSeconds;
            RadiusReferencePixels = radiusReferencePixels;
            DamageMultiplier = damageMultiplier;
            InterruptsAttack = interruptsAttack;
            EnergyBonus = energyBonus;
            ScoreBonus = scoreBonus;
            VfxKey = vfxKey;
            IsConfigured = true;
        }

        public string WeakpointRuleId { get; }

        public double WindowStartSeconds { get; }

        public double WindowEndSeconds { get; }

        public float RadiusReferencePixels { get; }

        public double DamageMultiplier { get; }

        public bool InterruptsAttack { get; }

        public long EnergyBonus { get; }

        public long ScoreBonus { get; }

        public string VfxKey { get; }

        public bool IsConfigured { get; }

        public bool HasHitbox => IsConfigured && RadiusReferencePixels > 0f;

        public bool IsOpenAt(double elapsedSeconds)
        {
            return HasHitbox &&
                   IsFinite(elapsedSeconds) &&
                   elapsedSeconds >= WindowStartSeconds &&
                   elapsedSeconds <= WindowEndSeconds;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    public readonly struct EnemyDefinition
    {
        internal EnemyDefinition(
            string enemyId,
            string displayNameKey,
            EnemyTier tier,
            long maximumHp,
            string movePatternId,
            float moveSpeedReferencePixelsPerSecond,
            string attackSetId,
            EnemyDefenseDefinition defense,
            EnemyWeakpointDefinition weakpoint,
            string stanceVulnerability,
            long contactDamage,
            long scoreValue,
            string assetKey,
            int poolPrewarm)
        {
            EnemyId = enemyId;
            DisplayNameKey = displayNameKey;
            Tier = tier;
            MaximumHp = maximumHp;
            MovePatternId = movePatternId;
            MoveSpeedReferencePixelsPerSecond = moveSpeedReferencePixelsPerSecond;
            AttackSetId = attackSetId;
            Defense = defense;
            Weakpoint = weakpoint;
            StanceVulnerability = stanceVulnerability;
            ContactDamage = contactDamage;
            ScoreValue = scoreValue;
            AssetKey = assetKey;
            PoolPrewarm = poolPrewarm;
            IsConfigured = true;
        }

        public string EnemyId { get; }

        public string DisplayNameKey { get; }

        public EnemyTier Tier { get; }

        public long MaximumHp { get; }

        public string MovePatternId { get; }

        public float MoveSpeedReferencePixelsPerSecond { get; }

        public string AttackSetId { get; }

        public EnemyDefenseDefinition Defense { get; }

        public EnemyWeakpointDefinition Weakpoint { get; }

        public string StanceVulnerability { get; }

        public long ContactDamage { get; }

        public long ScoreValue { get; }

        public string AssetKey { get; }

        public int PoolPrewarm { get; }

        public bool IsConfigured { get; }
    }

    public static class EnemyDefinitionFactory
    {
        public static EnemyDefinition Create(IConfigProvider configProvider, string enemyId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            EnemyConfig enemy = configProvider.GetEnemy(enemyId);
            DefenseRuleConfig defense = configProvider.GetDefenseRule(enemy.DefenseRuleId);
            WeakpointRuleConfig weakpoint = configProvider.GetWeakpointRule(enemy.WeakpointRuleId);

            RequireNonEmpty(enemy.EnemyId, nameof(enemy.EnemyId), enemy.EnemyId);
            RequirePositive(enemy.EnemyId, nameof(enemy.MaxHp), enemy.MaxHp);
            RequireNonNegative(
                enemy.EnemyId,
                nameof(enemy.MoveSpeedRefPxSec),
                enemy.MoveSpeedRefPxSec);
            RequireNonNegative(enemy.EnemyId, nameof(enemy.ContactDamage), enemy.ContactDamage);
            RequireNonNegative(enemy.EnemyId, nameof(enemy.ScoreValue), enemy.ScoreValue);
            RequireInt32NonNegative(enemy.EnemyId, nameof(enemy.PoolPrewarm), enemy.PoolPrewarm);
            RequireNonNegative(defense.DefenseRuleId, nameof(defense.ArmorHp), defense.ArmorHp);
            RequireFiniteNonNegative(
                weakpoint.WeakpointRuleId,
                nameof(weakpoint.WindowStartSec),
                weakpoint.WindowStartSec);
            RequireFiniteNonNegative(
                weakpoint.WeakpointRuleId,
                nameof(weakpoint.WindowEndSec),
                weakpoint.WindowEndSec);
            if (weakpoint.WindowEndSec < weakpoint.WindowStartSec)
            {
                throw Invalid(
                    weakpoint.WeakpointRuleId,
                    nameof(weakpoint.WindowEndSec),
                    weakpoint.WindowEndSec,
                    "Weakpoint window end must be at or after its start.");
            }

            RequireInt32NonNegative(
                weakpoint.WeakpointRuleId,
                nameof(weakpoint.RadiusRefPx),
                weakpoint.RadiusRefPx);
            RequireFiniteNonNegative(
                weakpoint.WeakpointRuleId,
                nameof(weakpoint.DamageMultiplier),
                weakpoint.DamageMultiplier);
            RequireNonNegative(
                weakpoint.WeakpointRuleId,
                nameof(weakpoint.EnergyBonus),
                weakpoint.EnergyBonus);
            RequireNonNegative(
                weakpoint.WeakpointRuleId,
                nameof(weakpoint.ScoreBonus),
                weakpoint.ScoreBonus);

            return new EnemyDefinition(
                enemy.EnemyId,
                enemy.DisplayNameKey,
                ParseTier(enemy.EnemyId, enemy.Tier),
                enemy.MaxHp,
                enemy.MovePatternId,
                enemy.MoveSpeedRefPxSec,
                enemy.AttackSetId,
                new EnemyDefenseDefinition(
                    defense.DefenseRuleId,
                    defense.ArmorHp,
                    defense.BreakEffectGroupId ?? string.Empty),
                new EnemyWeakpointDefinition(
                    weakpoint.WeakpointRuleId,
                    weakpoint.WindowStartSec,
                    weakpoint.WindowEndSec,
                    weakpoint.RadiusRefPx,
                    weakpoint.DamageMultiplier,
                    weakpoint.InterruptAttack,
                    weakpoint.EnergyBonus,
                    weakpoint.ScoreBonus,
                    weakpoint.VfxKey ?? string.Empty),
                enemy.StanceVulnerability,
                enemy.ContactDamage,
                enemy.ScoreValue,
                enemy.AssetKey,
                checked((int)enemy.PoolPrewarm));
        }

        private static EnemyTier ParseTier(string enemyId, string configuredTier)
        {
            switch (configuredTier)
            {
                case "Normal": return EnemyTier.Normal;
                case "Elite": return EnemyTier.Elite;
                case "Boss": return EnemyTier.Boss;
                default:
                    throw new ArgumentException(
                        $"Enemy '{enemyId}' has unsupported tier '{configuredTier}'.",
                        nameof(configuredTier));
            }
        }

        private static void RequireNonEmpty(string rowId, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw Invalid(rowId, field, value, "Configured string must be non-empty.");
            }
        }

        private static void RequirePositive(string rowId, string field, long value)
        {
            if (value <= 0)
            {
                throw Invalid(rowId, field, value, "Configured value must be positive.");
            }
        }

        private static void RequireNonNegative(string rowId, string field, long value)
        {
            if (value < 0)
            {
                throw Invalid(rowId, field, value, "Configured value must be non-negative.");
            }
        }

        private static void RequireInt32NonNegative(string rowId, string field, long value)
        {
            if (value < 0 || value > int.MaxValue)
            {
                throw Invalid(
                    rowId,
                    field,
                    value,
                    "Configured value must fit a non-negative Int32.");
            }
        }

        private static void RequireFiniteNonNegative(
            string rowId,
            string field,
            double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw Invalid(
                    rowId,
                    field,
                    value,
                    "Configured value must be finite and non-negative.");
            }
        }

        private static ArgumentOutOfRangeException Invalid(
            string rowId,
            string field,
            object value,
            string message)
        {
            return new ArgumentOutOfRangeException(
                field,
                value,
                $"Enemy definition '{rowId}.{field}' is invalid. {message}");
        }
    }

    public readonly struct EnemyAttackTimeline
    {
        internal EnemyAttackTimeline(
            string attackId,
            string attackSetId,
            double cooldownSeconds,
            double windupSeconds,
            double activeSeconds,
            string interruptGestureType,
            double interruptStartSeconds,
            double interruptEndSeconds,
            string effectGroupId)
        {
            AttackId = attackId;
            AttackSetId = attackSetId;
            CooldownSeconds = cooldownSeconds;
            WindupSeconds = windupSeconds;
            ActiveSeconds = activeSeconds;
            RecoverySeconds = Math.Max(
                0d,
                cooldownSeconds - windupSeconds - activeSeconds);
            InterruptGestureType = interruptGestureType;
            InterruptStartSeconds = interruptStartSeconds;
            InterruptEndSeconds = interruptEndSeconds;
            EffectGroupId = effectGroupId;
            IsConfigured = true;
        }

        public string AttackId { get; }

        public string AttackSetId { get; }

        public double CooldownSeconds { get; }

        public double WindupSeconds { get; }

        public double ActiveSeconds { get; }

        public double RecoverySeconds { get; }

        public string InterruptGestureType { get; }

        public double InterruptStartSeconds { get; }

        public double InterruptEndSeconds { get; }

        public string EffectGroupId { get; }

        public bool IsConfigured { get; }

        public bool GestureMatches(string gestureType)
        {
            return string.Equals(InterruptGestureType, "Any", StringComparison.Ordinal) ||
                   string.Equals(InterruptGestureType, gestureType, StringComparison.Ordinal);
        }

        public bool IsInsideInterruptWindow(double elapsedSeconds)
        {
            return IsFinite(elapsedSeconds) &&
                   elapsedSeconds >= InterruptStartSeconds &&
                   elapsedSeconds <= InterruptEndSeconds;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    public static class EnemyAttackTimelineFactory
    {
        public static EnemyAttackTimeline Create(
            IConfigProvider configProvider,
            string attackSetId,
            string attackId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (string.IsNullOrWhiteSpace(attackSetId))
            {
                throw new ArgumentException(
                    "Attack set id must be non-empty.",
                    nameof(attackSetId));
            }

            if (string.IsNullOrWhiteSpace(attackId))
            {
                throw new ArgumentException("Attack id must be non-empty.", nameof(attackId));
            }

            IReadOnlyList<EnemyAttackConfig> attacks =
                configProvider.GetEnemyAttacks(attackSetId);
            for (int index = 0; index < attacks.Count; index++)
            {
                EnemyAttackConfig attack = attacks[index];
                if (string.Equals(attack.AttackId, attackId, StringComparison.Ordinal))
                {
                    return Create(attack);
                }
            }

            throw new KeyNotFoundException(
                $"Enemy attack '{attackId}' does not belong to attack set '{attackSetId}'.");
        }

        public static EnemyAttackTimeline Create(EnemyAttackConfig attack)
        {
            if (attack == null)
            {
                throw new ArgumentNullException(nameof(attack));
            }

            RequireDuration(attack.AttackId, nameof(attack.CooldownSec), attack.CooldownSec);
            RequireDuration(attack.AttackId, nameof(attack.WindupSec), attack.WindupSec);
            RequireDuration(attack.AttackId, nameof(attack.ActiveSec), attack.ActiveSec);
            RequireDuration(
                attack.AttackId,
                nameof(attack.InterruptStartSec),
                attack.InterruptStartSec);
            RequireDuration(
                attack.AttackId,
                nameof(attack.InterruptEndSec),
                attack.InterruptEndSec);
            if (attack.InterruptEndSec < attack.InterruptStartSec)
            {
                throw Invalid(
                    attack.AttackId,
                    nameof(attack.InterruptEndSec),
                    attack.InterruptEndSec,
                    "Interrupt window end must be at or after its start.");
            }

            double activeEnd = attack.WindupSec + attack.ActiveSec;
            if (attack.CooldownSec < activeEnd)
            {
                throw Invalid(
                    attack.AttackId,
                    nameof(attack.CooldownSec),
                    attack.CooldownSec,
                    "Cooldown must cover the complete windup and active interval.");
            }

            if (attack.InterruptEndSec > activeEnd)
            {
                throw Invalid(
                    attack.AttackId,
                    nameof(attack.InterruptEndSec),
                    attack.InterruptEndSec,
                    "Interrupt window cannot extend beyond windup plus active time.");
            }

            ValidateGestureType(attack.AttackId, attack.GestureInterruptType);
            return new EnemyAttackTimeline(
                attack.AttackId,
                attack.AttackSetId,
                attack.CooldownSec,
                attack.WindupSec,
                attack.ActiveSec,
                attack.GestureInterruptType,
                attack.InterruptStartSec,
                attack.InterruptEndSec,
                attack.EffectGroupId ?? string.Empty);
        }

        private static void ValidateGestureType(string attackId, string gestureType)
        {
            switch (gestureType)
            {
                case "Any":
                case "Horizontal":
                case "Vertical":
                case "Diagonal":
                case "Arc":
                case "Circle":
                case "Charged":
                    return;
                default:
                    throw new ArgumentException(
                        $"Enemy attack '{attackId}' has unsupported interrupt gesture '{gestureType}'.",
                        nameof(gestureType));
            }
        }

        private static void RequireDuration(string attackId, string field, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw Invalid(
                    attackId,
                    field,
                    value,
                    "Attack timing must be finite and non-negative.");
            }
        }

        private static ArgumentOutOfRangeException Invalid(
            string attackId,
            string field,
            object value,
            string message)
        {
            return new ArgumentOutOfRangeException(
                field,
                value,
                $"Enemy attack timeline '{attackId}.{field}' is invalid. {message}");
        }
    }
}
