using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    public static class EnemyAttackTriggerTypes
    {
        public const string Cooldown = "Cooldown";
        public const string Distance = "Distance";
        public const string Support = "Support";
        public const string HpThreshold = "HpThreshold";
    }

    public enum EnemyAttackActionKind
    {
        None = 0,
        Melee = 1,
        Projectile = 2,
        Charge = 3,
        Support = 4
    }

    public readonly struct EnemyAttackTriggerContext
    {
        public EnemyAttackTriggerContext(
            bool cooldownReady,
            bool targetInDistance,
            bool hpThresholdReached,
            string supportTargetId)
        {
            CooldownReady = cooldownReady;
            TargetInDistance = targetInDistance;
            HpThresholdReached = hpThresholdReached;
            SupportTargetId = supportTargetId ?? string.Empty;
            IsValid = true;
        }

        public bool CooldownReady { get; }

        public bool TargetInDistance { get; }

        public bool HpThresholdReached { get; }

        public string SupportTargetId { get; }

        public bool HasSupportTarget => !string.IsNullOrWhiteSpace(SupportTargetId);

        public bool IsValid { get; }
    }

    public readonly struct EnemyAttackDefinition
    {
        internal EnemyAttackDefinition(
            string attackId,
            string attackSetId,
            long order,
            string triggerType,
            long damage,
            string projectileId,
            string effectGroupId,
            double weight,
            EnemyAttackActionKind actionKind,
            EnemyAttackTimeline timeline)
        {
            AttackId = attackId;
            AttackSetId = attackSetId;
            Order = order;
            TriggerType = triggerType;
            Damage = damage;
            ProjectileId = projectileId ?? string.Empty;
            EffectGroupId = effectGroupId ?? string.Empty;
            Weight = weight;
            ActionKind = actionKind;
            Timeline = timeline;
            IsConfigured = true;
        }

        public string AttackId { get; }

        public string AttackSetId { get; }

        public long Order { get; }

        public string TriggerType { get; }

        public long Damage { get; }

        public string ProjectileId { get; }

        public string EffectGroupId { get; }

        public double Weight { get; }

        public EnemyAttackActionKind ActionKind { get; }

        public EnemyAttackTimeline Timeline { get; }

        public bool IsConfigured { get; }

        public EnemyAttackAction CreateAction(in EnemyAttackTriggerContext context)
        {
            if (!context.IsValid)
            {
                throw new ArgumentException(
                    "Enemy attack trigger context must be initialized.",
                    nameof(context));
            }

            string supportTargetId = ActionKind == EnemyAttackActionKind.Support
                ? context.SupportTargetId
                : string.Empty;
            if (ActionKind == EnemyAttackActionKind.Support &&
                string.IsNullOrWhiteSpace(supportTargetId))
            {
                throw new InvalidOperationException(
                    $"Support attack '{AttackId}' requires a live support target.");
            }

            return new EnemyAttackAction(
                AttackId,
                ActionKind,
                Damage,
                ProjectileId,
                EffectGroupId,
                supportTargetId);
        }
    }

    public readonly struct EnemyAttackAction
    {
        internal EnemyAttackAction(
            string attackId,
            EnemyAttackActionKind kind,
            long damage,
            string projectileId,
            string effectGroupId,
            string supportTargetId)
        {
            AttackId = attackId;
            Kind = kind;
            Damage = damage;
            ProjectileId = projectileId ?? string.Empty;
            EffectGroupId = effectGroupId ?? string.Empty;
            SupportTargetId = supportTargetId ?? string.Empty;
            IsConfigured = true;
        }

        public string AttackId { get; }

        public EnemyAttackActionKind Kind { get; }

        public long Damage { get; }

        public string ProjectileId { get; }

        public string EffectGroupId { get; }

        public string SupportTargetId { get; }

        public bool IsConfigured { get; }
    }

    public interface IEnemyAttackStrategy
    {
        string TriggerType { get; }

        bool IsEligible(in EnemyAttackTriggerContext context);
    }

    public sealed class AttackStrategyRegistry
    {
        private readonly Dictionary<string, IEnemyAttackStrategy> strategies =
            new Dictionary<string, IEnemyAttackStrategy>(StringComparer.Ordinal);

        public AttackStrategyRegistry(IEnumerable<IEnemyAttackStrategy> configuredStrategies)
        {
            if (configuredStrategies == null)
            {
                throw new ArgumentNullException(nameof(configuredStrategies));
            }

            foreach (IEnemyAttackStrategy strategy in configuredStrategies)
            {
                if (strategy == null || string.IsNullOrWhiteSpace(strategy.TriggerType))
                {
                    throw new ArgumentException(
                        "Attack strategies and their trigger types must be non-null.",
                        nameof(configuredStrategies));
                }

                if (!strategies.TryAdd(strategy.TriggerType, strategy))
                {
                    throw new ArgumentException(
                        $"Attack strategy '{strategy.TriggerType}' is registered more than once.",
                        nameof(configuredStrategies));
                }
            }
        }

        public static AttackStrategyRegistry CreateDefault()
        {
            return new AttackStrategyRegistry(new IEnemyAttackStrategy[]
            {
                new CooldownAttackStrategy(),
                new DistanceAttackStrategy(),
                new SupportAttackStrategy(),
                new HpThresholdAttackStrategy(),
            });
        }

        public IEnemyAttackStrategy Get(string triggerType)
        {
            if (triggerType != null && strategies.TryGetValue(triggerType, out IEnemyAttackStrategy strategy))
            {
                return strategy;
            }

            throw new KeyNotFoundException(
                $"No enemy attack strategy is registered for trigger type '{triggerType}'.");
        }

        public bool IsEligible(
            in EnemyAttackDefinition attack,
            in EnemyAttackTriggerContext context)
        {
            if (!attack.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy attack definition must be configured.",
                    nameof(attack));
            }

            if (!context.IsValid)
            {
                throw new ArgumentException(
                    "Enemy attack trigger context must be initialized.",
                    nameof(context));
            }

            return Get(attack.TriggerType).IsEligible(context);
        }

        public EnemyAttackDefinition Select(
            IReadOnlyList<EnemyAttackDefinition> attacks,
            in EnemyAttackTriggerContext context,
            double unitSelection)
        {
            if (attacks == null)
            {
                throw new ArgumentNullException(nameof(attacks));
            }

            if (double.IsNaN(unitSelection) ||
                double.IsInfinity(unitSelection) ||
                unitSelection < 0d ||
                unitSelection >= 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitSelection),
                    unitSelection,
                    "Attack selection must be a finite value in [0, 1).");
            }

            double totalWeight = 0d;
            for (int index = 0; index < attacks.Count; index++)
            {
                EnemyAttackDefinition attack = attacks[index];
                if (IsEligible(attack, context))
                {
                    totalWeight += attack.Weight;
                }
            }

            if (totalWeight <= 0d || double.IsInfinity(totalWeight))
            {
                return default;
            }

            double target = unitSelection * totalWeight;
            double accumulated = 0d;
            EnemyAttackDefinition lastEligible = default;
            for (int index = 0; index < attacks.Count; index++)
            {
                EnemyAttackDefinition attack = attacks[index];
                if (!IsEligible(attack, context))
                {
                    continue;
                }

                lastEligible = attack;
                accumulated += attack.Weight;
                if (target < accumulated)
                {
                    return attack;
                }
            }

            return lastEligible;
        }
    }

    public static class EnemyAttackDefinitionFactory
    {
        public static IReadOnlyList<EnemyAttackDefinition> Create(
            IConfigProvider configProvider,
            string attackSetId,
            AttackStrategyRegistry registry = null)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (string.IsNullOrWhiteSpace(attackSetId))
            {
                throw new ArgumentException(
                    "Attack set id must be non-empty.",
                    nameof(attackSetId));
            }

            AttackStrategyRegistry resolvedRegistry = registry ??
                AttackStrategyRegistry.CreateDefault();
            IReadOnlyList<EnemyAttackConfig> configured =
                configProvider.GetEnemyAttacks(attackSetId);
            var rows = new EnemyAttackConfig[configured.Count];
            for (int index = 0; index < configured.Count; index++)
            {
                rows[index] = configured[index] ??
                    throw new ArgumentException(
                        $"Attack set '{attackSetId}' contains a null row.",
                        nameof(configProvider));
            }

            Array.Sort(rows, AttackOrderComparer.Instance);
            var definitions = new EnemyAttackDefinition[rows.Length];
            for (int index = 0; index < rows.Length; index++)
            {
                EnemyAttackConfig row = rows[index];
                long expectedOrder = index + 1L;
                if (!string.Equals(row.AttackSetId, attackSetId, StringComparison.Ordinal) ||
                    row.Order != expectedOrder)
                {
                    throw new ArgumentException(
                        $"Attack set '{attackSetId}' must have contiguous order starting at 1.",
                        nameof(configProvider));
                }

                resolvedRegistry.Get(row.TriggerType);
                ValidateRow(configProvider, row);
                EnemyAttackActionKind kind = ResolveActionKind(row);
                definitions[index] = new EnemyAttackDefinition(
                    row.AttackId,
                    row.AttackSetId,
                    row.Order,
                    row.TriggerType,
                    row.Damage,
                    row.ProjectileId,
                    row.EffectGroupId,
                    row.Weight,
                    kind,
                    EnemyAttackTimelineFactory.Create(row));
            }

            return Array.AsReadOnly(definitions);
        }

        private static void ValidateRow(IConfigProvider configProvider, EnemyAttackConfig row)
        {
            if (string.IsNullOrWhiteSpace(row.AttackId) ||
                string.IsNullOrWhiteSpace(row.EffectGroupId) ||
                row.Damage < 0L ||
                float.IsNaN(row.Weight) ||
                float.IsInfinity(row.Weight) ||
                row.Weight <= 0f)
            {
                throw new ArgumentException(
                    $"Enemy attack '{row.AttackId}' contains invalid strategy values.",
                    nameof(row));
            }

            if (!string.IsNullOrEmpty(row.ProjectileId))
            {
                ProjectileConfig projectile = configProvider.GetProjectile(row.ProjectileId);
                if (projectile.Damage != row.Damage)
                {
                    throw new ArgumentException(
                        $"Enemy attack '{row.AttackId}' damage must match projectile '{row.ProjectileId}'.",
                        nameof(row));
                }
            }
        }

        private static EnemyAttackActionKind ResolveActionKind(EnemyAttackConfig row)
        {
            if (string.Equals(row.TriggerType, EnemyAttackTriggerTypes.Support, StringComparison.Ordinal))
            {
                if (row.Damage != 0L || !string.IsNullOrEmpty(row.ProjectileId))
                {
                    throw new ArgumentException(
                        $"Support attack '{row.AttackId}' cannot deal direct or projectile damage.",
                        nameof(row));
                }

                return EnemyAttackActionKind.Support;
            }

            if (!string.IsNullOrEmpty(row.ProjectileId))
            {
                return EnemyAttackActionKind.Projectile;
            }

            if (row.Damage <= 0L)
            {
                throw new ArgumentException(
                    $"Enemy attack '{row.AttackId}' must configure damage or a projectile.",
                    nameof(row));
            }

            if (string.Equals(row.TriggerType, EnemyAttackTriggerTypes.Distance, StringComparison.Ordinal) ||
                string.Equals(row.TriggerType, EnemyAttackTriggerTypes.HpThreshold, StringComparison.Ordinal))
            {
                return EnemyAttackActionKind.Charge;
            }

            return EnemyAttackActionKind.Melee;
        }

        private sealed class AttackOrderComparer : IComparer<EnemyAttackConfig>
        {
            public static readonly AttackOrderComparer Instance = new AttackOrderComparer();

            public int Compare(EnemyAttackConfig left, EnemyAttackConfig right)
            {
                int order = left.Order.CompareTo(right.Order);
                return order != 0
                    ? order
                    : string.CompareOrdinal(left.AttackId, right.AttackId);
            }
        }
    }

    public sealed class CooldownAttackStrategy : IEnemyAttackStrategy
    {
        public string TriggerType => EnemyAttackTriggerTypes.Cooldown;

        public bool IsEligible(in EnemyAttackTriggerContext context) => context.CooldownReady;
    }

    public sealed class DistanceAttackStrategy : IEnemyAttackStrategy
    {
        public string TriggerType => EnemyAttackTriggerTypes.Distance;

        public bool IsEligible(in EnemyAttackTriggerContext context) => context.TargetInDistance;
    }

    public sealed class SupportAttackStrategy : IEnemyAttackStrategy
    {
        public string TriggerType => EnemyAttackTriggerTypes.Support;

        public bool IsEligible(in EnemyAttackTriggerContext context) => context.HasSupportTarget;
    }

    public sealed class HpThresholdAttackStrategy : IEnemyAttackStrategy
    {
        public string TriggerType => EnemyAttackTriggerTypes.HpThreshold;

        public bool IsEligible(in EnemyAttackTriggerContext context) => context.HpThresholdReached;
    }
}
