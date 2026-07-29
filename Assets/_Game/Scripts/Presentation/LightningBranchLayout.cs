using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    /// <summary>
    /// 从同一条参考像素笔迹确定性生成闪电分支；不改变命中路径，也不依赖随机全局状态。
    /// </summary>
    public static class LightningBranchLayout
    {
        /// <summary>按路径长度与配置间距计算需要显示的分支数量，并受预热渲染器容量限制。</summary>
        public static int CountBranches(
            IReadOnlyList<Vector2> path,
            float spacingReferencePixels,
            int maximumBranchCount)
        {
            ValidatePath(path);
            if (!IsFinite(spacingReferencePixels) || spacingReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(spacingReferencePixels));
            }

            if (maximumBranchCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumBranchCount));
            }

            float totalLength = CalculateLength(path);
            return Mathf.Min(
                maximumBranchCount,
                Mathf.FloorToInt(totalLength / spacingReferencePixels));
        }

        /// <summary>
        /// 把指定序号的分支折线写入调用方复用的缓冲区；相同笔迹ID、路径与参数必定得到相同结果。
        /// </summary>
        public static bool TryWriteBranch(
            ulong strokeId,
            int branchIndex,
            IReadOnlyList<Vector2> path,
            float spacingReferencePixels,
            float lengthReferencePixels,
            float jitterReferencePixels,
            int segmentCount,
            Vector2[] destination)
        {
            ValidatePath(path);
            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId));
            }

            if (branchIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(branchIndex));
            }

            if (!IsFinite(spacingReferencePixels) || spacingReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(spacingReferencePixels));
            }

            if (!IsFinite(lengthReferencePixels) || lengthReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthReferencePixels));
            }

            if (!IsFinite(jitterReferencePixels) || jitterReferencePixels < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(jitterReferencePixels));
            }

            if (segmentCount < 2 || segmentCount > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentCount));
            }

            if (destination == null || destination.Length < segmentCount + 1)
            {
                throw new ArgumentException(
                    "Destination must hold the complete branch polyline.",
                    nameof(destination));
            }

            float totalLength = CalculateLength(path);
            float targetDistance = (branchIndex + 0.5f) * spacingReferencePixels;
            if (targetDistance > totalLength)
            {
                return false;
            }

            if (!TrySamplePath(path, targetDistance, out Vector2 origin, out Vector2 tangent))
            {
                return false;
            }

            uint state = Seed(strokeId, branchIndex);
            float side = NextUnit(ref state) < 0.5f ? -1f : 1f;
            float lengthScale = Mathf.Lerp(0.78f, 1.08f, NextUnit(ref state));
            Vector2 normal = new Vector2(-tangent.y, tangent.x) * side;
            destination[0] = origin;
            for (int pointIndex = 1; pointIndex <= segmentCount; pointIndex++)
            {
                float progress = pointIndex / (float)segmentCount;
                float alongJitter = SignedUnit(ref state) *
                    jitterReferencePixels *
                    (1f - (progress * 0.35f));
                float lateralDistance = lengthReferencePixels * lengthScale * progress;
                destination[pointIndex] =
                    origin +
                    (normal * lateralDistance) +
                    (tangent * alongJitter);
            }

            return true;
        }

        // 沿折线路径按距离采样稳定原点和切线，忽略零长度片段。
        private static bool TrySamplePath(
            IReadOnlyList<Vector2> path,
            float targetDistance,
            out Vector2 point,
            out Vector2 tangent)
        {
            float traversed = 0f;
            for (int index = 1; index < path.Count; index++)
            {
                Vector2 start = path[index - 1];
                Vector2 delta = path[index] - start;
                float segmentLength = delta.magnitude;
                if (segmentLength <= Mathf.Epsilon)
                {
                    continue;
                }

                if (traversed + segmentLength >= targetDistance)
                {
                    float t = Mathf.Clamp01((targetDistance - traversed) / segmentLength);
                    point = Vector2.LerpUnclamped(start, path[index], t);
                    tangent = delta / segmentLength;
                    return true;
                }

                traversed += segmentLength;
            }

            point = default;
            tangent = default;
            return false;
        }

        // 计算参考像素折线路径总长。
        private static float CalculateLength(IReadOnlyList<Vector2> path)
        {
            float total = 0f;
            for (int index = 1; index < path.Count; index++)
            {
                total += Vector2.Distance(path[index - 1], path[index]);
            }

            return total;
        }

        // 为每个笔迹和分支建立独立伪随机序列，不访问UnityEngine.Random。
        private static uint Seed(ulong strokeId, int branchIndex)
        {
            uint folded = (uint)strokeId ^ (uint)(strokeId >> 32);
            uint seed = folded ^ unchecked((uint)(branchIndex + 1) * 0x9E3779B9u);
            return seed == 0u ? 0xA341316Cu : seed;
        }

        // xorshift32提供足够的视觉抖动且跨平台可重复。
        private static float NextUnit(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0x00FFFFFFu) / 16777216f;
        }

        private static float SignedUnit(ref uint state)
        {
            return (NextUnit(ref state) * 2f) - 1f;
        }

        private static void ValidatePath(IReadOnlyList<Vector2> path)
        {
            if (path == null || path.Count < 2)
            {
                throw new ArgumentException(
                    "Lightning branches require at least two path points.",
                    nameof(path));
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
