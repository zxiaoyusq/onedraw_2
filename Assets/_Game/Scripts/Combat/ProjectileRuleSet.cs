namespace OneStrokeDemon.Combat
{
    /// <summary>冻结一个投射物运行时所需的配置字段。</summary>
    public readonly struct ProjectileRuleSet
    {
        /// <summary>由配置工厂创建并标记完整规则集。</summary>
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

        // 默认结构 IsConfigured=false；控制器只接受工厂构造的完整快照。
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
