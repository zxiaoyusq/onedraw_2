using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>提供确定性的笔迹简化、重采样和几何指标计算纯规则。</summary>
    public static class StrokeGeometry
    {
        /// <summary>按设置执行 RDP 与必要重采样，并从同一点集创建完整几何快照。</summary>
        public static StrokeGeometryData Process(
            StrokeData stroke,
            StrokeGeometrySettings settings)
        {
            if (stroke == null)
            {
                throw new ArgumentNullException(nameof(stroke));
            }

            // 先去重并简化；只有仍超过配置上限时才按弧长等距重采样。
            Vector2[] processedPoints = SimplifyRdp(
                stroke.Points,
                settings.RdpEpsilonReferencePixels);
            if (processedPoints.Length > settings.MaximumProcessedPointCount)
            {
                processedPoints = Resample(
                    processedPoints,
                    settings.MaximumProcessedPointCount);
            }

            // 视觉、识别和命中共享该处理后数组及由它计算的全部指标。
            float length = CalculateLengthCore(processedPoints);
            CalculateCurvatureCore(
                processedPoints,
                out float signedCurvature,
                out float totalCurvature);
            float closureDistance = CalculateClosureDistanceCore(processedPoints);
            return new StrokeGeometryData(
                stroke,
                processedPoints,
                length,
                CalculateBoundsCore(processedPoints),
                CalculateSignedAreaCore(processedPoints),
                closureDistance,
                length > 0f ? closureDistance / length : 0f,
                signedCurvature,
                totalCurvature);
        }

        /// <summary>使用点到线段距离的迭代 RDP 算法简化点集并精确保留首尾。</summary>
        public static Vector2[] SimplifyRdp(
            IReadOnlyList<Vector2> points,
            float epsilonReferencePixels)
        {
            ValidateTolerance(epsilonReferencePixels, nameof(epsilonReferencePixels));
            Vector2[] uniquePoints = CopyWithoutConsecutiveDuplicates(points);
            if (uniquePoints.Length <= 2)
            {
                return uniquePoints;
            }

            // 用显式范围栈代替递归，避免长笔迹造成调用栈增长。
            int lastIndex = uniquePoints.Length - 1;
            var keep = new bool[uniquePoints.Length];
            var rangeStack = new int[uniquePoints.Length * 2];
            int stackCount = 0;
            keep[0] = true;
            keep[lastIndex] = true;
            PushRange(rangeStack, ref stackCount, 0, lastIndex);
            float epsilonSquared = epsilonReferencePixels * epsilonReferencePixels;

            while (stackCount > 0)
            {
                PopRange(rangeStack, ref stackCount, out int startIndex, out int endIndex);
                int farthestIndex = -1;
                float farthestDistanceSquared = 0f;
                for (int index = startIndex + 1; index < endIndex; index++)
                {
                    float distanceSquared = DistanceToSegmentSquared(
                        uniquePoints[index],
                        uniquePoints[startIndex],
                        uniquePoints[endIndex]);
                    if (distanceSquared > farthestDistanceSquared)
                    {
                        farthestDistanceSquared = distanceSquared;
                        farthestIndex = index;
                    }
                }

                // 小于或等于 epsilon 的中间点都可删除；超过时保留最远点并拆分区间。
                if (farthestIndex < 0 || farthestDistanceSquared <= epsilonSquared)
                {
                    continue;
                }

                keep[farthestIndex] = true;
                PushRange(rangeStack, ref stackCount, startIndex, farthestIndex);
                PushRange(rangeStack, ref stackCount, farthestIndex, endIndex);
            }

            int keptCount = 0;
            for (int index = 0; index < keep.Length; index++)
            {
                if (keep[index])
                {
                    keptCount++;
                }
            }

            var result = new Vector2[keptCount];
            int resultIndex = 0;
            for (int index = 0; index < keep.Length; index++)
            {
                if (keep[index])
                {
                    result[resultIndex++] = uniquePoints[index];
                }
            }

            return result;
        }

        /// <summary>沿累计弧长等距重采样为目标点数，并精确保留首尾点。</summary>
        public static Vector2[] Resample(
            IReadOnlyList<Vector2> points,
            int targetPointCount)
        {
            if (targetPointCount < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetPointCount),
                    "Resampling requires at least two target points.");
            }

            Vector2[] uniquePoints = CopyWithoutConsecutiveDuplicates(points);
            if (uniquePoints.Length <= 1)
            {
                return uniquePoints;
            }

            // 累计长度允许用单调目标距离在线性时间内定位每个输出点所在段。
            var cumulativeLengths = new float[uniquePoints.Length];
            for (int index = 1; index < uniquePoints.Length; index++)
            {
                cumulativeLengths[index] = cumulativeLengths[index - 1] +
                                           Vector2.Distance(uniquePoints[index - 1], uniquePoints[index]);
            }

            float totalLength = cumulativeLengths[cumulativeLengths.Length - 1];
            if (totalLength <= 0f)
            {
                return new[] { uniquePoints[0] };
            }

            var result = new Vector2[targetPointCount];
            result[0] = uniquePoints[0];
            result[targetPointCount - 1] = uniquePoints[uniquePoints.Length - 1];
            int segmentIndex = 0;
            for (int resultIndex = 1; resultIndex < targetPointCount - 1; resultIndex++)
            {
                float targetDistance = totalLength * resultIndex / (targetPointCount - 1);
                while (segmentIndex < uniquePoints.Length - 2 &&
                       cumulativeLengths[segmentIndex + 1] < targetDistance)
                {
                    segmentIndex++;
                }

                float segmentStartDistance = cumulativeLengths[segmentIndex];
                float segmentLength = cumulativeLengths[segmentIndex + 1] - segmentStartDistance;
                float segmentRatio = segmentLength > 0f
                    ? (targetDistance - segmentStartDistance) / segmentLength
                    : 0f;
                result[resultIndex] = Vector2.LerpUnclamped(
                    uniquePoints[segmentIndex],
                    uniquePoints[segmentIndex + 1],
                    segmentRatio);
            }

            return result;
        }

        /// <summary>验证点集并计算相邻点欧氏距离总和。</summary>
        public static float CalculateLength(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            return CalculateLengthCore(points);
        }

        /// <summary>验证点集并计算轴对齐参考像素包围盒。</summary>
        public static Rect CalculateBounds(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            return CalculateBoundsCore(points);
        }

        /// <summary>验证点集并用隐式闭合鞋带公式计算带方向面积。</summary>
        public static float CalculateSignedArea(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            return CalculateSignedAreaCore(points);
        }

        /// <summary>计算不区分顺逆时针的绝对面积。</summary>
        public static float CalculateArea(IReadOnlyList<Vector2> points)
        {
            return Math.Abs(CalculateSignedArea(points));
        }

        /// <summary>验证点集并计算首尾点直线距离。</summary>
        public static float CalculateClosureDistance(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            return CalculateClosureDistanceCore(points);
        }

        /// <summary>计算首尾距离与路径长度之比；零长度返回零。</summary>
        public static float CalculateClosureRatio(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            float length = CalculateLengthCore(points);
            return length > 0f ? CalculateClosureDistanceCore(points) / length : 0f;
        }

        /// <summary>计算保留左右转向符号的累计转角弧度。</summary>
        public static float CalculateSignedCurvatureRadians(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            CalculateCurvatureCore(points, out float signedCurvature, out _);
            return signedCurvature;
        }

        /// <summary>计算不区分转向的累计绝对转角弧度。</summary>
        public static float CalculateTotalCurvatureRadians(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            CalculateCurvatureCore(points, out _, out float totalCurvature);
            return totalCurvature;
        }

        /// <summary>计算累计绝对转角除以 π 的归一化曲率。</summary>
        public static float CalculateNormalizedCurvature(IReadOnlyList<Vector2> points)
        {
            return CalculateTotalCurvatureRadians(points) / Mathf.PI;
        }

        /// <summary>验证点集并移除连续重复点，不合并路径中非相邻的同坐标点。</summary>
        private static Vector2[] CopyWithoutConsecutiveDuplicates(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            if (points.Count == 0)
            {
                return Array.Empty<Vector2>();
            }

            var result = new Vector2[points.Count];
            int count = 0;
            Vector2 previous = default;
            for (int index = 0; index < points.Count; index++)
            {
                Vector2 point = points[index];
                if (count == 0 || point.x != previous.x || point.y != previous.y)
                {
                    result[count++] = point;
                    previous = point;
                }
            }

            if (count == result.Length)
            {
                return result;
            }

            var trimmed = new Vector2[count];
            Array.Copy(result, trimmed, count);
            return trimmed;
        }

        /// <summary>在已验证点集上以双精度累计路径长度。</summary>
        private static float CalculateLengthCore(IReadOnlyList<Vector2> points)
        {
            double totalLength = 0d;
            for (int index = 1; index < points.Count; index++)
            {
                double deltaX = points[index].x - points[index - 1].x;
                double deltaY = points[index].y - points[index - 1].y;
                totalLength += Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            }

            return (float)totalLength;
        }

        /// <summary>在已验证点集上计算包围盒；空集返回零矩形。</summary>
        private static Rect CalculateBoundsCore(IReadOnlyList<Vector2> points)
        {
            if (points.Count == 0)
            {
                return Rect.zero;
            }

            float minimumX = points[0].x;
            float maximumX = minimumX;
            float minimumY = points[0].y;
            float maximumY = minimumY;
            for (int index = 1; index < points.Count; index++)
            {
                Vector2 point = points[index];
                minimumX = Math.Min(minimumX, point.x);
                maximumX = Math.Max(maximumX, point.x);
                minimumY = Math.Min(minimumY, point.y);
                maximumY = Math.Max(maximumY, point.y);
            }

            return Rect.MinMaxRect(minimumX, minimumY, maximumX, maximumY);
        }

        /// <summary>在已验证点集上计算隐式闭合带方向面积。</summary>
        private static float CalculateSignedAreaCore(IReadOnlyList<Vector2> points)
        {
            if (points.Count < 3)
            {
                return 0f;
            }

            double twiceArea = 0d;
            for (int index = 0; index < points.Count; index++)
            {
                Vector2 current = points[index];
                Vector2 next = points[(index + 1) % points.Count];
                twiceArea += (double)current.x * next.y - (double)next.x * current.y;
            }

            return (float)(twiceArea * 0.5d);
        }

        /// <summary>在已验证点集上计算首尾距离。</summary>
        private static float CalculateClosureDistanceCore(IReadOnlyList<Vector2> points)
        {
            return points.Count > 1 ? Vector2.Distance(points[0], points[points.Count - 1]) : 0f;
        }

        /// <summary>跳过零长度段，累计相邻有效段之间的有向和绝对转角。</summary>
        private static void CalculateCurvatureCore(
            IReadOnlyList<Vector2> points,
            out float signedCurvature,
            out float totalCurvature)
        {
            double signedTotal = 0d;
            double absoluteTotal = 0d;
            bool hasPreviousSegment = false;
            Vector2 previousSegment = default;
            for (int index = 1; index < points.Count; index++)
            {
                Vector2 segment = points[index] - points[index - 1];
                if (segment.x == 0f && segment.y == 0f)
                {
                    continue;
                }

                // Atan2(叉积, 点积)同时得到稳定转向符号和 [-π, π] 最短夹角。
                if (hasPreviousSegment)
                {
                    double cross = (double)previousSegment.x * segment.y -
                                   (double)previousSegment.y * segment.x;
                    double dot = (double)previousSegment.x * segment.x +
                                 (double)previousSegment.y * segment.y;
                    double angle = Math.Atan2(cross, dot);
                    signedTotal += angle;
                    absoluteTotal += Math.Abs(angle);
                }

                previousSegment = segment;
                hasPreviousSegment = true;
            }

            signedCurvature = (float)signedTotal;
            totalCurvature = (float)absoluteTotal;
        }

        /// <summary>计算点到有限线段最近点的平方距离。</summary>
        private static float DistanceToSegmentSquared(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared <= 0f)
            {
                return (point - start).sqrMagnitude;
            }

            float projection = Vector2.Dot(point - start, segment) / segmentLengthSquared;
            projection = Mathf.Clamp01(projection);
            Vector2 closest = start + segment * projection;
            return (point - closest).sqrMagnitude;
        }

        /// <summary>把至少包含一个中间点的索引范围压入 RDP 显式栈。</summary>
        private static void PushRange(int[] stack, ref int stackCount, int startIndex, int endIndex)
        {
            if (endIndex - startIndex <= 1)
            {
                return;
            }

            stack[stackCount++] = startIndex;
            stack[stackCount++] = endIndex;
        }

        /// <summary>以后进先出顺序弹出一个 RDP 索引范围。</summary>
        private static void PopRange(
            int[] stack,
            ref int stackCount,
            out int startIndex,
            out int endIndex)
        {
            endIndex = stack[--stackCount];
            startIndex = stack[--stackCount];
        }

        /// <summary>验证几何容差有限且非负。</summary>
        private static void ValidateTolerance(float tolerance, string parameterName)
        {
            if (float.IsNaN(tolerance) || float.IsInfinity(tolerance) || tolerance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Geometry tolerances must be finite and non-negative.");
            }
        }

        /// <summary>验证点集存在且每个坐标分量均为有限值。</summary>
        private static void ValidatePoints(IReadOnlyList<Vector2> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            for (int index = 0; index < points.Count; index++)
            {
                Vector2 point = points[index];
                if (float.IsNaN(point.x) || float.IsInfinity(point.x) ||
                    float.IsNaN(point.y) || float.IsInfinity(point.y))
                {
                    throw new ArgumentException(
                        $"Stroke point at index {index} must be finite.",
                        nameof(points));
                }
            }
        }
    }
}
