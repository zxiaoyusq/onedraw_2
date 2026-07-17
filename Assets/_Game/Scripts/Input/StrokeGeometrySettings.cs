using System;

namespace OneStrokeDemon.Input
{
    /// <summary>控制 RDP 简化容差和几何处理后最大点数的不可变设置。</summary>
    public readonly struct StrokeGeometrySettings
    {
        /// <summary>创建并验证几何处理设置。</summary>
        public StrokeGeometrySettings(
            float rdpEpsilonReferencePixels,
            int maximumProcessedPointCount)
        {
            if (!IsFinite(rdpEpsilonReferencePixels) || rdpEpsilonReferencePixels < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rdpEpsilonReferencePixels),
                    "RDP epsilon must be finite and non-negative.");
            }

            if (maximumProcessedPointCount < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumProcessedPointCount),
                    "Processed strokes need room for at least two points.");
            }

            RdpEpsilonReferencePixels = rdpEpsilonReferencePixels;
            MaximumProcessedPointCount = maximumProcessedPointCount;
        }

        /// <summary>获取 RDP 点到线段容差，单位为参考像素。</summary>
        public float RdpEpsilonReferencePixels { get; }

        /// <summary>获取处理后允许的最大点数。</summary>
        public int MaximumProcessedPointCount { get; }

        /// <summary>判断浮点数不是 NaN 或无穷。</summary>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
