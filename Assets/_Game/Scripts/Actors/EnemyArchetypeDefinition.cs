using System;
using System.Collections.Generic;
using System.Text;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    public readonly struct EnemyArchetypeDefinition
    {
        internal EnemyArchetypeDefinition(
            in EnemyDefinition enemy,
            in EnemyMovementDefinition movement,
            IReadOnlyList<EnemyAttackDefinition> attacks,
            in EnemyDefenseRule defense,
            string displayNameZhCN,
            string displayNameEnUS,
            string assetType,
            string teachingSignature)
        {
            Enemy = enemy;
            Movement = movement;
            Attacks = attacks ?? throw new ArgumentNullException(nameof(attacks));
            Defense = defense;
            DisplayNameZhCN = displayNameZhCN ?? string.Empty;
            DisplayNameEnUS = displayNameEnUS ?? string.Empty;
            AssetType = assetType ?? string.Empty;
            TeachingSignature = teachingSignature ?? string.Empty;
            IsConfigured = true;
        }

        public EnemyDefinition Enemy { get; }

        public EnemyMovementDefinition Movement { get; }

        public IReadOnlyList<EnemyAttackDefinition> Attacks { get; }

        public EnemyDefenseRule Defense { get; }

        public string DisplayNameZhCN { get; }

        public string DisplayNameEnUS { get; }

        public string AssetType { get; }

        public string TeachingSignature { get; }

        public bool IsConfigured { get; }
    }

    public static class EnemyArchetypeCatalog
    {
        public static IReadOnlyList<EnemyArchetypeDefinition> CreateCombatRoster(
            IConfigProvider configProvider,
            MovementStrategyRegistry movementRegistry = null,
            AttackStrategyRegistry attackRegistry = null)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            MovementStrategyRegistry resolvedMovement = movementRegistry ??
                MovementStrategyRegistry.CreateDefault();
            AttackStrategyRegistry resolvedAttack = attackRegistry ??
                AttackStrategyRegistry.CreateDefault();
            IReadOnlyList<EnemyConfig> configuredRows = configProvider.GetEnemies();
            var archetypes = new List<EnemyArchetypeDefinition>(configuredRows.Count);
            var teachingSignatures = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < configuredRows.Count; index++)
            {
                EnemyConfig row = configuredRows[index] ??
                    throw new ArgumentException(
                        "Configured enemy roster contains a null row.",
                        nameof(configProvider));
                if (string.Equals(row.Tier, "Boss", StringComparison.Ordinal))
                {
                    continue;
                }

                EnemyArchetypeDefinition archetype = Create(
                    configProvider,
                    row.EnemyId,
                    resolvedMovement,
                    resolvedAttack);
                if (!teachingSignatures.Add(archetype.TeachingSignature))
                {
                    throw new ArgumentException(
                        $"Enemy '{row.EnemyId}' duplicates an existing combat teaching signature.",
                        nameof(configProvider));
                }

                archetypes.Add(archetype);
            }

            archetypes.Sort(EnemyArchetypeComparer.Instance);
            return Array.AsReadOnly(archetypes.ToArray());
        }

        public static EnemyArchetypeDefinition Create(
            IConfigProvider configProvider,
            string enemyId,
            MovementStrategyRegistry movementRegistry = null,
            AttackStrategyRegistry attackRegistry = null)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy id must be non-empty.", nameof(enemyId));
            }

            MovementStrategyRegistry resolvedMovement = movementRegistry ??
                MovementStrategyRegistry.CreateDefault();
            AttackStrategyRegistry resolvedAttack = attackRegistry ??
                AttackStrategyRegistry.CreateDefault();
            EnemyConfig row = configProvider.GetEnemy(enemyId);
            EnemyDefinition enemy = EnemyDefinitionFactory.Create(configProvider, enemyId);
            EnemyMovementDefinition movement = EnemyMovementDefinitionFactory.Create(
                configProvider,
                enemyId,
                resolvedMovement);
            IReadOnlyList<EnemyAttackDefinition> attacks = EnemyAttackDefinitionFactory.Create(
                configProvider,
                enemy.AttackSetId,
                resolvedAttack);
            EnemyDefenseRule defense = new DefenseRuleService(configProvider).Get(
                row.DefenseRuleId);
            TextConfig displayName = configProvider.GetText(enemy.DisplayNameKey);
            AssetManifestConfig asset = configProvider.GetAsset(enemy.AssetKey);

            if (attacks.Count == 0)
            {
                throw new ArgumentException(
                    $"Enemy '{enemyId}' must configure at least one attack.",
                    nameof(configProvider));
            }

            for (int index = 0; index < attacks.Count; index++)
            {
                ValidateTelegraph(enemyId, attacks[index]);
            }

            if (string.IsNullOrWhiteSpace(displayName.ZhCN) ||
                string.IsNullOrWhiteSpace(displayName.EnUS))
            {
                throw new ArgumentException(
                    $"Enemy '{enemyId}' must have configured Chinese and English display names.",
                    nameof(configProvider));
            }

            if (!string.Equals(asset.AssetType, "Sprite", StringComparison.Ordinal) &&
                !string.Equals(asset.AssetType, "Prefab", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Enemy '{enemyId}' asset '{enemy.AssetKey}' must be a Sprite or Prefab.",
                    nameof(configProvider));
            }

            string teachingSignature = BuildTeachingSignature(
                configProvider,
                enemy,
                movement,
                attacks,
                defense);
            return new EnemyArchetypeDefinition(
                enemy,
                movement,
                attacks,
                defense,
                displayName.ZhCN,
                displayName.EnUS,
                asset.AssetType,
                teachingSignature);
        }

        private static void ValidateTelegraph(
            string enemyId,
            in EnemyAttackDefinition attack)
        {
            EnemyAttackTimeline timeline = attack.Timeline;
            if (timeline.WindupSeconds <= 0d ||
                timeline.InterruptStartSeconds >= timeline.WindupSeconds ||
                timeline.InterruptEndSeconds < timeline.WindupSeconds)
            {
                throw new ArgumentException(
                    $"Enemy '{enemyId}' attack '{attack.AttackId}' must expose a positive windup " +
                    "with an interrupt window spanning the execution boundary.",
                    nameof(attack));
            }
        }

        private static string BuildTeachingSignature(
            IConfigProvider configProvider,
            in EnemyDefinition enemy,
            in EnemyMovementDefinition movement,
            IReadOnlyList<EnemyAttackDefinition> attacks,
            in EnemyDefenseRule defense)
        {
            var signature = new StringBuilder(192);
            signature.Append("tier=").Append(enemy.Tier)
                .Append(";move=").Append(movement.PatternType)
                .Append(";vulnerability=").Append(enemy.StanceVulnerability)
                .Append(";defense=").Append(defense.RequiredGestureType)
                .Append('/').Append(defense.RequiredStanceId)
                .Append(";weakpoint=")
                .Append(enemy.Weakpoint.HasHitbox ? "timed" : "none")
                .Append('/').Append(enemy.Weakpoint.InterruptsAttack)
                .Append(";attacks=");
            for (int index = 0; index < attacks.Count; index++)
            {
                if (index > 0)
                {
                    signature.Append(',');
                }

                EnemyAttackDefinition attack = attacks[index];
                signature.Append(attack.TriggerType)
                    .Append('/').Append(attack.ActionKind)
                    .Append('/').Append(attack.Timeline.InterruptGestureType);
                if (!string.IsNullOrEmpty(attack.ProjectileId))
                {
                    ProjectileConfig projectile = configProvider.GetProjectile(
                        attack.ProjectileId);
                    signature.Append("/projectile:")
                        .Append(projectile.Cuttable)
                        .Append('/').Append(projectile.Reflectable)
                        .Append('/').Append(projectile.RequiredStanceId);
                }
            }

            return signature.ToString();
        }

        private sealed class EnemyArchetypeComparer : IComparer<EnemyArchetypeDefinition>
        {
            internal static readonly EnemyArchetypeComparer Instance =
                new EnemyArchetypeComparer();

            public int Compare(
                EnemyArchetypeDefinition left,
                EnemyArchetypeDefinition right)
            {
                return StringComparer.Ordinal.Compare(
                    left.Enemy.EnemyId,
                    right.Enemy.EnemyId);
            }
        }
    }
}
