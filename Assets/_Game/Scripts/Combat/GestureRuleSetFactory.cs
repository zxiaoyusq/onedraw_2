using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    public static class GestureRuleSetFactory
    {
        public static IReadOnlyList<GestureRule> FromConfig(IConfigProvider configProvider)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            IReadOnlyList<StrokeRuleConfig> rows = configProvider.GetStrokeRules();
            if (rows == null || rows.Count == 0)
            {
                throw new ArgumentException(
                    "StrokeRules must contain at least one gesture rule.",
                    nameof(configProvider));
            }

            var rules = new GestureRule[rows.Count];
            for (int index = 0; index < rows.Count; index++)
            {
                StrokeRuleConfig row = rows[index] ?? throw new ArgumentException(
                    $"StrokeRules row at index {index} is null.",
                    nameof(configProvider));
                rules[index] = new GestureRule(
                    row.RuleId,
                    ParseGestureType(row.RuleId, row.GestureType),
                    row.MinLengthRefPx,
                    row.DirectionToleranceDeg,
                    row.CloseDistanceRefPx,
                    row.MinAreaRefPx2,
                    row.MinArcCurvature,
                    row.ChargeHoldSec);
            }

            Array.Sort(rules, CompareRuleIds);
            return Array.AsReadOnly(rules);
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
                        $"Stroke rule '{ruleId}' has unsupported gestureType '{configuredType}'.",
                        nameof(configuredType));
            }
        }

        private static int CompareRuleIds(GestureRule left, GestureRule right)
        {
            return string.CompareOrdinal(left.RuleId, right.RuleId);
        }
    }
}
