using System;

namespace OneStrokeDemon.Input
{
    /// <summary>控制采样最小点距、最大路径长度和缓冲容量的不可变设置。</summary>
    public readonly struct StrokeSamplingSettings
    {
        /// <summary>创建并验证一组采样设置。</summary>
        public StrokeSamplingSettings(
            float minimumPointDistanceReferencePixels,
            float maximumStrokeLengthReferencePixels,
            int maximumPointCount)
        {
            if (!IsFinite(minimumPointDistanceReferencePixels) ||
                minimumPointDistanceReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumPointDistanceReferencePixels),
                    "Minimum point distance must be finite and greater than zero.");
            }

            if (!IsFinite(maximumStrokeLengthReferencePixels) ||
                maximumStrokeLengthReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumStrokeLengthReferencePixels),
                    "Maximum stroke length must be finite and greater than zero.");
            }

            if (maximumPointCount < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumPointCount),
                    "A stroke needs room for at least two points.");
            }

            MinimumPointDistanceReferencePixels = minimumPointDistanceReferencePixels;
            MaximumStrokeLengthReferencePixels = maximumStrokeLengthReferencePixels;
            MaximumPointCount = maximumPointCount;
        }

        /// <summary>获取相邻有效采样点的最小距离。</summary>
        public float MinimumPointDistanceReferencePixels { get; }

        /// <summary>获取一笔允许的最大累计长度。</summary>
        public float MaximumStrokeLengthReferencePixels { get; }

        /// <summary>获取一笔允许保存的最大采样点数。</summary>
        public int MaximumPointCount { get; }

        /// <summary>判断浮点数不是 NaN 或无穷。</summary>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
