using System;
using OneStrokeDemon.Core;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 把配置表中的容量、耗尽策略和预热数量转换为 Core 对象池定义。
    /// </summary>
    public static class ObjectPoolConfiguration
    {
        /// <summary>敌人池共享容量族 ID。</summary>
        public const string EnemyFamilyId = "enemy";
        /// <summary>投射物池共享容量族 ID。</summary>
        public const string ProjectileFamilyId = "projectile";
        /// <summary>视觉特效池共享容量族 ID。</summary>
        public const string VfxFamilyId = "vfx";
        /// <summary>伤害数字池共享容量族 ID。</summary>
        public const string DamageNumberFamilyId = "damage-number";
        /// <summary>唯一伤害数字池 ID。</summary>
        public const string DamageNumberPoolId = "damage-number/default";

        private const string EnemyPoolPrefix = "enemy/";
        private const string ProjectilePoolPrefix = "projectile/";
        private const string VfxPoolPrefix = "vfx/";

        /// <summary>从 Global 配置创建敌人池族容量与耗尽策略。</summary>
        public static PoolFamilyDefinition CreateEnemyFamily(IConfigProvider configProvider)
        {
            return CreateFamily(
                configProvider,
                EnemyFamilyId,
                ConfigIds.GlobalKeys.MaxActiveEnemies,
                ConfigIds.GlobalKeys.EnemyPoolExhaustionPolicy);
        }

        /// <summary>从 Global 配置创建投射物池族容量与耗尽策略。</summary>
        public static PoolFamilyDefinition CreateProjectileFamily(IConfigProvider configProvider)
        {
            return CreateFamily(
                configProvider,
                ProjectileFamilyId,
                ConfigIds.GlobalKeys.MaxActiveProjectiles,
                ConfigIds.GlobalKeys.ProjectilePoolExhaustionPolicy);
        }

        /// <summary>从 Global 配置创建视觉特效池族容量与耗尽策略。</summary>
        public static PoolFamilyDefinition CreateVfxFamily(IConfigProvider configProvider)
        {
            return CreateFamily(
                configProvider,
                VfxFamilyId,
                ConfigIds.GlobalKeys.MaxActiveVfx,
                ConfigIds.GlobalKeys.VfxPoolExhaustionPolicy);
        }

        /// <summary>从 Global 配置创建伤害数字池族容量与耗尽策略。</summary>
        public static PoolFamilyDefinition CreateDamageNumberFamily(
            IConfigProvider configProvider)
        {
            return CreateFamily(
                configProvider,
                DamageNumberFamilyId,
                ConfigIds.GlobalKeys.DamageNumberPoolSize,
                ConfigIds.GlobalKeys.DamageNumberPoolExhaustionPolicy);
        }

        /// <summary>按敌人配置的预热数量创建一个具体敌人池。</summary>
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

        /// <summary>按全局每类型预热数量创建一个具体投射物池。</summary>
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

        /// <summary>按特效提示配置的预热数量创建一个具体视觉特效池。</summary>
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

        /// <summary>按全局伤害数字池大小创建唯一伤害数字池。</summary>
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

        /// <summary>把敌人 ID 转换为稳定且带类型前缀的池 ID。</summary>
        public static string GetEnemyPoolId(string enemyId) =>
            EnemyPoolPrefix + RequireId(enemyId, nameof(enemyId));

        /// <summary>把投射物 ID 转换为稳定且带类型前缀的池 ID。</summary>
        public static string GetProjectilePoolId(string projectileId) =>
            ProjectilePoolPrefix + RequireId(projectileId, nameof(projectileId));

        /// <summary>把特效键转换为稳定且带类型前缀的池 ID。</summary>
        public static string GetVfxPoolId(string vfxKey) =>
            VfxPoolPrefix + RequireId(vfxKey, nameof(vfxKey));

        /// <summary>读取指定 Global 容量和策略，创建共享池族定义。</summary>
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

        /// <summary>读取类型为 int 且可安全转换为正 int 的 Global 配置。</summary>
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

        /// <summary>读取并严格解析对象池耗尽策略，拒绝大小写或未知枚举值。</summary>
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

        /// <summary>把配置 long 安全转换为允许为零的运行时预热数量。</summary>
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

        /// <summary>验证配置 ID 非空，并返回原值用于组成池 ID 或查询配置。</summary>
        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Configured pool row id must be non-empty.", parameterName);
            }

            return value;
        }

        /// <summary>验证调用方提供了配置查询接口。</summary>
        private static void RequireProvider(IConfigProvider configProvider)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }
        }

        /// <summary>创建指向指定 Global 键及其数值要求的参数异常。</summary>
        private static ArgumentException InvalidGlobal(string key, string requirement)
        {
            return new ArgumentException(
                $"Configured Global row '{key}' {requirement}.",
                key);
        }
    }
}
