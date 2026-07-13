using System;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    public readonly struct HitRecord
    {
        internal HitRecord(
            ulong strokeId,
            IHittable target,
            int targetId,
            bool isWeakpoint,
            float pathParameter,
            float pathDistanceReferencePixels,
            GestureMatchResult gesture,
            double timestamp)
        {
            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId), "Stroke id must be positive.");
            }

            Target = target ?? throw new ArgumentNullException(nameof(target));
            if (targetId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetId), "Hit target id must be non-zero.");
            }

            if (!IsFinite(pathParameter) || pathParameter < 0f || pathParameter > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pathParameter),
                    "Path parameter must be finite and normalized.");
            }

            if (!IsFinite(pathDistanceReferencePixels) || pathDistanceReferencePixels < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pathDistanceReferencePixels),
                    "Path distance must be finite and non-negative.");
            }

            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), "Hit timestamp must be finite.");
            }

            Gesture = gesture ?? throw new ArgumentNullException(nameof(gesture));
            StrokeId = strokeId;
            TargetId = targetId;
            IsWeakpoint = isWeakpoint;
            PathParameter = pathParameter;
            PathDistanceReferencePixels = pathDistanceReferencePixels;
            Timestamp = timestamp;
        }

        public ulong StrokeId { get; }

        public IHittable Target { get; }

        public int TargetId { get; }

        public bool IsWeakpoint { get; }

        public float PathParameter { get; }

        public float PathDistanceReferencePixels { get; }

        public GestureMatchResult Gesture { get; }

        public GestureType GestureType => Gesture.GestureType;

        public string GestureRuleId => Gesture.RuleId;

        public double Timestamp { get; }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
