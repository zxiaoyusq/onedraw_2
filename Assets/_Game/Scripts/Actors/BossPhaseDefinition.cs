using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    // 定义 BossPhaseDefinition 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct BossPhaseDefinition
    {
        // 初始化 BossPhaseDefinition，并建立角色运行时所需的初始状态。
        internal BossPhaseDefinition(
            string bossPhaseId,
            string enemyId,
            long order,
            double enterHpRatio,
            double exitHpRatio,
            string onEnterEffectGroupId,
            string descriptionKey,
            string descriptionZhCN,
            string descriptionEnUS,
            in EnemyDefinition combatProfile,
            in EnemyMovementDefinition movement,
            IReadOnlyList<EnemyAttackDefinition> attacks,
            in EnemyDefenseRule defense)
        {
            BossPhaseId = bossPhaseId;
            EnemyId = enemyId;
            Order = order;
            EnterHpRatio = enterHpRatio;
            ExitHpRatio = exitHpRatio;
            OnEnterEffectGroupId = onEnterEffectGroupId;
            DescriptionKey = descriptionKey;
            DescriptionZhCN = descriptionZhCN;
            DescriptionEnUS = descriptionEnUS;
            CombatProfile = combatProfile;
            Movement = movement;
            Attacks = attacks ?? Array.Empty<EnemyAttackDefinition>();
            Defense = defense;
            IsConfigured = true;
        }

        public string BossPhaseId { get; }

        public string EnemyId { get; }

        public long Order { get; }

        public double EnterHpRatio { get; }

        public double ExitHpRatio { get; }

        public string OnEnterEffectGroupId { get; }

        public string DescriptionKey { get; }

        public string DescriptionZhCN { get; }

        public string DescriptionEnUS { get; }

        public EnemyDefinition CombatProfile { get; }

        public EnemyMovementDefinition Movement { get; }

        public IReadOnlyList<EnemyAttackDefinition> Attacks { get; }

        public EnemyDefenseRule Defense { get; }

        public bool IsConfigured { get; }
    }

    // 定义 BossPhaseCatalog 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public static class BossPhaseCatalog
    {
        private const double RatioTolerance = 0.000001d;

        // 创建 Create 对应的角色逻辑，并返回或发布一致的状态结果。
        public static IReadOnlyList<BossPhaseDefinition> Create(
            IConfigProvider configProvider,
            string enemyId,
            MovementStrategyRegistry movementRegistry = null,
            AttackStrategyRegistry attackRegistry = null)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            EnemyDefinition boss = EnemyDefinitionFactory.Create(configProvider, enemyId);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (boss.Tier != EnemyTier.Boss)
            {
                throw new ArgumentException(
                    $"Enemy '{boss.EnemyId}' must be tier Boss before phases are loaded.",
                    nameof(enemyId));
            }

            IReadOnlyList<BossPhaseConfig> configured =
                configProvider.GetBossPhases(boss.EnemyId);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configured.Count == 0)
            {
                throw new ArgumentException(
                    $"Boss '{boss.EnemyId}' must configure at least one phase.",
                    nameof(configProvider));
            }

            var rows = new BossPhaseConfig[configured.Count];
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < configured.Count; index++)
            {
                rows[index] = configured[index] ??
                    throw new ArgumentException(
                        $"Boss '{boss.EnemyId}' contains a null phase row.",
                        nameof(configProvider));
            }

            Array.Sort(rows, PhaseOrderComparer.Instance);
            ValidateCoverage(boss.EnemyId, rows);
            MovementStrategyRegistry resolvedMovement = movementRegistry ??
                MovementStrategyRegistry.CreateDefault();
            AttackStrategyRegistry resolvedAttack = attackRegistry ??
                AttackStrategyRegistry.CreateDefault();
            var defenseService = new DefenseRuleService(configProvider);
            var phases = new BossPhaseDefinition[rows.Length];
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < rows.Length; index++)
            {
                BossPhaseConfig row = rows[index];
                RequireNonEmpty(row.BossPhaseId, nameof(row.BossPhaseId), row.BossPhaseId);
                RequireNonEmpty(row.BossPhaseId, nameof(row.OnEnterEffectGroupId), row.OnEnterEffectGroupId);
                RequireNonEmpty(row.BossPhaseId, nameof(row.DescriptionKey), row.DescriptionKey);
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (configProvider.GetSkillEffects(row.OnEnterEffectGroupId).Count == 0)
                {
                    throw Invalid(
                        row.BossPhaseId,
                        nameof(row.OnEnterEffectGroupId),
                        row.OnEnterEffectGroupId,
                        "Phase entry effect group must contain at least one effect.");
                }

                TextConfig description = configProvider.GetText(row.DescriptionKey);
                EnemyDefinition profile = EnemyDefinitionFactory.CreateBossPhase(
                    configProvider,
                    boss.EnemyId,
                    row.MovementPatternId,
                    row.AttackSetId,
                    row.DefenseRuleId,
                    row.WeakpointRuleId);
                EnemyMovementDefinition movement = EnemyMovementDefinitionFactory.Create(
                    configProvider,
                    profile,
                    resolvedMovement);
                IReadOnlyList<EnemyAttackDefinition> attacks =
                    EnemyAttackDefinitionFactory.Create(
                        configProvider,
                        profile.AttackSetId,
                        resolvedAttack);
                phases[index] = new BossPhaseDefinition(
                    row.BossPhaseId,
                    row.EnemyId,
                    row.Order,
                    row.EnterHpRatio,
                    row.ExitHpRatio,
                    row.OnEnterEffectGroupId,
                    row.DescriptionKey,
                    description.ZhCN,
                    description.EnUS,
                    profile,
                    movement,
                    attacks,
                    defenseService.Get(row.DefenseRuleId));
            }

            return Array.AsReadOnly(phases);
        }

        // 校验 ValidateCoverage 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateCoverage(string enemyId, BossPhaseConfig[] phases)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!Approximately(phases[0].EnterHpRatio, 1d))
            {
                throw Invalid(
                    phases[0].BossPhaseId,
                    nameof(BossPhaseConfig.EnterHpRatio),
                    phases[0].EnterHpRatio,
                    $"Boss '{enemyId}' first phase must enter at HP ratio 1.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!Approximately(phases[phases.Length - 1].ExitHpRatio, 0d))
            {
                throw Invalid(
                    phases[phases.Length - 1].BossPhaseId,
                    nameof(BossPhaseConfig.ExitHpRatio),
                    phases[phases.Length - 1].ExitHpRatio,
                    $"Boss '{enemyId}' last phase must exit at HP ratio 0.");
            }

            double previousExit = 1d;
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < phases.Length; index++)
            {
                BossPhaseConfig phase = phases[index];
                long expectedOrder = index + 1L;
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (!string.Equals(phase.EnemyId, enemyId, StringComparison.Ordinal) ||
                    phase.Order != expectedOrder)
                {
                    throw Invalid(
                        phase.BossPhaseId,
                        nameof(phase.Order),
                        phase.Order,
                        $"Boss '{enemyId}' phase order must be contiguous from 1.");
                }

                RequireRatio(phase.BossPhaseId, nameof(phase.EnterHpRatio), phase.EnterHpRatio);
                RequireRatio(phase.BossPhaseId, nameof(phase.ExitHpRatio), phase.ExitHpRatio);
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (phase.EnterHpRatio <= phase.ExitHpRatio)
                {
                    throw Invalid(
                        phase.BossPhaseId,
                        nameof(phase.ExitHpRatio),
                        phase.ExitHpRatio,
                        "Phase enter ratio must be greater than exit ratio.");
                }

                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (!Approximately(previousExit, phase.EnterHpRatio))
                {
                    throw Invalid(
                        phase.BossPhaseId,
                        nameof(phase.EnterHpRatio),
                        phase.EnterHpRatio,
                        $"Boss '{enemyId}' phases must have continuous HP coverage.");
                }

                previousExit = phase.ExitHpRatio;
            }
        }

        // 处理 RequireRatio 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireRatio(string phaseId, string field, double value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value < 0d ||
                value > 1d)
            {
                throw Invalid(
                    phaseId,
                    field,
                    value,
                    "Boss phase HP ratio must be finite and inside [0, 1].");
            }
        }

        // 处理 RequireNonEmpty 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireNonEmpty(
            string phaseId,
            string field,
            string value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(value))
            {
                throw Invalid(
                    phaseId,
                    field,
                    value,
                    "Configured phase string must be non-empty.");
            }
        }

        // 处理 Approximately 对应的角色逻辑，并返回或发布一致的状态结果。
        private static bool Approximately(double left, double right)
        {
            return Math.Abs(left - right) <= RatioTolerance;
        }

        // 处理 Invalid 对应的角色逻辑，并返回或发布一致的状态结果。
        private static ArgumentException Invalid(
            string phaseId,
            string field,
            object value,
            string message)
        {
            return new ArgumentException(
                $"Boss phase '{phaseId}.{field}' value '{value}' is invalid. {message}",
                field);
        }

        // 定义 PhaseOrderComparer 的角色领域数据与行为边界，供上层流程以明确契约使用。
        private sealed class PhaseOrderComparer : IComparer<BossPhaseConfig>
        {
            public static readonly PhaseOrderComparer Instance =
                new PhaseOrderComparer();

            // 比较 Compare 对应的角色逻辑，并返回或发布一致的状态结果。
            public int Compare(BossPhaseConfig left, BossPhaseConfig right)
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (ReferenceEquals(left, right)) return 0;
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (left == null) return -1;
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (right == null) return 1;
                int order = left.Order.CompareTo(right.Order);
                return order != 0
                    ? order
                    : string.CompareOrdinal(left.BossPhaseId, right.BossPhaseId);
            }
        }
    }

    // 定义 BossPhaseTransition 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct BossPhaseTransition
    {
        // 初始化 BossPhaseTransition，并建立角色运行时所需的初始状态。
        internal BossPhaseTransition(
            ulong sequence,
            string previousPhaseId,
            in BossPhaseDefinition currentPhase,
            double hpRatio)
        {
            Sequence = sequence;
            PreviousPhaseId = previousPhaseId ?? string.Empty;
            CurrentPhase = currentPhase;
            HpRatio = hpRatio;
            IsValid = true;
        }

        public ulong Sequence { get; }

        public string PreviousPhaseId { get; }

        public BossPhaseDefinition CurrentPhase { get; }

        public double HpRatio { get; }

        public bool IsValid { get; }
    }

    // 定义 BossPhaseStateMachine 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class BossPhaseStateMachine
    {
        private static readonly IReadOnlyList<BossPhaseTransition> NoTransitions =
            Array.Empty<BossPhaseTransition>();

        private readonly IReadOnlyList<BossPhaseDefinition> phases;
        private int currentIndex = -1;
        private ulong nextSequence = 1UL;

        // 初始化 BossPhaseStateMachine，并建立角色运行时所需的初始状态。
        public BossPhaseStateMachine(IReadOnlyList<BossPhaseDefinition> configuredPhases)
        {
            phases = configuredPhases ??
                throw new ArgumentNullException(nameof(configuredPhases));
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (phases.Count == 0)
            {
                throw new ArgumentException(
                    "Boss phase state machine requires at least one phase.",
                    nameof(configuredPhases));
            }
        }

        public bool IsStarted => currentIndex >= 0;

        public BossPhaseDefinition Current => IsStarted
            ? phases[currentIndex]
            : default;

        // 处理 Start 对应的角色逻辑，并返回或发布一致的状态结果。
        public BossPhaseTransition Start(double hpRatio)
        {
            ValidateRatio(hpRatio);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (IsStarted)
            {
                throw new InvalidOperationException(
                    "Boss phase state machine can only start once.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (hpRatio <= 0d || hpRatio > phases[0].EnterHpRatio)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hpRatio),
                    hpRatio,
                    "A living Boss must start inside the first configured phase.");
            }

            currentIndex = 0;
            return CreateTransition(string.Empty, phases[0], hpRatio);
        }

        // 处理 Advance 对应的角色逻辑，并返回或发布一致的状态结果。
        public IReadOnlyList<BossPhaseTransition> Advance(double hpRatio)
        {
            ValidateRatio(hpRatio);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!IsStarted)
            {
                throw new InvalidOperationException(
                    "Boss phase state machine must start before it advances.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentIndex >= phases.Count - 1 ||
                hpRatio > phases[currentIndex].ExitHpRatio)
            {
                return NoTransitions;
            }

            var transitions = new List<BossPhaseTransition>();
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            while (currentIndex < phases.Count - 1 &&
                   hpRatio <= phases[currentIndex].ExitHpRatio)
            {
                string previous = phases[currentIndex].BossPhaseId;
                currentIndex++;
                transitions.Add(CreateTransition(previous, phases[currentIndex], hpRatio));
            }

            return transitions.AsReadOnly();
        }

        // 创建 CreateTransition 对应的角色逻辑，并返回或发布一致的状态结果。
        private BossPhaseTransition CreateTransition(
            string previousPhaseId,
            in BossPhaseDefinition phase,
            double hpRatio)
        {
            ulong sequence = nextSequence;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (sequence == 0UL || sequence == ulong.MaxValue)
            {
                throw new OverflowException("Boss phase transition sequence is exhausted.");
            }

            nextSequence = sequence + 1UL;
            return new BossPhaseTransition(sequence, previousPhaseId, phase, hpRatio);
        }

        // 校验 ValidateRatio 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateRatio(double hpRatio)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(hpRatio) ||
                double.IsInfinity(hpRatio) ||
                hpRatio < 0d ||
                hpRatio > 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hpRatio),
                    hpRatio,
                    "Boss HP ratio must be finite and inside [0, 1].");
            }
        }
    }
}
