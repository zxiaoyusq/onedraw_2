using System;
using OneStrokeDemon.Input;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    public sealed class StrokeHitResolver
    {
        private readonly StrokeHitResolverSettings settings;
        private readonly IStrokeHitQuery query;
        private readonly StrokeHitCandidate[] candidateBuffer;
        private readonly IHittable[] targetBuffer;
        private readonly int[] targetIdBuffer;
        private readonly bool[] weakpointBuffer;
        private readonly float[] distanceBuffer;

        public StrokeHitResolver(
            StrokeHitResolverSettings resolverSettings,
            IStrokeHitQuery hitQuery)
        {
            settings = resolverSettings;
            query = hitQuery ?? throw new ArgumentNullException(nameof(hitQuery));
            candidateBuffer = new StrokeHitCandidate[settings.QueryCapacity];
            targetBuffer = new IHittable[settings.MaximumUniqueTargets];
            targetIdBuffer = new int[settings.MaximumUniqueTargets];
            weakpointBuffer = new bool[settings.MaximumUniqueTargets];
            distanceBuffer = new float[settings.MaximumUniqueTargets];
        }

        public StrokeHitResolverSettings Settings => settings;

        public int Resolve(
            StrokeGeometryData geometry,
            GestureMatchResult gesture,
            StrokeHitRule hitRule,
            HitRecord[] results)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException(nameof(geometry));
            }

            if (gesture == null)
            {
                throw new ArgumentNullException(nameof(gesture));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (results.Length < settings.MaximumUniqueTargets)
            {
                throw new ArgumentException(
                    $"Result buffer must contain at least {settings.MaximumUniqueTargets} entries.",
                    nameof(results));
            }

            Array.Clear(results, 0, results.Length);
            if (gesture.StrokeId != geometry.StrokeId)
            {
                throw new ArgumentException(
                    "Geometry and gesture must describe the same stroke.",
                    nameof(gesture));
            }

            if (!gesture.IsMatch || geometry.PointCount < 2 || geometry.LengthReferencePixels <= 0f)
            {
                return 0;
            }

            if (!string.Equals(gesture.RuleId, hitRule.RuleId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Gesture rule '{gesture.RuleId}' cannot use hit rule '{hitRule.RuleId}'.",
                    nameof(hitRule));
            }

            int uniqueTargetCount = 0;
            float cumulativeDistance = 0f;
            try
            {
                for (int segmentIndex = 0; segmentIndex < geometry.PointCount - 1; segmentIndex++)
                {
                    Vector2 start = geometry.Points[segmentIndex];
                    Vector2 end = geometry.Points[segmentIndex + 1];
                    float segmentLength = Vector2.Distance(start, end);
                    if (segmentLength <= 0f)
                    {
                        continue;
                    }

                    int candidateCount = query.QuerySegment(
                        start,
                        end,
                        hitRule.RadiusReferencePixels,
                        candidateBuffer);
                    if (candidateCount < 0 || candidateCount >= candidateBuffer.Length)
                    {
                        throw new InvalidOperationException(
                            "Stroke hit query returned an invalid or saturated candidate count.");
                    }

                    for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
                    {
                        StrokeHitCandidate candidate = candidateBuffer[candidateIndex];
                        IHittable target = candidate.Target;
                        if (target == null || !target.CanReceiveStrokeHit)
                        {
                            continue;
                        }

                        int targetId = target.HitTargetId;
                        if (targetId == 0)
                        {
                            throw new InvalidOperationException(
                                "IHittable.HitTargetId must be non-zero while the target is active.");
                        }

                        float hitDistance = cumulativeDistance +
                            candidate.SegmentParameter * segmentLength;
                        int existingIndex = FindTarget(targetId, uniqueTargetCount);
                        if (existingIndex >= 0)
                        {
                            if (hitDistance < distanceBuffer[existingIndex])
                            {
                                distanceBuffer[existingIndex] = hitDistance;
                            }

                            weakpointBuffer[existingIndex] |= candidate.IsWeakpoint;
                            continue;
                        }

                        if (uniqueTargetCount >= settings.MaximumUniqueTargets)
                        {
                            throw new InvalidOperationException(
                                "Stroke hit count exceeds configured active target capacity.");
                        }

                        targetBuffer[uniqueTargetCount] = target;
                        targetIdBuffer[uniqueTargetCount] = targetId;
                        weakpointBuffer[uniqueTargetCount] = candidate.IsWeakpoint;
                        distanceBuffer[uniqueTargetCount] = hitDistance;
                        uniqueTargetCount++;
                    }

                    cumulativeDistance += segmentLength;
                }

                SortByPath(uniqueTargetCount);
                for (int index = 0; index < uniqueTargetCount; index++)
                {
                    float normalizedPath = Mathf.Clamp01(
                        distanceBuffer[index] / geometry.LengthReferencePixels);
                    results[index] = new HitRecord(
                        geometry.StrokeId,
                        targetBuffer[index],
                        targetIdBuffer[index],
                        weakpointBuffer[index],
                        normalizedPath,
                        distanceBuffer[index],
                        gesture,
                        geometry.EndedAt);
                }

                return uniqueTargetCount;
            }
            finally
            {
                Array.Clear(candidateBuffer, 0, candidateBuffer.Length);
                Array.Clear(targetBuffer, 0, targetBuffer.Length);
                Array.Clear(targetIdBuffer, 0, targetIdBuffer.Length);
                Array.Clear(weakpointBuffer, 0, weakpointBuffer.Length);
                Array.Clear(distanceBuffer, 0, distanceBuffer.Length);
            }
        }

        private int FindTarget(int targetId, int count)
        {
            for (int index = 0; index < count; index++)
            {
                if (targetIdBuffer[index] == targetId)
                {
                    return index;
                }
            }

            return -1;
        }

        private void SortByPath(int count)
        {
            for (int index = 1; index < count; index++)
            {
                IHittable target = targetBuffer[index];
                int targetId = targetIdBuffer[index];
                bool weakpoint = weakpointBuffer[index];
                float distance = distanceBuffer[index];
                int insertionIndex = index;
                while (insertionIndex > 0 && ComesBefore(
                           distance,
                           targetId,
                           distanceBuffer[insertionIndex - 1],
                           targetIdBuffer[insertionIndex - 1]))
                {
                    targetBuffer[insertionIndex] = targetBuffer[insertionIndex - 1];
                    targetIdBuffer[insertionIndex] = targetIdBuffer[insertionIndex - 1];
                    weakpointBuffer[insertionIndex] = weakpointBuffer[insertionIndex - 1];
                    distanceBuffer[insertionIndex] = distanceBuffer[insertionIndex - 1];
                    insertionIndex--;
                }

                targetBuffer[insertionIndex] = target;
                targetIdBuffer[insertionIndex] = targetId;
                weakpointBuffer[insertionIndex] = weakpoint;
                distanceBuffer[insertionIndex] = distance;
            }
        }

        private static bool ComesBefore(
            float leftDistance,
            int leftTargetId,
            float rightDistance,
            int rightTargetId)
        {
            return leftDistance < rightDistance ||
                   (leftDistance == rightDistance && leftTargetId < rightTargetId);
        }
    }
}
