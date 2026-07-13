using System;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T360
{
    [Category("ComboScore")]
    public sealed class ComboScoreTests
    {
        private GameplayConfigService config;
        private readonly FixedRandomSource nonCritical = new FixedRandomSource();

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), RuntimeConfigTestFixture.Source);
        }

        [Test]
        public void ComboTimeoutComesFromGlobalAndBoundaryIsInclusive()
        {
            ComboService combo = ComboService.FromConfig(config);
            double timeout = combo.TimeoutSeconds;

            Assert.That(combo.TimeoutSeconds, Is.EqualTo(1.8d).Within(0.000001d));
            Assert.That(combo.RegisterHit(10d).Count, Is.EqualTo(1));
            double secondHit = 10d + timeout;
            Assert.That(combo.RegisterHit(secondHit).Count, Is.EqualTo(2));
            double restartedHit = secondHit + timeout + 0.000001d;
            Assert.That(combo.RegisterHit(restartedHit).Count, Is.EqualTo(1));
            Assert.That(combo.AdvanceTime(restartedHit + timeout).IsActive, Is.True);
            Assert.That(combo.AdvanceTime(restartedHit + timeout + 0.000001d).IsActive, Is.False);
        }

        [Test]
        public void SameStrokeTargetsAdvanceComboAndConfiguredMultiplierCaps()
        {
            ComboService combo = ComboService.FromConfig(config);
            DamageRuleSet rules = DamageRuleSetFactory.Create(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointNone);

            DamageResult first = Calculate(rules, combo.RegisterHit(4d).Count, 101);
            DamageResult second = Calculate(rules, combo.RegisterHit(4d).Count, 202);
            DamageResult capped = Calculate(rules, 100, 303);

            Assert.That(first.ComboMultiplier, Is.EqualTo(1d));
            Assert.That(first.Damage, Is.EqualTo(12));
            Assert.That(first.ScoreAward, Is.EqualTo(112));
            Assert.That(first.EnergyAward, Is.EqualTo(3));
            Assert.That(second.ComboMultiplier, Is.EqualTo(1.1d).Within(0.000001d));
            Assert.That(second.Damage, Is.EqualTo(13));
            Assert.That(second.ScoreAward, Is.EqualTo(123));
            Assert.That(second.EnergyAward, Is.EqualTo(3));
            Assert.That(capped.ComboMultiplier, Is.EqualTo(1.5d));
            Assert.That(capped.Damage, Is.EqualTo(18));
            Assert.That(capped.ScoreAward, Is.EqualTo(168));
            Assert.That(capped.EnergyAward, Is.EqualTo(5));
        }

        [Test]
        public void ScoreServiceAggregatesDamageScoreEnergyAndHitDimensions()
        {
            DamageRuleSet rules = DamageRuleSetFactory.Create(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointForeheadTalisman);
            var weakpointContext = new DamageContext(
                8,
                101,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                true,
                1,
                2d);
            var bodyContext = new DamageContext(
                8,
                202,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                false,
                2,
                2d);
            DamageResult weakpoint = DamageCalculator.Calculate(weakpointContext, rules, nonCritical);
            DamageResult body = DamageCalculator.Calculate(bodyContext, rules, nonCritical);
            var score = new ScoreService();

            score.Record(weakpoint);
            CombatScoreSnapshot totals = score.Record(body);

            Assert.That(totals.TotalDamage, Is.EqualTo(61));
            Assert.That(totals.TotalScore, Is.EqualTo(521));
            Assert.That(totals.TotalEnergyEarned, Is.EqualTo(14));
            Assert.That(totals.HitCount, Is.EqualTo(2));
            Assert.That(totals.WeakpointHitCount, Is.EqualTo(1));
            Assert.That(totals.DirectionMatchCount, Is.EqualTo(2));
            Assert.That(totals.CriticalHitCount, Is.Zero);
        }

        [Test]
        public void ResetAndMonotonicGuardsPreventStaleStatePublication()
        {
            ComboService combo = ComboService.FromConfig(config);
            combo.RegisterHit(5d);
            Assert.Throws<ArgumentOutOfRangeException>(() => combo.RegisterHit(4.9d));
            Assert.That(combo.Current.Count, Is.EqualTo(1));
            combo.Reset();
            Assert.That(combo.Current.IsActive, Is.False);
            Assert.That(combo.RegisterHit(1d).Count, Is.EqualTo(1));

            var score = new ScoreService();
            Assert.Throws<ArgumentException>(() => score.Record(default));
            Assert.That(score.Current.HitCount, Is.Zero);
            score.Record(Calculate(
                DamageRuleSetFactory.Create(
                    config,
                    ConfigIds.Stances.StanceBlade,
                    ConfigIds.DefenseRules.DefenseNone,
                    ConfigIds.WeakpointRules.WeakpointNone),
                1,
                101));
            score.Reset();
            Assert.That(score.Current.TotalScore, Is.Zero);
            Assert.That(score.Current.TotalEnergyEarned, Is.Zero);
            Assert.That(score.Current.TotalDamage, Is.Zero);
            Assert.That(score.Current.HitCount, Is.Zero);
        }

        private DamageResult Calculate(in DamageRuleSet rules, int comboCount, int targetId)
        {
            var context = new DamageContext(
                7,
                targetId,
                GestureType.Horizontal,
                rules.StanceId,
                false,
                comboCount,
                4d);
            return DamageCalculator.Calculate(context, rules, nonCritical);
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            public double NextUnitInterval()
            {
                return 0.5d;
            }
        }
    }
}
