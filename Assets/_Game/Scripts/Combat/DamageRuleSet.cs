using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    /// <summary>冻结一次伤害计算所需的公式、架势、防御、弱点和奖励参数。</summary>
    public readonly struct DamageRuleSet
    {
        /// <summary>由配置工厂创建完整规则快照。</summary>
        internal DamageRuleSet(
            string formulaId,
            string stanceId,
            string defenseRuleId,
            string weakpointRuleId,
            GestureType requiredGestureType,
            string requiredStanceId,
            long baseDamage,
            double stanceDamageMultiplier,
            double criticalChance,
            double criticalMultiplier,
            double formulaWeakpointMultiplier,
            double ruleWeakpointMultiplier,
            double formulaWrongDirectionMultiplier,
            double matchingDirectionMultiplier,
            double wrongGestureMultiplier,
            double comboStep,
            double comboMaximumMultiplier,
            long energyPerHit,
            long scorePerHit,
            double scorePerDamage,
            long weakpointEnergyBonus,
            long weakpointScoreBonus,
            bool weakpointInterruptsAttack,
            long reflectedDamage)
        {
            FormulaId = formulaId;
            StanceId = stanceId;
            DefenseRuleId = defenseRuleId;
            WeakpointRuleId = weakpointRuleId;
            RequiredGestureType = requiredGestureType;
            RequiredStanceId = requiredStanceId;
            BaseDamage = baseDamage;
            StanceDamageMultiplier = stanceDamageMultiplier;
            CriticalChance = criticalChance;
            CriticalMultiplier = criticalMultiplier;
            FormulaWeakpointMultiplier = formulaWeakpointMultiplier;
            RuleWeakpointMultiplier = ruleWeakpointMultiplier;
            FormulaWrongDirectionMultiplier = formulaWrongDirectionMultiplier;
            MatchingDirectionMultiplier = matchingDirectionMultiplier;
            WrongGestureMultiplier = wrongGestureMultiplier;
            ComboStep = comboStep;
            ComboMaximumMultiplier = comboMaximumMultiplier;
            EnergyPerHit = energyPerHit;
            ScorePerHit = scorePerHit;
            ScorePerDamage = scorePerDamage;
            WeakpointEnergyBonus = weakpointEnergyBonus;
            WeakpointScoreBonus = weakpointScoreBonus;
            WeakpointInterruptsAttack = weakpointInterruptsAttack;
            ReflectedDamage = reflectedDamage;
        }

        // 所有属性均来自配置关联链，计算器不再访问配置服务或 Inspector。
        public string FormulaId { get; }
        public string StanceId { get; }
        public string DefenseRuleId { get; }
        public string WeakpointRuleId { get; }
        public GestureType RequiredGestureType { get; }
        public string RequiredStanceId { get; }
        public long BaseDamage { get; }
        public double StanceDamageMultiplier { get; }
        public double CriticalChance { get; }
        public double CriticalMultiplier { get; }
        public double FormulaWeakpointMultiplier { get; }
        public double RuleWeakpointMultiplier { get; }
        public double FormulaWrongDirectionMultiplier { get; }
        public double MatchingDirectionMultiplier { get; }
        public double WrongGestureMultiplier { get; }
        public double ComboStep { get; }
        public double ComboMaximumMultiplier { get; }
        public long EnergyPerHit { get; }
        public long ScorePerHit { get; }
        public double ScorePerDamage { get; }
        public long WeakpointEnergyBonus { get; }
        public long WeakpointScoreBonus { get; }
        public bool WeakpointInterruptsAttack { get; }
        public long ReflectedDamage { get; }
    }
}
