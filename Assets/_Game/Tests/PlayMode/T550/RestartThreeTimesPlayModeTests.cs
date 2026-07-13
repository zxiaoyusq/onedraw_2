using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T550
{
    [Category("T550")]
    public sealed class RestartThreeTimesPlayModeTests
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
        public IEnumerator RestartThreeTimesThenNextLevelDisposesEveryOldSessionAndPoolLease()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            root = new GameObject("T550 Result Navigation Root");
            var factory = new PooledBattleSessionFactory(config, root.transform);
            var navigation = new BattleResultNavigation(
                factory,
                ConfigIds.Levels.Lv001Tutorial);

            for (int restart = 0; restart < 3; restart += 1)
            {
                PooledBattleSession previous = factory.Sessions[restart];
                navigation.Restart();
                yield return null;

                Assert.That(previous.Disposed, Is.True);
                Assert.That(previous.ReleasedOnDispose, Is.True);
                Assert.That(previous.MarkerObject == null, Is.True);
                Assert.That(navigation.Current.LevelId,
                    Is.EqualTo(ConfigIds.Levels.Lv001Tutorial));
                Assert.That(factory.ActiveSessionCount, Is.EqualTo(1));
            }

            var resultService = new ResultService(config, new MemoryStore());
            ResultReceipt victory = resultService.Settle(new ResultRequest(
                "settlement-playmode-next",
                ConfigIds.Levels.Lv001Tutorial,
                BattleSettlement.Victory,
                new BattleResultMetrics(2000L, 2, 0L, 120.9d)));

            Assert.That(victory.CanGoNext, Is.True);
            navigation.GoNext(victory);
            yield return null;

            Assert.That(navigation.Current.LevelId,
                Is.EqualTo(ConfigIds.Levels.Lv002Cave));
            Assert.That(navigation.Generation, Is.EqualTo(5U));
            Assert.That(factory.ActiveSessionCount, Is.EqualTo(1));

            navigation.Dispose();
            yield return null;

            Assert.That(factory.ActiveSessionCount, Is.Zero);
            Assert.That(factory.Sessions, Has.Count.EqualTo(5));
            for (int index = 0; index < factory.Sessions.Count; index += 1)
            {
                Assert.That(factory.Sessions[index].Disposed, Is.True, $"session {index}");
                Assert.That(factory.Sessions[index].ReleasedOnDispose, Is.True, $"session {index}");
                Assert.That(factory.Sessions[index].MarkerObject == null, Is.True, $"session {index}");
            }
        }

        private static IEnumerator LoadRuntimeConfiguration()
        {
            yield return SceneManager.LoadSceneAsync(
                SceneNames.Bootstrap,
                LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.MainMenu &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));
            Assert.That(GameplayConfigRuntime.IsReady, Is.True);
        }

        private sealed class PooledBattleSessionFactory : IBattleSessionFactory
        {
            private readonly IConfigProvider config;
            private readonly Transform parent;

            public PooledBattleSessionFactory(IConfigProvider config, Transform parent)
            {
                this.config = config;
                this.parent = parent;
            }

            public List<PooledBattleSession> Sessions { get; } =
                new List<PooledBattleSession>();

            public int ActiveSessionCount
            {
                get
                {
                    int count = 0;
                    for (int index = 0; index < Sessions.Count; index += 1)
                    {
                        if (!Sessions[index].Disposed)
                        {
                            count += 1;
                        }
                    }

                    return count;
                }
            }

            public IBattleSession Create(string levelId)
            {
                config.GetLevel(levelId);
                var session = new PooledBattleSession(levelId, parent);
                Sessions.Add(session);
                return session;
            }
        }

        private sealed class PooledBattleSession : IBattleSession
        {
            private readonly ObjectPoolService pool;
            private readonly PooledMarker marker;

            public PooledBattleSession(string levelId, Transform parent)
            {
                LevelId = levelId;
                pool = new ObjectPoolService();
                pool.RegisterFamily(new PoolFamilyDefinition(
                    "t550-session",
                    1,
                    PoolExhaustionPolicy.Reject));
                pool.RegisterPool(new PoolDefinition(
                    "t550-session/marker",
                    "t550-session",
                    0,
                    () => new PooledMarker(parent, levelId)));
                PoolAcquireResult acquired = pool.Acquire("t550-session/marker");
                marker = (PooledMarker)acquired.Item;
                Assert.That(acquired.IsAcquired, Is.True);
                Assert.That(pool.DetectLeaks().Count, Is.EqualTo(1));
            }

            public string LevelId { get; }

            public bool Disposed { get; private set; }

            public bool ReleasedOnDispose { get; private set; }

            public GameObject MarkerObject => marker.GameObject;

            public void Dispose()
            {
                if (Disposed)
                {
                    return;
                }

                pool.Dispose();
                ReleasedOnDispose = !marker.IsPoolActive && !marker.GameObject.activeSelf;
                Object.Destroy(marker.GameObject);
                Disposed = true;
            }
        }

        private sealed class PooledMarker : IPoolable
        {
            public PooledMarker(Transform parent, string levelId)
            {
                GameObject = new GameObject($"T550 Session Marker {levelId}");
                GameObject.transform.SetParent(parent, false);
                GameObject.SetActive(false);
            }

            public GameObject GameObject { get; }

            public bool IsPoolActive { get; private set; }

            public void AcquireFromPool(in PoolLease lease)
            {
                IsPoolActive = true;
                GameObject.SetActive(true);
            }

            public void ReleaseToPool(in PoolReleaseContext context)
            {
                IsPoolActive = false;
                GameObject.SetActive(false);
            }
        }

        private sealed class MemoryStore : IProgressSaveStore
        {
            private string payload;

            public bool TryRead(out string value)
            {
                value = payload;
                return payload != null;
            }

            public void Write(string value)
            {
                payload = value;
            }
        }
    }
}
