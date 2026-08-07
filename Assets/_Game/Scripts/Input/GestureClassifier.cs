using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>根据不可变配置规则和笔迹几何指标确定笔势类型与置信度。</summary>
    public sealed class GestureClassifier
    {
        private readonly GestureRule[] rules;
        private readonly ReadOnlyCollection<GestureRule> readOnlyRules;

        /// <summary>复制、校验并按规则 ID 排序识别规则，冻结外部修改影响。</summary>
        public GestureClassifier(IReadOnlyList<GestureRule> sourceRules)
        {
            if (sourceRules == null)
            {
                throw new ArgumentNullException(nameof(sourceRules));
            }

            if (sourceRules.Count == 0)
            {
                throw new ArgumentException("At least one gesture rule is required.", nameof(sourceRules));
            }

            // 构造时显式拒绝重复 ID，避免分类时出现依赖输入顺序的覆盖行为。
            rules = new GestureRule[sourceRules.Count];
            for (int index = 0; index < sourceRules.Count; index++)
            {
                GestureRule rule = sourceRules[index] ?? throw new ArgumentException(
                    $"Gesture rule at index {index} is null.",
                    nameof(sourceRules));
                for (int previousIndex = 0; previousIndex < index; previousIndex++)
                {
                    if (string.Equals(
                        rules[previousIndex].RuleId,
                        rule.RuleId,
                        StringComparison.Ordinal))
                    {
                        throw new ArgumentException(
                            $"Duplicate gesture rule ID '{rule.RuleId}'.",
                            nameof(sourceRules));
                    }
                }

                rules[index] = rule;
            }

            // ID 排序为相同优先级和置信度提供跨平台确定性兜底顺序。
            Array.Sort(rules, CompareRuleIds);
            readOnlyRules = Array.AsReadOnly(rules);
        }

        /// <summary>获取分类器内部冻结的规则只读视图。</summary>
        public IReadOnlyList<GestureRule> Rules => readOnlyRules;

        /// <summary>计算几何指标，按类型优先级、置信度和规则 ID 选择唯一最佳匹配。</summary>
        public GestureMatchResult Classify(StrokeGeometryData geometry)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException(nameof(geometry));
            }

            GestureMetrics metrics = CalculateMetrics(geometry);
            GestureRule bestRule = null;
            float bestConfidence = 0f;
            int bestPriority = int.MinValue;
            for (int index = 0; index < rules.Length; index++)
            {
                GestureRule rule = rules[index];
                if (!TryMatch(rule, geometry, metrics, out float confidence))
                {
                    continue;
                }

                // 优先识别高价值复杂图形；同类再按置信度和 Ordinal ID 稳定裁决。
                int priority = GetPriority(rule.GestureType);
                if (bestRule == null ||
                    priority > bestPriority ||
                    priority == bestPriority && confidence > bestConfidence ||
                    priority == bestPriority && confidence == bestConfidence &&
                    string.CompareOrdinal(rule.RuleId, bestRule.RuleId) < 0)
                {
                    bestRule = rule;
                    bestConfidence = confidence;
                    bestPriority = priority;
                }
            }

            return CreateResult(geometry.StrokeId, bestRule, bestConfidence, metrics);
        }

        /// <summary>从处理后几何快照计算方向、速度、曲率、闭合、面积和停留指标。</summary>
        private static GestureMetrics CalculateMetrics(StrokeGeometryData geometry)
        {
            float angleDegrees = 0f;
            if (geometry.PointCount >= 2)
            {
                Vector2 displacement = geometry.Points[geometry.PointCount - 1] - geometry.Points[0];
                if (displacement.sqrMagnitude > 0f)
                {
                    // 方向只关心无向直线轴，因此归一化到 [0, 180) 度。
                    angleDegrees = (float)(Math.Atan2(displacement.y, displacement.x) * 180d / Math.PI);
                    angleDegrees %= 180f;
                    if (angleDegrees < 0f)
                    {
                        angleDegrees += 180f;
                    }
                }
            }

            double speed = geometry.Duration > 0d
                ? geometry.LengthReferencePixels / geometry.Duration
                : 0d;
            if (double.IsInfinity(speed))
            {
                speed = double.MaxValue;
            }

            return new GestureMetrics(
                geometry.LengthReferencePixels,
                speed,
                angleDegrees,
                geometry.NormalizedCurvature,
                geometry.ClosureRatio,
                geometry.ClosureDistanceReferencePixels,
                geometry.AreaReferencePixelsSquared,
                geometry.InitialHoldDuration);
        }

        /// <summary>检查单条规则的全部阈值，并返回最弱一项决定的置信度。</summary>
        private static bool TryMatch(
            GestureRule rule,
            StrokeGeometryData geometry,
            GestureMetrics metrics,
            out float confidence)
        {
            if (metrics.LengthReferencePixels < rule.MinimumLengthReferencePixels)
            {
                confidence = 0f;
                return false;
            }

            // 所有笔势先满足共同最低长度，再按类型附加方向或形状条件。
            confidence = ScoreLowerBound(
                metrics.LengthReferencePixels,
                rule.MinimumLengthReferencePixels);
            switch (rule.GestureType)
            {
                case GestureType.Any:
                    return true;
                case GestureType.Horizontal:
                    return MatchDirection(
                        HorizontalDeviation(metrics.DirectionAngleDegrees),
                        rule.DirectionToleranceDegrees,
                        ref confidence);
                case GestureType.Vertical:
                    return MatchDirection(
                        Math.Abs(metrics.DirectionAngleDegrees - 90f),
                        rule.DirectionToleranceDegrees,
                        ref confidence);
                case GestureType.Diagonal:
                    return MatchDirection(
                        DiagonalDeviation(metrics.DirectionAngleDegrees),
                        rule.DirectionToleranceDegrees,
                        ref confidence);
                case GestureType.Arc:
                    return MatchLowerBound(
                        metrics.NormalizedCurvature,
                        rule.MinimumNormalizedCurvature,
                        ref confidence);
                case GestureType.Circle:
                    return MatchUpperBound(
                               metrics.ClosureDistanceReferencePixels,
                               rule.CloseDistanceReferencePixels,
                               ref confidence) &&
                           MatchLowerBound(
                               metrics.AreaReferencePixelsSquared,
                               rule.MinimumAreaReferencePixelsSquared,
                               ref confidence) &&
                           MatchLowerBound(
                               metrics.NormalizedCurvature,
                               rule.MinimumNormalizedCurvature,
                               ref confidence);
                case GestureType.Triangle:
                    if (!TriangleGestureMatcher.TryMatch(
                            geometry.Points,
                            rule.CloseDistanceReferencePixels,
                            rule.MinimumAreaReferencePixelsSquared,
                            rule.ShapeFitToleranceReferencePixels,
                            rule.MinimumCornerAngleDegrees,
                            out float shapeConfidence))
                    {
                        confidence = 0f;
                        return false;
                    }

                    confidence = Math.Min(confidence, shapeConfidence);
                    return true;
                case GestureType.Charged:
                    return MatchLowerBound(
                        metrics.InitialHoldSeconds,
                        rule.ChargeHoldSeconds,
                        ref confidence);
                default:
                    confidence = 0f;
                    return false;
            }
        }

        /// <summary>以“偏差不超过容差”的上界规则匹配方向。</summary>
        private static bool MatchDirection(
            double deviationDegrees,
            double toleranceDegrees,
            ref float confidence)
        {
            return MatchUpperBound(deviationDegrees, toleranceDegrees, ref confidence);
        }

        /// <summary>匹配下界阈值，并把当前置信度收紧到该维度得分。</summary>
        private static bool MatchLowerBound(
            double value,
            double minimum,
            ref float confidence)
        {
            if (value < minimum)
            {
                confidence = 0f;
                return false;
            }

            confidence = Math.Min(confidence, ScoreLowerBound(value, minimum));
            return true;
        }

        /// <summary>匹配上界阈值，并把当前置信度收紧到该维度得分。</summary>
        private static bool MatchUpperBound(
            double value,
            double maximum,
            ref float confidence)
        {
            if (value > maximum)
            {
                confidence = 0f;
                return false;
            }

            confidence = Math.Min(confidence, ScoreUpperBound(value, maximum));
            return true;
        }

        /// <summary>把达到或超过下界的程度映射到零到一得分。</summary>
        private static float ScoreLowerBound(double value, double minimum)
        {
            if (minimum <= 0d)
            {
                return 1f;
            }

            return Clamp01(0.5d + 0.5d * (value - minimum) / minimum);
        }

        /// <summary>把接近零且不超过上界的程度映射到零到一得分。</summary>
        private static float ScoreUpperBound(double value, double maximum)
        {
            if (maximum <= 0d)
            {
                return value <= 0d ? 1f : 0f;
            }

            return Clamp01(1d - 0.5d * value / maximum);
        }

        /// <summary>把双精度值确定性夹紧到零到一并转换为 float。</summary>
        private static float Clamp01(double value)
        {
            if (value <= 0d)
            {
                return 0f;
            }

            if (value >= 1d)
            {
                return 1f;
            }

            return (float)value;
        }

        /// <summary>计算角度到水平轴零度或一百八十度的最小偏差。</summary>
        private static float HorizontalDeviation(float angleDegrees)
        {
            return Math.Min(angleDegrees, 180f - angleDegrees);
        }

        /// <summary>计算角度到两条对角轴四十五度或一百三十五度的最小偏差。</summary>
        private static float DiagonalDeviation(float angleDegrees)
        {
            return Math.Min(
                Math.Abs(angleDegrees - 45f),
                Math.Abs(angleDegrees - 135f));
        }

        /// <summary>返回复杂图形优先于方向和 Any 的固定识别优先级。</summary>
        private static int GetPriority(GestureType gestureType)
        {
            switch (gestureType)
            {
                case GestureType.Triangle:
                    return 6;
                case GestureType.Circle:
                    return 5;
                case GestureType.Charged:
                    return 4;
                case GestureType.Arc:
                    return 3;
                case GestureType.Horizontal:
                case GestureType.Vertical:
                case GestureType.Diagonal:
                    return 2;
                case GestureType.Any:
                    return 1;
                default:
                    return 0;
            }
        }

        /// <summary>把最佳规则和完整指标组装为匹配或未匹配结果。</summary>
        private static GestureMatchResult CreateResult(
            ulong strokeId,
            GestureRule rule,
            float confidence,
            GestureMetrics metrics)
        {
            return new GestureMatchResult(
                strokeId,
                rule?.RuleId,
                rule?.GestureType ?? GestureType.None,
                rule == null ? 0f : confidence,
                metrics.LengthReferencePixels,
                metrics.AverageSpeedReferencePixelsPerSecond,
                metrics.DirectionAngleDegrees,
                metrics.NormalizedCurvature,
                metrics.ClosureRatio,
                metrics.ClosureDistanceReferencePixels,
                metrics.AreaReferencePixelsSquared,
                metrics.InitialHoldSeconds);
        }

        /// <summary>按规则 ID 的序数顺序比较两条规则。</summary>
        private static int CompareRuleIds(GestureRule left, GestureRule right)
        {
            return string.CompareOrdinal(left.RuleId, right.RuleId);
        }

        /// <summary>分类过程内部使用的一组不可变几何指标。</summary>
        private readonly struct GestureMetrics
        {
            /// <summary>创建完整指标快照。</summary>
            public GestureMetrics(
                float lengthReferencePixels,
                double averageSpeedReferencePixelsPerSecond,
                float directionAngleDegrees,
                float normalizedCurvature,
                float closureRatio,
                float closureDistanceReferencePixels,
                float areaReferencePixelsSquared,
                double initialHoldSeconds)
            {
                LengthReferencePixels = lengthReferencePixels;
                AverageSpeedReferencePixelsPerSecond = averageSpeedReferencePixelsPerSecond;
                DirectionAngleDegrees = directionAngleDegrees;
                NormalizedCurvature = normalizedCurvature;
                ClosureRatio = closureRatio;
                ClosureDistanceReferencePixels = closureDistanceReferencePixels;
                AreaReferencePixelsSquared = areaReferencePixelsSquared;
                InitialHoldSeconds = initialHoldSeconds;
            }

            /// <summary>处理后路径长度。</summary>
            public float LengthReferencePixels { get; }
            /// <summary>平均绘制速度。</summary>
            public double AverageSpeedReferencePixelsPerSecond { get; }
            /// <summary>无向首尾位移角。</summary>
            public float DirectionAngleDegrees { get; }
            /// <summary>归一化绝对曲率。</summary>
            public float NormalizedCurvature { get; }
            /// <summary>首尾距离与路径长度之比。</summary>
            public float ClosureRatio { get; }
            /// <summary>首尾闭合距离。</summary>
            public float ClosureDistanceReferencePixels { get; }
            /// <summary>隐式闭合面积。</summary>
            public float AreaReferencePixelsSquared { get; }
            /// <summary>起笔停留秒数。</summary>
            public double InitialHoldSeconds { get; }
        }
    }
}
