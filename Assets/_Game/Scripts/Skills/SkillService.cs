using System;
using System.Collections.Generic;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    // 定义 SkillService 的技能领域契约，明确条件、目标或效果执行边界。
    public sealed class SkillService
    {
        private static readonly IReadOnlyList<SkillEffectStepResult> NoSteps =
            Array.Empty<SkillEffectStepResult>();

        private readonly IConfigProvider configProvider;
        private readonly PlayerCombatController player;
        private readonly EffectExecutorRegistry registry;
        private readonly Dictionary<string, double> cooldownUntilBySkill =
            new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly List<ISkillEffectTarget> selectedTargets =
            new List<ISkillEffectTarget>();
        private double lastTimestamp;
        private bool hasTimestamp;

        // 初始化 SkillService，并建立技能运行时所需的初始状态。
        public SkillService(
            IConfigProvider configuredProvider,
            PlayerCombatController playerController,
            EffectExecutorRegistry executorRegistry = null)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            player = playerController ??
                throw new ArgumentNullException(nameof(playerController));
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (!player.IsInitialized)
            {
                throw new ArgumentException(
                    "Player combat controller must be initialized before SkillService.",
                    nameof(playerController));
            }

            registry = executorRegistry ?? EffectExecutorRegistry.CreateDefault(configProvider);
        }

        public EffectExecutorRegistry Executors => registry;

        // 获取 GetCooldownUntil 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public double GetCooldownUntil(string skillId)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentException("Skill id must be non-empty.", nameof(skillId));
            }

            return cooldownUntilBySkill.TryGetValue(skillId, out double cooldownUntil)
                ? cooldownUntil
                : 0d;
        }

        // 执行 ExecuteEffectGroup 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public IReadOnlyList<SkillEffectStepResult> ExecuteEffectGroup(
            string effectGroupId,
            string sourceId,
            SkillEffectContext effectContext)
        {
            ValidateEffectGroupRequest(effectGroupId, sourceId, effectContext);
            ObserveTimestamp(effectContext.Timestamp);
            SkillEffectConfig[] effects = PrepareEffectGroup(effectGroupId);
            return ExecutePreparedEffects(effects, effectContext, sourceId);
        }

        // 尝试执行 TryActivate 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public SkillActivationResult TryActivate(
            in SkillActivationRequest request,
            SkillEffectContext effectContext)
        {
            ValidateRequest(request, effectContext);
            ObserveTimestamp(request.Timestamp);

            SkillConfig skill = configProvider.GetSkill(request.SkillId);
            SkillEffectConfig[] effects = PrepareEffects(skill);
            double cooldownUntil = GetCooldownUntil(skill.SkillId);

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (!string.Equals(request.TriggerType, skill.TriggerType, StringComparison.Ordinal))
            {
                return Rejected(SkillActivationStatus.TriggerMismatch, skill, cooldownUntil);
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (RequiresGesture(skill) &&
                (!request.GestureIsValid ||
                 (!string.Equals(skill.GestureType, "Any", StringComparison.Ordinal) &&
                  !string.Equals(request.GestureType, skill.GestureType, StringComparison.Ordinal))))
            {
                return Rejected(SkillActivationStatus.GestureInvalid, skill, cooldownUntil);
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (RequiresGesture(skill) && request.InputElapsedSeconds > skill.InputWindowSec)
            {
                return Rejected(SkillActivationStatus.InputWindowExpired, skill, cooldownUntil);
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (request.Timestamp < cooldownUntil)
            {
                return Rejected(SkillActivationStatus.CooldownActive, skill, cooldownUntil);
            }

            double nextCooldownUntil = request.Timestamp + skill.CooldownSec;
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (double.IsNaN(nextCooldownUntil) || double.IsInfinity(nextCooldownUntil))
            {
                throw new SkillEffectConfigurationException(
                    $"Skill '{skill.SkillId}' cooldown exceeds timestamp capacity.",
                    skill.EffectGroupId,
                    0L);
            }

            SkillEnergySpendResult spend = player.TrySpendSkillEnergy(
                skill.SkillId,
                request.Timestamp);
            // 按效果或目标类型选择对应的技能处理分支。
            switch (spend.Status)
            {
                case SkillEnergySpendStatus.Spent:
                    break;
                case SkillEnergySpendStatus.WrongStance:
                    return Rejected(SkillActivationStatus.WrongStance, skill, cooldownUntil);
                case SkillEnergySpendStatus.InsufficientEnergy:
                    return Rejected(SkillActivationStatus.InsufficientEnergy, skill, cooldownUntil);
                case SkillEnergySpendStatus.PlayerDead:
                    return Rejected(SkillActivationStatus.PlayerDead, skill, cooldownUntil);
                default:
                    throw new InvalidOperationException(
                        $"Unexpected skill energy spend status '{spend.Status}'.");
            }

            IReadOnlyList<SkillEffectStepResult> steps = ExecutePreparedEffects(
                effects,
                effectContext,
                skill.SkillId);

            cooldownUntilBySkill[skill.SkillId] = nextCooldownUntil;
            return new SkillActivationResult(
                SkillActivationStatus.Activated,
                skill.SkillId,
                skill.EffectGroupId,
                nextCooldownUntil,
                steps);
        }

        // 处理 PrepareEffects 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private SkillEffectConfig[] PrepareEffects(SkillConfig skill)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (skill.CooldownSec < 0f ||
                float.IsNaN(skill.CooldownSec) ||
                float.IsInfinity(skill.CooldownSec) ||
                skill.InputWindowSec < 0f ||
                float.IsNaN(skill.InputWindowSec) ||
                float.IsInfinity(skill.InputWindowSec))
            {
                throw new SkillEffectConfigurationException(
                    $"Skill '{skill.SkillId}' contains an invalid cooldown or input window.",
                    skill.EffectGroupId,
                    0L);
            }

            return PrepareEffectGroup(skill.EffectGroupId);
        }

        // 处理 PrepareEffectGroup 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private SkillEffectConfig[] PrepareEffectGroup(string effectGroupId)
        {
            IReadOnlyList<SkillEffectConfig> configured =
                configProvider.GetSkillEffects(effectGroupId);
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (configured.Count == 0)
            {
                throw new SkillEffectConfigurationException(
                    $"Effect group '{effectGroupId}' cannot be empty.",
                    effectGroupId,
                    0L);
            }

            var effects = new SkillEffectConfig[configured.Count];
            // 逐项处理技能目标或效果，保持配置顺序与执行结果稳定。
            for (int i = 0; i < configured.Count; i++)
            {
                effects[i] = configured[i] ??
                    throw new SkillEffectConfigurationException(
                        "Skill effect row cannot be null.",
                        effectGroupId,
                        i + 1L);
            }

            Array.Sort(effects, SkillEffectOrderComparer.Instance);
            // 逐项处理技能目标或效果，保持配置顺序与执行结果稳定。
            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffectConfig effect = effects[i];
                long expectedOrder = i + 1L;
                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (!string.Equals(
                        effect.EffectGroupId,
                        effectGroupId,
                        StringComparison.Ordinal) ||
                    effect.Order != expectedOrder)
                {
                    throw new SkillEffectConfigurationException(
                        $"Effect group '{effectGroupId}' must have contiguous order starting at 1.",
                        effectGroupId,
                        effect.Order);
                }

                registry.Validate(effect);
                SkillTargetSelector.Validate(
                    effect.TargetType,
                    effect.EffectGroupId,
                    effect.Order);
                SkillConditionEvaluator.Validate(
                    effect.Condition,
                    effect.EffectGroupId,
                    effect.Order);
            }

            return effects;
        }

        // 执行 ExecutePreparedEffects 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private IReadOnlyList<SkillEffectStepResult> ExecutePreparedEffects(
            SkillEffectConfig[] effects,
            SkillEffectContext effectContext,
            string sourceId)
        {
            var steps = new List<SkillEffectStepResult>(effects.Length);
            // 逐项处理技能目标或效果，保持配置顺序与执行结果稳定。
            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffectConfig effect = effects[i];
                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (!SkillConditionEvaluator.Evaluate(
                        effect.Condition,
                        effectContext,
                        effect.EffectGroupId,
                        effect.Order))
                {
                    steps.Add(new SkillEffectStepResult(
                        effect.Order,
                        effect.EffectType,
                        effect.TargetType,
                        SkillEffectStepStatus.ConditionNotMet,
                        0,
                        0));
                    continue;
                }

                SkillTargetSelector.Select(
                    effect.TargetType,
                    effectContext.World,
                    selectedTargets);
                IEffectExecutor executor = registry.Get(effect);
                int affected = executor.Execute(
                    effect,
                    effectContext,
                    selectedTargets,
                    sourceId);
                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (affected < 0)
                {
                    throw new InvalidOperationException(
                        $"Effect executor '{executor.EffectType}' returned a negative affected count.");
                }

                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (!string.IsNullOrEmpty(effect.VfxKey))
                {
                    effectContext.World.PlayVfx(
                        effect.VfxKey,
                        selectedTargets,
                        sourceId,
                        effectContext.Timestamp);
                }

                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (!string.IsNullOrEmpty(effect.AudioKey))
                {
                    effectContext.World.PlayAudio(
                        effect.AudioKey,
                        sourceId,
                        effectContext.Timestamp);
                }

                steps.Add(new SkillEffectStepResult(
                    effect.Order,
                    effect.EffectType,
                    effect.TargetType,
                    SkillEffectStepStatus.Executed,
                    selectedTargets.Count,
                    affected));
            }

            return steps.AsReadOnly();
        }

        // 处理 RequiresGesture 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private static bool RequiresGesture(SkillConfig skill)
        {
            return string.Equals(skill.TriggerType, SkillTriggerTypes.Gesture, StringComparison.Ordinal) ||
                   string.Equals(skill.TriggerType, SkillTriggerTypes.Ultimate, StringComparison.Ordinal);
        }

        // 处理 Rejected 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private static SkillActivationResult Rejected(
            SkillActivationStatus status,
            SkillConfig skill,
            double cooldownUntil)
        {
            return new SkillActivationResult(
                status,
                skill.SkillId,
                skill.EffectGroupId,
                cooldownUntil,
                NoSteps);
        }

        // 校验 ValidateRequest 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void ValidateRequest(
            in SkillActivationRequest request,
            SkillEffectContext effectContext)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (!request.IsValid)
            {
                throw new ArgumentException(
                    "Skill activation request must be initialized.",
                    nameof(request));
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (string.IsNullOrWhiteSpace(request.SkillId) ||
                string.IsNullOrWhiteSpace(request.TriggerType))
            {
                throw new ArgumentException(
                    "Skill id and trigger type must be non-empty.",
                    nameof(request));
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (double.IsNaN(request.Timestamp) ||
                double.IsInfinity(request.Timestamp) ||
                request.Timestamp < 0d ||
                double.IsNaN(request.InputElapsedSeconds) ||
                double.IsInfinity(request.InputElapsedSeconds) ||
                request.InputElapsedSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    "Skill timestamps and input elapsed seconds must be finite and non-negative.");
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (effectContext == null)
            {
                throw new ArgumentNullException(nameof(effectContext));
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (effectContext.Timestamp != request.Timestamp)
            {
                throw new ArgumentException(
                    "Activation and effect contexts must use the same timestamp.",
                    nameof(effectContext));
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (hasTimestamp && request.Timestamp < lastTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Timestamp,
                    $"Skill timestamp cannot move backwards from {lastTimestamp}.");
            }
        }

        // 校验 ValidateEffectGroupRequest 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void ValidateEffectGroupRequest(
            string effectGroupId,
            string sourceId,
            SkillEffectContext effectContext)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (string.IsNullOrWhiteSpace(effectGroupId))
            {
                throw new ArgumentException(
                    "Effect group id must be non-empty.",
                    nameof(effectGroupId));
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "Effect source id must be non-empty.",
                    nameof(sourceId));
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (effectContext == null)
            {
                throw new ArgumentNullException(nameof(effectContext));
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (hasTimestamp && effectContext.Timestamp < lastTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectContext),
                    effectContext.Timestamp,
                    $"Effect timestamp cannot move backwards from {lastTimestamp}.");
            }
        }

        // 处理 ObserveTimestamp 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private void ObserveTimestamp(double timestamp)
        {
            lastTimestamp = timestamp;
            hasTimestamp = true;
        }

        // 定义 SkillEffectOrderComparer 的技能领域契约，明确条件、目标或效果执行边界。
        private sealed class SkillEffectOrderComparer : IComparer<SkillEffectConfig>
        {
            public static readonly SkillEffectOrderComparer Instance =
                new SkillEffectOrderComparer();

            // 处理 Compare 对应的技能逻辑，并保持条件、目标与效果结果一致。
            public int Compare(SkillEffectConfig left, SkillEffectConfig right)
            {
                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (left == null)
                {
                    return -1;
                }

                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (right == null)
                {
                    return 1;
                }

                return left.Order.CompareTo(right.Order);
            }
        }
    }
}
