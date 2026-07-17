using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Combat
{
    /// <summary>从全局容量和笔势配置创建命中解析器与规则设置。</summary>
    public static class StrokeHitSettingsFactory
    {
        private const int TechnicalHitboxesPerTarget = 2;

        /// <summary>按敌人、投射物上限和每目标技术命中盒数计算预分配容量。</summary>
        public static StrokeHitResolverSettings CreateResolverSettings(IConfigProvider configProvider)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            int maximumEnemies = ReadPositiveGlobal(
                configProvider,
                ConfigIds.GlobalKeys.MaxActiveEnemies);
            int maximumProjectiles = ReadPositiveGlobal(
                configProvider,
                ConfigIds.GlobalKeys.MaxActiveProjectiles);
            int maximumUniqueTargets = checked(maximumEnemies + maximumProjectiles);
            // 额外一个槽位专门用于检测物理查询饱和，不能把满缓冲误当完整结果。
            int maximumExpectedHitboxes = checked(maximumUniqueTargets * TechnicalHitboxesPerTarget);
            return new StrokeHitResolverSettings(
                maximumUniqueTargets,
                checked(maximumExpectedHitboxes + 1));
        }

        /// <summary>从一条 StrokeRules 配置创建对应命中半径规则。</summary>
        public static StrokeHitRule CreateRule(StrokeRuleConfig strokeRule)
        {
            if (strokeRule == null)
            {
                throw new ArgumentNullException(nameof(strokeRule));
            }

            return new StrokeHitRule(strokeRule.RuleId, strokeRule.HitRadiusRefPx);
        }

        /// <summary>读取类型正确且能转换为正 int 的 Global 配置。</summary>
        private static int ReadPositiveGlobal(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            if (!string.Equals(row.ValueType, "int", StringComparison.Ordinal) ||
                !row.IntValue.HasValue ||
                row.IntValue.Value < 1 ||
                row.IntValue.Value > int.MaxValue)
            {
                throw new ArgumentException(
                    $"Global '{key}' must provide a positive runtime integer.",
                    nameof(configProvider));
            }

            return (int)row.IntValue.Value;
        }
    }
}
