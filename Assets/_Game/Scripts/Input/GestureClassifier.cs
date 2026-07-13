using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    public sealed class GestureClassifier
    {
        private readonly GestureRule[] rules;
        private readonly ReadOnlyCollection<GestureRule> readOnlyRules;

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

            Array.Sort(rules, CompareRuleIds);
            readOnlyRules = Array.AsReadOnly(rules);
        }

        public IReadOnlyList<GestureRule> Rules => readOnlyRules;

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
                if (!TryMatch(rule, metrics, out float confidence))
                {
                    continue;
                }

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

        private static GestureMetrics CalculateMetrics(StrokeGeometryData geometry)
        {
            float angleDegrees = 0f;
            if (geometry.PointCount >= 2)
            {
                Vector2 displacement = geometry.Points[geometry.PointCount - 1] - geometry.Points[0];
                if (displacement.sqrMagnitude > 0f)
                {
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

        private static bool TryMatch(
            GestureRule rule,
            GestureMetrics metrics,
            out float confidence)
        {
            if (metrics.LengthReferencePixels < rule.MinimumLengthReferencePixels)
            {
                confidence = 0f;
                return false;
            }

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

        private static bool MatchDirection(
            double deviationDegrees,
            double toleranceDegrees,
            ref float confidence)
        {
            return MatchUpperBound(deviationDegrees, toleranceDegrees, ref confidence);
        }

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

        private static float ScoreLowerBound(double value, double minimum)
        {
            if (minimum <= 0d)
            {
                return 1f;
            }

            return Clamp01(0.5d + 0.5d * (value - minimum) / minimum);
        }

        private static float ScoreUpperBound(double value, double maximum)
        {
            if (maximum <= 0d)
            {
                return value <= 0d ? 1f : 0f;
            }

            return Clamp01(1d - 0.5d * value / maximum);
        }

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

        private static float HorizontalDeviation(float angleDegrees)
        {
            return Math.Min(angleDegrees, 180f - angleDegrees);
        }

        private static float DiagonalDeviation(float angleDegrees)
        {
            return Math.Min(
                Math.Abs(angleDegrees - 45f),
                Math.Abs(angleDegrees - 135f));
        }

        private static int GetPriority(GestureType gestureType)
        {
            switch (gestureType)
            {
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

        private static int CompareRuleIds(GestureRule left, GestureRule right)
        {
            return string.CompareOrdinal(left.RuleId, right.RuleId);
        }

        private readonly struct GestureMetrics
        {
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

            public float LengthReferencePixels { get; }
            public double AverageSpeedReferencePixelsPerSecond { get; }
            public float DirectionAngleDegrees { get; }
            public float NormalizedCurvature { get; }
            public float ClosureRatio { get; }
            public float ClosureDistanceReferencePixels { get; }
            public float AreaReferencePixelsSquared { get; }
            public double InitialHoldSeconds { get; }
        }
    }
}
