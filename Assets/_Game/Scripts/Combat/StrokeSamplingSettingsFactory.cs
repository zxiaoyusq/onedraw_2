using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    public static class StrokeSamplingSettingsFactory
    {
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
