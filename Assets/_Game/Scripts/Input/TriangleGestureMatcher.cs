using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>以无分配几何规则判断处理后笔迹是否接近一个三角形。</summary>
    public static class TriangleGestureMatcher
    {
        private const float MinimumSegmentSquared = 0.000001f;

        /// <summary>
        /// 从笔迹中选择最远两点作为一条候选边，再选择离该边最远的第三点；随后验证闭合、面积、
        /// 三个内角以及全部处理点到三条候选边的偏差。该入口只识别形状，不拥有任何战斗效果。
        /// </summary>
        public static bool TryMatch(
            IReadOnlyList<Vector2> points,
            float maximumClosureDistanceReferencePixels,
            float minimumAreaReferencePixelsSquared,
            float maximumEdgeDeviationReferencePixels,
            float minimumCornerAngleDegrees,
            out float confidence)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            ValidatePositive(maximumClosureDistanceReferencePixels, nameof(maximumClosureDistanceReferencePixels));
            ValidatePositive(minimumAreaReferencePixelsSquared, nameof(minimumAreaReferencePixelsSquared));
            ValidatePositive(maximumEdgeDeviationReferencePixels, nameof(maximumEdgeDeviationReferencePixels));
            ValidatePositive(minimumCornerAngleDegrees, nameof(minimumCornerAngleDegrees));
            if (minimumCornerAngleDegrees > 90f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumCornerAngleDegrees),
                    "Minimum corner angle cannot exceed 90 degrees.");
            }

            // 闭合路径至少需要起点、三个角点中的另外两点和回到起点附近的终点。
            if (points.Count < 4)
            {
                confidence = 0f;
                return false;
            }

            float closureDistance = Vector2.Distance(points[0], points[points.Count - 1]);
            if (closureDistance > maximumClosureDistanceReferencePixels)
            {
                confidence = 0f;
                return false;
            }

            FindFarthestPair(points, out int firstIndex, out int secondIndex, out float baselineSquared);
            if (baselineSquared <= MinimumSegmentSquared)
            {
                confidence = 0f;
                return false;
            }

            Vector2 first = points[firstIndex];
            Vector2 second = points[secondIndex];
            int thirdIndex = FindFarthestFromLine(points, first, second, out float thirdDistance);
            if (thirdIndex < 0)
            {
                confidence = 0f;
                return false;
            }

            Vector2 third = points[thirdIndex];
            float area = 0.5f * Mathf.Sqrt(baselineSquared) * thirdDistance;
            if (area < minimumAreaReferencePixelsSquared)
            {
                confidence = 0f;
                return false;
            }

            float minimumActualAngle = Math.Min(
                InteriorAngle(first, second, third),
                Math.Min(
                    InteriorAngle(second, first, third),
                    InteriorAngle(third, first, second)));
            if (minimumActualAngle < minimumCornerAngleDegrees)
            {
                confidence = 0f;
                return false;
            }

            float maximumDeviation = 0f;
            for (int index = 0; index < points.Count; index++)
            {
                float distance = Mathf.Sqrt(Math.Min(
                    DistanceToSegmentSquared(points[index], first, second),
                    Math.Min(
                        DistanceToSegmentSquared(points[index], second, third),
                        DistanceToSegmentSquared(points[index], third, first))));
                maximumDeviation = Math.Max(maximumDeviation, distance);
                if (maximumDeviation > maximumEdgeDeviationReferencePixels)
                {
                    confidence = 0f;
                    return false;
                }
            }

            // 每项在刚达到阈值时给0.5分，明显优于阈值时逐步接近1；最弱项决定结果。
            confidence = Math.Min(
                ScoreUpperBound(closureDistance, maximumClosureDistanceReferencePixels),
                Math.Min(
                    ScoreLowerBound(area, minimumAreaReferencePixelsSquared),
                    Math.Min(
                        ScoreUpperBound(maximumDeviation, maximumEdgeDeviationReferencePixels),
                        ScoreLowerBound(minimumActualAngle, minimumCornerAngleDegrees))));
            return true;
        }

        /// <summary>寻找欧氏距离最大的两个处理点，稳定保留最先出现的同距组合。</summary>
        private static void FindFarthestPair(
            IReadOnlyList<Vector2> points,
            out int firstIndex,
            out int secondIndex,
            out float distanceSquared)
        {
            firstIndex = 0;
            secondIndex = 0;
            distanceSquared = 0f;
            for (int first = 0; first < points.Count - 1; first++)
            {
                for (int second = first + 1; second < points.Count; second++)
                {
                    float candidate = (points[second] - points[first]).sqrMagnitude;
                    if (candidate > distanceSquared)
                    {
                        firstIndex = first;
                        secondIndex = second;
                        distanceSquared = candidate;
                    }
                }
            }
        }

        /// <summary>返回离无限基线最远的点索引及其垂距，作为第三个角点候选。</summary>
        private static int FindFarthestFromLine(
            IReadOnlyList<Vector2> points,
            Vector2 start,
            Vector2 end,
            out float distance)
        {
            Vector2 baseline = end - start;
            float baselineLength = baseline.magnitude;
            int farthestIndex = -1;
            distance = 0f;
            for (int index = 0; index < points.Count; index++)
            {
                Vector2 offset = points[index] - start;
                float candidate = Math.Abs(baseline.x * offset.y - baseline.y * offset.x) /
                                  baselineLength;
                if (candidate > distance)
                {
                    distance = candidate;
                    farthestIndex = index;
                }
            }

            return farthestIndex;
        }

        /// <summary>计算顶点处由另外两点构成的内角，结果为0到180度。</summary>
        private static float InteriorAngle(Vector2 vertex, Vector2 first, Vector2 second)
        {
            Vector2 firstDirection = first - vertex;
            Vector2 secondDirection = second - vertex;
            float denominator = Mathf.Sqrt(firstDirection.sqrMagnitude * secondDirection.sqrMagnitude);
            if (denominator <= MinimumSegmentSquared)
            {
                return 0f;
            }

            double cosine = Math.Max(-1d, Math.Min(1d, Vector2.Dot(firstDirection, secondDirection) / denominator));
            return (float)(Math.Acos(cosine) * 180d / Math.PI);
        }

        /// <summary>计算点到线段的平方距离，避免匹配循环产生临时分配。</summary>
        private static float DistanceToSegmentSquared(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float segmentSquared = segment.sqrMagnitude;
            if (segmentSquared <= MinimumSegmentSquared)
            {
                return (point - start).sqrMagnitude;
            }

            float projection = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentSquared);
            Vector2 closest = start + segment * projection;
            return (point - closest).sqrMagnitude;
        }

        /// <summary>把达到下界的程度转换为0到1置信度。</summary>
        private static float ScoreLowerBound(float value, float minimum)
        {
            return Mathf.Clamp01(0.5f + 0.5f * (value - minimum) / minimum);
        }

        /// <summary>把低于上界的裕量转换为0到1置信度。</summary>
        private static float ScoreUpperBound(float value, float maximum)
        {
            return Mathf.Clamp01(1f - 0.5f * value / maximum);
        }

        /// <summary>验证形状阈值是有限正数。</summary>
        private static void ValidatePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Triangle shape thresholds must be finite and positive.");
            }
        }
    }
}
