using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    /// <summary>把选中的 StrokeRules 行映射为 Input 几何处理设置。</summary>
    public static class StrokeGeometrySettingsFactory
    {
        /// <summary>读取 RDP 容差和最大点数，并验证 long 可安全转换为运行时 int。</summary>
        public static StrokeGeometrySettings FromConfig(StrokeRuleConfig strokeRule)
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

            return new StrokeGeometrySettings(
                strokeRule.RdpEpsilonRefPx,
                (int)strokeRule.MaxPointCount);
        }
    }
}
