using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    /// <summary>把配置 StrokeRules 表映射为 Input 程序集可消费的不可变识别规则。</summary>
    public static class GestureRuleSetFactory
    {
        /// <summary>读取全部笔势配置，显式转换类型并按规则 ID 稳定排序。</summary>
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
                rules[index] = CreateRule(row);
            }

            // 返回前复制并排序，使分类行为不依赖配置提供者的集合实现。
            Array.Sort(rules, CompareRuleIds);
            return Array.AsReadOnly(rules);
        }

        /// <summary>读取指定配置行并映射为单条不可变笔势规则。</summary>
        public static GestureRule FromConfig(
            IConfigProvider configProvider,
            string ruleId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException(
                    "Gesture rule ID must be non-empty.",
                    nameof(ruleId));
            }

            return CreateRule(configProvider.GetStrokeRule(ruleId));
        }

        /// <summary>把一行配置完整映射为Input层规则，确保全表与单规则入口语义一致。</summary>
        private static GestureRule CreateRule(StrokeRuleConfig row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            return new GestureRule(
                row.RuleId,
                ParseGestureType(row.RuleId, row.GestureType),
                row.MinLengthRefPx,
                row.DirectionToleranceDeg,
                row.CloseDistanceRefPx,
                row.MinAreaRefPx2,
                row.MinArcCurvature,
                row.ChargeHoldSec);
        }

        /// <summary>把配置字符串显式映射为支持的笔势类型，未知值立即失败。</summary>
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

        /// <summary>按规则 ID 的序数顺序比较两条规则。</summary>
        private static int CompareRuleIds(GestureRule left, GestureRule right)
        {
            return string.CompareOrdinal(left.RuleId, right.RuleId);
        }
    }
}
