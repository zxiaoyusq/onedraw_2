using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Combat
{
    public static class StrokeHitSettingsFactory
    {
        private const int TechnicalHitboxesPerTarget = 2;

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
            int maximumExpectedHitboxes = checked(maximumUniqueTargets * TechnicalHitboxesPerTarget);
            return new StrokeHitResolverSettings(
                maximumUniqueTargets,
                checked(maximumExpectedHitboxes + 1));
        }

        public static StrokeHitRule CreateRule(StrokeRuleConfig strokeRule)
        {
            if (strokeRule == null)
            {
                throw new ArgumentNullException(nameof(strokeRule));
            }

            return new StrokeHitRule(strokeRule.RuleId, strokeRule.HitRadiusRefPx);
        }

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
