namespace OneStrokeDemon.Combat
{
    public readonly struct ProjectileRuleSet
    {
        internal ProjectileRuleSet(
            string projectileId,
            string movePatternId,
            float speedReferencePixelsPerSecond,
            float lifetimeSeconds,
            long damage,
            bool cuttable,
            bool reflectable,
            string requiredStanceId,
            float hitRadiusReferencePixels,
            string assetKey,
            string vfxKey)
        {
            ProjectileId = projectileId;
            MovePatternId = movePatternId;
            SpeedReferencePixelsPerSecond = speedReferencePixelsPerSecond;
            LifetimeSeconds = lifetimeSeconds;
            Damage = damage;
            Cuttable = cuttable;
            Reflectable = reflectable;
            RequiredStanceId = requiredStanceId;
            HitRadiusReferencePixels = hitRadiusReferencePixels;
            AssetKey = assetKey;
            VfxKey = vfxKey;
            IsConfigured = true;
        }

        public string ProjectileId { get; }

        public string MovePatternId { get; }

        public float SpeedReferencePixelsPerSecond { get; }

        public float LifetimeSeconds { get; }

        public long Damage { get; }

        public bool Cuttable { get; }

        public bool Reflectable { get; }

        public string RequiredStanceId { get; }

        public float HitRadiusReferencePixels { get; }

        public string AssetKey { get; }

        public string VfxKey { get; }

        public bool IsConfigured { get; }
    }
}
