using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using OneStrokeDemon.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T500
{
    [Category("T500")]
    public sealed class WaveRunnerPlayModeTests
    {
        private GameObject root;

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
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            PointerInputRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator TutorialRunsSixConfiguredWavesAndPauseFreezesEverything()
        {
            yield return LoadRuntimeConfiguration();
            root = new GameObject("T500 Tutorial Level World");
            var world = new GameObjectSpawnWorld(root.transform);
            var runner = new LevelRunner(
                GameplayConfigRuntime.Current,
                ConfigIds.Levels.Lv001Tutorial,
                world);
            var events = new List<LevelRuntimeEvent>();

            runner.SetPaused(true);
            Append(events, runner.Advance(60d));
            Assert.That(runner.ElapsedSeconds, Is.Zero);
            Assert.That(runner.State, Is.EqualTo(LevelRunnerState.Ready));
            Assert.That(world.TotalSpawned, Is.Zero);

            runner.SetPaused(false);
            Append(events, runner.Advance(0d));
            Assert.That(runner.State, Is.EqualTo(LevelRunnerState.Running));
            Assert.That(runner.CurrentWave.Definition.WaveId, Is.EqualTo(ConfigIds.Waves.Wave00101));
            Assert.That(runner.CurrentWave.Definition.MusicKey, Is.EqualTo("bgm_cave_01"));

            for (int waveIndex = 0; waveIndex < 5; waveIndex++)
            {
                CompleteAllDefeatedWave(runner, world, events);
            }

            WaveDefinition finalWave = runner.CurrentWave.Definition;
            Assert.That(finalWave.Order, Is.EqualTo(6));
            Assert.That(finalWave.EndCondition, Is.EqualTo(WaveEndCondition.PlayerConfirmed));
            Append(events, runner.Advance(finalWave.StartDelaySeconds));
            Assert.That(world.ActiveCount, Is.EqualTo(4));
            Append(events, runner.Advance(30d));
            Assert.That(runner.State, Is.EqualTo(LevelRunnerState.Running));
            Assert.That(runner.CurrentWaveIndex, Is.EqualTo(5));

            long[] finalEnemies = world.ActiveEntityIds;
            for (int index = 0; index < finalEnemies.Length; index++)
            {
                Assert.That(runner.NotifyEnemyDefeated(finalEnemies[index]), Is.True);
                world.Release(finalEnemies[index]);
            }

            Assert.That(runner.ConfirmPlayerAction(), Is.True);
            Append(events, runner.Advance(finalWave.EndDelaySeconds));

            Assert.That(runner.State, Is.EqualTo(LevelRunnerState.Completed));
            Assert.That(world.TotalSpawned, Is.EqualTo(15));
            Assert.That(Count(events, LevelRuntimeEventKind.WaveStarted), Is.EqualTo(6));
            Assert.That(Count(events, LevelRuntimeEventKind.EnemySpawned), Is.EqualTo(15));
            Assert.That(Count(events, LevelRuntimeEventKind.WaveCompleted), Is.EqualTo(6));
            Assert.That(Count(events, LevelRuntimeEventKind.LevelCompleted), Is.EqualTo(1));
            Assert.That(world.ActiveCount, Is.Zero);

            for (int index = 0; index < world.SpawnedPositions.Count; index++)
            {
                Vector2 position = world.SpawnedPositions[index];
                Assert.That(position.x, Is.InRange(0f, 1f));
                Assert.That(position.y, Is.InRange(0f, 1f));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator BossLevelCompletesOnlyAfterConfiguredBossEntityIsDefeated()
        {
            yield return LoadRuntimeConfiguration();
            root = new GameObject("T500 Boss Level World");
            var world = new GameObjectSpawnWorld(root.transform);
            var runner = new LevelRunner(
                GameplayConfigRuntime.Current,
                ConfigIds.Levels.Lv003Boss,
                world);
            var events = new List<LevelRuntimeEvent>();

            Append(events, runner.Advance(0d));
            CompleteAllDefeatedWave(runner, world, events);
            Assert.That(runner.CurrentWave.Definition.WaveId, Is.EqualTo(ConfigIds.Waves.Wave00302));

            Append(events, runner.Advance(
                runner.CurrentWave.Definition.StartDelaySeconds));
            Assert.That(world.ActiveCount, Is.EqualTo(1));
            long bossEntityId = world.ActiveEntityIds[0];
            Assert.That(world.GetRequest(bossEntityId).IsBoss, Is.True);
            Assert.That(
                world.GetRequest(bossEntityId).EnemyId,
                Is.EqualTo(ConfigIds.Enemies.BossTombKing));
            Assert.That(runner.State, Is.EqualTo(LevelRunnerState.Running));

            Assert.That(runner.NotifyEnemyDefeated(bossEntityId), Is.True);
            world.Release(bossEntityId);
            Append(events, runner.Advance(0d));
            Assert.That(runner.State, Is.EqualTo(LevelRunnerState.Completed));
            Assert.That(Count(events, LevelRuntimeEventKind.LevelCompleted), Is.EqualTo(1));
            Assert.That(world.TotalSpawned, Is.EqualTo(12));
            yield return null;
        }

        private static void CompleteAllDefeatedWave(
            LevelRunner runner,
            GameObjectSpawnWorld world,
            List<LevelRuntimeEvent> events)
        {
            WaveDefinition wave = runner.CurrentWave.Definition;
            double lastDue = 0d;
            for (int index = 0; index < wave.Spawns.Count; index++)
            {
                SpawnDefinition spawn = wave.Spawns[index];
                double due = spawn.SpawnTimeSeconds +
                    ((spawn.Count - 1) * spawn.IntervalSeconds);
                if (due > lastDue)
                {
                    lastDue = due;
                }
            }

            Append(events, runner.Advance(wave.StartDelaySeconds + lastDue));
            long[] active = world.ActiveEntityIds;
            Assert.That(active.Length, Is.GreaterThan(0), wave.WaveId);
            for (int index = 0; index < active.Length; index++)
            {
                Assert.That(runner.NotifyEnemyDefeated(active[index]), Is.True, wave.WaveId);
                world.Release(active[index]);
            }

            Append(events, runner.Advance(0d));
            Append(events, runner.Advance(wave.EndDelaySeconds));
        }

        private static int Count(
            IReadOnlyList<LevelRuntimeEvent> events,
            LevelRuntimeEventKind kind)
        {
            int count = 0;
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static void Append(
            List<LevelRuntimeEvent> target,
            LevelAdvanceReport report)
        {
            for (int index = 0; index < report.Events.Count; index++)
            {
                target.Add(report.Events[index]);
            }
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
        }

        private sealed class GameObjectSpawnWorld : ILevelSpawnWorld
        {
            private readonly Transform parent;
            private readonly Dictionary<long, GameObject> objects =
                new Dictionary<long, GameObject>();
            private readonly Dictionary<long, LevelSpawnRequest> requests =
                new Dictionary<long, LevelSpawnRequest>();
            private readonly List<Vector2> spawnedPositions = new List<Vector2>();
            private long nextEntityId = 1L;

            public GameObjectSpawnWorld(Transform configuredParent)
            {
                parent = configuredParent;
            }

            public int ActiveCount => objects.Count;

            public int TotalSpawned { get; private set; }

            public IReadOnlyList<Vector2> SpawnedPositions => spawnedPositions;

            public long[] ActiveEntityIds
            {
                get
                {
                    var ids = new long[objects.Count];
                    objects.Keys.CopyTo(ids, 0);
                    return ids;
                }
            }

            public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
            {
                entityId = nextEntityId++;
                var instance = new GameObject($"{request.EnemyId} #{entityId}");
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = new Vector3(
                    (float)request.Position.X,
                    (float)request.Position.Y,
                    0f);
                instance.transform.localScale = new Vector3(
                    request.Facing == SpawnFacing.Left ? -1f : 1f,
                    1f,
                    1f);
                objects.Add(entityId, instance);
                requests.Add(entityId, request);
                spawnedPositions.Add(instance.transform.localPosition);
                TotalSpawned++;
                return true;
            }

            public LevelSpawnRequest GetRequest(long entityId)
            {
                return requests[entityId];
            }

            public void Release(long entityId)
            {
                Object.DestroyImmediate(objects[entityId]);
                objects.Remove(entityId);
                requests.Remove(entityId);
            }
        }
    }
}
