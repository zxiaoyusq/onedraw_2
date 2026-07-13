using System;

namespace OneStrokeDemon.Combat
{
    public enum ProjectileFaction
    {
        None = 0,
        Player = 1,
        Enemy = 2
    }

    public readonly struct ProjectileOwner
    {
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

        public ProjectileFaction Faction { get; }

        public int EntityId { get; }

        public bool IsValid { get; }
    }

    public readonly struct ProjectileOwnership
    {
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

        public static ProjectileOwnership FromInitialOwner(ProjectileOwner owner)
        {
            RequireOwner(owner, nameof(owner));
            return new ProjectileOwnership(owner, owner, 0);
        }

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

        public bool CanDamage(ProjectileOwner target)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot query an uninitialized projectile ownership.");
            }

            RequireOwner(target, nameof(target));
            return target.Faction != CurrentOwner.Faction;
        }

        private static void RequireOwner(ProjectileOwner owner, string parameterName)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Projectile owner must be initialized.", parameterName);
            }
        }
    }

    public readonly struct ProjectileDamageSource
    {
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

        public string ProjectileId { get; }

        public long Damage { get; }

        public ProjectileOwner CurrentOwner { get; }

        public ProjectileOwner OriginalOwner { get; }

        public int ReflectionCount { get; }

        public bool IsValid { get; }
    }
}
