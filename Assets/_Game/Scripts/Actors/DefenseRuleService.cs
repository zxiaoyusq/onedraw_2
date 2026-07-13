using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    public readonly struct EnemyDefenseRule
    {
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

    public readonly struct EnemyDefenseEvaluation
    {
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

    public sealed class DefenseRuleService
    {
        private readonly IConfigProvider configProvider;

        public DefenseRuleService(IConfigProvider configuredProvider)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
        }

        public EnemyDefenseRule Get(string defenseRuleId)
        {
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

        public EnemyDefenseEvaluation Evaluate(
            string defenseRuleId,
            string gestureType,
            string stanceId)
        {
            if (string.IsNullOrWhiteSpace(gestureType))
            {
                throw new ArgumentException(
                    "Gesture type must be non-empty.",
                    nameof(gestureType));
            }

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

        private static void Validate(DefenseRuleConfig row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

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

        private static bool IsSupportedGesture(string gestureType)
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
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   value >= 0d;
        }
    }
}
