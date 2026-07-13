using System;

namespace OneStrokeDemon.Input
{
    public sealed class GestureRule
    {
        public GestureRule(
            string ruleId,
            GestureType gestureType,
            float minimumLengthReferencePixels,
            float directionToleranceDegrees,
            float closeDistanceReferencePixels,
            float minimumAreaReferencePixelsSquared,
            float minimumNormalizedCurvature,
            double chargeHoldSeconds)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException("Gesture rule IDs must not be empty.", nameof(ruleId));
            }

            if (!IsSupported(gestureType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gestureType),
                    $"Unsupported gesture type '{gestureType}'.");
            }

            ValidateFiniteNonNegative(minimumLengthReferencePixels, nameof(minimumLengthReferencePixels));
            ValidateFiniteNonNegative(directionToleranceDegrees, nameof(directionToleranceDegrees));
            if (directionToleranceDegrees > 90f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(directionToleranceDegrees),
                    "Direction tolerance cannot exceed 90 degrees.");
            }

            ValidateFiniteNonNegative(closeDistanceReferencePixels, nameof(closeDistanceReferencePixels));
            ValidateFiniteNonNegative(
                minimumAreaReferencePixelsSquared,
                nameof(minimumAreaReferencePixelsSquared));
            ValidateFiniteNonNegative(minimumNormalizedCurvature, nameof(minimumNormalizedCurvature));
            if (double.IsNaN(chargeHoldSeconds) ||
                double.IsInfinity(chargeHoldSeconds) ||
                chargeHoldSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chargeHoldSeconds),
                    "Charge hold duration must be finite and non-negative.");
            }

            RuleId = ruleId;
            GestureType = gestureType;
            MinimumLengthReferencePixels = minimumLengthReferencePixels;
            DirectionToleranceDegrees = directionToleranceDegrees;
            CloseDistanceReferencePixels = closeDistanceReferencePixels;
            MinimumAreaReferencePixelsSquared = minimumAreaReferencePixelsSquared;
            MinimumNormalizedCurvature = minimumNormalizedCurvature;
            ChargeHoldSeconds = chargeHoldSeconds;
        }

        public string RuleId { get; }

        public GestureType GestureType { get; }

        public float MinimumLengthReferencePixels { get; }

        public float DirectionToleranceDegrees { get; }

        public float CloseDistanceReferencePixels { get; }

        public float MinimumAreaReferencePixelsSquared { get; }

        public float MinimumNormalizedCurvature { get; }

        public double ChargeHoldSeconds { get; }

        private static bool IsSupported(GestureType gestureType)
        {
            return gestureType == GestureType.Any ||
                   gestureType == GestureType.Horizontal ||
                   gestureType == GestureType.Vertical ||
                   gestureType == GestureType.Diagonal ||
                   gestureType == GestureType.Arc ||
                   gestureType == GestureType.Circle ||
                   gestureType == GestureType.Charged;
        }

        private static void ValidateFiniteNonNegative(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Gesture thresholds must be finite and non-negative.");
            }
        }
    }
}
