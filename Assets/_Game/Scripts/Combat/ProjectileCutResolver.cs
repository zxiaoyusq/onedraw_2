using System;

namespace OneStrokeDemon.Combat
{
    public enum ProjectileStrokeOutcome
    {
        None = 0,
        FriendlyOwned = 1,
        RequiredStanceMismatch = 2,
        Uncuttable = 3,
        Cut = 4,
        Reflected = 5
    }

    public readonly struct ProjectileStrokeResolution
    {
        internal ProjectileStrokeResolution(ProjectileStrokeOutcome outcome)
        {
            Outcome = outcome;
            IsResolved = true;
        }

        public ProjectileStrokeOutcome Outcome { get; }

        public bool IsResolved { get; }

        public bool ReleasesProjectile => Outcome == ProjectileStrokeOutcome.Cut;

        public bool ChangesOwnership => Outcome == ProjectileStrokeOutcome.Reflected;
    }

    public static class ProjectileCutResolver
    {
        public static ProjectileStrokeResolution Resolve(
            in ProjectileRuleSet rules,
            in ProjectileOwnership ownership,
            string stanceId,
            ProjectileOwner reflector)
        {
            if (!rules.IsConfigured)
            {
                throw new ArgumentException("Projectile rules must be configured.", nameof(rules));
            }

            if (!ownership.IsValid)
            {
                throw new ArgumentException("Projectile ownership must be initialized.", nameof(ownership));
            }

            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException("Current stance id must be non-empty.", nameof(stanceId));
            }

            if (!reflector.IsValid)
            {
                throw new ArgumentException("Reflector owner must be initialized.", nameof(reflector));
            }

            if (reflector.Faction == ownership.CurrentOwner.Faction)
            {
                return new ProjectileStrokeResolution(ProjectileStrokeOutcome.FriendlyOwned);
            }

            if (!string.IsNullOrEmpty(rules.RequiredStanceId) &&
                !string.Equals(rules.RequiredStanceId, stanceId, StringComparison.Ordinal))
            {
                return new ProjectileStrokeResolution(
                    ProjectileStrokeOutcome.RequiredStanceMismatch);
            }

            if (rules.Reflectable)
            {
                return new ProjectileStrokeResolution(ProjectileStrokeOutcome.Reflected);
            }

            return new ProjectileStrokeResolution(
                rules.Cuttable
                    ? ProjectileStrokeOutcome.Cut
                    : ProjectileStrokeOutcome.Uncuttable);
        }
    }
}
