using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OneStrokeDemon.Skills
{
    // 定义 SkillConditionEvaluator 的技能领域契约，明确条件、目标或效果执行边界。
    internal static class SkillConditionEvaluator
    {
        private static readonly Regex Pattern = new Regex(
            "^(?<name>[A-Za-z][A-Za-z0-9_]*)\\s*(?<operator>>=|<=|==|!=|>|<)\\s*" +
            "(?<value>-?(?:[0-9]+(?:\\.[0-9]+)?|\\.[0-9]+))$",
            RegexOptions.CultureInvariant);

        // 校验 Validate 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public static void Validate(string condition, string effectGroupId, long order)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (string.IsNullOrWhiteSpace(condition))
            {
                return;
            }

            Match match = Pattern.Match(condition.Trim());
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
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

        // 评估 Evaluate 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public static bool Evaluate(
            string condition,
            SkillEffectContext context,
            string effectGroupId,
            long order)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            Validate(condition, effectGroupId, order);
            Match match = Pattern.Match(condition.Trim());
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (!context.TryGetConditionValue(match.Groups["name"].Value, out double actual))
            {
                return false;
            }

            double expected = double.Parse(
                match.Groups["value"].Value,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture);
            // 按效果或目标类型选择对应的技能处理分支。
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
