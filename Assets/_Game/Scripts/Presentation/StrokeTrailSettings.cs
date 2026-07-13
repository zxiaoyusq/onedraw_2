using System;

namespace OneStrokeDemon.Presentation
{
    public readonly struct StrokeTrailPoolSettings
    {
        public StrokeTrailPoolSettings(
            int capacity,
            int maximumActiveTrailCount,
            int maximumPointCount)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Pool capacity must be positive.");
            }

            if (maximumActiveTrailCount < 1 || maximumActiveTrailCount > capacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumActiveTrailCount),
                    "Maximum active trail count must fit inside the pool.");
            }

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

    public readonly struct StrokeTrailStyle
    {
        public StrokeTrailStyle(
            string stanceId,
            float widthReferencePixels,
            float lifetimeSeconds,
            int sortingLayerId,
            int sortingOrder)
        {
            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException("Stance id is required.", nameof(stanceId));
            }

            if (!IsFinite(widthReferencePixels) || widthReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(widthReferencePixels),
                    "Trail width must be finite and positive.");
            }

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

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
