using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
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

    public static class SkillTriggerTypes
    {
        public const string Passive = "Passive";
        public const string Gesture = "Gesture";
        public const string Ultimate = "Ultimate";
    }

    public enum SkillTargetFaction
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2
    }

    public enum SkillEnemyTier
    {
        None = 0,
        Normal = 1,
        Elite = 2,
        Boss = 3
    }

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

    public sealed class SkillEffectContext
    {
        private readonly IReadOnlyDictionary<string, double> variables;

        public SkillEffectContext(
            ISkillEffectWorld world,
            double timestamp,
            IReadOnlyDictionary<string, double> conditionVariables = null)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
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

        public bool TryGetConditionValue(string name, out double value)
        {
            if (variables != null && name != null && variables.TryGetValue(name, out value))
            {
                return true;
            }

            value = 0d;
            return false;
        }
    }

    public interface IEffectExecutor
    {
        string EffectType { get; }

        int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId);
    }

    public enum SkillEffectStepStatus
    {
        None = 0,
        Executed = 1,
        ConditionNotMet = 2
    }

    public readonly struct SkillEffectStepResult
    {
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

    public readonly struct SkillActivationRequest
    {
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

    public readonly struct SkillActivationResult
    {
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

    public sealed class SkillEffectConfigurationException : InvalidOperationException
    {
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
