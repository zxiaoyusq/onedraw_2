using System.Collections;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T690
{
    /// <summary>验证火鱼通过真实对象池路径实例化、播放动画并可正常回收。</summary>
    [Category("T690")]
    public sealed class FireFishAnimationPoolPlayModeTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator FireFishPoolInstanceAnimatesAndReturnsCleanly()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.MainMenu &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));
            root = new GameObject("T690 Fire Fish Pool");
            using var pool = new EnemyArchetypePool(
                GameplayConfigRuntime.Current,
                AssetRegistryRuntime.Current,
                root.transform);
            var world = new NoOpAttackWorld();
            EnemyArchetypeSpawnResult spawned = pool.Spawn(
                ConfigIds.Enemies.EnemyFireFish,
                69001,
                Time.timeAsDouble,
                world);

            Assert.That(spawned.IsSpawned, Is.True);
            Assert.That(spawned.Actor.AssetType, Is.EqualTo("Prefab"));
            Animator animator = spawned.Actor.GetComponent<Animator>();
            SpriteRenderer renderer = spawned.Actor.GetComponent<SpriteRenderer>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(renderer, Is.Not.Null);
            Sprite firstFrame = renderer.sprite;

            float animationDeadline = Time.realtimeSinceStartup + 0.5f;
            while (renderer.sprite == firstFrame && Time.realtimeSinceStartup < animationDeadline)
            {
                yield return null;
            }

            Assert.That(renderer.sprite, Is.Not.SameAs(firstFrame));
            Assert.That(pool.Release(spawned).WasReleased, Is.True);
            Assert.That(spawned.Actor.gameObject.activeSelf, Is.False);
            pool.AssertNoLeaks();
        }

        private sealed class NoOpAttackWorld : IEnemyAttackWorld
        {
            public void ExecuteAttack(
                EnemyController source,
                in EnemyAttackAction action,
                double timestamp)
            {
            }
        }
    }
}
