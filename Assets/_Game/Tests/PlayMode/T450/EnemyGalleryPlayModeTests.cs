using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T450
{
    [Category("T450")]
    public sealed class EnemyGalleryPlayModeTests
    {
        private GameObject galleryRoot;

        [SetUp]
        public void SetUp()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            PointerInputRuntime.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (galleryRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(galleryRoot);
            }

            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            PointerInputRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator SixConfiguredEnemiesSpawnMoveTelegraphAttackAndReturnToPools()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            IAssetRegistry assets = AssetRegistryRuntime.Current;
            galleryRoot = new GameObject("T450 Enemy Gallery");
            var world = new RecordingAttackWorld();
            using var pool = new EnemyArchetypePool(
                config,
                assets,
                galleryRoot.transform);

            Assert.That(pool.Archetypes.Count, Is.EqualTo(6));
            Assert.That(pool.Snapshot.FamilyCount, Is.EqualTo(1));
            Assert.That(pool.Snapshot.PoolCount, Is.EqualTo(6));
            int expectedPrewarm = 0;
            for (int index = 0; index < pool.Archetypes.Count; index++)
            {
                expectedPrewarm += pool.Archetypes[index].Enemy.PoolPrewarm;
            }

            Assert.That(pool.Snapshot.AllocatedCount, Is.EqualTo(expectedPrewarm));

            var spawned = new List<EnemyArchetypeSpawnResult>(pool.Archetypes.Count);
            double startedAt = Time.timeAsDouble;
            for (int index = 0; index < pool.Archetypes.Count; index++)
            {
                EnemyArchetypeDefinition archetype = pool.Archetypes[index];
                EnemyArchetypeSpawnResult result = pool.Spawn(
                    archetype.Enemy.EnemyId,
                    45001 + index,
                    startedAt,
                    world);
                spawned.Add(result);

                Assert.That(result.IsSpawned, Is.True, archetype.Enemy.EnemyId);
                Assert.That(result.ReusedOldest, Is.False);
                Assert.That(result.Actor.gameObject.activeSelf, Is.True);
                Assert.That(result.Actor.Controller.State.State, Is.EqualTo(EnemyState.Move));
                Assert.That(
                    result.Actor.Controller.Damage.MaximumHp,
                    Is.EqualTo(archetype.Enemy.MaximumHp));
                Assert.That(
                    result.Actor.Controller.Damage.MaximumArmor,
                    Is.EqualTo(archetype.Defense.ArmorHp));
                Assert.That(result.Actor.AssetKey, Is.EqualTo(archetype.Enemy.AssetKey));
                Assert.That(result.Actor.AssetType, Is.EqualTo(archetype.AssetType));
                AssertAssetBinding(result.Actor, archetype, assets);

                EnemyMovementSample start = result.Actor.Strategy.SampleMovement(0d);
                EnemyMovementSample advanced = result.Actor.AdvanceMovement(0.25d);
                Assert.That(start.IsValid, Is.True);
                Assert.That(advanced.IsValid, Is.True);
                Assert.That(
                    result.Actor.transform.localPosition,
                    Is.EqualTo(new Vector3(
                        (float)advanced.XReferencePixels,
                        (float)advanced.YReferencePixels,
                        0f)));

                var trigger = new EnemyAttackTriggerContext(
                    cooldownReady: true,
                    targetInDistance: true,
                    hpThresholdReached: true,
                    supportTargetId: "gallery_ally");
                Assert.That(
                    result.Actor.TryBeginAttack(trigger, 0d, startedAt),
                    Is.True,
                    archetype.Enemy.EnemyId);
                EnemyAttackTelegraphSnapshot telegraph = result.Actor.Strategy.Telegraph;
                Assert.That(telegraph.IsVisible, Is.True);
                Assert.That(telegraph.AttackId, Is.EqualTo(archetype.Attacks[0].AttackId));
                Assert.That(
                    telegraph.ExpectedExecuteAt - startedAt,
                    Is.EqualTo(archetype.Attacks[0].Timeline.WindupSeconds)
                        .Within(0.000001d));
            }

            Assert.That(pool.DetectLeaks().Count, Is.EqualTo(6));
            for (int index = 0; index < spawned.Count; index++)
            {
                EnemyArchetypeSpawnResult result = spawned[index];
                double executeAt = result.Actor.Strategy.Telegraph.ExpectedExecuteAt;
                result.Actor.Tick(executeAt);
                Assert.That(result.Actor.Controller.State.State, Is.EqualTo(EnemyState.Attack));
                Assert.That(result.Actor.Strategy.Telegraph.IsVisible, Is.False);
            }

            Assert.That(world.Actions.Count, Is.EqualTo(6));
            Assert.That(world.Count(EnemyAttackActionKind.Projectile), Is.EqualTo(2));
            Assert.That(world.Count(EnemyAttackActionKind.Charge), Is.EqualTo(1));
            Assert.That(world.Count(EnemyAttackActionKind.Melee), Is.EqualTo(2));
            Assert.That(world.Count(EnemyAttackActionKind.Support), Is.EqualTo(1));

            for (int index = 0; index < spawned.Count; index++)
            {
                EnemyArchetypeActor actor = spawned[index].Actor;
                PoolReleaseResult released = pool.Release(spawned[index]);
                Assert.That(released.WasReleased, Is.True);
                Assert.That(actor.IsPoolActive, Is.False);
                Assert.That(actor.Controller.IsSpawned, Is.False);
                Assert.That(actor.Controller.State.State, Is.EqualTo(EnemyState.None));
                Assert.That(actor.gameObject.activeSelf, Is.False);
                Assert.That(actor.transform.parent, Is.SameAs(galleryRoot.transform));
                Assert.That(actor.transform.localPosition, Is.EqualTo(Vector3.zero));
            }

            pool.AssertNoLeaks();
            Assert.That(pool.Snapshot.ActiveCount, Is.Zero);
            yield return null;
        }

        private static void AssertAssetBinding(
            EnemyArchetypeActor actor,
            in EnemyArchetypeDefinition archetype,
            IAssetRegistry assets)
        {
            if (string.Equals(archetype.AssetType, "Sprite", StringComparison.Ordinal))
            {
                Sprite expected = assets.GetSprite(archetype.Enemy.AssetKey);
                Assert.That(actor.SourceAsset, Is.SameAs(expected));
                Assert.That(actor.GetComponent<SpriteRenderer>().sprite, Is.SameAs(expected));
            }
            else
            {
                GameObject expected = assets.GetPrefab(archetype.Enemy.AssetKey);
                Assert.That(actor.SourceAsset, Is.SameAs(expected));
            }

            WeakpointController weakpoint = actor.GetComponentInChildren<WeakpointController>(true);
            Assert.That(
                weakpoint != null,
                Is.EqualTo(archetype.Enemy.Weakpoint.HasHitbox));
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
            Assert.That(GameplayConfigRuntime.IsReady, Is.True);
            Assert.That(AssetRegistryRuntime.IsReady, Is.True);
        }

        private sealed class RecordingAttackWorld : IEnemyAttackWorld
        {
            private readonly List<EnemyAttackAction> actions =
                new List<EnemyAttackAction>();

            public IReadOnlyList<EnemyAttackAction> Actions => actions;

            public void ExecuteAttack(in EnemyAttackAction action, double timestamp)
            {
                actions.Add(action);
            }

            public int Count(EnemyAttackActionKind kind)
            {
                int count = 0;
                for (int index = 0; index < actions.Count; index++)
                {
                    if (actions[index].Kind == kind)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
