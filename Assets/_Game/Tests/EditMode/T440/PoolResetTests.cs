using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T440
{
    [Category("T440")]
    public sealed class PoolResetTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T440");
        }

        [Test]
        public void RejectPolicyPrewarmsAndNeverExceedsSharedFamilyCapacity()
        {
            var created = new List<FakePoolable>();
            using var service = new ObjectPoolService();
            service.RegisterFamily(new PoolFamilyDefinition(
                "enemy",
                2,
                PoolExhaustionPolicy.Reject));
            service.RegisterPool(new PoolDefinition(
                "enemy/ghost",
                "enemy",
                1,
                () => CreateFake(created)));
            service.RegisterPool(new PoolDefinition(
                "enemy/bat",
                "enemy",
                0,
                () => CreateFake(created)));

            Assert.That(service.GetPoolAllocatedCount("enemy/ghost"), Is.EqualTo(1));
            Assert.That(service.GetPoolAllocatedCount("enemy/bat"), Is.Zero);
            PoolAcquireResult ghost = service.Acquire("enemy/ghost");
            PoolAcquireResult bat = service.Acquire("enemy/bat");
            PoolAcquireResult rejected = service.Acquire("enemy/ghost");

            Assert.That(ghost.IsAcquired, Is.True);
            Assert.That(bat.IsAcquired, Is.True);
            Assert.That(rejected.Status, Is.EqualTo(PoolAcquireStatus.RejectedAtCapacity));
            Assert.That(rejected.Item, Is.Null);
            Assert.That(service.GetSnapshot().ActiveCount, Is.EqualTo(2));
            Assert.That(service.GetSnapshot().AllocatedCount, Is.EqualTo(2));
            Assert.That(created, Has.Count.EqualTo(2));
        }

        [Test]
        public void ReuseOldestPolicyFullyReleasesOldestAcrossFamilyBeforeAcquire()
        {
            var poolAItems = new List<FakePoolable>();
            var poolBItems = new List<FakePoolable>();
            using var service = new ObjectPoolService();
            service.RegisterFamily(new PoolFamilyDefinition(
                "vfx",
                2,
                PoolExhaustionPolicy.ReuseOldest));
            service.RegisterPool(new PoolDefinition(
                "vfx/a",
                "vfx",
                2,
                () => CreateFake(poolAItems)));
            service.RegisterPool(new PoolDefinition(
                "vfx/b",
                "vfx",
                1,
                () => CreateFake(poolBItems)));

            PoolAcquireResult first = service.Acquire("vfx/a");
            PoolAcquireResult second = service.Acquire("vfx/a");
            var firstItem = (FakePoolable)first.Item;
            firstItem.DirtyValue = 440;
            PoolAcquireResult replacement = service.Acquire("vfx/b");

            Assert.That(replacement.IsAcquired, Is.True);
            Assert.That(replacement.ReusedOldest, Is.True);
            Assert.That(firstItem.IsPoolActive, Is.False);
            Assert.That(firstItem.DirtyValue, Is.Zero);
            Assert.That(firstItem.LastReleaseReason, Is.EqualTo(PoolReleaseReason.ReusedOldest));
            Assert.That(second.Item.IsPoolActive, Is.True);
            Assert.That(replacement.Item.IsPoolActive, Is.True);
            Assert.That(service.GetSnapshot().ActiveCount, Is.EqualTo(2));
        }

        [Test]
        public void ReleaseRejectsStaleAndDoubleLeaseWithoutTouchingCurrentItem()
        {
            using var service = new ObjectPoolService();
            service.RegisterFamily(new PoolFamilyDefinition(
                "projectile",
                1,
                PoolExhaustionPolicy.Reject));
            service.RegisterPool(new PoolDefinition(
                "projectile/ghost",
                "projectile",
                1,
                () => new FakePoolable()));

            PoolAcquireResult first = service.Acquire("projectile/ghost");
            Assert.That(
                service.Release(first.Item, first.Lease).Status,
                Is.EqualTo(PoolReleaseStatus.Released));
            PoolAcquireResult second = service.Acquire("projectile/ghost");

            Assert.That(second.Item, Is.SameAs(first.Item));
            Assert.That(
                service.Release(second.Item, first.Lease).Status,
                Is.EqualTo(PoolReleaseStatus.StaleLease));
            Assert.That(second.Item.IsPoolActive, Is.True);
            Assert.That(
                service.Release(second.Item, second.Lease).Status,
                Is.EqualTo(PoolReleaseStatus.Released));
            Assert.That(
                service.Release(second.Item, second.Lease).Status,
                Is.EqualTo(PoolReleaseStatus.AlreadyReleased));
            Assert.That(
                service.Release(new FakePoolable(), default).Status,
                Is.EqualTo(PoolReleaseStatus.UnknownItem));
        }

        [Test]
        public void LeakReportNamesActiveLeasesAndRestartInvalidatesAllOfThem()
        {
            using var service = new ObjectPoolService();
            service.RegisterFamily(new PoolFamilyDefinition(
                "damage-number",
                2,
                PoolExhaustionPolicy.ReuseOldest));
            service.RegisterPool(new PoolDefinition(
                "damage-number/default",
                "damage-number",
                2,
                () => new FakePoolable()));
            PoolAcquireResult first = service.Acquire("damage-number/default");
            PoolAcquireResult second = service.Acquire("damage-number/default");

            PoolLeakReport leaks = service.DetectLeaks();
            Assert.That(leaks.Count, Is.EqualTo(2));
            Assert.That(leaks.Leaks[0].Lease.PoolId, Is.EqualTo("damage-number/default"));
            Assert.That(() => service.AssertNoLeaks(), Throws.InvalidOperationException);
            PoolRestartReport restart = service.Restart();

            Assert.That(restart.ReleasedCount, Is.EqualTo(2));
            Assert.That(restart.Generation, Is.Not.EqualTo(restart.PreviousGeneration));
            Assert.That(first.Item.IsPoolActive, Is.False);
            Assert.That(second.Item.IsPoolActive, Is.False);
            Assert.That(service.DetectLeaks().HasLeaks, Is.False);
            Assert.That(service.GetSnapshot().ActiveCount, Is.Zero);
            Assert.That(() => service.AssertNoLeaks(), Throws.Nothing);
        }

        [Test]
        public void ConfigurationMapsAllFourFamiliesAndConfiguredPrewarmValues()
        {
            PoolFamilyDefinition enemy = ObjectPoolConfiguration.CreateEnemyFamily(config);
            PoolFamilyDefinition projectile = ObjectPoolConfiguration.CreateProjectileFamily(config);
            PoolFamilyDefinition vfx = ObjectPoolConfiguration.CreateVfxFamily(config);
            PoolFamilyDefinition damageNumber =
                ObjectPoolConfiguration.CreateDamageNumberFamily(config);

            AssertFamily(enemy, "enemy", 18, PoolExhaustionPolicy.Reject);
            AssertFamily(projectile, "projectile", 40, PoolExhaustionPolicy.Reject);
            AssertFamily(vfx, "vfx", 60, PoolExhaustionPolicy.ReuseOldest);
            AssertFamily(damageNumber, "damage-number", 30, PoolExhaustionPolicy.ReuseOldest);

            PoolDefinition enemyPool = ObjectPoolConfiguration.CreateEnemyPool(
                config,
                ConfigIds.Enemies.EnemySkeletonGhost,
                () => new FakePoolable());
            PoolDefinition projectilePool = ObjectPoolConfiguration.CreateProjectilePool(
                config,
                ConfigIds.Projectiles.ProjGhostFire,
                () => new FakePoolable());
            PoolDefinition vfxPool = ObjectPoolConfiguration.CreateVfxPool(
                config,
                ConfigIds.VfxCues.VfxUltimatePrepare,
                () => new FakePoolable());
            PoolDefinition damagePool = ObjectPoolConfiguration.CreateDamageNumberPool(
                config,
                () => new FakePoolable());

            Assert.That(enemyPool.PoolId, Is.EqualTo("enemy/enemy_skeleton_ghost"));
            Assert.That(enemyPool.PrewarmCount, Is.EqualTo(5));
            Assert.That(projectilePool.PoolId, Is.EqualTo("projectile/proj_ghost_fire"));
            Assert.That(projectilePool.PrewarmCount, Is.EqualTo(8));
            Assert.That(vfxPool.PoolId, Is.EqualTo("vfx/vfx_ultimate_prepare"));
            Assert.That(vfxPool.PrewarmCount, Is.EqualTo(1));
            Assert.That(damagePool.PoolId, Is.EqualTo("damage-number/default"));
            Assert.That(damagePool.PrewarmCount, Is.EqualTo(30));
        }

        private static FakePoolable CreateFake(ICollection<FakePoolable> created)
        {
            var item = new FakePoolable();
            created.Add(item);
            return item;
        }

        private static void AssertFamily(
            in PoolFamilyDefinition definition,
            string familyId,
            int capacity,
            PoolExhaustionPolicy policy)
        {
            Assert.That(definition.FamilyId, Is.EqualTo(familyId));
            Assert.That(definition.Capacity, Is.EqualTo(capacity));
            Assert.That(definition.ExhaustionPolicy, Is.EqualTo(policy));
        }

        private sealed class FakePoolable : IPoolable
        {
            public bool IsPoolActive { get; private set; }

            public int DirtyValue { get; set; }

            public PoolLease Lease { get; private set; }

            public PoolReleaseReason LastReleaseReason { get; private set; }

            public void AcquireFromPool(in PoolLease lease)
            {
                Assert.That(IsPoolActive, Is.False);
                Lease = lease;
                IsPoolActive = true;
            }

            public void ReleaseToPool(in PoolReleaseContext context)
            {
                LastReleaseReason = context.Reason;
                DirtyValue = 0;
                Lease = default;
                IsPoolActive = false;
            }
        }
    }
}
