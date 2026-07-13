using System.Collections;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T440
{
    [Category("T440")]
    public sealed class RestartThreeTimesPlayModeTests
    {
        private GameObject poolRoot;
        private GameObject runtimeReference;

        [SetUp]
        public void SetUp()
        {
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (poolRoot != null)
            {
                Object.DestroyImmediate(poolRoot);
            }

            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator SpawnKillClearAndRestartThreeTimesLeavesNoOldState()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            poolRoot = new GameObject("T440 Pool Root");
            runtimeReference = new GameObject("T440 Runtime Reference");
            runtimeReference.transform.SetParent(poolRoot.transform, false);
            using var service = CreateConfiguredService(config);
            var listenerCounts = new int[3];
            var frozenListenerCounts = new int[3];
            EnemyController previousEnemy = null;
            ProjectileController previousProjectile = null;
            VfxPoolItem previousVfx = null;
            DamageNumberPoolItem previousDamageNumber = null;

            for (int cycle = 0; cycle < 3; cycle++)
            {
                PoolAcquireResult enemyResult = service.Acquire(
                    ObjectPoolConfiguration.GetEnemyPoolId(
                        ConfigIds.Enemies.EnemySkeletonGhost));
                PoolAcquireResult projectileResult = service.Acquire(
                    ObjectPoolConfiguration.GetProjectilePoolId(
                        ConfigIds.Projectiles.ProjGhostFire));
                PoolAcquireResult vfxResult = service.Acquire(
                    ObjectPoolConfiguration.GetVfxPoolId(
                        ConfigIds.VfxCues.VfxUltimatePrepare));
                PoolAcquireResult damageNumberResult = service.Acquire(
                    ObjectPoolConfiguration.DamageNumberPoolId);
                var enemy = (EnemyController)enemyResult.Item;
                var projectile = (ProjectileController)projectileResult.Item;
                var vfx = (VfxPoolItem)vfxResult.Item;
                var damageNumber = (DamageNumberPoolItem)damageNumberResult.Item;

                Assert.That(enemyResult.IsAcquired, Is.True);
                Assert.That(projectileResult.IsAcquired, Is.True);
                Assert.That(vfxResult.IsAcquired, Is.True);
                Assert.That(damageNumberResult.IsAcquired, Is.True);
                if (cycle > 0)
                {
                    Assert.That(enemy, Is.SameAs(previousEnemy));
                    Assert.That(projectile, Is.SameAs(previousProjectile));
                    Assert.That(vfx, Is.SameAs(previousVfx));
                    Assert.That(damageNumber, Is.SameAs(previousDamageNumber));
                }

                int capturedCycle = cycle;
                enemy.CombatEventPublished += _ => listenerCounts[capturedCycle]++;
                enemy.Spawn(
                    config,
                    ConfigIds.Enemies.EnemySkeletonGhost,
                    44000 + cycle,
                    cycle);
                Assert.That(enemy.CompleteSpawn(cycle), Is.True);
                BuffConfig slow = config.GetBuff(ConfigIds.Buffs.BuffSlow30);
                Assert.That(
                    enemy.ApplyBuff(slow, slow.DurationSec, "T440", cycle + 0.1d).Changed,
                    Is.True);
                Assert.That(
                    enemy.IncrementCounter("restart_counter", 1d, 3d, "T440", cycle + 0.2d),
                    Is.True);
                Assert.That(
                    enemy.ApplyDamage(10000L, "T440_kill", cycle + 0.3d).DeathTriggered,
                    Is.True);

                ProjectileRuleSet projectileRules = ProjectileRuleSetFactory.Create(
                    config,
                    ConfigIds.Projectiles.ProjGhostFire);
                projectile.Spawn(
                    projectileRules,
                    44100 + cycle,
                    new ProjectileOwner(ProjectileFaction.Enemy, 44200 + cycle),
                    runtimeReference.transform,
                    new Vector2(12f + cycle, 24f),
                    Vector2.right);
                projectile.Tick(0.25f);
                vfx.Play(null, new Vector3(30f + cycle, 40f, 0f));
                vfx.Advance(0.25f);
                damageNumber.Show(
                    -55L - cycle,
                    44000 + cycle,
                    "T440_damage",
                    new Vector3(50f, 60f + cycle, 0f));

                for (int earlier = 0; earlier < cycle; earlier++)
                {
                    Assert.That(
                        listenerCounts[earlier],
                        Is.EqualTo(frozenListenerCounts[earlier]),
                        $"cycle {earlier} listener leaked into cycle {cycle}");
                }

                Assert.That(service.DetectLeaks().Count, Is.EqualTo(4));
                PoolRestartReport restart = service.Restart();
                Assert.That(restart.ReleasedCount, Is.EqualTo(4));
                frozenListenerCounts[cycle] = listenerCounts[cycle];
                AssertFullyReset(enemy, projectile, vfx, damageNumber);
                service.AssertNoLeaks();
                Assert.That(service.GetSnapshot().ActiveCount, Is.Zero);

                for (int earlier = 0; earlier <= cycle; earlier++)
                {
                    Assert.That(listenerCounts[earlier], Is.EqualTo(frozenListenerCounts[earlier]));
                }

                previousEnemy = enemy;
                previousProjectile = projectile;
                previousVfx = vfx;
                previousDamageNumber = damageNumber;
                yield return null;
            }

            Assert.That(service.Generation, Is.EqualTo(4U));
            Assert.That(service.GetSnapshot().AllocatedCount, Is.EqualTo(44));
        }

        private ObjectPoolService CreateConfiguredService(IConfigProvider config)
        {
            var service = new ObjectPoolService();
            service.RegisterFamily(ObjectPoolConfiguration.CreateEnemyFamily(config));
            service.RegisterFamily(ObjectPoolConfiguration.CreateProjectileFamily(config));
            service.RegisterFamily(ObjectPoolConfiguration.CreateVfxFamily(config));
            service.RegisterFamily(ObjectPoolConfiguration.CreateDamageNumberFamily(config));
            service.RegisterPool(ObjectPoolConfiguration.CreateEnemyPool(
                config,
                ConfigIds.Enemies.EnemySkeletonGhost,
                CreateEnemy));
            service.RegisterPool(ObjectPoolConfiguration.CreateProjectilePool(
                config,
                ConfigIds.Projectiles.ProjGhostFire,
                CreateProjectile));
            service.RegisterPool(ObjectPoolConfiguration.CreateVfxPool(
                config,
                ConfigIds.VfxCues.VfxUltimatePrepare,
                () => CreateVfx(config)));
            service.RegisterPool(ObjectPoolConfiguration.CreateDamageNumberPool(
                config,
                CreateDamageNumber));
            return service;
        }

        private EnemyController CreateEnemy()
        {
            var item = new GameObject("T440 Pooled Enemy");
            item.transform.SetParent(poolRoot.transform, false);
            item.SetActive(false);
            item.AddComponent<Damageable>();
            EnemyController controller = item.AddComponent<EnemyController>();
            controller.enabled = false;
            return controller;
        }

        private ProjectileController CreateProjectile()
        {
            var item = new GameObject("T440 Pooled Projectile");
            item.transform.SetParent(poolRoot.transform, false);
            item.SetActive(false);
            ProjectileController controller = item.AddComponent<ProjectileController>();
            controller.enabled = false;
            return controller;
        }

        private VfxPoolItem CreateVfx(IConfigProvider config)
        {
            var item = new GameObject("T440 Pooled VFX");
            item.transform.SetParent(poolRoot.transform, false);
            item.SetActive(false);
            VfxPoolItem vfx = item.AddComponent<VfxPoolItem>();
            vfx.enabled = false;
            vfx.Configure(config, ConfigIds.VfxCues.VfxUltimatePrepare);
            return vfx;
        }

        private DamageNumberPoolItem CreateDamageNumber()
        {
            var item = new GameObject("T440 Pooled Damage Number");
            item.transform.SetParent(poolRoot.transform, false);
            item.SetActive(false);
            DamageNumberPoolItem damageNumber = item.AddComponent<DamageNumberPoolItem>();
            damageNumber.enabled = false;
            return damageNumber;
        }

        private void AssertFullyReset(
            EnemyController enemy,
            ProjectileController projectile,
            VfxPoolItem vfx,
            DamageNumberPoolItem damageNumber)
        {
            Assert.That(enemy.IsPoolActive, Is.False);
            Assert.That(enemy.IsSpawned, Is.False);
            Assert.That(enemy.State.State, Is.EqualTo(EnemyState.None));
            Assert.That(enemy.Damage.IsActive, Is.False);
            Assert.That(enemy.Buffs.Count, Is.Zero);
            Assert.That(enemy.TryGetCounter("restart_counter", out _), Is.False);
            AssertTransformReset(enemy.transform);

            Assert.That(projectile.IsPoolActive, Is.False);
            Assert.That(projectile.IsActive, Is.False);
            Assert.That(projectile.Rules.IsConfigured, Is.False);
            Assert.That(projectile.Ownership.IsValid, Is.False);
            Assert.That(projectile.ReferenceSpace, Is.Null);
            Assert.That(projectile.ReferencePosition, Is.EqualTo(Vector2.zero));
            Assert.That(projectile.TravelDirection, Is.EqualTo(Vector2.zero));
            Assert.That(projectile.ElapsedSeconds, Is.Zero);
            Assert.That(projectile.HitTarget.HitTargetId, Is.Zero);
            Assert.That(projectile.HitCollider.enabled, Is.False);
            AssertTransformReset(projectile.transform);

            Assert.That(vfx.IsPoolActive, Is.False);
            Assert.That(vfx.IsConfigured, Is.True);
            Assert.That(vfx.IsPlaying, Is.False);
            Assert.That(vfx.ElapsedSeconds, Is.Zero);
            Assert.That(vfx.FollowTarget, Is.Null);
            AssertTransformReset(vfx.transform);

            Assert.That(damageNumber.IsPoolActive, Is.False);
            Assert.That(damageNumber.IsVisible, Is.False);
            Assert.That(damageNumber.Amount, Is.Zero);
            Assert.That(damageNumber.TargetId, Is.Zero);
            Assert.That(damageNumber.SourceId, Is.Empty);
            AssertTransformReset(damageNumber.transform);
        }

        private void AssertTransformReset(Transform item)
        {
            Assert.That(item.parent, Is.SameAs(poolRoot.transform));
            Assert.That(item.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(item.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(item.localScale, Is.EqualTo(Vector3.one));
            Assert.That(item.gameObject.activeSelf, Is.False);
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
    }
}
