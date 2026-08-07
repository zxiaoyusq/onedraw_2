using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T430
{
    [Category("T430")]
    public sealed class EnemyStrategyTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T430");
        }

        [Test]
        public void MovementRegistryMapsEveryConfiguredPatternAndRejectsUnknownType()
        {
            MovementStrategyRegistry registry = MovementStrategyRegistry.CreateDefault();
            var expectations = new Dictionary<string, string>
            {
                ["enemy_fire_fish"] = EnemyMovementPatternTypes.Sine,
                ["enemy_wheel_zombie"] = EnemyMovementPatternTypes.Linear,
                ["enemy_talisman_bat"] = EnemyMovementPatternTypes.Dive,
                ["enemy_soul_puppet"] = EnemyMovementPatternTypes.Hover,
                ["boss_tomb_king"] = EnemyMovementPatternTypes.Boss,
            };

            foreach (KeyValuePair<string, string> expectation in expectations)
            {
                EnemyMovementDefinition definition = EnemyMovementDefinitionFactory.Create(
                    config,
                    expectation.Key,
                    registry);
                Assert.That(definition.PatternType, Is.EqualTo(expectation.Value));
                Assert.That(registry.Get(definition.PatternType), Is.Not.Null);
                Assert.That(registry.Sample(definition, 0d).IsValid, Is.True);
            }

            Assert.That(
                () => registry.Get("Teleport"),
                Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void ApproachHoverAndDiveUseOnlyConfiguredPathValues()
        {
            MovementStrategyRegistry registry = MovementStrategyRegistry.CreateDefault();
            EnemyMovementDefinition approach = EnemyMovementDefinitionFactory.Create(
                config,
                "enemy_wheel_zombie",
                registry);
            double approachHalfTime =
                approach.DirectDistanceReferencePixels /
                approach.SpeedReferencePixelsPerSecond /
                2d;
            EnemyMovementSample approachStart = registry.Sample(approach, 0d);
            EnemyMovementSample approachHalf = registry.Sample(approach, approachHalfTime);
            EnemyMovementSample approachEnd = registry.Sample(
                approach,
                approachHalfTime * 2d);

            Assert.That(approachHalf.Progress, Is.EqualTo(0.5d).Within(0.000001d));
            Assert.That(
                approachHalf.XReferencePixels,
                Is.EqualTo((approachStart.XReferencePixels + approachEnd.XReferencePixels) / 2d)
                    .Within(0.000001d));
            Assert.That(approachEnd.Completed, Is.True);

            EnemyMovementDefinition hover = EnemyMovementDefinitionFactory.Create(
                config,
                "enemy_soul_puppet",
                registry);
            double hoverQuarterCycle = 1d / hover.Frequency / 4d;
            EnemyMovementSample hoverStart = registry.Sample(hover, 0d);
            EnemyMovementSample hoverPeak = registry.Sample(hover, hoverQuarterCycle);
            Assert.That(
                hoverPeak.YReferencePixels - hoverStart.YReferencePixels,
                Is.EqualTo(hover.AmplitudeReferencePixels).Within(0.000001d));

            EnemyMovementDefinition dive = EnemyMovementDefinitionFactory.Create(
                config,
                "enemy_talisman_bat",
                registry);
            double diveHalfTime =
                dive.DirectDistanceReferencePixels /
                dive.SpeedReferencePixelsPerSecond /
                2d;
            EnemyMovementSample diveHalf = registry.Sample(dive, diveHalfTime);
            Assert.That(diveHalf.Progress, Is.EqualTo(0.25d).Within(0.000001d));
            Assert.That(
                diveHalf.YReferencePixels,
                Is.GreaterThan((dive.StartYReferencePixels + dive.EndYReferencePixels) / 2d));
        }

        [Test]
        public void AttackRegistrySelectsConfiguredProjectileChargeMeleeAndSupportActions()
        {
            AttackStrategyRegistry registry = AttackStrategyRegistry.CreateDefault();
            IReadOnlyList<EnemyAttackDefinition> puppet =
                EnemyAttackDefinitionFactory.Create(config, "attackset_soul_puppet", registry);
            var bothEligible = new EnemyAttackTriggerContext(
                cooldownReady: true,
                targetInDistance: false,
                hpThresholdReached: false,
                supportTargetId: "ally_1");

            EnemyAttackDefinition support = registry.Select(puppet, bothEligible, 0d);
            EnemyAttackDefinition projectile = registry.Select(puppet, bothEligible, 0.9d);
            Assert.That(support.AttackId, Is.EqualTo("atk_puppet_shield"));
            Assert.That(support.ActionKind, Is.EqualTo(EnemyAttackActionKind.Support));
            Assert.That(support.CreateAction(bothEligible).SupportTargetId, Is.EqualTo("ally_1"));
            Assert.That(projectile.AttackId, Is.EqualTo("atk_puppet_bolt"));
            Assert.That(projectile.ActionKind, Is.EqualTo(EnemyAttackActionKind.Projectile));
            Assert.That(projectile.ProjectileId, Is.EqualTo("proj_seal_bolt"));

            EnemyAttackDefinition charge = EnemyAttackDefinitionFactory.Create(
                config,
                "attackset_wheel_zombie",
                registry)[0];
            EnemyAttackDefinition melee = EnemyAttackDefinitionFactory.Create(
                config,
                "attackset_stone_turtle",
                registry)[0];
            Assert.That(charge.ActionKind, Is.EqualTo(EnemyAttackActionKind.Charge));
            Assert.That(melee.ActionKind, Is.EqualTo(EnemyAttackActionKind.Melee));

            var noneEligible = new EnemyAttackTriggerContext(false, false, false, string.Empty);
            Assert.That(registry.Select(puppet, noneEligible, 0d).IsConfigured, Is.False);
            Assert.That(
                () => registry.Get("AnimationEvent"),
                Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void DefenseServiceKeepsChargedAndStanceGatesButOrdinaryShapesMatchAny()
        {
            var service = new DefenseRuleService(config);
            EnemyDefenseEvaluation shellMatch = service.Evaluate(
                "defense_turtle_shell",
                "Charged",
                "stance_blade");
            EnemyDefenseEvaluation shellWrongStance = service.Evaluate(
                "defense_turtle_shell",
                "Charged",
                "stance_talisman");
            EnemyDefenseEvaluation sealOrdinaryShape = service.Evaluate(
                "defense_direction_seal",
                "Vertical",
                "stance_blade");

            Assert.That(shellMatch.Matches, Is.True);
            Assert.That(shellMatch.ConfiguredDamageMultiplier, Is.EqualTo(1.5d));
            Assert.That(shellMatch.ReflectedDamage, Is.Zero);
            Assert.That(shellWrongStance.Matches, Is.False);
            Assert.That(
                shellWrongStance.ConfiguredDamageMultiplier,
                Is.EqualTo(0.1d).Within(0.000001d));
            Assert.That(sealOrdinaryShape.Matches, Is.True);
            Assert.That(
                sealOrdinaryShape.ConfiguredDamageMultiplier,
                Is.EqualTo(1.3d).Within(0.000001d));
            Assert.That(sealOrdinaryShape.ReflectedDamage, Is.Zero);
            Assert.That(sealOrdinaryShape.BreakEffectGroupId, Is.EqualTo("fx_break_seal"));
        }

        [Test]
        public void DamageReductionBuffUsesConfiguredMagnitudeAndExpiresAtConfiguredBoundary()
        {
            BuffConfig shield = config.GetBuff("buff_shield_50");
            var buffs = new EnemyBuffContainer();
            buffs.Spawn(10d);

            EnemyBuffApplyResult applied = buffs.Apply(
                shield,
                shield.DurationSec,
                "enemy_soul_puppet",
                10d);

            Assert.That(applied.Status, Is.EqualTo(EnemyBuffApplyStatus.Applied));
            Assert.That(buffs.GetIncomingDamageMultiplier(), Is.EqualTo(0.5d));
            Assert.That(buffs.Tick(12.999d), Is.Zero);
            Assert.That(buffs.GetIncomingDamageMultiplier(), Is.EqualTo(0.5d));
            Assert.That(buffs.Tick(13d), Is.EqualTo(1));
            Assert.That(buffs.GetIncomingDamageMultiplier(), Is.EqualTo(1d));
        }
    }
}
