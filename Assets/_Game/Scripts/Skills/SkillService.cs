using System;
using System.Collections.Generic;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
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

        public SkillService(
            IConfigProvider configuredProvider,
            PlayerCombatController playerController,
            EffectExecutorRegistry executorRegistry = null)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            player = playerController ??
                throw new ArgumentNullException(nameof(playerController));
            if (!player.IsInitialized)
            {
                throw new ArgumentException(
                    "Player combat controller must be initialized before SkillService.",
                    nameof(playerController));
            }

            registry = executorRegistry ?? EffectExecutorRegistry.CreateDefault(configProvider);
        }

        public EffectExecutorRegistry Executors => registry;

        public double GetCooldownUntil(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentException("Skill id must be non-empty.", nameof(skillId));
            }

            return cooldownUntilBySkill.TryGetValue(skillId, out double cooldownUntil)
                ? cooldownUntil
                : 0d;
        }

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

        public SkillActivationResult TryActivate(
            in SkillActivationRequest request,
            SkillEffectContext effectContext)
        {
            ValidateRequest(request, effectContext);
            ObserveTimestamp(request.Timestamp);

            SkillConfig skill = configProvider.GetSkill(request.SkillId);
            SkillEffectConfig[] effects = PrepareEffects(skill);
            double cooldownUntil = GetCooldownUntil(skill.SkillId);

            if (!string.Equals(request.TriggerType, skill.TriggerType, StringComparison.Ordinal))
            {
                return Rejected(SkillActivationStatus.TriggerMismatch, skill, cooldownUntil);
            }

            if (RequiresGesture(skill) &&
                (!request.GestureIsValid ||
                 (!string.Equals(skill.GestureType, "Any", StringComparison.Ordinal) &&
                  !string.Equals(request.GestureType, skill.GestureType, StringComparison.Ordinal))))
            {
                return Rejected(SkillActivationStatus.GestureInvalid, skill, cooldownUntil);
            }

            if (RequiresGesture(skill) && request.InputElapsedSeconds > skill.InputWindowSec)
            {
                return Rejected(SkillActivationStatus.InputWindowExpired, skill, cooldownUntil);
            }

            if (request.Timestamp < cooldownUntil)
            {
                return Rejected(SkillActivationStatus.CooldownActive, skill, cooldownUntil);
            }

            double nextCooldownUntil = request.Timestamp + skill.CooldownSec;
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

        private SkillEffectConfig[] PrepareEffects(SkillConfig skill)
        {
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

        private SkillEffectConfig[] PrepareEffectGroup(string effectGroupId)
        {
            IReadOnlyList<SkillEffectConfig> configured =
                configProvider.GetSkillEffects(effectGroupId);
            if (configured.Count == 0)
            {
                throw new SkillEffectConfigurationException(
                    $"Effect group '{effectGroupId}' cannot be empty.",
                    effectGroupId,
                    0L);
            }

            var effects = new SkillEffectConfig[configured.Count];
            for (int i = 0; i < configured.Count; i++)
            {
                effects[i] = configured[i] ??
                    throw new SkillEffectConfigurationException(
                        "Skill effect row cannot be null.",
                        effectGroupId,
                        i + 1L);
            }

            Array.Sort(effects, SkillEffectOrderComparer.Instance);
            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffectConfig effect = effects[i];
                long expectedOrder = i + 1L;
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

        private IReadOnlyList<SkillEffectStepResult> ExecutePreparedEffects(
            SkillEffectConfig[] effects,
            SkillEffectContext effectContext,
            string sourceId)
        {
            var steps = new List<SkillEffectStepResult>(effects.Length);
            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffectConfig effect = effects[i];
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
                if (affected < 0)
                {
                    throw new InvalidOperationException(
                        $"Effect executor '{executor.EffectType}' returned a negative affected count.");
                }

                if (!string.IsNullOrEmpty(effect.VfxKey))
                {
                    effectContext.World.PlayVfx(
                        effect.VfxKey,
                        selectedTargets,
                        sourceId,
                        effectContext.Timestamp);
                }

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

        private static bool RequiresGesture(SkillConfig skill)
        {
            return string.Equals(skill.TriggerType, SkillTriggerTypes.Gesture, StringComparison.Ordinal) ||
                   string.Equals(skill.TriggerType, SkillTriggerTypes.Ultimate, StringComparison.Ordinal);
        }

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

        private void ValidateRequest(
            in SkillActivationRequest request,
            SkillEffectContext effectContext)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException(
                    "Skill activation request must be initialized.",
                    nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.SkillId) ||
                string.IsNullOrWhiteSpace(request.TriggerType))
            {
                throw new ArgumentException(
                    "Skill id and trigger type must be non-empty.",
                    nameof(request));
            }

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

            if (effectContext == null)
            {
                throw new ArgumentNullException(nameof(effectContext));
            }

            if (effectContext.Timestamp != request.Timestamp)
            {
                throw new ArgumentException(
                    "Activation and effect contexts must use the same timestamp.",
                    nameof(effectContext));
            }

            if (hasTimestamp && request.Timestamp < lastTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Timestamp,
                    $"Skill timestamp cannot move backwards from {lastTimestamp}.");
            }
        }

        private void ValidateEffectGroupRequest(
            string effectGroupId,
            string sourceId,
            SkillEffectContext effectContext)
        {
            if (string.IsNullOrWhiteSpace(effectGroupId))
            {
                throw new ArgumentException(
                    "Effect group id must be non-empty.",
                    nameof(effectGroupId));
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "Effect source id must be non-empty.",
                    nameof(sourceId));
            }

            if (effectContext == null)
            {
                throw new ArgumentNullException(nameof(effectContext));
            }

            if (hasTimestamp && effectContext.Timestamp < lastTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectContext),
                    effectContext.Timestamp,
                    $"Effect timestamp cannot move backwards from {lastTimestamp}.");
            }
        }

        private void ObserveTimestamp(double timestamp)
        {
            lastTimestamp = timestamp;
            hasTimestamp = true;
        }

        private sealed class SkillEffectOrderComparer : IComparer<SkillEffectConfig>
        {
            public static readonly SkillEffectOrderComparer Instance =
                new SkillEffectOrderComparer();

            public int Compare(SkillEffectConfig left, SkillEffectConfig right)
            {
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                if (left == null)
                {
                    return -1;
                }

                if (right == null)
                {
                    return 1;
                }

                return left.Order.CompareTo(right.Order);
            }
        }
    }
}
