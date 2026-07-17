using System;

namespace OneStrokeDemon.Combat
{
    /// <summary>投射物当前归属的阵营。</summary>
    public enum ProjectileFaction
    {
        None = 0,
        Player = 1,
        Enemy = 2
    }

    /// <summary>以阵营和非零运行时实体 ID 标识投射物所有者。</summary>
    public readonly struct ProjectileOwner
    {
        /// <summary>创建玩家或敌方阵营的有效所有者。</summary>
        public ProjectileOwner(ProjectileFaction faction, int entityId)
        {
            if (faction != ProjectileFaction.Player && faction != ProjectileFaction.Enemy)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(faction),
                    "Projectile owner faction must be Player or Enemy.");
            }

            if (entityId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entityId),
                    "Projectile owner entity id must be non-zero.");
            }

            Faction = faction;
            EntityId = entityId;
            IsValid = true;
        }

        // 默认结构 IsValid=false，用于明确区分尚未初始化的所有者。
        public ProjectileFaction Faction { get; }

        public int EntityId { get; }

        public bool IsValid { get; }
    }

    /// <summary>保存投射物当前所有者、不可变原始所有者和反弹次数。</summary>
    public readonly struct ProjectileOwnership
    {
        /// <summary>创建内部有效归属快照。</summary>
        private ProjectileOwnership(
            ProjectileOwner currentOwner,
            ProjectileOwner originalOwner,
            int reflectionCount)
        {
            CurrentOwner = currentOwner;
            OriginalOwner = originalOwner;
            ReflectionCount = reflectionCount;
            IsValid = true;
        }

        public ProjectileOwner CurrentOwner { get; }

        public ProjectileOwner OriginalOwner { get; }

        public int ReflectionCount { get; }

        public bool IsValid { get; }

        /// <summary>从初始所有者创建零次反弹的归属。</summary>
        public static ProjectileOwnership FromInitialOwner(ProjectileOwner owner)
        {
            RequireOwner(owner, nameof(owner));
            return new ProjectileOwnership(owner, owner, 0);
        }

        /// <summary>把当前归属切换到对立阵营反弹者，同时保留原始来源。</summary>
        public ProjectileOwnership ReflectTo(ProjectileOwner reflector)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot reflect an uninitialized projectile ownership.");
            }

            RequireOwner(reflector, nameof(reflector));
            if (reflector.Faction == CurrentOwner.Faction)
            {
                throw new ArgumentException(
                    "A projectile can only be reflected to the opposing faction.",
                    nameof(reflector));
            }

            return new ProjectileOwnership(
                reflector,
                OriginalOwner,
                checked(ReflectionCount + 1));
        }

        /// <summary>判断当前归属是否可以伤害指定目标阵营。</summary>
        public bool CanDamage(ProjectileOwner target)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot query an uninitialized projectile ownership.");
            }

            RequireOwner(target, nameof(target));
            return target.Faction != CurrentOwner.Faction;
        }

        /// <summary>验证所有者已初始化。</summary>
        private static void RequireOwner(ProjectileOwner owner, string parameterName)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Projectile owner must be initialized.", parameterName);
            }
        }
    }

    /// <summary>保存投射物命中时可追溯的伤害、当前来源和原始来源。</summary>
    public readonly struct ProjectileDamageSource
    {
        /// <summary>从当前规则与归属创建有效伤害来源快照。</summary>
        internal ProjectileDamageSource(
            in ProjectileRuleSet rules,
            in ProjectileOwnership ownership)
        {
            ProjectileId = rules.ProjectileId;
            Damage = rules.Damage;
            CurrentOwner = ownership.CurrentOwner;
            OriginalOwner = ownership.OriginalOwner;
            ReflectionCount = ownership.ReflectionCount;
            IsValid = true;
        }

        // 伤害来源在投射物回收前冻结，后续归属变化不会改写旧命中事实。
        public string ProjectileId { get; }

        public long Damage { get; }

        public ProjectileOwner CurrentOwner { get; }

        public ProjectileOwner OriginalOwner { get; }

        public int ReflectionCount { get; }

        public bool IsValid { get; }
    }
}
