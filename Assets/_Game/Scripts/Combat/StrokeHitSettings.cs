using System;

namespace OneStrokeDemon.Combat
{
    public readonly struct StrokeHitResolverSettings
    {
        public StrokeHitResolverSettings(int maximumUniqueTargets, int queryCapacity)
        {
            if (maximumUniqueTargets < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumUniqueTargets),
                    "Maximum unique targets must be positive.");
            }

            if (queryCapacity <= maximumUniqueTargets)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(queryCapacity),
                    "Query capacity must include hitbox and saturation room beyond unique targets.");
            }

            MaximumUniqueTargets = maximumUniqueTargets;
            QueryCapacity = queryCapacity;
        }

        public int MaximumUniqueTargets { get; }

        public int QueryCapacity { get; }
    }

    public readonly struct StrokeHitRule
    {
        public StrokeHitRule(string ruleId, float radiusReferencePixels)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException("Stroke hit rule id is required.", nameof(ruleId));
            }

            if (float.IsNaN(radiusReferencePixels) ||
                float.IsInfinity(radiusReferencePixels) ||
                radiusReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radiusReferencePixels),
                    "Stroke hit radius must be finite and positive.");
            }

            RuleId = ruleId;
            RadiusReferencePixels = radiusReferencePixels;
        }

        public string RuleId { get; }

        public float RadiusReferencePixels { get; }
    }
}
