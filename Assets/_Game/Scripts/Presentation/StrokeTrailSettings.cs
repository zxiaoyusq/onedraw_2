using System;

namespace OneStrokeDemon.Presentation
{
    // 定义 StrokeTrailPoolSettings 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public readonly struct StrokeTrailPoolSettings
    {
        // 初始化 StrokeTrailPoolSettings，并建立表现层所需的引用与初始显示状态。
        public StrokeTrailPoolSettings(
            int capacity,
            int maximumActiveTrailCount,
            int maximumPointCount)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Pool capacity must be positive.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (maximumActiveTrailCount < 1 || maximumActiveTrailCount > capacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumActiveTrailCount),
                    "Maximum active trail count must fit inside the pool.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (maximumPointCount < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumPointCount),
                    "Trails need room for at least two points.");
            }

            Capacity = capacity;
            MaximumActiveTrailCount = maximumActiveTrailCount;
            MaximumPointCount = maximumPointCount;
        }

        public int Capacity { get; }

        public int MaximumActiveTrailCount { get; }

        public int MaximumPointCount { get; }
    }

    // 定义 StrokeTrailStyle 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public readonly struct StrokeTrailStyle
    {
        // 初始化 StrokeTrailStyle，并建立表现层所需的引用与初始显示状态。
        public StrokeTrailStyle(
            string stanceId,
            float widthReferencePixels,
            float lifetimeSeconds,
            int sortingLayerId,
            int sortingOrder)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException("Stance id is required.", nameof(stanceId));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsFinite(widthReferencePixels) || widthReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(widthReferencePixels),
                    "Trail width must be finite and positive.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsFinite(lifetimeSeconds) || lifetimeSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lifetimeSeconds),
                    "Trail lifetime must be finite and positive.");
            }

            StanceId = stanceId;
            WidthReferencePixels = widthReferencePixels;
            LifetimeSeconds = lifetimeSeconds;
            SortingLayerId = sortingLayerId;
            SortingOrder = sortingOrder;
        }

        public string StanceId { get; }

        public float WidthReferencePixels { get; }

        public float LifetimeSeconds { get; }

        public int SortingLayerId { get; }

        public int SortingOrder { get; }

        // 判断是否 IsFinite 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
