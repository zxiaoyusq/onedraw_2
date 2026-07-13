using System;
using OneStrokeDemon.Core;

namespace OneStrokeDemon.Config
{
    public static class ObjectPoolConfiguration
    {
        public const string EnemyFamilyId = "enemy";
        public const string ProjectileFamilyId = "projectile";
        public const string VfxFamilyId = "vfx";
        public const string DamageNumberFamilyId = "damage-number";
        public const string DamageNumberPoolId = "damage-number/default";

        private const string EnemyPoolPrefix = "enemy/";
        private const string ProjectilePoolPrefix = "projectile/";
        private const string VfxPoolPrefix = "vfx/";

        public static PoolFamilyDefinition CreateEnemyFamily(IConfigProvider configProvider)
        {
            return CreateFamily(
                configProvider,
                EnemyFamilyId,
                ConfigIds.GlobalKeys.MaxActiveEnemies,
                ConfigIds.GlobalKeys.EnemyPoolExhaustionPolicy);
        }

        public static PoolFamilyDefinition CreateProjectileFamily(IConfigProvider configProvider)
        {
            return CreateFamily(
                configProvider,
                ProjectileFamilyId,
                ConfigIds.GlobalKeys.MaxActiveProjectiles,
                ConfigIds.GlobalKeys.ProjectilePoolExhaustionPolicy);
        }

        public static PoolFamilyDefinition CreateVfxFamily(IConfigProvider configProvider)
        {
            return CreateFamily(
                configProvider,
                VfxFamilyId,
                ConfigIds.GlobalKeys.MaxActiveVfx,
                ConfigIds.GlobalKeys.VfxPoolExhaustionPolicy);
        }

        public static PoolFamilyDefinition CreateDamageNumberFamily(
            IConfigProvider configProvider)
        {
            return CreateFamily(
                configProvider,
                DamageNumberFamilyId,
                ConfigIds.GlobalKeys.DamageNumberPoolSize,
                ConfigIds.GlobalKeys.DamageNumberPoolExhaustionPolicy);
        }

        public static PoolDefinition CreateEnemyPool(
            IConfigProvider configProvider,
            string enemyId,
            Func<IPoolable> factory)
        {
            RequireProvider(configProvider);
            EnemyConfig row = configProvider.GetEnemy(RequireId(enemyId, nameof(enemyId)));
            return new PoolDefinition(
                GetEnemyPoolId(row.EnemyId),
                EnemyFamilyId,
                CheckedNonNegativeInt(row.PoolPrewarm, $"Enemies.{row.EnemyId}.poolPrewarm"),
                factory);
        }

        public static PoolDefinition CreateProjectilePool(
            IConfigProvider configProvider,
            string projectileId,
            Func<IPoolable> factory)
        {
            RequireProvider(configProvider);
            ProjectileConfig row = configProvider.GetProjectile(
                RequireId(projectileId, nameof(projectileId)));
            int prewarm = ReadPositiveInt(
                configProvider,
                ConfigIds.GlobalKeys.ProjectilePoolPrewarmPerType);
            return new PoolDefinition(
                GetProjectilePoolId(row.ProjectileId),
                ProjectileFamilyId,
                prewarm,
                factory);
        }

        public static PoolDefinition CreateVfxPool(
            IConfigProvider configProvider,
            string vfxKey,
            Func<IPoolable> factory)
        {
            RequireProvider(configProvider);
            VfxCueConfig row = configProvider.GetVfxCue(RequireId(vfxKey, nameof(vfxKey)));
            return new PoolDefinition(
                GetVfxPoolId(row.VfxKey),
                VfxFamilyId,
                CheckedNonNegativeInt(row.PoolPrewarm, $"VfxCues.{row.VfxKey}.poolPrewarm"),
                factory);
        }

        public static PoolDefinition CreateDamageNumberPool(
            IConfigProvider configProvider,
            Func<IPoolable> factory)
        {
            RequireProvider(configProvider);
            int size = ReadPositiveInt(
                configProvider,
                ConfigIds.GlobalKeys.DamageNumberPoolSize);
            return new PoolDefinition(
                DamageNumberPoolId,
                DamageNumberFamilyId,
                size,
                factory);
        }

        public static string GetEnemyPoolId(string enemyId) =>
            EnemyPoolPrefix + RequireId(enemyId, nameof(enemyId));

        public static string GetProjectilePoolId(string projectileId) =>
            ProjectilePoolPrefix + RequireId(projectileId, nameof(projectileId));

        public static string GetVfxPoolId(string vfxKey) =>
            VfxPoolPrefix + RequireId(vfxKey, nameof(vfxKey));

        private static PoolFamilyDefinition CreateFamily(
            IConfigProvider configProvider,
            string familyId,
            string capacityKey,
            string policyKey)
        {
            RequireProvider(configProvider);
            return new PoolFamilyDefinition(
                familyId,
                ReadPositiveInt(configProvider, capacityKey),
                ReadPolicy(configProvider, policyKey));
        }

        private static int ReadPositiveInt(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            if (!string.Equals(row.ValueType, "int", StringComparison.Ordinal) ||
                !row.IntValue.HasValue)
            {
                throw InvalidGlobal(key, "must be an int value");
            }

            long value = row.IntValue.Value;
            if (value < 1L || value > int.MaxValue)
            {
                throw InvalidGlobal(key, "must fit in a positive runtime integer");
            }

            return (int)value;
        }

        private static PoolExhaustionPolicy ReadPolicy(
            IConfigProvider configProvider,
            string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            if (!string.Equals(row.ValueType, "string", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(row.StringValue) ||
                !Enum.TryParse(row.StringValue, false, out PoolExhaustionPolicy policy) ||
                !Enum.IsDefined(typeof(PoolExhaustionPolicy), policy))
            {
                throw InvalidGlobal(
                    key,
                    "must be exactly 'Reject' or 'ReuseOldest'");
            }

            return policy;
        }

        private static int CheckedNonNegativeInt(long value, string field)
        {
            if (value < 0L || value > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    field,
                    value,
                    "Configured prewarm count must fit in a non-negative runtime integer.");
            }

            return (int)value;
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Configured pool row id must be non-empty.", parameterName);
            }

            return value;
        }

        private static void RequireProvider(IConfigProvider configProvider)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }
        }

        private static ArgumentException InvalidGlobal(string key, string requirement)
        {
            return new ArgumentException(
                $"Configured Global row '{key}' {requirement}.",
                key);
        }
    }
}
