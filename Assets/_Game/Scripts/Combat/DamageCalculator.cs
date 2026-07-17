using System;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    /// <summary>用不可变上下文、规则和可注入随机源计算单次命中的伤害与奖励。</summary>
    public static class DamageCalculator
    {
        /// <summary>按方向、弱点、连斩和暴击顺序计算完整结算结果。</summary>
        public static DamageResult Calculate(
            in DamageContext context,
            in DamageRuleSet rules,
            IRandomSource randomSource)
        {
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            if (!string.Equals(context.StanceId, rules.StanceId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Damage context stance '{context.StanceId}' does not match rule stance '{rules.StanceId}'.",
                    nameof(context));
            }

            // 方向成立必须同时满足手势与配置要求的架势；失败时组合两层惩罚倍率。
            bool directionMatched =
                (rules.RequiredGestureType == GestureType.Any ||
                 rules.RequiredGestureType == context.GestureType) &&
                (string.IsNullOrEmpty(rules.RequiredStanceId) ||
                 string.Equals(rules.RequiredStanceId, context.StanceId, StringComparison.Ordinal));
            double directionMultiplier = directionMatched
                ? rules.MatchingDirectionMultiplier
                : rules.FormulaWrongDirectionMultiplier * rules.WrongGestureMultiplier;
            double comboMultiplier = Math.Min(
                1d + ((context.ComboCount - 1d) * rules.ComboStep),
                rules.ComboMaximumMultiplier);
            double weakpointMultiplier = context.IsWeakpoint
                ? rules.FormulaWeakpointMultiplier * rules.RuleWeakpointMultiplier
                : 1d;
            // 随机源必须严格遵守 [0,1) 合同，避免异常实现静默扭曲暴击率。
            double criticalRoll = randomSource.NextUnitInterval();
            if (double.IsNaN(criticalRoll) || double.IsInfinity(criticalRoll) ||
                criticalRoll < 0d || criticalRoll >= 1d)
            {
                throw new InvalidOperationException(
                    $"Random source returned {criticalRoll}; expected a finite value in [0, 1)." );
            }

            bool isCritical = criticalRoll < rules.CriticalChance;
            double criticalMultiplier = isCritical ? rules.CriticalMultiplier : 1d;
            // 所有乘法保持双精度，最终伤害、评分和能量分别统一四舍五入一次。
            double rawDamage =
                rules.BaseDamage *
                rules.StanceDamageMultiplier *
                directionMultiplier *
                weakpointMultiplier *
                comboMultiplier *
                criticalMultiplier;
            long baseScore = checked(
                rules.ScorePerHit + (context.IsWeakpoint ? rules.WeakpointScoreBonus : 0L));
            long baseEnergy = checked(
                rules.EnergyPerHit + (context.IsWeakpoint ? rules.WeakpointEnergyBonus : 0L));
            double rewardMultiplier = directionMultiplier * comboMultiplier;

            return new DamageResult(
                context.StrokeId,
                context.TargetId,
                rules.FormulaId,
                rules.StanceId,
                rules.DefenseRuleId,
                rules.WeakpointRuleId,
                RoundAward(rawDamage),
                RoundAward(
                    (baseScore * rewardMultiplier) +
                    (rawDamage * rules.ScorePerDamage)),
                RoundAward(baseEnergy * rewardMultiplier),
                directionMatched ? 0L : rules.ReflectedDamage,
                directionMatched,
                context.IsWeakpoint,
                isCritical,
                context.IsWeakpoint && rules.WeakpointInterruptsAttack,
                directionMultiplier,
                weakpointMultiplier,
                comboMultiplier,
                criticalMultiplier);
        }

        /// <summary>验证奖励非负有限并使用 AwayFromZero 规则安全转换为 Int64。</summary>
        private static long RoundAward(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new OverflowException($"Calculated combat award '{value}' is not finite and non-negative.");
            }

            double rounded = Math.Round(value, 0, MidpointRounding.AwayFromZero);
            if (rounded > long.MaxValue)
            {
                throw new OverflowException($"Calculated combat award '{value}' exceeds Int64 capacity.");
            }

            return checked((long)rounded);
        }
    }

    /// <summary>保存一次已解析命中的最终伤害、奖励、判定标志和倍率明细。</summary>
    public readonly struct DamageResult
    {
        /// <summary>由伤害计算器创建完整且标记为已解析的结果。</summary>
        internal DamageResult(
            ulong strokeId,
            int targetId,
            string formulaId,
            string stanceId,
            string defenseRuleId,
            string weakpointRuleId,
            long damage,
            long scoreAward,
            long energyAward,
            long reflectedDamage,
            bool directionMatched,
            bool isWeakpoint,
            bool isCritical,
            bool shouldInterruptAttack,
            double directionMultiplier,
            double weakpointMultiplier,
            double comboMultiplier,
            double criticalMultiplier)
        {
            StrokeId = strokeId;
            TargetId = targetId;
            FormulaId = formulaId;
            StanceId = stanceId;
            DefenseRuleId = defenseRuleId;
            WeakpointRuleId = weakpointRuleId;
            Damage = damage;
            ScoreAward = scoreAward;
            EnergyAward = energyAward;
            ReflectedDamage = reflectedDamage;
            DirectionMatched = directionMatched;
            IsWeakpoint = isWeakpoint;
            IsCritical = isCritical;
            ShouldInterruptAttack = shouldInterruptAttack;
            DirectionMultiplier = directionMultiplier;
            WeakpointMultiplier = weakpointMultiplier;
            ComboMultiplier = comboMultiplier;
            CriticalMultiplier = criticalMultiplier;
            IsResolved = true;
        }

        // 以下属性共同构成只读战斗事实，表现与角色系统只能消费，不能重新计算。
        public ulong StrokeId { get; }
        public int TargetId { get; }
        public string FormulaId { get; }
        public string StanceId { get; }
        public string DefenseRuleId { get; }
        public string WeakpointRuleId { get; }
        public long Damage { get; }
        public long ScoreAward { get; }
        public long EnergyAward { get; }
        public long ReflectedDamage { get; }
        public bool DirectionMatched { get; }
        public bool IsWeakpoint { get; }
        public bool IsCritical { get; }
        public bool ShouldInterruptAttack { get; }
        public double DirectionMultiplier { get; }
        public double WeakpointMultiplier { get; }
        public double ComboMultiplier { get; }
        public double CriticalMultiplier { get; }
        public bool IsResolved { get; }
    }
}
