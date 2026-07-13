using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Combat
{
    public static class ProjectileRuleSetFactory
    {
        public static ProjectileRuleSet Create(
            IConfigProvider configProvider,
            string projectileId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            ProjectileConfig row = configProvider.GetProjectile(projectileId);
            RequireText(projectileId, nameof(row.ProjectileId), row.ProjectileId);
            RequireText(row.ProjectileId, nameof(row.MovePatternId), row.MovePatternId);
            RequireNonNegative(row.ProjectileId, nameof(row.SpeedRefPxSec), row.SpeedRefPxSec);
            RequireNonNegative(row.ProjectileId, nameof(row.LifeSec), row.LifeSec);
            RequireNonNegative(row.ProjectileId, nameof(row.Damage), row.Damage);
            RequireNonNegative(row.ProjectileId, nameof(row.HitRadiusRefPx), row.HitRadiusRefPx);
            RequireText(row.ProjectileId, nameof(row.AssetKey), row.AssetKey);
            RequireText(row.ProjectileId, nameof(row.VfxKey), row.VfxKey);

            return new ProjectileRuleSet(
                row.ProjectileId,
                row.MovePatternId,
                row.SpeedRefPxSec,
                row.LifeSec,
                row.Damage,
                row.Cuttable,
                row.Reflectable,
                row.RequiredStanceId ?? string.Empty,
                row.HitRadiusRefPx,
                row.AssetKey,
                row.VfxKey);
        }

        private static void RequireText(string rowId, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"Configured projectile field '{rowId}.{field}' must be non-empty.",
                    field);
            }
        }

        private static void RequireNonNegative(string rowId, string field, long value)
        {
            if (value < 0L)
            {
                throw Invalid(rowId, field, value);
            }
        }

        private static void RequireNonNegative(string rowId, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw Invalid(rowId, field, value);
            }
        }

        private static ArgumentOutOfRangeException Invalid(
            string rowId,
            string field,
            object value)
        {
            return new ArgumentOutOfRangeException(
                field,
                value,
                $"Configured value for '{rowId}.{field}' is outside the supported projectile range.");
        }
    }
}
