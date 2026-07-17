using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    // 定义 EnemyAttackTriggerTypes 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public static class EnemyAttackTriggerTypes
    {
        public const string Cooldown = "Cooldown";
        public const string Distance = "Distance";
        public const string Support = "Support";
        public const string HpThreshold = "HpThreshold";
    }

    // 定义 EnemyAttackActionKind 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum EnemyAttackActionKind
    {
        None = 0,
        Melee = 1,
        Projectile = 2,
        Charge = 3,
        Support = 4
    }

    // 定义 EnemyAttackTriggerContext 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyAttackTriggerContext
    {
        // 初始化 EnemyAttackTriggerContext，并建立角色运行时所需的初始状态。
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

    // 定义 EnemyAttackDefinition 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyAttackDefinition
    {
        // 初始化 EnemyAttackDefinition，并建立角色运行时所需的初始状态。
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

        // 创建 CreateAction 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyAttackAction CreateAction(in EnemyAttackTriggerContext context)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!context.IsValid)
            {
                throw new ArgumentException(
                    "Enemy attack trigger context must be initialized.",
                    nameof(context));
            }

            string supportTargetId = ActionKind == EnemyAttackActionKind.Support
                ? context.SupportTargetId
                : string.Empty;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

    // 定义 EnemyAttackAction 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyAttackAction
    {
        // 初始化 EnemyAttackAction，并建立角色运行时所需的初始状态。
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

    // 定义 IEnemyAttackStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public interface IEnemyAttackStrategy
    {
        string TriggerType { get; }

        bool IsEligible(in EnemyAttackTriggerContext context);
    }

    // 定义 AttackStrategyRegistry 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class AttackStrategyRegistry
    {
        private readonly Dictionary<string, IEnemyAttackStrategy> strategies =
            new Dictionary<string, IEnemyAttackStrategy>(StringComparer.Ordinal);

        // 初始化 AttackStrategyRegistry，并建立角色运行时所需的初始状态。
        public AttackStrategyRegistry(IEnumerable<IEnemyAttackStrategy> configuredStrategies)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configuredStrategies == null)
            {
                throw new ArgumentNullException(nameof(configuredStrategies));
            }

            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            foreach (IEnemyAttackStrategy strategy in configuredStrategies)
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (strategy == null || string.IsNullOrWhiteSpace(strategy.TriggerType))
                {
                    throw new ArgumentException(
                        "Attack strategies and their trigger types must be non-null.",
                        nameof(configuredStrategies));
                }

                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (!strategies.TryAdd(strategy.TriggerType, strategy))
                {
                    throw new ArgumentException(
                        $"Attack strategy '{strategy.TriggerType}' is registered more than once.",
                        nameof(configuredStrategies));
                }
            }
        }

        // 创建 CreateDefault 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 获取 Get 对应的角色逻辑，并返回或发布一致的状态结果。
        public IEnemyAttackStrategy Get(string triggerType)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (triggerType != null && strategies.TryGetValue(triggerType, out IEnemyAttackStrategy strategy))
            {
                return strategy;
            }

            throw new KeyNotFoundException(
                $"No enemy attack strategy is registered for trigger type '{triggerType}'.");
        }

        // 判断是否 IsEligible 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool IsEligible(
            in EnemyAttackDefinition attack,
            in EnemyAttackTriggerContext context)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!attack.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy attack definition must be configured.",
                    nameof(attack));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!context.IsValid)
            {
                throw new ArgumentException(
                    "Enemy attack trigger context must be initialized.",
                    nameof(context));
            }

            return Get(attack.TriggerType).IsEligible(context);
        }

        // 选择 Select 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyAttackDefinition Select(
            IReadOnlyList<EnemyAttackDefinition> attacks,
            in EnemyAttackTriggerContext context,
            double unitSelection)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (attacks == null)
            {
                throw new ArgumentNullException(nameof(attacks));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < attacks.Count; index++)
            {
                EnemyAttackDefinition attack = attacks[index];
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (IsEligible(attack, context))
                {
                    totalWeight += attack.Weight;
                }
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (totalWeight <= 0d || double.IsInfinity(totalWeight))
            {
                return default;
            }

            double target = unitSelection * totalWeight;
            double accumulated = 0d;
            EnemyAttackDefinition lastEligible = default;
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < attacks.Count; index++)
            {
                EnemyAttackDefinition attack = attacks[index];
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (!IsEligible(attack, context))
                {
                    continue;
                }

                lastEligible = attack;
                accumulated += attack.Weight;
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (target < accumulated)
                {
                    return attack;
                }
            }

            return lastEligible;
        }
    }

    // 定义 EnemyAttackDefinitionFactory 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public static class EnemyAttackDefinitionFactory
    {
        // 创建 Create 对应的角色逻辑，并返回或发布一致的状态结果。
        public static IReadOnlyList<EnemyAttackDefinition> Create(
            IConfigProvider configProvider,
            string attackSetId,
            AttackStrategyRegistry registry = null)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < configured.Count; index++)
            {
                rows[index] = configured[index] ??
                    throw new ArgumentException(
                        $"Attack set '{attackSetId}' contains a null row.",
                        nameof(configProvider));
            }

            Array.Sort(rows, AttackOrderComparer.Instance);
            var definitions = new EnemyAttackDefinition[rows.Length];
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < rows.Length; index++)
            {
                EnemyAttackConfig row = rows[index];
                long expectedOrder = index + 1L;
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 校验 ValidateRow 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateRow(IConfigProvider configProvider, EnemyAttackConfig row)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!string.IsNullOrEmpty(row.ProjectileId))
            {
                ProjectileConfig projectile = configProvider.GetProjectile(row.ProjectileId);
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (projectile.Damage != row.Damage)
                {
                    throw new ArgumentException(
                        $"Enemy attack '{row.AttackId}' damage must match projectile '{row.ProjectileId}'.",
                        nameof(row));
                }
            }
        }

        // 解析 ResolveActionKind 对应的角色逻辑，并返回或发布一致的状态结果。
        private static EnemyAttackActionKind ResolveActionKind(EnemyAttackConfig row)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.Equals(row.TriggerType, EnemyAttackTriggerTypes.Support, StringComparison.Ordinal))
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (row.Damage != 0L || !string.IsNullOrEmpty(row.ProjectileId))
                {
                    throw new ArgumentException(
                        $"Support attack '{row.AttackId}' cannot deal direct or projectile damage.",
                        nameof(row));
                }

                return EnemyAttackActionKind.Support;
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!string.IsNullOrEmpty(row.ProjectileId))
            {
                return EnemyAttackActionKind.Projectile;
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (row.Damage <= 0L)
            {
                throw new ArgumentException(
                    $"Enemy attack '{row.AttackId}' must configure damage or a projectile.",
                    nameof(row));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.Equals(row.TriggerType, EnemyAttackTriggerTypes.Distance, StringComparison.Ordinal) ||
                string.Equals(row.TriggerType, EnemyAttackTriggerTypes.HpThreshold, StringComparison.Ordinal))
            {
                return EnemyAttackActionKind.Charge;
            }

            return EnemyAttackActionKind.Melee;
        }

        // 定义 AttackOrderComparer 的角色领域数据与行为边界，供上层流程以明确契约使用。
        private sealed class AttackOrderComparer : IComparer<EnemyAttackConfig>
        {
            public static readonly AttackOrderComparer Instance = new AttackOrderComparer();

            // 比较 Compare 对应的角色逻辑，并返回或发布一致的状态结果。
            public int Compare(EnemyAttackConfig left, EnemyAttackConfig right)
            {
                int order = left.Order.CompareTo(right.Order);
                return order != 0
                    ? order
                    : string.CompareOrdinal(left.AttackId, right.AttackId);
            }
        }
    }

    // 定义 CooldownAttackStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class CooldownAttackStrategy : IEnemyAttackStrategy
    {
        public string TriggerType => EnemyAttackTriggerTypes.Cooldown;

        // 判断是否 IsEligible 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool IsEligible(in EnemyAttackTriggerContext context) => context.CooldownReady;
    }

    // 定义 DistanceAttackStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class DistanceAttackStrategy : IEnemyAttackStrategy
    {
        public string TriggerType => EnemyAttackTriggerTypes.Distance;

        // 判断是否 IsEligible 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool IsEligible(in EnemyAttackTriggerContext context) => context.TargetInDistance;
    }

    // 定义 SupportAttackStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class SupportAttackStrategy : IEnemyAttackStrategy
    {
        public string TriggerType => EnemyAttackTriggerTypes.Support;

        // 判断是否 IsEligible 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool IsEligible(in EnemyAttackTriggerContext context) => context.HasSupportTarget;
    }

    // 定义 HpThresholdAttackStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class HpThresholdAttackStrategy : IEnemyAttackStrategy
    {
        public string TriggerType => EnemyAttackTriggerTypes.HpThreshold;

        // 判断是否 IsEligible 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool IsEligible(in EnemyAttackTriggerContext context) => context.HpThresholdReached;
    }
}
