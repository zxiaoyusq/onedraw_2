using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    // 定义 EnemyTier 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum EnemyTier
    {
        None = 0,
        Normal = 1,
        Elite = 2,
        Boss = 3
    }

    // 定义 EnemyDefenseDefinition 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyDefenseDefinition
    {
        // 初始化 EnemyDefenseDefinition，并建立角色运行时所需的初始状态。
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

    // 定义 EnemyWeakpointDefinition 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyWeakpointDefinition
    {
        // 初始化 EnemyWeakpointDefinition，并建立角色运行时所需的初始状态。
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

        // 判断是否 IsOpenAt 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool IsOpenAt(double elapsedSeconds)
        {
            return HasHitbox &&
                   IsFinite(elapsedSeconds) &&
                   elapsedSeconds >= WindowStartSeconds &&
                   elapsedSeconds <= WindowEndSeconds;
        }

        // 判断是否 IsFinite 对应的角色逻辑，并返回或发布一致的状态结果。
        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    // 定义 EnemyDefinition 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyDefinition
    {
        // 初始化 EnemyDefinition，并建立角色运行时所需的初始状态。
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

    // 定义 EnemyDefinitionFactory 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public static class EnemyDefinitionFactory
    {
        // 创建 Create 对应的角色逻辑，并返回或发布一致的状态结果。
        public static EnemyDefinition Create(IConfigProvider configProvider, string enemyId)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            EnemyConfig enemy = configProvider.GetEnemy(enemyId);
            return Create(
                configProvider,
                enemy,
                enemy.MovePatternId,
                enemy.AttackSetId,
                enemy.DefenseRuleId,
                enemy.WeakpointRuleId);
        }

        // 创建 CreateBossPhase 对应的角色逻辑，并返回或发布一致的状态结果。
        public static EnemyDefinition CreateBossPhase(
            IConfigProvider configProvider,
            string enemyId,
            string movePatternId,
            string attackSetId,
            string defenseRuleId,
            string weakpointRuleId)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            EnemyConfig enemy = configProvider.GetEnemy(enemyId);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (ParseTier(enemy.EnemyId, enemy.Tier) != EnemyTier.Boss)
            {
                throw new ArgumentException(
                    $"Enemy '{enemy.EnemyId}' must be tier Boss before applying a phase profile.",
                    nameof(enemyId));
            }

            return Create(
                configProvider,
                enemy,
                movePatternId,
                attackSetId,
                defenseRuleId,
                weakpointRuleId);
        }

        // 创建 Create 对应的角色逻辑，并返回或发布一致的状态结果。
        private static EnemyDefinition Create(
            IConfigProvider configProvider,
            EnemyConfig enemy,
            string movePatternId,
            string attackSetId,
            string defenseRuleId,
            string weakpointRuleId)
        {
            RequireNonEmpty(enemy.EnemyId, nameof(movePatternId), movePatternId);
            RequireNonEmpty(enemy.EnemyId, nameof(attackSetId), attackSetId);
            RequireNonEmpty(enemy.EnemyId, nameof(defenseRuleId), defenseRuleId);
            RequireNonEmpty(enemy.EnemyId, nameof(weakpointRuleId), weakpointRuleId);
            configProvider.GetMovePattern(movePatternId);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider.GetEnemyAttacks(attackSetId).Count == 0)
            {
                throw Invalid(
                    enemy.EnemyId,
                    nameof(attackSetId),
                    attackSetId,
                    "Configured attack set must contain at least one attack.");
            }

            DefenseRuleConfig defense = configProvider.GetDefenseRule(defenseRuleId);
            WeakpointRuleConfig weakpoint = configProvider.GetWeakpointRule(weakpointRuleId);

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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
                movePatternId,
                enemy.MoveSpeedRefPxSec,
                attackSetId,
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

        // 处理 ParseTier 对应的角色逻辑，并返回或发布一致的状态结果。
        private static EnemyTier ParseTier(string enemyId, string configuredTier)
        {
            // 按当前枚举或状态选择对应的角色行为分支。
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

        // 处理 RequireNonEmpty 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireNonEmpty(string rowId, string field, string value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(value))
            {
                throw Invalid(rowId, field, value, "Configured string must be non-empty.");
            }
        }

        // 处理 RequirePositive 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequirePositive(string rowId, string field, long value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (value <= 0)
            {
                throw Invalid(rowId, field, value, "Configured value must be positive.");
            }
        }

        // 处理 RequireNonNegative 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireNonNegative(string rowId, string field, long value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (value < 0)
            {
                throw Invalid(rowId, field, value, "Configured value must be non-negative.");
            }
        }

        // 处理 RequireInt32NonNegative 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireInt32NonNegative(string rowId, string field, long value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (value < 0 || value > int.MaxValue)
            {
                throw Invalid(
                    rowId,
                    field,
                    value,
                    "Configured value must fit a non-negative Int32.");
            }
        }

        // 处理 RequireFiniteNonNegative 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireFiniteNonNegative(
            string rowId,
            string field,
            double value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw Invalid(
                    rowId,
                    field,
                    value,
                    "Configured value must be finite and non-negative.");
            }
        }

        // 处理 Invalid 对应的角色逻辑，并返回或发布一致的状态结果。
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

    // 定义 EnemyAttackTimeline 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyAttackTimeline
    {
        // 初始化 EnemyAttackTimeline，并建立角色运行时所需的初始状态。
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

        // 处理 GestureMatches 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool GestureMatches(string gestureType)
        {
            return string.Equals(InterruptGestureType, "Any", StringComparison.Ordinal) ||
                   string.Equals(InterruptGestureType, gestureType, StringComparison.Ordinal);
        }

        // 判断是否 IsInsideInterruptWindow 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool IsInsideInterruptWindow(double elapsedSeconds)
        {
            return IsFinite(elapsedSeconds) &&
                   elapsedSeconds >= InterruptStartSeconds &&
                   elapsedSeconds <= InterruptEndSeconds;
        }

        // 判断是否 IsFinite 对应的角色逻辑，并返回或发布一致的状态结果。
        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    // 定义 EnemyAttackTimelineFactory 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public static class EnemyAttackTimelineFactory
    {
        // 创建 Create 对应的角色逻辑，并返回或发布一致的状态结果。
        public static EnemyAttackTimeline Create(
            IConfigProvider configProvider,
            string attackSetId,
            string attackId)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(attackSetId))
            {
                throw new ArgumentException(
                    "Attack set id must be non-empty.",
                    nameof(attackSetId));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(attackId))
            {
                throw new ArgumentException("Attack id must be non-empty.", nameof(attackId));
            }

            IReadOnlyList<EnemyAttackConfig> attacks =
                configProvider.GetEnemyAttacks(attackSetId);
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < attacks.Count; index++)
            {
                EnemyAttackConfig attack = attacks[index];
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (string.Equals(attack.AttackId, attackId, StringComparison.Ordinal))
                {
                    return Create(attack);
                }
            }

            throw new KeyNotFoundException(
                $"Enemy attack '{attackId}' does not belong to attack set '{attackSetId}'.");
        }

        // 创建 Create 对应的角色逻辑，并返回或发布一致的状态结果。
        public static EnemyAttackTimeline Create(EnemyAttackConfig attack)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (attack.InterruptEndSec < attack.InterruptStartSec)
            {
                throw Invalid(
                    attack.AttackId,
                    nameof(attack.InterruptEndSec),
                    attack.InterruptEndSec,
                    "Interrupt window end must be at or after its start.");
            }

            double activeEnd = attack.WindupSec + attack.ActiveSec;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (attack.CooldownSec < activeEnd)
            {
                throw Invalid(
                    attack.AttackId,
                    nameof(attack.CooldownSec),
                    attack.CooldownSec,
                    "Cooldown must cover the complete windup and active interval.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 校验 ValidateGestureType 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateGestureType(string attackId, string gestureType)
        {
            // 按当前枚举或状态选择对应的角色行为分支。
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

        // 处理 RequireDuration 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireDuration(string attackId, string field, double value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw Invalid(
                    attackId,
                    field,
                    value,
                    "Attack timing must be finite and non-negative.");
            }
        }

        // 处理 Invalid 对应的角色逻辑，并返回或发布一致的状态结果。
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
