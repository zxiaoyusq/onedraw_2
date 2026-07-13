using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    public static class StrokeGeometry
    {
        public static StrokeGeometryData Process(
            StrokeData stroke,
            StrokeGeometrySettings settings)
        {
            if (stroke == null)
            {
                throw new ArgumentNullException(nameof(stroke));
            }

            Vector2[] processedPoints = SimplifyRdp(
                stroke.Points,
                settings.RdpEpsilonReferencePixels);
            if (processedPoints.Length > settings.MaximumProcessedPointCount)
            {
                processedPoints = Resample(
                    processedPoints,
                    settings.MaximumProcessedPointCount);
            }

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

        public static float CalculateLength(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            return CalculateLengthCore(points);
        }

        public static Rect CalculateBounds(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            return CalculateBoundsCore(points);
        }

        public static float CalculateSignedArea(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            return CalculateSignedAreaCore(points);
        }

        public static float CalculateArea(IReadOnlyList<Vector2> points)
        {
            return Math.Abs(CalculateSignedArea(points));
        }

        public static float CalculateClosureDistance(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            return CalculateClosureDistanceCore(points);
        }

        public static float CalculateClosureRatio(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            float length = CalculateLengthCore(points);
            return length > 0f ? CalculateClosureDistanceCore(points) / length : 0f;
        }

        public static float CalculateSignedCurvatureRadians(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            CalculateCurvatureCore(points, out float signedCurvature, out _);
            return signedCurvature;
        }

        public static float CalculateTotalCurvatureRadians(IReadOnlyList<Vector2> points)
        {
            ValidatePoints(points);
            CalculateCurvatureCore(points, out _, out float totalCurvature);
            return totalCurvature;
        }

        public static float CalculateNormalizedCurvature(IReadOnlyList<Vector2> points)
        {
            return CalculateTotalCurvatureRadians(points) / Mathf.PI;
        }

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

        private static float CalculateClosureDistanceCore(IReadOnlyList<Vector2> points)
        {
            return points.Count > 1 ? Vector2.Distance(points[0], points[points.Count - 1]) : 0f;
        }

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

        private static void PushRange(int[] stack, ref int stackCount, int startIndex, int endIndex)
        {
            if (endIndex - startIndex <= 1)
            {
                return;
            }

            stack[stackCount++] = startIndex;
            stack[stackCount++] = endIndex;
        }

        private static void PopRange(
            int[] stack,
            ref int stackCount,
            out int startIndex,
            out int endIndex)
        {
            endIndex = stack[--stackCount];
            startIndex = stack[--stackCount];
        }

        private static void ValidateTolerance(float tolerance, string parameterName)
        {
            if (float.IsNaN(tolerance) || float.IsInfinity(tolerance) || tolerance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Geometry tolerances must be finite and non-negative.");
            }
        }

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
