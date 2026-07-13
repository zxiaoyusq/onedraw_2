using System;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    public readonly struct DamageContext
    {
        public DamageContext(
            ulong strokeId,
            int targetId,
            GestureType gestureType,
            string stanceId,
            bool isWeakpoint,
            int comboCount,
            double timestamp)
        {
            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId), "Stroke id must be positive.");
            }

            if (targetId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetId), "Target id must be non-zero.");
            }

            if (gestureType == GestureType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(gestureType), "A resolved gesture is required.");
            }

            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException("Stance id must be non-empty.", nameof(stanceId));
            }

            if (comboCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(comboCount), "Combo count must be positive.");
            }

            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), "Timestamp must be finite.");
            }

            StrokeId = strokeId;
            TargetId = targetId;
            GestureType = gestureType;
            StanceId = stanceId;
            IsWeakpoint = isWeakpoint;
            ComboCount = comboCount;
            Timestamp = timestamp;
        }

        public ulong StrokeId { get; }

        public int TargetId { get; }

        public GestureType GestureType { get; }

        public string StanceId { get; }

        public bool IsWeakpoint { get; }

        public int ComboCount { get; }

        public double Timestamp { get; }

        public static DamageContext FromHitRecord(
            in HitRecord hit,
            string stanceId,
            int comboCount)
        {
            return new DamageContext(
                hit.StrokeId,
                hit.TargetId,
                hit.GestureType,
                stanceId,
                hit.IsWeakpoint,
                comboCount,
                hit.Timestamp);
        }
    }
}
