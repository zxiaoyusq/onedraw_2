using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    public static class DamageRuleSetFactory
    {
        public static DamageRuleSet Create(
            IConfigProvider configProvider,
            string stanceId,
            string defenseRuleId,
            string weakpointRuleId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            StanceConfig stance = configProvider.GetStance(stanceId);
            DamageFormulaConfig formula = configProvider.GetDamageFormula(stance.DamageFormulaId);
            DefenseRuleConfig defense = configProvider.GetDefenseRule(defenseRuleId);
            WeakpointRuleConfig weakpoint = configProvider.GetWeakpointRule(weakpointRuleId);
            GestureType requiredGesture = ParseGestureType(defense.DefenseRuleId, defense.RequiredGestureType);

            RequireNonNegative(formula.FormulaId, nameof(formula.BaseDamage), formula.BaseDamage);
            RequireRange(formula.FormulaId, nameof(formula.CriticalChance), formula.CriticalChance, 0d, 1d);
            RequireNonNegative(formula.FormulaId, nameof(formula.CriticalMultiplier), formula.CriticalMultiplier);
            RequireNonNegative(formula.FormulaId, nameof(formula.WeakpointMultiplier), formula.WeakpointMultiplier);
            RequireNonNegative(formula.FormulaId, nameof(formula.WrongDirectionMultiplier), formula.WrongDirectionMultiplier);
            RequireNonNegative(formula.FormulaId, nameof(formula.ComboStep), formula.ComboStep);
            RequireRange(formula.FormulaId, nameof(formula.ComboMaxMultiplier), formula.ComboMaxMultiplier, 1d, double.MaxValue);
            RequireNonNegative(formula.FormulaId, nameof(formula.EnergyPerHit), formula.EnergyPerHit);
            RequireNonNegative(formula.FormulaId, nameof(formula.ScorePerHit), formula.ScorePerHit);
            RequireNonNegative(formula.FormulaId, nameof(formula.ScorePerDamage), formula.ScorePerDamage);
            RequireNonNegative(stance.StanceId, nameof(stance.DamageMultiplier), stance.DamageMultiplier);
            RequireNonNegative(defense.DefenseRuleId, nameof(defense.BreakDamageMultiplier), defense.BreakDamageMultiplier);
            RequireNonNegative(defense.DefenseRuleId, nameof(defense.WrongGestureDamageMultiplier), defense.WrongGestureDamageMultiplier);
            RequireNonNegative(defense.DefenseRuleId, nameof(defense.ReflectDamage), defense.ReflectDamage);
            RequireNonNegative(weakpoint.WeakpointRuleId, nameof(weakpoint.DamageMultiplier), weakpoint.DamageMultiplier);
            RequireNonNegative(weakpoint.WeakpointRuleId, nameof(weakpoint.EnergyBonus), weakpoint.EnergyBonus);
            RequireNonNegative(weakpoint.WeakpointRuleId, nameof(weakpoint.ScoreBonus), weakpoint.ScoreBonus);

            return new DamageRuleSet(
                formula.FormulaId,
                stance.StanceId,
                defense.DefenseRuleId,
                weakpoint.WeakpointRuleId,
                requiredGesture,
                defense.RequiredStanceId ?? string.Empty,
                formula.BaseDamage,
                stance.DamageMultiplier,
                formula.CriticalChance,
                formula.CriticalMultiplier,
                formula.WeakpointMultiplier,
                weakpoint.DamageMultiplier,
                formula.WrongDirectionMultiplier,
                defense.BreakDamageMultiplier,
                defense.WrongGestureDamageMultiplier,
                formula.ComboStep,
                formula.ComboMaxMultiplier,
                formula.EnergyPerHit,
                formula.ScorePerHit,
                formula.ScorePerDamage,
                weakpoint.EnergyBonus,
                weakpoint.ScoreBonus,
                weakpoint.InterruptAttack,
                defense.ReflectDamage);
        }

        public static DamageRuleSet CreateForEnemy(
            IConfigProvider configProvider,
            string stanceId,
            string enemyId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            EnemyConfig enemy = configProvider.GetEnemy(enemyId);
            return Create(
                configProvider,
                stanceId,
                enemy.DefenseRuleId,
                enemy.WeakpointRuleId);
        }

        private static GestureType ParseGestureType(string ruleId, string configuredType)
        {
            switch (configuredType)
            {
                case "Any":
                    return GestureType.Any;
                case "Horizontal":
                    return GestureType.Horizontal;
                case "Vertical":
                    return GestureType.Vertical;
                case "Diagonal":
                    return GestureType.Diagonal;
                case "Arc":
                    return GestureType.Arc;
                case "Circle":
                    return GestureType.Circle;
                case "Charged":
                    return GestureType.Charged;
                default:
                    throw new ArgumentException(
                        $"Defense rule '{ruleId}' has unsupported requiredGestureType '{configuredType}'.",
                        nameof(configuredType));
            }
        }

        private static void RequireNonNegative(string rowId, string field, long value)
        {
            if (value < 0)
            {
                throw Invalid(rowId, field, value);
            }
        }

        private static void RequireNonNegative(string rowId, string field, double value)
        {
            RequireRange(rowId, field, value, 0d, double.MaxValue);
        }

        private static void RequireRange(
            string rowId,
            string field,
            double value,
            double minimum,
            double maximum)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum)
            {
                throw Invalid(rowId, field, value);
            }
        }

        private static ArgumentOutOfRangeException Invalid(string rowId, string field, object value)
        {
            return new ArgumentOutOfRangeException(
                field,
                value,
                $"Configured value for '{rowId}.{field}' is outside the supported damage rule range.");
        }
    }
}
