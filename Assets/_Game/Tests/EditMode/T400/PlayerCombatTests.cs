using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T400
{
    [Category("PlayerCombat")]
    public sealed class PlayerCombatTests
    {
        private GameplayConfigService config;
        private PlayerCombatSettings settings;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), RuntimeConfigTestFixture.Source);
            settings = PlayerCombatSettingsFactory.Create(
                config,
                ConfigIds.Players.PlayerMoyan);
        }

        [Test]
        public void FactoryMapsPlayerAndDefaultStanceWithoutInspectorValues()
        {
            PlayerConfig player = config.GetPlayer(ConfigIds.Players.PlayerMoyan);
            SkillConfig ultimate = config.GetSkill(player.UltimateSkillId);
            PlayerCombatModel model = CreateModel();

            Assert.That(settings.PlayerId, Is.EqualTo(player.PlayerId));
            Assert.That(settings.MaximumHp, Is.EqualTo(player.MaxHp));
            Assert.That(settings.MaximumEnergy, Is.EqualTo(player.MaxEnergy));
            Assert.That(settings.DefaultStanceId, Is.EqualTo(player.DefaultStanceId));
            Assert.That(settings.UltimateSkillId, Is.EqualTo(player.UltimateSkillId));
            Assert.That(settings.UltimateEnergyCost, Is.EqualTo(ultimate.EnergyCost));
            Assert.That(settings.HitInvulnerabilitySeconds,
                Is.EqualTo(player.HitInvulnSec).Within(0.000001d));
            Assert.That(model.Current.CurrentHp, Is.EqualTo(player.MaxHp));
            Assert.That(model.Current.CurrentEnergy, Is.Zero);
            Assert.That(model.Current.StanceId, Is.EqualTo(player.DefaultStanceId));
        }

        [Test]
        public void DamageUsesConfiguredInvulnerabilityAndDeathTransitionsOnce()
        {
            PlayerCombatModel model = CreateModel();
            PlayerDamageResult first = model.ApplyDamage(10L, 1d);
            PlayerDamageResult blocked = model.ApplyDamage(90L, 1d);
            double boundary = 1d + settings.HitInvulnerabilitySeconds;
            PlayerDamageResult lethal = model.ApplyDamage(long.MaxValue, boundary);
            PlayerDamageResult duplicate = model.ApplyDamage(1L, boundary);
            StanceSwitchResult deadSwitch = model.TrySwitchStance(
                ConfigIds.Stances.StanceTalisman,
                boundary);

            Assert.That(first.Status, Is.EqualTo(PlayerDamageStatus.Applied));
            Assert.That(first.AppliedDamage, Is.EqualTo(10L));
            Assert.That(first.State.CurrentHp, Is.EqualTo(settings.MaximumHp - 10L));
            Assert.That(blocked.Status, Is.EqualTo(PlayerDamageStatus.Invulnerable));
            Assert.That(blocked.AppliedDamage, Is.Zero);
            Assert.That(lethal.AppliedDamage, Is.EqualTo(settings.MaximumHp - 10L));
            Assert.That(lethal.State.CurrentHp, Is.Zero);
            Assert.That(lethal.DeathTriggered, Is.True);
            Assert.That(duplicate.Status, Is.EqualTo(PlayerDamageStatus.AlreadyDead));
            Assert.That(duplicate.DeathTriggered, Is.False);
            Assert.That(deadSwitch.Status, Is.EqualTo(StanceSwitchStatus.PlayerDead));
            Assert.That(deadSwitch.DidSwitch, Is.False);
            Assert.That(model.Current.StanceId, Is.EqualTo(settings.DefaultStanceId));
        }

        [Test]
        public void ControllerPublishesOrderedHpAndSingleDeathEvents()
        {
            var gameObject = new GameObject("T400 Player Controller");
            try
            {
                var controller = gameObject.AddComponent<PlayerCombatController>();
                controller.Initialize(config, ConfigIds.Players.PlayerMoyan);
                var events = new List<PlayerCombatEvent>();
                controller.CombatEventPublished += events.Add;

                controller.ApplyDamage(long.MaxValue, 0d, "enemy_attack");
                controller.ApplyDamage(long.MaxValue, 0d, "same_frame_attack");

                Assert.That(events, Has.Count.EqualTo(2));
                Assert.That(events[0].Sequence, Is.EqualTo(1));
                Assert.That(events[0].EventType, Is.EqualTo(PlayerCombatEventType.HpChanged));
                Assert.That(events[0].SignedAmount, Is.EqualTo(-settings.MaximumHp));
                Assert.That(events[0].State.IsDead, Is.True);
                Assert.That(events[1].Sequence, Is.EqualTo(2));
                Assert.That(events[1].EventType, Is.EqualTo(PlayerCombatEventType.Died));
                Assert.That(events[1].SourceId, Is.EqualTo("enemy_attack"));
                Assert.That(events.FindAll(item => item.EventType == PlayerCombatEventType.Died),
                    Has.Count.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DamageEnergyAwardSaturatesAtConfiguredCapacityWithoutOverflow()
        {
            PlayerCombatModel model = CreateModel();
            DamageResult damage = ResolveDamageEnergy();
            PlayerEnergyResult first = model.GainEnergy(damage);
            PlayerEnergyResult capped = model.GainEnergy(long.MaxValue);
            PlayerEnergyResult full = model.GainEnergy(1L);

            Assert.That(first.AppliedAmount, Is.EqualTo(damage.EnergyAward));
            Assert.That(capped.AppliedAmount,
                Is.EqualTo(settings.MaximumEnergy - damage.EnergyAward));
            Assert.That(capped.State.CurrentEnergy, Is.EqualTo(settings.MaximumEnergy));
            Assert.That(full.Status, Is.EqualTo(PlayerEnergyStatus.AtCapacity));
            Assert.That(full.AppliedAmount, Is.Zero);
        }

        [Test]
        public void SkillEnergyCostAndRequiredStanceComeFromSkillRows()
        {
            var gameObject = new GameObject("T400 Skill Energy Controller");
            try
            {
                var controller = gameObject.AddComponent<PlayerCombatController>();
                controller.Initialize(config, ConfigIds.Players.PlayerMoyan);
                SkillConfig talismanSkill = config.GetSkill(ConfigIds.Skills.SkillTalismanBind);
                controller.GainEnergy(settings.MaximumEnergy, 0d, "test_gain");

                SkillEnergySpendResult wrongStance = controller.TrySpendSkillEnergy(
                    talismanSkill.SkillId,
                    0d);
                StanceSwitchResult switched = controller.TrySwitchStance(
                    talismanSkill.RequiredStanceId,
                    0d);
                SkillEnergySpendResult spent = controller.TrySpendSkillEnergy(
                    talismanSkill.SkillId,
                    0d);
                SkillEnergySpendResult ultimate = controller.TrySpendUltimateEnergy(0d);

                Assert.That(wrongStance.Status, Is.EqualTo(SkillEnergySpendStatus.WrongStance));
                Assert.That(wrongStance.ConfiguredEnergyCost,
                    Is.EqualTo(talismanSkill.EnergyCost));
                Assert.That(controller.Model.Settings.UltimateEnergyCost,
                    Is.EqualTo(config.GetSkill(settings.UltimateSkillId).EnergyCost));
                Assert.That(switched.DidSwitch, Is.True);
                Assert.That(spent.Status, Is.EqualTo(SkillEnergySpendStatus.Spent));
                Assert.That(spent.EnergyResult.AppliedAmount,
                    Is.EqualTo(talismanSkill.EnergyCost));
                Assert.That(controller.Current.CurrentEnergy,
                    Is.EqualTo(settings.MaximumEnergy - talismanSkill.EnergyCost));
                Assert.That(ultimate.Status,
                    Is.EqualTo(SkillEnergySpendStatus.InsufficientEnergy));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void StanceSwitchUsesDestinationCooldownAndPublishesConfiguredEffectIntent()
        {
            var service = new StanceService(config, settings.DefaultStanceId);
            StanceConfig talisman = config.GetStance(ConfigIds.Stances.StanceTalisman);
            StanceSwitchResult first = service.TrySwitch(talisman.StanceId, 5d);
            StanceSwitchResult blocked = service.TrySwitch(
                settings.DefaultStanceId,
                5d + (talisman.SwitchCooldownSec * 0.5d));
            StanceSwitchResult boundary = service.TrySwitch(
                settings.DefaultStanceId,
                5d + talisman.SwitchCooldownSec);

            Assert.That(first.Status, Is.EqualTo(StanceSwitchStatus.Switched));
            Assert.That(first.Previous.StanceId, Is.EqualTo(settings.DefaultStanceId));
            Assert.That(first.Current.StanceId, Is.EqualTo(talisman.StanceId));
            Assert.That(first.OnSwitchEffectGroupId,
                Is.EqualTo(talisman.OnSwitchEffectGroupId));
            Assert.That(first.NextSwitchAvailableAt,
                Is.EqualTo(5d + talisman.SwitchCooldownSec).Within(0.000001d));
            Assert.That(blocked.Status, Is.EqualTo(StanceSwitchStatus.CooldownActive));
            Assert.That(blocked.Current.StanceId, Is.EqualTo(talisman.StanceId));
            Assert.That(blocked.OnSwitchEffectGroupId, Is.Empty);
            Assert.That(boundary.Status, Is.EqualTo(StanceSwitchStatus.Switched));
        }

        [Test]
        public void CurrentStanceExposesAllConfiguredCombatAndPresentationModifiers()
        {
            var service = new StanceService(config, ConfigIds.Stances.StanceBlade);
            StanceSnapshot blade = service.Current;
            StanceSnapshot talisman = service.TrySwitch(
                ConfigIds.Stances.StanceTalisman,
                0d).Current;

            AssertStanceMatchesConfig(blade, config.GetStance(blade.StanceId));
            AssertStanceMatchesConfig(talisman, config.GetStance(talisman.StanceId));
            Assert.That(talisman.StrokeWidthReferencePixels,
                Is.GreaterThan(blade.StrokeWidthReferencePixels));
            Assert.That(talisman.ProjectileCutMultiplier,
                Is.GreaterThan(blade.ProjectileCutMultiplier));
            Assert.That(talisman.GhostDamageMultiplier,
                Is.GreaterThan(blade.GhostDamageMultiplier));
        }

        [Test]
        public void InvalidAmountsAndBackwardsTimestampsFailBeforeStateMutation()
        {
            PlayerCombatModel model = CreateModel();
            model.ApplyDamage(1L, 2d);
            PlayerCombatSnapshot before = model.Current;

            Assert.Throws<ArgumentOutOfRangeException>(() => model.ApplyDamage(-1L, 2d));
            Assert.Throws<ArgumentOutOfRangeException>(() => model.ApplyDamage(1L, 1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => model.GainEnergy(-1L));
            Assert.Throws<ArgumentOutOfRangeException>(() => model.TrySpendEnergy(-1L));
            Assert.That(model.Current.CurrentHp, Is.EqualTo(before.CurrentHp));
            Assert.That(model.Current.CurrentEnergy, Is.EqualTo(before.CurrentEnergy));

            var service = new StanceService(config, settings.DefaultStanceId);
            service.TrySwitch(ConfigIds.Stances.StanceTalisman, 2d);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.TrySwitch(settings.DefaultStanceId, 1d));
            Assert.That(service.Current.StanceId,
                Is.EqualTo(ConfigIds.Stances.StanceTalisman));
        }

        private PlayerCombatModel CreateModel()
        {
            return new PlayerCombatModel(
                settings,
                new StanceService(config, settings.DefaultStanceId));
        }

        private DamageResult ResolveDamageEnergy()
        {
            DamageRuleSet rules = DamageRuleSetFactory.Create(
                config,
                settings.DefaultStanceId,
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointNone);
            var context = new DamageContext(
                1,
                100,
                GestureType.Horizontal,
                settings.DefaultStanceId,
                false,
                1,
                0d);
            return DamageCalculator.Calculate(context, rules, new NonCriticalRandom());
        }

        private static void AssertStanceMatchesConfig(
            in StanceSnapshot snapshot,
            StanceConfig row)
        {
            Assert.That(snapshot.StanceId, Is.EqualTo(row.StanceId));
            Assert.That(snapshot.DamageFormulaId, Is.EqualTo(row.DamageFormulaId));
            Assert.That(snapshot.DamageMultiplier,
                Is.EqualTo(row.DamageMultiplier).Within(0.000001d));
            Assert.That(snapshot.GhostDamageMultiplier,
                Is.EqualTo(row.GhostDamageMultiplier).Within(0.000001d));
            Assert.That(snapshot.ProjectileCutMultiplier,
                Is.EqualTo(row.ProjectileCutMultiplier).Within(0.000001d));
            Assert.That(snapshot.StrokeWidthReferencePixels,
                Is.EqualTo(row.StrokeWidthRefPx));
            Assert.That(snapshot.SwitchCooldownSeconds,
                Is.EqualTo(row.SwitchCooldownSec).Within(0.000001d));
            Assert.That(snapshot.OnSwitchEffectGroupId,
                Is.EqualTo(row.OnSwitchEffectGroupId));
            Assert.That(snapshot.AssetKey, Is.EqualTo(row.AssetKey));
        }

        private sealed class NonCriticalRandom : IRandomSource
        {
            public double NextUnitInterval()
            {
                return 0.5d;
            }
        }
    }
}
