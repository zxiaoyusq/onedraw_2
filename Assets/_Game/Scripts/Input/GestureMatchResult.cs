namespace OneStrokeDemon.Input
{
    public sealed class GestureMatchResult
    {
        internal GestureMatchResult(
            ulong strokeId,
            string ruleId,
            GestureType gestureType,
            float confidence,
            float lengthReferencePixels,
            double averageSpeedReferencePixelsPerSecond,
            float directionAngleDegrees,
            float normalizedCurvature,
            float closureRatio,
            float closureDistanceReferencePixels,
            float areaReferencePixelsSquared,
            double initialHoldSeconds)
        {
            StrokeId = strokeId;
            RuleId = ruleId ?? string.Empty;
            GestureType = gestureType;
            Confidence = confidence;
            LengthReferencePixels = lengthReferencePixels;
            AverageSpeedReferencePixelsPerSecond = averageSpeedReferencePixelsPerSecond;
            DirectionAngleDegrees = directionAngleDegrees;
            NormalizedCurvature = normalizedCurvature;
            ClosureRatio = closureRatio;
            ClosureDistanceReferencePixels = closureDistanceReferencePixels;
            AreaReferencePixelsSquared = areaReferencePixelsSquared;
            InitialHoldSeconds = initialHoldSeconds;
        }

        public ulong StrokeId { get; }

        public bool IsMatch => GestureType != GestureType.None;

        public string RuleId { get; }

        public GestureType GestureType { get; }

        public float Confidence { get; }

        public float LengthReferencePixels { get; }

        public double AverageSpeedReferencePixelsPerSecond { get; }

        public float DirectionAngleDegrees { get; }

        public float NormalizedCurvature { get; }

        public float ClosureRatio { get; }

        public float ClosureDistanceReferencePixels { get; }

        public float AreaReferencePixelsSquared { get; }

        public double InitialHoldSeconds { get; }
    }
}
