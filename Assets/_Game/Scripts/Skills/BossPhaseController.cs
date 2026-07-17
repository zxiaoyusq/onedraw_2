using System;
using System.Collections.Generic;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    // 定义 BossPhaseChangedEvent 的技能领域契约，明确条件、目标或效果执行边界。
    public readonly struct BossPhaseChangedEvent
    {
        // 初始化 BossPhaseChangedEvent，并建立技能运行时所需的初始状态。
        internal BossPhaseChangedEvent(
            in BossPhaseTransition transition,
            in EnemyPhaseProfileResult profileResult,
            double timestamp,
            IReadOnlyList<SkillEffectStepResult> entryEffectSteps)
        {
            Transition = transition;
            ProfileResult = profileResult;
            Timestamp = timestamp;
            EntryEffectSteps = entryEffectSteps ?? Array.Empty<SkillEffectStepResult>();
            IsValid = true;
        }

        public BossPhaseTransition Transition { get; }

        public EnemyPhaseProfileResult ProfileResult { get; }

        public double Timestamp { get; }

        public IReadOnlyList<SkillEffectStepResult> EntryEffectSteps { get; }

        public bool IsValid { get; }
    }

    // 定义 BossPhaseController 的技能领域契约，明确条件、目标或效果执行边界。
    public sealed class BossPhaseController : IDisposable
    {
        private static readonly IReadOnlyList<BossPhaseTransition> NoTransitions =
            Array.Empty<BossPhaseTransition>();

        private readonly IConfigProvider configProvider;
        private readonly EnemyController boss;
        private readonly IEnemyAttackWorld attackWorld;
        private readonly SkillService skillService;
        private readonly BossPhaseEffectWorld effectWorld;
        private readonly IReadOnlyList<BossPhaseDefinition> phases;
        private readonly BossPhaseStateMachine stateMachine;
        private EnemyStrategyRuntime strategy;
        private double lastTimestamp;
        private double pendingTimestamp;
        private bool hasTimestamp;
        private bool subscribed;
        private bool applyingPhase;
        private bool pendingHpObservation;
        private bool ended;
        private bool disposed;

        // 初始化 BossPhaseController，并建立技能运行时所需的初始状态。
        public BossPhaseController(
            IConfigProvider configuredProvider,
            EnemyController bossController,
            IEnemyAttackWorld configuredAttackWorld,
            SkillService configuredSkillService,
            ISkillEffectWorld configuredEffectWorld)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            boss = bossController ??
                throw new ArgumentNullException(nameof(bossController));
            attackWorld = configuredAttackWorld ??
                throw new ArgumentNullException(nameof(configuredAttackWorld));
            skillService = configuredSkillService ??
                throw new ArgumentNullException(nameof(configuredSkillService));
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (!boss.IsAlive || boss.Definition.Tier != EnemyTier.Boss)
            {
                throw new ArgumentException(
                    "Boss controller must be spawned, alive, and tier Boss.",
                    nameof(bossController));
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (boss.State.State != EnemyState.Move)
            {
                throw new ArgumentException(
                    "Boss controller must complete Spawn before phases start.",
                    nameof(bossController));
            }

            var bossTarget = new EnemySkillEffectTarget(boss);
            effectWorld = new BossPhaseEffectWorld(
                configuredEffectWorld ??
                    throw new ArgumentNullException(nameof(configuredEffectWorld)),
                bossTarget);
            phases = BossPhaseCatalog.Create(configProvider, boss.Definition.EnemyId);
            stateMachine = new BossPhaseStateMachine(phases);
        }

        public event Action<BossPhaseChangedEvent> PhaseChanged;

        public bool IsStarted => stateMachine.IsStarted;

        public bool HasEnded => ended;

        public IReadOnlyList<BossPhaseDefinition> Phases => phases;

        public BossPhaseDefinition CurrentPhase => stateMachine.Current;

        public EnemyStrategyRuntime Strategy => strategy ??
            throw new InvalidOperationException(
                "Boss phase strategy is not active.");

        // 处理 Start 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public BossPhaseChangedEvent Start(double timestamp)
        {
            ThrowIfDisposed();
            ObserveTimestamp(timestamp);
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (IsStarted)
            {
                throw new InvalidOperationException(
                    "Boss phase controller can only start once.");
            }

            boss.CombatEventPublished += OnBossCombatEvent;
            subscribed = true;
            try
            {
                BossPhaseTransition transition =
                    stateMachine.Start(boss.Damage.HpRatio);
                BossPhaseChangedEvent phaseEvent =
                    ApplyTransition(transition, timestamp);
                ProcessPendingObservation(timestamp);
                return phaseEvent;
            }
            catch
            {
                Unsubscribe();
                strategy?.Dispose();
                strategy = null;
                throw;
            }
        }

        // 处理 ObserveCurrentHp 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public IReadOnlyList<BossPhaseTransition> ObserveCurrentHp(double timestamp)
        {
            ThrowIfDisposed();
            RequireStarted();
            ObserveTimestamp(timestamp);
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (ended || !boss.IsAlive || boss.Damage.IsDead)
            {
                return NoTransitions;
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (applyingPhase)
            {
                pendingHpObservation = true;
                pendingTimestamp = Math.Max(pendingTimestamp, timestamp);
                return NoTransitions;
            }

            IReadOnlyList<BossPhaseTransition> transitions =
                stateMachine.Advance(boss.Damage.HpRatio);
            ApplyTransitions(transitions, timestamp);
            ProcessPendingObservation(timestamp);
            return transitions;
        }

        // 处理 SampleMovement 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public EnemyMovementSample SampleMovement(double phaseElapsedSeconds)
        {
            ThrowIfDisposed();
            RequireActive();
            return Strategy.SampleMovement(phaseElapsedSeconds);
        }

        // 尝试执行 TryBeginAttack 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool TryBeginAttack(
            in EnemyAttackTriggerContext context,
            double unitSelection,
            double timestamp)
        {
            ThrowIfDisposed();
            RequireActive();
            ObserveTimestamp(timestamp);
            return Strategy.TryBeginAttack(context, unitSelection, timestamp);
        }

        // 按时间推进 Tick 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public int Tick(double timestamp)
        {
            ThrowIfDisposed();
            RequireActive();
            ObserveTimestamp(timestamp);
            return Strategy.Tick(timestamp);
        }

        // 释放 Dispose 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public void Dispose()
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (disposed)
            {
                return;
            }

            Unsubscribe();
            strategy?.Dispose();
            strategy = null;
            PhaseChanged = null;
            disposed = true;
        }

        // 应用 ApplyTransitions 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void ApplyTransitions(
            IReadOnlyList<BossPhaseTransition> transitions,
            double timestamp)
        {
            // 逐项处理技能目标或效果，保持配置顺序与执行结果稳定。
            for (int index = 0; index < transitions.Count; index++)
            {
                ApplyTransition(transitions[index], timestamp);
                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (ended || !boss.IsAlive)
                {
                    break;
                }
            }
        }

        // 应用 ApplyTransition 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private BossPhaseChangedEvent ApplyTransition(
            in BossPhaseTransition transition,
            double timestamp)
        {
            applyingPhase = true;
            try
            {
                strategy?.Dispose();
                strategy = null;
                EnemyPhaseProfileResult profile = boss.ApplyBossPhaseProfile(
                    transition.CurrentPhase.CombatProfile,
                    transition.CurrentPhase.BossPhaseId,
                    timestamp);
                strategy = new EnemyStrategyRuntime(
                    boss,
                    configProvider,
                    attackWorld);
                IReadOnlyList<SkillEffectStepResult> steps =
                    skillService.ExecuteEffectGroup(
                        transition.CurrentPhase.OnEnterEffectGroupId,
                        transition.CurrentPhase.BossPhaseId,
                        new SkillEffectContext(effectWorld, timestamp));
                var phaseEvent = new BossPhaseChangedEvent(
                    transition,
                    profile,
                    timestamp,
                    steps);
                PhaseChanged?.Invoke(phaseEvent);
                return phaseEvent;
            }
            finally
            {
                applyingPhase = false;
            }
        }

        // 处理 ProcessPendingObservation 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void ProcessPendingObservation(double fallbackTimestamp)
        {
            // 逐项处理技能目标或效果，保持配置顺序与执行结果稳定。
            while (pendingHpObservation && !ended && boss.IsAlive)
            {
                double timestamp = Math.Max(fallbackTimestamp, pendingTimestamp);
                pendingHpObservation = false;
                pendingTimestamp = 0d;
                IReadOnlyList<BossPhaseTransition> transitions =
                    stateMachine.Advance(boss.Damage.HpRatio);
                ApplyTransitions(transitions, timestamp);
                fallbackTimestamp = timestamp;
            }
        }

        // 响应 OnBossCombatEvent 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void OnBossCombatEvent(EnemyCombatEvent combatEvent)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (disposed)
            {
                return;
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (combatEvent.EventType == EnemyCombatEventType.HpChanged)
            {
                ObserveCurrentHp(combatEvent.Timestamp);
            }
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            else if (combatEvent.EventType == EnemyCombatEventType.Died)
            {
                ended = true;
                strategy?.Dispose();
                strategy = null;
            }
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            else if (combatEvent.EventType == EnemyCombatEventType.Released)
            {
                Dispose();
            }
        }

        // 处理 ObserveTimestamp 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void ObserveTimestamp(double timestamp)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (double.IsNaN(timestamp) ||
                double.IsInfinity(timestamp) ||
                timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    "Boss phase timestamp must be finite and non-negative.");
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (hasTimestamp && timestamp < lastTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    $"Boss phase timestamp cannot move backwards from {lastTimestamp}.");
            }

            lastTimestamp = timestamp;
            hasTimestamp = true;
        }

        // 处理 RequireStarted 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void RequireStarted()
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (!IsStarted)
            {
                throw new InvalidOperationException(
                    "Boss phase controller must start before use.");
            }
        }

        // 处理 RequireActive 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void RequireActive()
        {
            RequireStarted();
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (ended || strategy == null)
            {
                throw new InvalidOperationException(
                    "Boss phase controller has no active combat strategy.");
            }
        }

        // 处理 ThrowIfDisposed 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void ThrowIfDisposed()
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BossPhaseController));
            }
        }

        // 处理 Unsubscribe 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void Unsubscribe()
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (subscribed)
            {
                boss.CombatEventPublished -= OnBossCombatEvent;
                subscribed = false;
            }
        }

        // 定义 BossPhaseEffectWorld 的技能领域契约，明确条件、目标或效果执行边界。
        private sealed class BossPhaseEffectWorld : ISkillEffectWorld
        {
            private readonly ISkillEffectWorld inner;
            private readonly EnemySkillEffectTarget bossTarget;

            // 初始化 BossPhaseEffectWorld，并建立技能运行时所需的初始状态。
            public BossPhaseEffectWorld(
                ISkillEffectWorld configuredInner,
                EnemySkillEffectTarget configuredBossTarget)
            {
                inner = configuredInner;
                bossTarget = configuredBossTarget;
            }

            public IReadOnlyList<ISkillEffectTarget> Targets => inner.Targets;

            public ISkillEffectTarget PrimaryTarget => bossTarget;

            // 处理 RepeatLastStroke 对应的技能逻辑，并保持条件、目标与效果结果一致。
            public int RepeatLastStroke(
                float damageMultiplier,
                float delaySeconds,
                string sourceId,
                double timestamp)
            {
                return inner.RepeatLastStroke(
                    damageMultiplier,
                    delaySeconds,
                    sourceId,
                    timestamp);
            }

            // 设置 SetTimeScale 对应的技能逻辑，并保持条件、目标与效果结果一致。
            public int SetTimeScale(
                float scale,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                return inner.SetTimeScale(scale, durationSeconds, sourceId, timestamp);
            }

            // 设置 SetNextStrokeDamageMultiplier 对应的技能逻辑，并保持条件、目标与效果结果一致。
            public int SetNextStrokeDamageMultiplier(
                float multiplier,
                string sourceId,
                double timestamp)
            {
                return inner.SetNextStrokeDamageMultiplier(
                    multiplier,
                    sourceId,
                    timestamp);
            }

            // 清理 ClearHostileProjectiles 对应的技能逻辑，并保持条件、目标与效果结果一致。
            public int ClearHostileProjectiles(string sourceId, double timestamp)
            {
                return inner.ClearHostileProjectiles(sourceId, timestamp);
            }

            // 处理 PlayVfx 对应的技能逻辑，并保持条件、目标与效果结果一致。
            public void PlayVfx(
                string vfxKey,
                IReadOnlyList<ISkillEffectTarget> targets,
                string sourceId,
                double timestamp)
            {
                inner.PlayVfx(vfxKey, targets, sourceId, timestamp);
            }

            // 处理 PlayAudio 对应的技能逻辑，并保持条件、目标与效果结果一致。
            public void PlayAudio(string audioKey, string sourceId, double timestamp)
            {
                inner.PlayAudio(audioKey, sourceId, timestamp);
            }
        }
    }
}
