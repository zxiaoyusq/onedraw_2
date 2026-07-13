using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OneStrokeDemon.Skills
{
    internal static class SkillConditionEvaluator
    {
        private static readonly Regex Pattern = new Regex(
            "^(?<name>[A-Za-z][A-Za-z0-9_]*)\\s*(?<operator>>=|<=|==|!=|>|<)\\s*" +
            "(?<value>-?(?:[0-9]+(?:\\.[0-9]+)?|\\.[0-9]+))$",
            RegexOptions.CultureInvariant);

        public static void Validate(string condition, string effectGroupId, long order)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return;
            }

            Match match = Pattern.Match(condition.Trim());
            if (!match.Success || !double.TryParse(
                    match.Groups["value"].Value,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out double expected) ||
                double.IsNaN(expected) ||
                double.IsInfinity(expected))
            {
                throw new SkillEffectConfigurationException(
                    $"Unsupported condition expression '{condition}'.",
                    effectGroupId,
                    order);
            }
        }

        public static bool Evaluate(
            string condition,
            SkillEffectContext context,
            string effectGroupId,
            long order)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            Validate(condition, effectGroupId, order);
            Match match = Pattern.Match(condition.Trim());
            if (!context.TryGetConditionValue(match.Groups["name"].Value, out double actual))
            {
                return false;
            }

            double expected = double.Parse(
                match.Groups["value"].Value,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture);
            switch (match.Groups["operator"].Value)
            {
                case ">=": return actual >= expected;
                case "<=": return actual <= expected;
                case "==": return actual == expected;
                case "!=": return actual != expected;
                case ">": return actual > expected;
                case "<": return actual < expected;
                default:
                    throw new SkillEffectConfigurationException(
                        $"Unsupported condition operator in '{condition}'.",
                        effectGroupId,
                        order);
            }
        }
    }
}
