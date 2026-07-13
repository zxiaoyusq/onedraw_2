using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T370
{
    [Category("ProjectileCut")]
    public sealed class ProjectileCutTests
    {
        private GameplayConfigService config;
        private ProjectileOwner enemy;
        private ProjectileOwner player;

        [SetUp]
        public void SetUp()
        {
            config = Load(RuntimeConfigTestFixture.LoadJson());
            enemy = new ProjectileOwner(ProjectileFaction.Enemy, 501);
            player = new ProjectileOwner(ProjectileFaction.Player, 101);
        }

        [Test]
        public void FactoryMapsEveryRuntimeValueFromProjectileTable()
        {
            ProjectileRuleSet rules = Rules(ConfigIds.Projectiles.ProjGhostFire);

            Assert.That(rules.ProjectileId, Is.EqualTo(ConfigIds.Projectiles.ProjGhostFire));
            Assert.That(rules.MovePatternId, Is.EqualTo(ConfigIds.MovePatterns.MoveGroundLeft));
            Assert.That(rules.SpeedReferencePixelsPerSecond, Is.EqualTo(260f));
            Assert.That(rules.LifetimeSeconds, Is.EqualTo(4f));
            Assert.That(rules.Damage, Is.EqualTo(8));
            Assert.That(rules.Cuttable, Is.True);
            Assert.That(rules.Reflectable, Is.True);
            Assert.That(rules.RequiredStanceId, Is.Empty);
            Assert.That(rules.HitRadiusReferencePixels, Is.EqualTo(26f));
            Assert.That(rules.AssetKey, Is.EqualTo(ConfigIds.Projectiles.ProjGhostFire));
            Assert.That(rules.VfxKey, Is.EqualTo(ConfigIds.VfxCues.VfxProjectileCut));
        }

        [Test]
        public void ReflectableRuleTakesPrecedenceAndPreservesOriginalEnemySource()
        {
            ProjectileRuleSet rules = Rules(ConfigIds.Projectiles.ProjGhostFire);
            ProjectileOwnership ownership = ProjectileOwnership.FromInitialOwner(enemy);

            ProjectileStrokeResolution resolution = ProjectileCutResolver.Resolve(
                rules,
                ownership,
                ConfigIds.Stances.StanceBlade,
                player);
            ProjectileOwnership reflected = ownership.ReflectTo(player);

            Assert.That(resolution.Outcome, Is.EqualTo(ProjectileStrokeOutcome.Reflected));
            Assert.That(resolution.ChangesOwnership, Is.True);
            Assert.That(resolution.ReleasesProjectile, Is.False);
            Assert.That(reflected.CurrentOwner.Faction, Is.EqualTo(ProjectileFaction.Player));
            Assert.That(reflected.CurrentOwner.EntityId, Is.EqualTo(101));
            Assert.That(reflected.OriginalOwner.Faction, Is.EqualTo(ProjectileFaction.Enemy));
            Assert.That(reflected.OriginalOwner.EntityId, Is.EqualTo(501));
            Assert.That(reflected.ReflectionCount, Is.EqualTo(1));
            Assert.That(reflected.CanDamage(enemy), Is.True);
            Assert.That(reflected.CanDamage(player), Is.False);
        }

        [Test]
        public void CuttableNonReflectableRuleReleasesProjectile()
        {
            ProjectileStrokeResolution resolution = ProjectileCutResolver.Resolve(
                Rules(ConfigIds.Projectiles.ProjRockfall),
                ProjectileOwnership.FromInitialOwner(enemy),
                ConfigIds.Stances.StanceBlade,
                player);

            Assert.That(resolution.Outcome, Is.EqualTo(ProjectileStrokeOutcome.Cut));
            Assert.That(resolution.ReleasesProjectile, Is.True);
            Assert.That(resolution.ChangesOwnership, Is.False);
        }

        [Test]
        public void BothInteractionFlagsFalseProducesConfiguredUncuttableBranch()
        {
            string json = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                JObject row = ((JArray)root["projectiles"])
                    .Children<JObject>()
                    .Single(item =>
                        item.Value<string>("projectileId") ==
                        ConfigIds.Projectiles.ProjRockfall);
                row["cuttable"] = false;
                row["reflectable"] = false;
            });
            GameplayConfigService uncuttableConfig = Load(json);
            ProjectileRuleSet rules = ProjectileRuleSetFactory.Create(
                uncuttableConfig,
                ConfigIds.Projectiles.ProjRockfall);

            ProjectileStrokeResolution resolution = ProjectileCutResolver.Resolve(
                rules,
                ProjectileOwnership.FromInitialOwner(enemy),
                ConfigIds.Stances.StanceBlade,
                player);

            Assert.That(rules.Cuttable, Is.False);
            Assert.That(rules.Reflectable, Is.False);
            Assert.That(resolution.Outcome, Is.EqualTo(ProjectileStrokeOutcome.Uncuttable));
            Assert.That(resolution.ReleasesProjectile, Is.False);
            Assert.That(resolution.ChangesOwnership, Is.False);
        }

        [Test]
        public void RequiredStanceGatesBothCutAndReflectionBeforeFlags()
        {
            ProjectileRuleSet rules = Rules(ConfigIds.Projectiles.ProjSealBolt);
            ProjectileOwnership ownership = ProjectileOwnership.FromInitialOwner(enemy);

            ProjectileStrokeResolution mismatch = ProjectileCutResolver.Resolve(
                rules,
                ownership,
                ConfigIds.Stances.StanceBlade,
                player);
            ProjectileStrokeResolution match = ProjectileCutResolver.Resolve(
                rules,
                ownership,
                ConfigIds.Stances.StanceTalisman,
                player);

            Assert.That(rules.RequiredStanceId, Is.EqualTo(ConfigIds.Stances.StanceTalisman));
            Assert.That(mismatch.Outcome,
                Is.EqualTo(ProjectileStrokeOutcome.RequiredStanceMismatch));
            Assert.That(match.Outcome, Is.EqualTo(ProjectileStrokeOutcome.Reflected));
        }

        [Test]
        public void ReflectedFriendlyProjectileCannotBeProcessedBySameFactionAgain()
        {
            ProjectileOwnership reflected =
                ProjectileOwnership.FromInitialOwner(enemy).ReflectTo(player);

            ProjectileStrokeResolution result = ProjectileCutResolver.Resolve(
                Rules(ConfigIds.Projectiles.ProjGhostFire),
                reflected,
                ConfigIds.Stances.StanceBlade,
                player);

            Assert.That(result.Outcome, Is.EqualTo(ProjectileStrokeOutcome.FriendlyOwned));
            Assert.That(result.ReleasesProjectile, Is.False);
            Assert.That(result.ChangesOwnership, Is.False);
        }

        [Test]
        public void InvalidOwnershipStanceAndReflectionAreRejectedWithoutFallback()
        {
            ProjectileRuleSet rules = Rules(ConfigIds.Projectiles.ProjGhostFire);
            ProjectileOwnership ownership = ProjectileOwnership.FromInitialOwner(enemy);

            Assert.Throws<ArgumentException>(() =>
                ProjectileCutResolver.Resolve(rules, default, "stance", player));
            Assert.Throws<ArgumentException>(() =>
                ProjectileCutResolver.Resolve(rules, ownership, string.Empty, player));
            Assert.Throws<ArgumentException>(() => ownership.ReflectTo(enemy));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ProjectileOwner(ProjectileFaction.None, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ProjectileOwner(ProjectileFaction.Player, 0));
        }

        [Test]
        public void WarmCutResolutionPathAllocatesNoManagedMemory()
        {
            ProjectileRuleSet rules = Rules(ConfigIds.Projectiles.ProjGhostFire);
            ProjectileOwnership ownership = ProjectileOwnership.FromInitialOwner(enemy);
            for (int index = 0; index < 16; index++)
            {
                ProjectileCutResolver.Resolve(
                    rules,
                    ownership,
                    ConfigIds.Stances.StanceBlade,
                    player);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 128; index++)
            {
                ProjectileCutResolver.Resolve(
                    rules,
                    ownership,
                    ConfigIds.Stances.StanceBlade,
                    player);
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        private ProjectileRuleSet Rules(string projectileId)
        {
            return ProjectileRuleSetFactory.Create(config, projectileId);
        }

        private static GameplayConfigService Load(string json)
        {
            var service = new GameplayConfigService();
            service.Load(json, RuntimeConfigTestFixture.Source);
            return service;
        }
    }
}
