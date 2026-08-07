using System;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T360
{
    [Category("DamageFormula")]
    public sealed class DamageFormulaTests
    {
        private GameplayConfigService config;
        private readonly FixedRandomSource nonCritical = new FixedRandomSource(0.5d);

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), RuntimeConfigTestFixture.Source);
        }

        [Test]
        public void StanceSelectsDamageFormulaThroughRequiredConfigForeignKey()
        {
            DamageRuleSet blade = DamageRuleSetFactory.Create(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointNone);
            DamageRuleSet talisman = DamageRuleSetFactory.Create(
                config,
                ConfigIds.Stances.StanceTalisman,
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointNone);

            Assert.That(config.GetStance(ConfigIds.Stances.StanceBlade).DamageFormulaId,
                Is.EqualTo(ConfigIds.DamageFormulas.DamagePlayerDefault));
            Assert.That(config.GetStance(ConfigIds.Stances.StanceTalisman).DamageFormulaId,
                Is.EqualTo(ConfigIds.DamageFormulas.DamageTalismanDefault));
            Assert.That(blade.FormulaId, Is.EqualTo(ConfigIds.DamageFormulas.DamagePlayerDefault));
            Assert.That(talisman.FormulaId, Is.EqualTo(ConfigIds.DamageFormulas.DamageTalismanDefault));
        }

        [Test]
        public void BodyHitUsesBaseAndStanceTablesWithAwayFromZeroRounding()
        {
            DamageResult blade = Calculate(
                Rules(ConfigIds.Stances.StanceBlade),
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                isWeakpoint: false,
                comboCount: 1,
                nonCritical);
            DamageResult talisman = Calculate(
                Rules(ConfigIds.Stances.StanceTalisman),
                GestureType.Horizontal,
                ConfigIds.Stances.StanceTalisman,
                isWeakpoint: false,
                comboCount: 1,
                nonCritical);

            Assert.That(blade.Damage, Is.EqualTo(12));
            Assert.That(blade.ScoreAward, Is.EqualTo(112));
            Assert.That(blade.EnergyAward, Is.EqualTo(3));
            Assert.That(talisman.Damage, Is.EqualTo(7));
            Assert.That(talisman.ScoreAward, Is.EqualTo(117));
            Assert.That(talisman.EnergyAward, Is.EqualTo(4));
        }

        [Test]
        public void WeakpointMultipliersAndBonusesAreIndependentlyAssertable()
        {
            DamageRuleSet rules = DamageRuleSetFactory.Create(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointForeheadTalisman);
            DamageResult body = Calculate(
                rules,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                isWeakpoint: false,
                comboCount: 1,
                nonCritical);
            DamageResult weakpoint = Calculate(
                rules,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                isWeakpoint: true,
                comboCount: 1,
                nonCritical);

            Assert.That(body.Damage, Is.EqualTo(12));
            Assert.That(body.ScoreAward, Is.EqualTo(112));
            Assert.That(body.EnergyAward, Is.EqualTo(3));
            Assert.That(body.ShouldInterruptAttack, Is.False);
            Assert.That(weakpoint.WeakpointMultiplier, Is.EqualTo(4d).Within(0.000001d));
            Assert.That(weakpoint.Damage, Is.EqualTo(48));
            Assert.That(weakpoint.ScoreAward, Is.EqualTo(398));
            Assert.That(weakpoint.EnergyAward, Is.EqualTo(11));
            Assert.That(weakpoint.ShouldInterruptAttack, Is.True);
        }

        [Test]
        public void ChargedMatchAndOrdinaryMissUseBothConfiguredDefenseBranches()
        {
            DamageRuleSet rules = DamageRuleSetFactory.Create(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.DefenseRules.DefenseTurtleShell,
                ConfigIds.WeakpointRules.WeakpointNone);
            DamageResult matched = Calculate(
                rules,
                GestureType.Charged,
                ConfigIds.Stances.StanceBlade,
                isWeakpoint: false,
                comboCount: 1,
                nonCritical);
            DamageResult missed = Calculate(
                rules,
                GestureType.Any,
                ConfigIds.Stances.StanceBlade,
                isWeakpoint: false,
                comboCount: 1,
                nonCritical);

            Assert.That(matched.DirectionMatched, Is.True);
            Assert.That(matched.DirectionMultiplier, Is.EqualTo(1.5d).Within(0.000001d));
            Assert.That(matched.Damage, Is.EqualTo(18));
            Assert.That(matched.ScoreAward, Is.EqualTo(168));
            Assert.That(matched.EnergyAward, Is.EqualTo(5));
            Assert.That(matched.ReflectedDamage, Is.Zero);
            Assert.That(missed.DirectionMatched, Is.False);
            Assert.That(missed.DirectionMultiplier, Is.EqualTo(0.015d).Within(0.000001d));
            Assert.That(missed.Damage, Is.Zero);
            Assert.That(missed.ScoreAward, Is.EqualTo(2));
            Assert.That(missed.EnergyAward, Is.Zero);
            Assert.That(missed.ReflectedDamage, Is.Zero);
        }

        [Test]
        public void RequiredStanceParticipatesInDirectionMatch()
        {
            DamageRuleSet rules = DamageRuleSetFactory.Create(
                config,
                ConfigIds.Stances.StanceTalisman,
                ConfigIds.DefenseRules.DefenseBossPins,
                ConfigIds.WeakpointRules.WeakpointNone);
            DamageResult result = Calculate(
                rules,
                GestureType.Vertical,
                ConfigIds.Stances.StanceTalisman,
                isWeakpoint: false,
                comboCount: 1,
                nonCritical);

            Assert.That(result.DirectionMatched, Is.False);
            Assert.That(result.ReflectedDamage, Is.EqualTo(4));
        }

        [Test]
        public void CriticalThresholdIsDeterministicAndAddsConfiguredDamageScore()
        {
            DamageRuleSet rules = Rules(ConfigIds.Stances.StanceBlade);
            DamageResult critical = Calculate(
                rules,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                isWeakpoint: false,
                comboCount: 1,
                new FixedRandomSource(0.079999d));
            DamageResult normal = Calculate(
                rules,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                isWeakpoint: false,
                comboCount: 1,
                new FixedRandomSource(0.08d));

            Assert.That(critical.IsCritical, Is.True);
            Assert.That(critical.Damage, Is.EqualTo(18));
            Assert.That(normal.IsCritical, Is.False);
            Assert.That(normal.Damage, Is.EqualTo(12));
            Assert.That(critical.ScoreAward, Is.EqualTo(118));
            Assert.That(normal.ScoreAward, Is.EqualTo(112));
            Assert.That(critical.EnergyAward, Is.EqualTo(normal.EnergyAward));
        }

        [Test]
        public void InvalidContextRandomAndStanceMismatchFailBeforePublication()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DamageContext(0, 1, GestureType.Horizontal, "stance", false, 1, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DamageContext(1, 1, GestureType.None, "stance", false, 1, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DamageContext(1, 1, GestureType.Horizontal, "stance", false, 0, 0d));

            DamageRuleSet rules = Rules(ConfigIds.Stances.StanceBlade);
            var wrongStance = new DamageContext(
                1,
                1,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceTalisman,
                false,
                1,
                0d);
            var valid = new DamageContext(
                1,
                1,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                false,
                1,
                0d);
            Assert.Throws<ArgumentException>(() =>
                DamageCalculator.Calculate(wrongStance, rules, nonCritical));
            Assert.Throws<InvalidOperationException>(() =>
                DamageCalculator.Calculate(valid, rules, new FixedRandomSource(1d)));
        }

        [Test]
        public void WarmCalculationHotPathAllocatesNoManagedMemory()
        {
            DamageRuleSet rules = Rules(ConfigIds.Stances.StanceBlade);
            var context = new DamageContext(
                1,
                1,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                false,
                4,
                0d);
            for (int index = 0; index < 16; index++)
            {
                DamageCalculator.Calculate(context, rules, nonCritical);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 128; index++)
            {
                DamageCalculator.Calculate(context, rules, nonCritical);
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        private DamageRuleSet Rules(string stanceId)
        {
            return DamageRuleSetFactory.Create(
                config,
                stanceId,
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointNone);
        }

        private static DamageResult Calculate(
            in DamageRuleSet rules,
            GestureType gestureType,
            string stanceId,
            bool isWeakpoint,
            int comboCount,
            IRandomSource randomSource)
        {
            var context = new DamageContext(
                1,
                101,
                gestureType,
                stanceId,
                isWeakpoint,
                comboCount,
                1d);
            return DamageCalculator.Calculate(context, rules, randomSource);
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly double value;

            public FixedRandomSource(double value)
            {
                this.value = value;
            }

            public double NextUnitInterval()
            {
                return value;
            }
        }
    }
}
