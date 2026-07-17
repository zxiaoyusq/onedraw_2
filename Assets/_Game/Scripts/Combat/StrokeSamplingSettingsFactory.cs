using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    /// <summary>把选中的 StrokeRules 行映射为 Input 采样设置。</summary>
    public static class StrokeSamplingSettingsFactory
    {
        /// <summary>读取最小点距、最大长度和最大点数，并验证运行时整数范围。</summary>
        public static StrokeSamplingSettings FromConfig(StrokeRuleConfig strokeRule)
        {
            if (strokeRule == null)
            {
                throw new ArgumentNullException(nameof(strokeRule));
            }

            if (strokeRule.MaxPointCount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(strokeRule),
                    $"Stroke rule '{strokeRule.RuleId}' maxPointCount exceeds the runtime limit.");
            }

            return new StrokeSamplingSettings(
                strokeRule.MinPointDistanceRefPx,
                strokeRule.MaxStrokeLengthRefPx,
                (int)strokeRule.MaxPointCount);
        }
    }
}
