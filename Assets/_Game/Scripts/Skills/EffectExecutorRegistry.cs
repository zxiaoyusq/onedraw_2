using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    public sealed class EffectExecutorRegistry
    {
        private readonly IConfigProvider configProvider;
        private readonly IReadOnlyDictionary<string, IEffectExecutor> executors;
        private readonly IReadOnlyList<string> registeredEffectTypes;

        public EffectExecutorRegistry(
            IConfigProvider configuredProvider,
            IEnumerable<IEffectExecutor> configuredExecutors)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            if (configuredExecutors == null)
            {
                throw new ArgumentNullException(nameof(configuredExecutors));
            }

            var mutable = new Dictionary<string, IEffectExecutor>(StringComparer.Ordinal);
            foreach (IEffectExecutor executor in configuredExecutors)
            {
                if (executor == null || string.IsNullOrWhiteSpace(executor.EffectType))
                {
                    throw new ArgumentException(
                        "Every effect executor must declare a non-empty effect type.",
                        nameof(configuredExecutors));
                }

                if (!mutable.TryAdd(executor.EffectType, executor))
                {
                    throw new ArgumentException(
                        $"Effect executor '{executor.EffectType}' is registered more than once.",
                        nameof(configuredExecutors));
                }
            }

            var names = new List<string>(mutable.Keys);
            names.Sort(StringComparer.Ordinal);
            executors = new ReadOnlyDictionary<string, IEffectExecutor>(mutable);
            registeredEffectTypes = names.AsReadOnly();
        }

        public IReadOnlyList<string> RegisteredEffectTypes => registeredEffectTypes;

        public static EffectExecutorRegistry CreateDefault(IConfigProvider configProvider)
        {
            return new EffectExecutorRegistry(
                configProvider,
                new IEffectExecutor[]
                {
                    new ApplyBuffEffectExecutor(configProvider),
                    new ClearProjectilesEffectExecutor(),
                    new DamageEffectExecutor(),
                    new DamageMultiplierEffectExecutor(),
                    new ExecuteBelowHpRatioEffectExecutor(),
                    new HealEffectExecutor(),
                    new IncrementCounterEffectExecutor(),
                    new KnockbackEffectExecutor(),
                    new PlayVfxEffectExecutor(),
                    new RemoveArmorEffectExecutor(),
                    new RepeatStrokeEffectExecutor(),
                    new TimeScaleEffectExecutor(),
                });
        }

        public IEffectExecutor Get(SkillEffectConfig effect)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            if (executors.TryGetValue(effect.EffectType, out IEffectExecutor executor))
            {
                return executor;
            }

            throw new SkillEffectConfigurationException(
                $"Effect type '{effect.EffectType}' has no registered executor.",
                effect.EffectGroupId,
                effect.Order);
        }

        public void Validate(SkillEffectConfig effect)
        {
            Get(effect);
            RequireFinite(effect.Value1, "value1", effect);
            RequireFinite(effect.Value2, "value2", effect);
            RequireFinite(effect.DurationSec, "durationSec", effect);
            if (effect.DurationSec < 0f)
            {
                Failure("durationSec must be non-negative.", effect);
            }

            switch (effect.EffectType)
            {
                case SkillEffectTypes.Damage:
                case SkillEffectTypes.Heal:
                case SkillEffectTypes.RemoveArmor:
                case SkillEffectTypes.Knockback:
                    RequireNonNegative(effect.Value1, "value1", effect);
                    break;
                case SkillEffectTypes.RepeatStroke:
                    RequireNonNegative(effect.Value1, "value1", effect);
                    RequireNonNegative(effect.Value2, "value2", effect);
                    break;
                case SkillEffectTypes.TimeScale:
                    if (effect.Value1 <= 0f || effect.Value1 > 1f)
                    {
                        Failure("TimeScale.value1 must be in (0, 1].", effect);
                    }
                    break;
                case SkillEffectTypes.ExecuteBelowHpRatio:
                    if (effect.Value1 < 0f || effect.Value1 > 1f)
                    {
                        Failure("ExecuteBelowHpRatio.value1 must be in [0, 1].", effect);
                    }
                    break;
                case SkillEffectTypes.DamageMultiplier:
                    if (effect.Value1 <= 0f)
                    {
                        Failure("DamageMultiplier.value1 must be positive.", effect);
                    }
                    break;
                case SkillEffectTypes.ApplyBuff:
                    if (string.IsNullOrWhiteSpace(effect.BuffId))
                    {
                        Failure("ApplyBuff requires buffId.", effect);
                    }

                    configProvider.GetBuff(effect.BuffId);
                    break;
                case SkillEffectTypes.PlayVfx:
                    if (string.IsNullOrWhiteSpace(effect.VfxKey))
                    {
                        Failure("PlayVfx requires vfxKey.", effect);
                    }
                    break;
            }
        }

        private static void RequireFinite(float value, string field, SkillEffectConfig effect)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                Failure($"{field} must be finite.", effect);
            }
        }

        private static void RequireNonNegative(float value, string field, SkillEffectConfig effect)
        {
            if (value < 0f)
            {
                Failure($"{field} must be non-negative.", effect);
            }
        }

        private static void Failure(string message, SkillEffectConfig effect)
        {
            throw new SkillEffectConfigurationException(
                message,
                effect.EffectGroupId,
                effect.Order);
        }
    }

    internal abstract class TargetEffectExecutor : IEffectExecutor
    {
        public abstract string EffectType { get; }

        public int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId)
        {
            int affected = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (Apply(targets[i], effect, context, sourceId))
                {
                    affected++;
                }
            }

            return affected;
        }

        protected abstract bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId);
    }

    internal sealed class DamageEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.Damage;

        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.ApplyDamage(effect.Value1, sourceId, context.Timestamp);
        }
    }

    internal sealed class HealEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.Heal;

        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.ApplyHealing(effect.Value1, sourceId, context.Timestamp);
        }
    }

    internal sealed class ApplyBuffEffectExecutor : TargetEffectExecutor
    {
        private readonly IConfigProvider configProvider;

        public ApplyBuffEffectExecutor(IConfigProvider configuredProvider)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
        }

        public override string EffectType => SkillEffectTypes.ApplyBuff;

        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            BuffConfig buff = configProvider.GetBuff(effect.BuffId);
            float duration = effect.DurationSec > 0f ? effect.DurationSec : buff.DurationSec;
            return target.ApplyBuff(buff, duration, sourceId, context.Timestamp);
        }
    }

    internal sealed class RemoveArmorEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.RemoveArmor;

        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.RemoveArmor(effect.Value1, sourceId, context.Timestamp);
        }
    }

    internal sealed class KnockbackEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.Knockback;

        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.ApplyKnockback(
                effect.Value1,
                effect.DurationSec,
                sourceId,
                context.Timestamp);
        }
    }

    internal sealed class ExecuteBelowHpRatioEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.ExecuteBelowHpRatio;

        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.ExecuteBelowHpRatio(effect.Value1, sourceId, context.Timestamp);
        }
    }

    internal sealed class IncrementCounterEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.IncrementCounter;

        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.IncrementCounter(
                effect.Value1,
                effect.Value2,
                sourceId,
                context.Timestamp);
        }
    }

    internal sealed class RepeatStrokeEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.RepeatStroke;

        public int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId)
        {
            return context.World.RepeatLastStroke(
                effect.Value1,
                effect.Value2,
                sourceId,
                context.Timestamp);
        }
    }

    internal sealed class TimeScaleEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.TimeScale;

        public int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId)
        {
            return context.World.SetTimeScale(
                effect.Value1,
                effect.DurationSec,
                sourceId,
                context.Timestamp);
        }
    }

    internal sealed class DamageMultiplierEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.DamageMultiplier;

        public int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId)
        {
            return context.World.SetNextStrokeDamageMultiplier(
                effect.Value1,
                sourceId,
                context.Timestamp);
        }
    }

    internal sealed class ClearProjectilesEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.ClearProjectiles;

        public int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId)
        {
            return context.World.ClearHostileProjectiles(sourceId, context.Timestamp);
        }
    }

    internal sealed class PlayVfxEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.PlayVfx;

        public int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId)
        {
            return SkillTargetSelector.IsWorldScope(effect.TargetType) ? 1 : targets.Count;
        }
    }
}
