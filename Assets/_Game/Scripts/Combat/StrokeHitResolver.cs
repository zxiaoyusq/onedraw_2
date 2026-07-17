using System;
using OneStrokeDemon.Input;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    /// <summary>沿处理后笔迹逐段查询、按目标 ID 去重、聚合弱点并按路径稳定排序。</summary>
    public sealed class StrokeHitResolver
    {
        private readonly StrokeHitResolverSettings settings;
        private readonly IStrokeHitQuery query;
        private readonly StrokeHitCandidate[] candidateBuffer;
        private readonly IHittable[] targetBuffer;
        private readonly int[] targetIdBuffer;
        private readonly bool[] weakpointBuffer;
        private readonly float[] distanceBuffer;

        /// <summary>按设置一次性分配候选、目标、ID、弱点和距离缓冲。</summary>
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

        /// <summary>获取解析器容量设置。</summary>
        public StrokeHitResolverSettings Settings => settings;

        /// <summary>解析一笔命中并写入调用方结果缓冲，返回唯一目标数量。</summary>
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

            // 不匹配、退化或零长度笔迹没有可命中语义，直接返回空结果。
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

            // 累计段长把每段局部接触参数转换为整条笔迹上的绝对路径距离。
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
                        // 同目标多 Collider 只保留最早路径距离，并把任一弱点命中聚合为 true。
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

                // 按首次路径接触排序，同距离再按稳定目标 ID 排序。
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
                // 无论成功或异常都清空引用和临时事实，避免跨笔残留及对象持有。
                Array.Clear(candidateBuffer, 0, candidateBuffer.Length);
                Array.Clear(targetBuffer, 0, targetBuffer.Length);
                Array.Clear(targetIdBuffer, 0, targetIdBuffer.Length);
                Array.Clear(weakpointBuffer, 0, weakpointBuffer.Length);
                Array.Clear(distanceBuffer, 0, distanceBuffer.Length);
            }
        }

        /// <summary>在线性预分配 ID 缓冲中查找已有目标。</summary>
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

        /// <summary>对小型固定缓冲执行稳定插入排序，并同步移动所有并行数组。</summary>
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

        /// <summary>比较两目标是否应按距离优先、ID 次优先排列。</summary>
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
