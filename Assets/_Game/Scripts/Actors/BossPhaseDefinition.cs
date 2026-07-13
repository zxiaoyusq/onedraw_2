using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    public readonly struct BossPhaseDefinition
    {
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

    public static class BossPhaseCatalog
    {
        private const double RatioTolerance = 0.000001d;

        public static IReadOnlyList<BossPhaseDefinition> Create(
            IConfigProvider configProvider,
            string enemyId,
            MovementStrategyRegistry movementRegistry = null,
            AttackStrategyRegistry attackRegistry = null)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            EnemyDefinition boss = EnemyDefinitionFactory.Create(configProvider, enemyId);
            if (boss.Tier != EnemyTier.Boss)
            {
                throw new ArgumentException(
                    $"Enemy '{boss.EnemyId}' must be tier Boss before phases are loaded.",
                    nameof(enemyId));
            }

            IReadOnlyList<BossPhaseConfig> configured =
                configProvider.GetBossPhases(boss.EnemyId);
            if (configured.Count == 0)
            {
                throw new ArgumentException(
                    $"Boss '{boss.EnemyId}' must configure at least one phase.",
                    nameof(configProvider));
            }

            var rows = new BossPhaseConfig[configured.Count];
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
            for (int index = 0; index < rows.Length; index++)
            {
                BossPhaseConfig row = rows[index];
                RequireNonEmpty(row.BossPhaseId, nameof(row.BossPhaseId), row.BossPhaseId);
                RequireNonEmpty(row.BossPhaseId, nameof(row.OnEnterEffectGroupId), row.OnEnterEffectGroupId);
                RequireNonEmpty(row.BossPhaseId, nameof(row.DescriptionKey), row.DescriptionKey);
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

        private static void ValidateCoverage(string enemyId, BossPhaseConfig[] phases)
        {
            if (!Approximately(phases[0].EnterHpRatio, 1d))
            {
                throw Invalid(
                    phases[0].BossPhaseId,
                    nameof(BossPhaseConfig.EnterHpRatio),
                    phases[0].EnterHpRatio,
                    $"Boss '{enemyId}' first phase must enter at HP ratio 1.");
            }

            if (!Approximately(phases[phases.Length - 1].ExitHpRatio, 0d))
            {
                throw Invalid(
                    phases[phases.Length - 1].BossPhaseId,
                    nameof(BossPhaseConfig.ExitHpRatio),
                    phases[phases.Length - 1].ExitHpRatio,
                    $"Boss '{enemyId}' last phase must exit at HP ratio 0.");
            }

            double previousExit = 1d;
            for (int index = 0; index < phases.Length; index++)
            {
                BossPhaseConfig phase = phases[index];
                long expectedOrder = index + 1L;
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
                if (phase.EnterHpRatio <= phase.ExitHpRatio)
                {
                    throw Invalid(
                        phase.BossPhaseId,
                        nameof(phase.ExitHpRatio),
                        phase.ExitHpRatio,
                        "Phase enter ratio must be greater than exit ratio.");
                }

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

        private static void RequireRatio(string phaseId, string field, double value)
        {
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

        private static void RequireNonEmpty(
            string phaseId,
            string field,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw Invalid(
                    phaseId,
                    field,
                    value,
                    "Configured phase string must be non-empty.");
            }
        }

        private static bool Approximately(double left, double right)
        {
            return Math.Abs(left - right) <= RatioTolerance;
        }

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

        private sealed class PhaseOrderComparer : IComparer<BossPhaseConfig>
        {
            public static readonly PhaseOrderComparer Instance =
                new PhaseOrderComparer();

            public int Compare(BossPhaseConfig left, BossPhaseConfig right)
            {
                if (ReferenceEquals(left, right)) return 0;
                if (left == null) return -1;
                if (right == null) return 1;
                int order = left.Order.CompareTo(right.Order);
                return order != 0
                    ? order
                    : string.CompareOrdinal(left.BossPhaseId, right.BossPhaseId);
            }
        }
    }

    public readonly struct BossPhaseTransition
    {
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

    public sealed class BossPhaseStateMachine
    {
        private static readonly IReadOnlyList<BossPhaseTransition> NoTransitions =
            Array.Empty<BossPhaseTransition>();

        private readonly IReadOnlyList<BossPhaseDefinition> phases;
        private int currentIndex = -1;
        private ulong nextSequence = 1UL;

        public BossPhaseStateMachine(IReadOnlyList<BossPhaseDefinition> configuredPhases)
        {
            phases = configuredPhases ??
                throw new ArgumentNullException(nameof(configuredPhases));
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

        public BossPhaseTransition Start(double hpRatio)
        {
            ValidateRatio(hpRatio);
            if (IsStarted)
            {
                throw new InvalidOperationException(
                    "Boss phase state machine can only start once.");
            }

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

        public IReadOnlyList<BossPhaseTransition> Advance(double hpRatio)
        {
            ValidateRatio(hpRatio);
            if (!IsStarted)
            {
                throw new InvalidOperationException(
                    "Boss phase state machine must start before it advances.");
            }

            if (currentIndex >= phases.Count - 1 ||
                hpRatio > phases[currentIndex].ExitHpRatio)
            {
                return NoTransitions;
            }

            var transitions = new List<BossPhaseTransition>();
            while (currentIndex < phases.Count - 1 &&
                   hpRatio <= phases[currentIndex].ExitHpRatio)
            {
                string previous = phases[currentIndex].BossPhaseId;
                currentIndex++;
                transitions.Add(CreateTransition(previous, phases[currentIndex], hpRatio));
            }

            return transitions.AsReadOnly();
        }

        private BossPhaseTransition CreateTransition(
            string previousPhaseId,
            in BossPhaseDefinition phase,
            double hpRatio)
        {
            ulong sequence = nextSequence;
            if (sequence == 0UL || sequence == ulong.MaxValue)
            {
                throw new OverflowException("Boss phase transition sequence is exhausted.");
            }

            nextSequence = sequence + 1UL;
            return new BossPhaseTransition(sequence, previousPhaseId, phase, hpRatio);
        }

        private static void ValidateRatio(double hpRatio)
        {
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
