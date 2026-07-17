using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    // 定义 SkillEffectTypes 的技能领域契约，明确条件、目标或效果执行边界。
    public static class SkillEffectTypes
    {
        public const string Damage = "Damage";
        public const string Heal = "Heal";
        public const string ApplyBuff = "ApplyBuff";
        public const string RemoveArmor = "RemoveArmor";
        public const string Knockback = "Knockback";
        public const string RepeatStroke = "RepeatStroke";
        public const string TimeScale = "TimeScale";
        public const string ExecuteBelowHpRatio = "ExecuteBelowHpRatio";
        public const string DamageMultiplier = "DamageMultiplier";
        public const string IncrementCounter = "IncrementCounter";
        public const string PlayVfx = "PlayVfx";
        public const string ClearProjectiles = "ClearProjectiles";
    }

    // 定义 SkillTargetTypes 的技能领域契约，明确条件、目标或效果执行边界。
    public static class SkillTargetTypes
    {
        public const string Target = "Target";
        public const string NextStroke = "NextStroke";
        public const string EnemiesInRadius = "EnemiesInRadius";
        public const string LastStrokeTargets = "LastStrokeTargets";
        public const string EnemiesInsideGesture = "EnemiesInsideGesture";
        public const string Battle = "Battle";
        public const string AllEnemies = "AllEnemies";
        public const string NormalEnemies = "NormalEnemies";
        public const string Boss = "Boss";
    }

    // 定义 SkillTriggerTypes 的技能领域契约，明确条件、目标或效果执行边界。
    public static class SkillTriggerTypes
    {
        public const string Passive = "Passive";
        public const string Gesture = "Gesture";
        public const string Ultimate = "Ultimate";
    }

    // 定义 SkillTargetFaction 的技能领域契约，明确条件、目标或效果执行边界。
    public enum SkillTargetFaction
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2
    }

    // 定义 SkillEnemyTier 的技能领域契约，明确条件、目标或效果执行边界。
    public enum SkillEnemyTier
    {
        None = 0,
        Normal = 1,
        Elite = 2,
        Boss = 3
    }

    // 定义 ISkillEffectTarget 的技能领域契约，明确条件、目标或效果执行边界。
    public interface ISkillEffectTarget
    {
        string TargetId { get; }

        SkillTargetFaction Faction { get; }

        SkillEnemyTier EnemyTier { get; }

        bool IsAlive { get; }

        bool IsInEffectRadius { get; }

        bool WasHitByLastStroke { get; }

        bool IsInsideGesture { get; }

        bool ApplyDamage(float amount, string sourceId, double timestamp);

        bool ApplyHealing(float amount, string sourceId, double timestamp);

        bool ApplyBuff(BuffConfig buff, float durationSeconds, string sourceId, double timestamp);

        bool RemoveArmor(float amount, string sourceId, double timestamp);

        bool ApplyKnockback(float distanceRefPixels, float durationSeconds, string sourceId, double timestamp);

        bool ExecuteBelowHpRatio(float threshold, string sourceId, double timestamp);

        bool IncrementCounter(float amount, float limit, string sourceId, double timestamp);
    }

    // 定义 ISkillEffectWorld 的技能领域契约，明确条件、目标或效果执行边界。
    public interface ISkillEffectWorld
    {
        IReadOnlyList<ISkillEffectTarget> Targets { get; }

        ISkillEffectTarget PrimaryTarget { get; }

        int RepeatLastStroke(float damageMultiplier, float delaySeconds, string sourceId, double timestamp);

        int SetTimeScale(float scale, float durationSeconds, string sourceId, double timestamp);

        int SetNextStrokeDamageMultiplier(float multiplier, string sourceId, double timestamp);

        int ClearHostileProjectiles(string sourceId, double timestamp);

        void PlayVfx(
            string vfxKey,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId,
            double timestamp);

        void PlayAudio(string audioKey, string sourceId, double timestamp);
    }

    // 定义 SkillEffectContext 的技能领域契约，明确条件、目标或效果执行边界。
    public sealed class SkillEffectContext
    {
        private readonly IReadOnlyDictionary<string, double> variables;

        // 初始化 SkillEffectContext，并建立技能运行时所需的初始状态。
        public SkillEffectContext(
            ISkillEffectWorld world,
            double timestamp,
            IReadOnlyDictionary<string, double> conditionVariables = null)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    "Effect timestamp must be finite and non-negative.");
            }

            Timestamp = timestamp;
            variables = conditionVariables;
        }

        public ISkillEffectWorld World { get; }

        public double Timestamp { get; }

        // 尝试执行 TryGetConditionValue 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool TryGetConditionValue(string name, out double value)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (variables != null && name != null && variables.TryGetValue(name, out value))
            {
                return true;
            }

            value = 0d;
            return false;
        }
    }

    // 定义 IEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    public interface IEffectExecutor
    {
        string EffectType { get; }

        int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId);
    }

    // 定义 SkillEffectStepStatus 的技能领域契约，明确条件、目标或效果执行边界。
    public enum SkillEffectStepStatus
    {
        None = 0,
        Executed = 1,
        ConditionNotMet = 2
    }

    // 定义 SkillEffectStepResult 的技能领域契约，明确条件、目标或效果执行边界。
    public readonly struct SkillEffectStepResult
    {
        // 初始化 SkillEffectStepResult，并建立技能运行时所需的初始状态。
        internal SkillEffectStepResult(
            long order,
            string effectType,
            string targetType,
            SkillEffectStepStatus status,
            int selectedTargetCount,
            int affectedCount)
        {
            Order = order;
            EffectType = effectType;
            TargetType = targetType;
            Status = status;
            SelectedTargetCount = selectedTargetCount;
            AffectedCount = affectedCount;
            IsValid = true;
        }

        public long Order { get; }

        public string EffectType { get; }

        public string TargetType { get; }

        public SkillEffectStepStatus Status { get; }

        public int SelectedTargetCount { get; }

        public int AffectedCount { get; }

        public bool IsValid { get; }
    }

    // 定义 SkillActivationStatus 的技能领域契约，明确条件、目标或效果执行边界。
    public enum SkillActivationStatus
    {
        None = 0,
        Activated = 1,
        TriggerMismatch = 2,
        GestureInvalid = 3,
        InputWindowExpired = 4,
        CooldownActive = 5,
        WrongStance = 6,
        InsufficientEnergy = 7,
        PlayerDead = 8
    }

    // 定义 SkillActivationRequest 的技能领域契约，明确条件、目标或效果执行边界。
    public readonly struct SkillActivationRequest
    {
        // 初始化 SkillActivationRequest，并建立技能运行时所需的初始状态。
        public SkillActivationRequest(
            string skillId,
            string triggerType,
            string gestureType,
            bool gestureIsValid,
            double inputElapsedSeconds,
            double timestamp)
        {
            SkillId = skillId;
            TriggerType = triggerType;
            GestureType = gestureType;
            GestureIsValid = gestureIsValid;
            InputElapsedSeconds = inputElapsedSeconds;
            Timestamp = timestamp;
            IsValid = true;
        }

        public string SkillId { get; }

        public string TriggerType { get; }

        public string GestureType { get; }

        public bool GestureIsValid { get; }

        public double InputElapsedSeconds { get; }

        public double Timestamp { get; }

        public bool IsValid { get; }
    }

    // 定义 SkillActivationResult 的技能领域契约，明确条件、目标或效果执行边界。
    public readonly struct SkillActivationResult
    {
        // 初始化 SkillActivationResult，并建立技能运行时所需的初始状态。
        internal SkillActivationResult(
            SkillActivationStatus status,
            string skillId,
            string effectGroupId,
            double cooldownUntil,
            IReadOnlyList<SkillEffectStepResult> steps)
        {
            Status = status;
            SkillId = skillId;
            EffectGroupId = effectGroupId;
            CooldownUntil = cooldownUntil;
            Steps = steps ?? Array.Empty<SkillEffectStepResult>();
            IsValid = true;
        }

        public SkillActivationStatus Status { get; }

        public string SkillId { get; }

        public string EffectGroupId { get; }

        public double CooldownUntil { get; }

        public IReadOnlyList<SkillEffectStepResult> Steps { get; }

        public bool IsValid { get; }

        public bool Succeeded => Status == SkillActivationStatus.Activated;
    }

    // 定义 SkillEffectConfigurationException 的技能领域契约，明确条件、目标或效果执行边界。
    public sealed class SkillEffectConfigurationException : InvalidOperationException
    {
        // 初始化 SkillEffectConfigurationException，并建立技能运行时所需的初始状态。
        public SkillEffectConfigurationException(
            string message,
            string effectGroupId,
            long order)
            : base(message)
        {
            EffectGroupId = effectGroupId ?? string.Empty;
            Order = order;
        }

        public string EffectGroupId { get; }

        public long Order { get; }
    }
}
