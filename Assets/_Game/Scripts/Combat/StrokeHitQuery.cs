using System;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    public readonly struct StrokeHitCandidate
    {
        public StrokeHitCandidate(
            IHittable target,
            bool isWeakpoint,
            float segmentParameter)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            if (float.IsNaN(segmentParameter) ||
                float.IsInfinity(segmentParameter) ||
                segmentParameter < 0f ||
                segmentParameter > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(segmentParameter),
                    "Segment parameter must be finite and normalized.");
            }

            IsWeakpoint = isWeakpoint;
            SegmentParameter = segmentParameter;
        }

        public IHittable Target { get; }

        public bool IsWeakpoint { get; }

        public float SegmentParameter { get; }
    }

    public interface IStrokeHitQuery
    {
        int QuerySegment(
            Vector2 startReferencePixels,
            Vector2 endReferencePixels,
            float radiusReferencePixels,
            StrokeHitCandidate[] results);
    }
}
