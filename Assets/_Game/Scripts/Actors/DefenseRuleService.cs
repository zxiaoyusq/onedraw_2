using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    // 定义 EnemyDefenseRule 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyDefenseRule
    {
        // 初始化 EnemyDefenseRule，并建立角色运行时所需的初始状态。
        internal EnemyDefenseRule(
            string defenseRuleId,
            long armorHp,
            string requiredGestureType,
            string requiredStanceId,
            double matchingDamageMultiplier,
            double mismatchingDamageMultiplier,
            long reflectedDamage,
            string breakEffectGroupId)
        {
            DefenseRuleId = defenseRuleId;
            ArmorHp = armorHp;
            RequiredGestureType = requiredGestureType;
            RequiredStanceId = requiredStanceId ?? string.Empty;
            MatchingDamageMultiplier = matchingDamageMultiplier;
            MismatchingDamageMultiplier = mismatchingDamageMultiplier;
            ReflectedDamage = reflectedDamage;
            BreakEffectGroupId = breakEffectGroupId ?? string.Empty;
            IsConfigured = true;
        }

        public string DefenseRuleId { get; }

        public long ArmorHp { get; }

        public string RequiredGestureType { get; }

        public string RequiredStanceId { get; }

        public double MatchingDamageMultiplier { get; }

        public double MismatchingDamageMultiplier { get; }

        public long ReflectedDamage { get; }

        public string BreakEffectGroupId { get; }

        public bool IsConfigured { get; }
    }

    // 定义 EnemyDefenseEvaluation 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyDefenseEvaluation
    {
        // 初始化 EnemyDefenseEvaluation，并建立角色运行时所需的初始状态。
        internal EnemyDefenseEvaluation(
            string defenseRuleId,
            bool gestureMatches,
            bool stanceMatches,
            double configuredDamageMultiplier,
            long reflectedDamage,
            string breakEffectGroupId)
        {
            DefenseRuleId = defenseRuleId;
            GestureMatches = gestureMatches;
            StanceMatches = stanceMatches;
            ConfiguredDamageMultiplier = configuredDamageMultiplier;
            ReflectedDamage = reflectedDamage;
            BreakEffectGroupId = breakEffectGroupId ?? string.Empty;
            IsValid = true;
        }

        public string DefenseRuleId { get; }

        public bool GestureMatches { get; }

        public bool StanceMatches { get; }

        public bool Matches => GestureMatches && StanceMatches;

        public double ConfiguredDamageMultiplier { get; }

        public long ReflectedDamage { get; }

        public string BreakEffectGroupId { get; }

        public bool IsValid { get; }
    }

    // 定义 DefenseRuleService 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class DefenseRuleService
    {
        private readonly IConfigProvider configProvider;

        // 初始化 DefenseRuleService，并建立角色运行时所需的初始状态。
        public DefenseRuleService(IConfigProvider configuredProvider)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
        }

        // 获取 Get 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDefenseRule Get(string defenseRuleId)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(defenseRuleId))
            {
                throw new ArgumentException(
                    "Defense rule id must be non-empty.",
                    nameof(defenseRuleId));
            }

            DefenseRuleConfig row = configProvider.GetDefenseRule(defenseRuleId);
            Validate(row);
            return new EnemyDefenseRule(
                row.DefenseRuleId,
                row.ArmorHp,
                row.RequiredGestureType,
                row.RequiredStanceId,
                row.BreakDamageMultiplier,
                row.WrongGestureDamageMultiplier,
                row.ReflectDamage,
                row.BreakEffectGroupId);
        }

        // 处理 Evaluate 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyDefenseEvaluation Evaluate(
            string defenseRuleId,
            string gestureType,
            string stanceId)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(gestureType))
            {
                throw new ArgumentException(
                    "Gesture type must be non-empty.",
                    nameof(gestureType));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException(
                    "Stance id must be non-empty.",
                    nameof(stanceId));
            }

            EnemyDefenseRule rule = Get(defenseRuleId);
            bool gestureMatches =
                string.Equals(rule.RequiredGestureType, "Any", StringComparison.Ordinal) ||
                string.Equals(rule.RequiredGestureType, gestureType, StringComparison.Ordinal);
            bool stanceMatches =
                string.IsNullOrEmpty(rule.RequiredStanceId) ||
                string.Equals(rule.RequiredStanceId, stanceId, StringComparison.Ordinal);
            bool matches = gestureMatches && stanceMatches;
            return new EnemyDefenseEvaluation(
                rule.DefenseRuleId,
                gestureMatches,
                stanceMatches,
                matches
                    ? rule.MatchingDamageMultiplier
                    : rule.MismatchingDamageMultiplier,
                matches ? 0L : rule.ReflectedDamage,
                rule.BreakEffectGroupId);
        }

        // 校验 Validate 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void Validate(DefenseRuleConfig row)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(row.DefenseRuleId) ||
                !IsSupportedGesture(row.RequiredGestureType) ||
                row.ArmorHp < 0L ||
                row.ReflectDamage < 0L ||
                !IsFiniteNonNegative(row.BreakDamageMultiplier) ||
                !IsFiniteNonNegative(row.WrongGestureDamageMultiplier))
            {
                throw new ArgumentException(
                    $"Defense rule '{row.DefenseRuleId}' contains invalid strategy values.",
                    nameof(row));
            }
        }

        // 判断是否 IsSupportedGesture 对应的角色逻辑，并返回或发布一致的状态结果。
        private static bool IsSupportedGesture(string gestureType)
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
                    return true;
                default:
                    return false;
            }
        }

        // 判断是否 IsFiniteNonNegative 对应的角色逻辑，并返回或发布一致的状态结果。
        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   value >= 0d;
        }
    }
}
