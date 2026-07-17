using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    // 定义 EffectExecutorRegistry 的技能领域契约，明确条件、目标或效果执行边界。
    public sealed class EffectExecutorRegistry
    {
        private readonly IConfigProvider configProvider;
        private readonly IReadOnlyDictionary<string, IEffectExecutor> executors;
        private readonly IReadOnlyList<string> registeredEffectTypes;

        // 初始化 EffectExecutorRegistry，并建立技能运行时所需的初始状态。
        public EffectExecutorRegistry(
            IConfigProvider configuredProvider,
            IEnumerable<IEffectExecutor> configuredExecutors)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (configuredExecutors == null)
            {
                throw new ArgumentNullException(nameof(configuredExecutors));
            }

            var mutable = new Dictionary<string, IEffectExecutor>(StringComparer.Ordinal);
            // 逐项处理技能目标或效果，保持配置顺序与执行结果稳定。
            foreach (IEffectExecutor executor in configuredExecutors)
            {
                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (executor == null || string.IsNullOrWhiteSpace(executor.EffectType))
                {
                    throw new ArgumentException(
                        "Every effect executor must declare a non-empty effect type.",
                        nameof(configuredExecutors));
                }

                // 检查技能条件或运行时边界，阻止无效状态继续执行。
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

        // 创建 CreateDefault 对应的技能逻辑，并保持条件、目标与效果结果一致。
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

        // 获取 Get 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public IEffectExecutor Get(SkillEffectConfig effect)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (executors.TryGetValue(effect.EffectType, out IEffectExecutor executor))
            {
                return executor;
            }

            throw new SkillEffectConfigurationException(
                $"Effect type '{effect.EffectType}' has no registered executor.",
                effect.EffectGroupId,
                effect.Order);
        }

        // 校验 Validate 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public void Validate(SkillEffectConfig effect)
        {
            Get(effect);
            RequireFinite(effect.Value1, "value1", effect);
            RequireFinite(effect.Value2, "value2", effect);
            RequireFinite(effect.DurationSec, "durationSec", effect);
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (effect.DurationSec < 0f)
            {
                Failure("durationSec must be non-negative.", effect);
            }

            // 按效果或目标类型选择对应的技能处理分支。
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
                    // 检查技能条件或运行时边界，阻止无效状态继续执行。
                    if (effect.Value1 <= 0f || effect.Value1 > 1f)
                    {
                        Failure("TimeScale.value1 must be in (0, 1].", effect);
                    }
                    break;
                case SkillEffectTypes.ExecuteBelowHpRatio:
                    // 检查技能条件或运行时边界，阻止无效状态继续执行。
                    if (effect.Value1 < 0f || effect.Value1 > 1f)
                    {
                        Failure("ExecuteBelowHpRatio.value1 must be in [0, 1].", effect);
                    }
                    break;
                case SkillEffectTypes.DamageMultiplier:
                    // 检查技能条件或运行时边界，阻止无效状态继续执行。
                    if (effect.Value1 <= 0f)
                    {
                        Failure("DamageMultiplier.value1 must be positive.", effect);
                    }
                    break;
                case SkillEffectTypes.ApplyBuff:
                    // 检查技能条件或运行时边界，阻止无效状态继续执行。
                    if (string.IsNullOrWhiteSpace(effect.BuffId))
                    {
                        Failure("ApplyBuff requires buffId.", effect);
                    }

                    configProvider.GetBuff(effect.BuffId);
                    break;
                case SkillEffectTypes.PlayVfx:
                    // 检查技能条件或运行时边界，阻止无效状态继续执行。
                    if (string.IsNullOrWhiteSpace(effect.VfxKey))
                    {
                        Failure("PlayVfx requires vfxKey.", effect);
                    }
                    break;
            }
        }

        // 处理 RequireFinite 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private static void RequireFinite(float value, string field, SkillEffectConfig effect)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                Failure($"{field} must be finite.", effect);
            }
        }

        // 处理 RequireNonNegative 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private static void RequireNonNegative(float value, string field, SkillEffectConfig effect)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (value < 0f)
            {
                Failure($"{field} must be non-negative.", effect);
            }
        }

        // 处理 Failure 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private static void Failure(string message, SkillEffectConfig effect)
        {
            throw new SkillEffectConfigurationException(
                message,
                effect.EffectGroupId,
                effect.Order);
        }
    }

    // 定义 TargetEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal abstract class TargetEffectExecutor : IEffectExecutor
    {
        public abstract string EffectType { get; }

        // 执行 Execute 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId)
        {
            int affected = 0;
            // 逐项处理技能目标或效果，保持配置顺序与执行结果稳定。
            for (int i = 0; i < targets.Count; i++)
            {
                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (Apply(targets[i], effect, context, sourceId))
                {
                    affected++;
                }
            }

            return affected;
        }

        // 应用 Apply 对应的技能逻辑，并保持条件、目标与效果结果一致。
        protected abstract bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId);
    }

    // 定义 DamageEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class DamageEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.Damage;

        // 应用 Apply 对应的技能逻辑，并保持条件、目标与效果结果一致。
        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.ApplyDamage(effect.Value1, sourceId, context.Timestamp);
        }
    }

    // 定义 HealEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class HealEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.Heal;

        // 应用 Apply 对应的技能逻辑，并保持条件、目标与效果结果一致。
        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.ApplyHealing(effect.Value1, sourceId, context.Timestamp);
        }
    }

    // 定义 ApplyBuffEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class ApplyBuffEffectExecutor : TargetEffectExecutor
    {
        private readonly IConfigProvider configProvider;

        // 初始化 ApplyBuffEffectExecutor，并建立技能运行时所需的初始状态。
        public ApplyBuffEffectExecutor(IConfigProvider configuredProvider)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
        }

        public override string EffectType => SkillEffectTypes.ApplyBuff;

        // 应用 Apply 对应的技能逻辑，并保持条件、目标与效果结果一致。
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

    // 定义 RemoveArmorEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class RemoveArmorEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.RemoveArmor;

        // 应用 Apply 对应的技能逻辑，并保持条件、目标与效果结果一致。
        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.RemoveArmor(effect.Value1, sourceId, context.Timestamp);
        }
    }

    // 定义 KnockbackEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class KnockbackEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.Knockback;

        // 应用 Apply 对应的技能逻辑，并保持条件、目标与效果结果一致。
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

    // 定义 ExecuteBelowHpRatioEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class ExecuteBelowHpRatioEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.ExecuteBelowHpRatio;

        // 应用 Apply 对应的技能逻辑，并保持条件、目标与效果结果一致。
        protected override bool Apply(
            ISkillEffectTarget target,
            SkillEffectConfig effect,
            SkillEffectContext context,
            string sourceId)
        {
            return target.ExecuteBelowHpRatio(effect.Value1, sourceId, context.Timestamp);
        }
    }

    // 定义 IncrementCounterEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class IncrementCounterEffectExecutor : TargetEffectExecutor
    {
        public override string EffectType => SkillEffectTypes.IncrementCounter;

        // 应用 Apply 对应的技能逻辑，并保持条件、目标与效果结果一致。
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

    // 定义 RepeatStrokeEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class RepeatStrokeEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.RepeatStroke;

        // 执行 Execute 对应的技能逻辑，并保持条件、目标与效果结果一致。
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

    // 定义 TimeScaleEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class TimeScaleEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.TimeScale;

        // 执行 Execute 对应的技能逻辑，并保持条件、目标与效果结果一致。
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

    // 定义 DamageMultiplierEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class DamageMultiplierEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.DamageMultiplier;

        // 执行 Execute 对应的技能逻辑，并保持条件、目标与效果结果一致。
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

    // 定义 ClearProjectilesEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class ClearProjectilesEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.ClearProjectiles;

        // 执行 Execute 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public int Execute(
            SkillEffectConfig effect,
            SkillEffectContext context,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId)
        {
            return context.World.ClearHostileProjectiles(sourceId, context.Timestamp);
        }
    }

    // 定义 PlayVfxEffectExecutor 的技能领域契约，明确条件、目标或效果执行边界。
    internal sealed class PlayVfxEffectExecutor : IEffectExecutor
    {
        public string EffectType => SkillEffectTypes.PlayVfx;

        // 执行 Execute 对应的技能逻辑，并保持条件、目标与效果结果一致。
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
