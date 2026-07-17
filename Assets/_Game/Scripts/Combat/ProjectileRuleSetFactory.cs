using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Combat
{
    /// <summary>从 Projectiles 配置行创建经运行时范围验证的投射物规则。</summary>
    public static class ProjectileRuleSetFactory
    {
        /// <summary>读取指定投射物并验证文本、数值和资源键后冻结规则。</summary>
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

        /// <summary>验证配置文本非空。</summary>
        private static void RequireText(string rowId, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"Configured projectile field '{rowId}.{field}' must be non-empty.",
                    field);
            }
        }

        /// <summary>验证整数配置非负。</summary>
        private static void RequireNonNegative(string rowId, string field, long value)
        {
            if (value < 0L)
            {
                throw Invalid(rowId, field, value);
            }
        }

        /// <summary>验证浮点配置有限且非负。</summary>
        private static void RequireNonNegative(string rowId, string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw Invalid(rowId, field, value);
            }
        }

        /// <summary>创建包含配置行、字段和值的范围异常。</summary>
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
