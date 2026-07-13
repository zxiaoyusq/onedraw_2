using System;

namespace OneStrokeDemon.Input
{
    public readonly struct StrokeGeometrySettings
    {
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

        public float RdpEpsilonReferencePixels { get; }

        public int MaximumProcessedPointCount { get; }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
