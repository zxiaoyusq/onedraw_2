using System;

namespace OneStrokeDemon.Input
{
    public readonly struct StrokeSamplingSettings
    {
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

        public float MinimumPointDistanceReferencePixels { get; }

        public float MaximumStrokeLengthReferencePixels { get; }

        public int MaximumPointCount { get; }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
