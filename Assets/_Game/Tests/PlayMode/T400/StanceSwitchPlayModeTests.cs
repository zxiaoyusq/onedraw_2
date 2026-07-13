using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using OneStrokeDemon.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T400
{
    [Category("StanceSwitch")]
    public sealed class StanceSwitchPlayModeTests
    {
        private GameObject playerObject;

        [SetUp]
        public void SetUp()
        {
            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }

            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator SwitchIntentImmediatelyChangesTrailDamageAndProjectileInteraction()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            PlayerCombatController controller = CreateController(config);
            var events = new List<PlayerCombatEvent>();
            controller.CombatEventPublished += events.Add;
            var enemy = new ProjectileOwner(ProjectileFaction.Enemy, 7001);
            var player = new ProjectileOwner(ProjectileFaction.Player, 101);
            ProjectileRuleSet sealBolt = ProjectileRuleSetFactory.Create(
                config,
                ConfigIds.Projectiles.ProjSealBolt);

            StrokeTrailStyle bladeTrail = StrokeTrailSettingsFactory.CreateStyle(
                config,
                controller.Current.StanceId,
                ConfigIds.VfxCues.VfxSlash);
            DamageRuleSet bladeDamage = controller.CreateDamageRules(
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointNone);
            ProjectileStrokeResolution bladeProjectile = ProjectileCutResolver.Resolve(
                sealBolt,
                ProjectileOwnership.FromInitialOwner(enemy),
                controller.Current.StanceId,
                player);
            double bladeCutMultiplier = controller.Current.Stance.ProjectileCutMultiplier;

            StanceSwitchResult switched = controller.TrySwitchStance(
                ConfigIds.Stances.StanceTalisman,
                0d);
            StanceConfig talismanRow = config.GetStance(ConfigIds.Stances.StanceTalisman);
            StrokeTrailStyle talismanTrail = StrokeTrailSettingsFactory.CreateStyle(
                config,
                controller.Current.StanceId,
                ConfigIds.VfxCues.VfxSlash);
            DamageRuleSet talismanDamage = controller.CreateDamageRules(
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointNone);
            ProjectileStrokeResolution talismanProjectile = ProjectileCutResolver.Resolve(
                sealBolt,
                ProjectileOwnership.FromInitialOwner(enemy),
                controller.Current.StanceId,
                player);
            StanceSwitchResult cooldownBlocked = controller.TrySwitchStance(
                ConfigIds.Stances.StanceBlade,
                talismanRow.SwitchCooldownSec * 0.5d);

            Assert.That(switched.Status, Is.EqualTo(StanceSwitchStatus.Switched));
            Assert.That(controller.Current.StanceId,
                Is.EqualTo(ConfigIds.Stances.StanceTalisman));
            Assert.That(switched.OnSwitchEffectGroupId,
                Is.EqualTo(talismanRow.OnSwitchEffectGroupId));
            Assert.That(bladeTrail.WidthReferencePixels,
                Is.EqualTo(config.GetStance(ConfigIds.Stances.StanceBlade).StrokeWidthRefPx));
            Assert.That(talismanTrail.WidthReferencePixels,
                Is.EqualTo(talismanRow.StrokeWidthRefPx));
            Assert.That(talismanTrail.WidthReferencePixels,
                Is.GreaterThan(bladeTrail.WidthReferencePixels));
            Assert.That(bladeDamage.FormulaId,
                Is.EqualTo(ConfigIds.DamageFormulas.DamagePlayerDefault));
            Assert.That(talismanDamage.FormulaId,
                Is.EqualTo(ConfigIds.DamageFormulas.DamageTalismanDefault));
            Assert.That(talismanDamage.StanceDamageMultiplier,
                Is.EqualTo(talismanRow.DamageMultiplier).Within(0.000001d));
            Assert.That(bladeProjectile.Outcome,
                Is.EqualTo(ProjectileStrokeOutcome.RequiredStanceMismatch));
            Assert.That(talismanProjectile.Outcome,
                Is.EqualTo(ProjectileStrokeOutcome.Reflected));
            Assert.That(controller.Current.Stance.ProjectileCutMultiplier,
                Is.EqualTo(talismanRow.ProjectileCutMultiplier).Within(0.000001d));
            Assert.That(controller.Current.Stance.ProjectileCutMultiplier,
                Is.GreaterThan(bladeCutMultiplier));
            Assert.That(cooldownBlocked.Status,
                Is.EqualTo(StanceSwitchStatus.CooldownActive));
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].EventType,
                Is.EqualTo(PlayerCombatEventType.StanceChanged));
            Assert.That(events[0].PreviousStanceId,
                Is.EqualTo(ConfigIds.Stances.StanceBlade));
            Assert.That(events[0].CurrentStanceId,
                Is.EqualTo(ConfigIds.Stances.StanceTalisman));
            Assert.That(events[0].EffectGroupId,
                Is.EqualTo(talismanRow.OnSwitchEffectGroupId));
        }

        [UnityTest]
        public IEnumerator ResolvedDamageEnergySkillCostAndSameFrameDeathShareOnePlayerState()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            PlayerCombatController controller = CreateController(config);
            var events = new List<PlayerCombatEvent>();
            controller.CombatEventPublished += events.Add;
            DamageRuleSet rules = controller.CreateDamageRules(
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointNone);
            var context = new DamageContext(
                81,
                901,
                GestureType.Horizontal,
                controller.Current.StanceId,
                false,
                1,
                0d);
            DamageResult damage = DamageCalculator.Calculate(
                context,
                rules,
                new NonCriticalRandom());

            PlayerEnergyResult earned = controller.GainEnergy(damage, 0d);
            controller.GainEnergy(controller.Current.MaximumEnergy, 0d, "test_fill");
            controller.TrySwitchStance(ConfigIds.Stances.StanceTalisman, 0d);
            SkillConfig talismanSkill = config.GetSkill(ConfigIds.Skills.SkillTalismanBind);
            SkillEnergySpendResult spent = controller.TrySpendSkillEnergy(
                talismanSkill.SkillId,
                0d);
            PlayerDamageResult lethal = controller.ApplyDamage(
                long.MaxValue,
                1d,
                ConfigIds.Projectiles.ProjGhostFire);
            PlayerDamageResult duplicate = controller.ApplyDamage(
                long.MaxValue,
                1d,
                ConfigIds.Projectiles.ProjRockfall);

            Assert.That(earned.AppliedAmount, Is.EqualTo(damage.EnergyAward));
            Assert.That(spent.Status, Is.EqualTo(SkillEnergySpendStatus.Spent));
            Assert.That(spent.ConfiguredEnergyCost, Is.EqualTo(talismanSkill.EnergyCost));
            Assert.That(controller.Current.CurrentEnergy,
                Is.EqualTo(controller.Current.MaximumEnergy - talismanSkill.EnergyCost));
            Assert.That(lethal.DeathTriggered, Is.True);
            Assert.That(duplicate.Status, Is.EqualTo(PlayerDamageStatus.AlreadyDead));
            Assert.That(events.FindAll(item => item.EventType == PlayerCombatEventType.Died),
                Has.Count.EqualTo(1));
            Assert.That(events[events.Count - 1].EventType,
                Is.EqualTo(PlayerCombatEventType.Died));
            Assert.That(events[events.Count - 1].SourceId,
                Is.EqualTo(ConfigIds.Projectiles.ProjGhostFire));
        }

        private PlayerCombatController CreateController(IConfigProvider config)
        {
            playerObject = new GameObject("T400 Player Combat");
            var controller = playerObject.AddComponent<PlayerCombatController>();
            controller.Initialize(config, ConfigIds.Players.PlayerMoyan);
            return controller;
        }

        private static IEnumerator LoadRuntimeConfiguration()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.MainMenu &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));
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
