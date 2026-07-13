using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T500
{
    [Category("T500")]
    public sealed class SpawnTimelineTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = Load(RuntimeConfigTestFixture.LoadJson(), "test:T500");
        }

        [Test]
        public void CatalogMapsEveryConfiguredWaveSpawnRegionAndModifier()
        {
            LevelDefinition tutorial = LevelCatalog.Create(
                config,
                ConfigIds.Levels.Lv001Tutorial);
            LevelDefinition cave = LevelCatalog.Create(
                config,
                ConfigIds.Levels.Lv002Cave);
            LevelDefinition boss = LevelCatalog.Create(
                config,
                ConfigIds.Levels.Lv003Boss);

            Assert.That(tutorial.Waves.Count, Is.EqualTo(6));
            Assert.That(cave.Waves.Count, Is.EqualTo(8));
            Assert.That(boss.Waves.Count, Is.EqualTo(2));
            Assert.That(CountSpawnRows(tutorial, cave, boss), Is.EqualTo(32));
            Assert.That(CountScheduledEnemies(tutorial, cave, boss), Is.EqualTo(67));

            WaveDefinition eliteWave = cave.Waves[4];
            SpawnDefinition elite = FindSpawn(
                eliteWave,
                ConfigIds.Spawns.Spawn00205A);
            Assert.That(elite.EnemyId, Is.EqualTo(ConfigIds.Enemies.EnemySoulPuppet));
            Assert.That(elite.Modifier.ModifierId, Is.EqualTo(ConfigIds.EnemyModifiers.ModifierElite));
            Assert.That(elite.Modifier.HpMultiplier, Is.EqualTo(1.6d).Within(0.000001d));
            Assert.That(elite.Modifier.DamageMultiplier, Is.EqualTo(1.25d).Within(0.000001d));
            Assert.That(elite.Modifier.SpeedMultiplier, Is.EqualTo(1.1d).Within(0.000001d));
            Assert.That(elite.Modifier.ScoreMultiplier, Is.EqualTo(1.8d).Within(0.000001d));
            Assert.That(elite.Modifier.ExtraBuffId, Is.EqualTo(ConfigIds.Buffs.BuffVulnerable));

            SpawnDefinition bossSpawn = boss.Waves[1].Spawns[0];
            Assert.That(bossSpawn.IsBoss, Is.True);
            Assert.That(bossSpawn.SpawnPoint.Lane, Is.EqualTo(SpawnLane.Boss));
            Assert.That(bossSpawn.SpawnPoint.Facing, Is.EqualTo(SpawnFacing.Left));
            Assert.That(boss.Waves[1].EndCondition, Is.EqualTo(WaveEndCondition.BossDefeated));
        }

        [Test]
        public void TimelineIsStableAndRequiresExplicitCommit()
        {
            LevelDefinition level = LevelCatalog.Create(
                config,
                ConfigIds.Levels.Lv001Tutorial);
            var scheduler = new SpawnScheduler(level.Waves[3]);

            Assert.That(scheduler.ScheduledCount, Is.EqualTo(2));
            Assert.That(scheduler.TryGetNextDue(0.19d, out _), Is.False);
            Assert.That(scheduler.TryGetNextDue(0.2d, out LevelSpawnRequest first), Is.True);
            Assert.That(first.SpawnId, Is.EqualTo(ConfigIds.Spawns.Spawn00104A));
            Assert.That(first.OccurrenceIndex, Is.Zero);
            Assert.That(first.ScheduleSequence, Is.EqualTo(1));

            Assert.That(scheduler.TryGetNextDue(100d, out LevelSpawnRequest uncommitted), Is.True);
            Assert.That(uncommitted.ScheduleSequence, Is.EqualTo(first.ScheduleSequence));
            scheduler.Commit(first);

            Assert.That(scheduler.TryGetNextDue(0.849d, out _), Is.False);
            Assert.That(scheduler.TryGetNextDue(0.85d, out LevelSpawnRequest second), Is.True);
            Assert.That(second.SpawnId, Is.EqualTo(ConfigIds.Spawns.Spawn00104A));
            Assert.That(second.OccurrenceIndex, Is.EqualTo(1));
            scheduler.Commit(second);
            Assert.That(scheduler.IsComplete, Is.True);
        }

        [Test]
        public void EverySpawnPatternStaysInsideConfiguredNormalizedRegion()
        {
            string[] levelIds =
            {
                ConfigIds.Levels.Lv001Tutorial,
                ConfigIds.Levels.Lv002Cave,
                ConfigIds.Levels.Lv003Boss,
            };

            var observedPatterns = new HashSet<SpawnPattern>();
            for (int levelIndex = 0; levelIndex < levelIds.Length; levelIndex++)
            {
                LevelDefinition level = LevelCatalog.Create(config, levelIds[levelIndex]);
                for (int waveIndex = 0; waveIndex < level.Waves.Count; waveIndex++)
                {
                    WaveDefinition wave = level.Waves[waveIndex];
                    var definitions = new Dictionary<string, SpawnDefinition>();
                    for (int spawnIndex = 0; spawnIndex < wave.Spawns.Count; spawnIndex++)
                    {
                        definitions.Add(wave.Spawns[spawnIndex].SpawnId, wave.Spawns[spawnIndex]);
                    }

                    var scheduler = new SpawnScheduler(wave);
                    while (scheduler.TryGetNextDue(1000d, out LevelSpawnRequest request))
                    {
                        SpawnDefinition spawn = definitions[request.SpawnId];
                        observedPatterns.Add(request.Pattern);
                        Assert.That(request.Position.IsNormalized, Is.True, request.SpawnId);
                        Assert.That(
                            Math.Abs(request.Position.X - spawn.SpawnPoint.NormalizedX),
                            Is.LessThanOrEqualTo(spawn.SpawnPoint.JitterX + 0.000001d),
                            request.SpawnId);
                        Assert.That(
                            Math.Abs(request.Position.Y - spawn.SpawnPoint.NormalizedY),
                            Is.LessThanOrEqualTo(spawn.SpawnPoint.JitterY + 0.000001d),
                            request.SpawnId);
                        scheduler.Commit(request);
                    }
                }
            }

            Assert.That(
                observedPatterns,
                Is.EquivalentTo(new[]
                {
                    SpawnPattern.Line,
                    SpawnPattern.Scatter,
                    SpawnPattern.Single,
                    SpawnPattern.Stagger,
                }));
        }

        [Test]
        public void RejectedWorldSpawnRemainsPendingForRetry()
        {
            LevelDefinition level = LevelCatalog.Create(
                config,
                ConfigIds.Levels.Lv001Tutorial);
            var world = new RecordingWorld { AcceptSpawns = false };
            var runner = new LevelRunner(level, world);

            runner.Advance(0.2d);
            Assert.That(runner.CurrentWave.Scheduler.EmittedCount, Is.Zero);
            Assert.That(world.Attempts, Is.EqualTo(1));

            world.AcceptSpawns = true;
            LevelAdvanceReport retried = runner.Advance(0d);
            Assert.That(runner.CurrentWave.Scheduler.EmittedCount, Is.EqualTo(1));
            Assert.That(world.Requests.Count, Is.EqualTo(1));
            Assert.That(Count(retried, LevelRuntimeEventKind.EnemySpawned), Is.EqualTo(1));
        }

        [Test]
        public void MaxAliveBackpressureReleasesOnlyOnePendingSpawnPerDefeat()
        {
            string json = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                FindRow(
                    (JArray)root["waves"],
                    "waveId",
                    ConfigIds.Waves.Wave00101)["maxAlive"] = 1;
            });
            var world = new RecordingWorld();
            var runner = new LevelRunner(
                Load(json, "test:T500-max-alive"),
                ConfigIds.Levels.Lv001Tutorial,
                world);

            runner.Advance(10d);
            Assert.That(world.Requests.Count, Is.EqualTo(1));
            Assert.That(runner.CurrentWave.ActiveCount, Is.EqualTo(1));
            Assert.That(runner.CurrentWave.Scheduler.RemainingCount, Is.EqualTo(1));

            Assert.That(runner.NotifyEnemyDefeated(1L), Is.True);
            runner.Advance(0d);
            Assert.That(world.Requests.Count, Is.EqualTo(2));
            Assert.That(runner.CurrentWave.ActiveCount, Is.EqualTo(1));
            Assert.That(runner.CurrentWave.Scheduler.RemainingCount, Is.Zero);
        }

        [Test]
        public void PausedClockAndLargeDeltaCannotCrossPlayerConfirmationGate()
        {
            string json = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                JObject wave = FindRow(
                    (JArray)root["waves"],
                    "waveId",
                    ConfigIds.Waves.Wave00101);
                wave["startTrigger"] = "PlayerConfirmed";
                wave["startDelaySec"] = 0.5d;
            });
            GameplayConfigService changed = Load(json, "test:T500-player-gate");
            var world = new RecordingWorld();
            var runner = new LevelRunner(
                changed,
                ConfigIds.Levels.Lv001Tutorial,
                world);

            runner.SetPaused(true);
            runner.Advance(100d);
            Assert.That(runner.ElapsedSeconds, Is.Zero);
            Assert.That(runner.ConfirmPlayerAction(), Is.False);

            runner.SetPaused(false);
            runner.Advance(100d);
            Assert.That(runner.CurrentWave.State, Is.EqualTo(WaveRunnerState.Waiting));
            Assert.That(world.Requests, Is.Empty);
            Assert.That(runner.ConfirmPlayerAction(), Is.True);
            runner.Advance(0.499d);
            Assert.That(runner.CurrentWave.State, Is.EqualTo(WaveRunnerState.Waiting));
            runner.Advance(0.001d);
            Assert.That(runner.CurrentWave.State, Is.EqualTo(WaveRunnerState.Running));
            Assert.That(world.Requests, Is.Empty);
            runner.Advance(0.2d);
            Assert.That(world.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void TimeElapsedAndPlayerConfirmedEndConditionsAreExplicit()
        {
            string timedJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                JObject wave = FindRow(
                    (JArray)root["waves"],
                    "waveId",
                    ConfigIds.Waves.Wave00101);
                wave["endCondition"] = "TimeElapsed";
                wave["endDelaySec"] = 1d;
            });
            var timed = new LevelRunner(
                Load(timedJson, "test:T500-time-end"),
                ConfigIds.Levels.Lv001Tutorial,
                new RecordingWorld());
            timed.Advance(0.999d);
            Assert.That(timed.CurrentWaveIndex, Is.Zero);
            timed.Advance(0.001d);
            Assert.That(timed.CurrentWaveIndex, Is.EqualTo(1));

            string confirmedJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                JObject wave = FindRow(
                    (JArray)root["waves"],
                    "waveId",
                    ConfigIds.Waves.Wave00101);
                wave["endCondition"] = "PlayerConfirmed";
                wave["endDelaySec"] = 0.5d;
            });
            var confirmed = new LevelRunner(
                Load(confirmedJson, "test:T500-confirm-end"),
                ConfigIds.Levels.Lv001Tutorial,
                new RecordingWorld());
            confirmed.Advance(20d);
            Assert.That(confirmed.CurrentWaveIndex, Is.Zero);
            Assert.That(confirmed.ConfirmPlayerAction(), Is.True);
            confirmed.Advance(0.499d);
            Assert.That(confirmed.CurrentWaveIndex, Is.Zero);
            confirmed.Advance(0.001d);
            Assert.That(confirmed.CurrentWaveIndex, Is.EqualTo(1));
        }

        [Test]
        public void CatalogRejectsWrongSpawnPointScopeAndBossWithoutConfiguredBossSpawn()
        {
            string scopeJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                FindRow(
                    (JArray)root["spawnPoints"],
                    "spawnPointId",
                    ConfigIds.SpawnPoints.SpawnAirMid)["levelId"] =
                    ConfigIds.Levels.Lv003Boss;
            });
            string bossJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                FindRow(
                    (JArray)root["spawns"],
                    "spawnId",
                    ConfigIds.Spawns.Spawn00302A)["enemyId"] =
                    ConfigIds.Enemies.EnemyFireFish;
            });
            string numericEnumJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                FindRow(
                    (JArray)root["spawns"],
                    "spawnId",
                    ConfigIds.Spawns.Spawn00101A)["spawnPattern"] = "1";
            });

            Assert.That(
                () => LevelCatalog.Create(
                    Load(scopeJson, "test:T500-scope"),
                    ConfigIds.Levels.Lv001Tutorial),
                Throws.ArgumentException.With.Message.Contains("not scoped"));
            Assert.That(
                () => LevelCatalog.Create(
                    Load(bossJson, "test:T500-boss"),
                    ConfigIds.Levels.Lv003Boss),
                Throws.ArgumentException.With.Message.Contains("must spawn configured boss"));
            Assert.That(
                () => LevelCatalog.Create(
                    Load(numericEnumJson, "test:T500-enum"),
                    ConfigIds.Levels.Lv001Tutorial),
                Throws.ArgumentException.With.Message.Contains("unsupported spawnPattern"));
        }

        private static int CountSpawnRows(params LevelDefinition[] levels)
        {
            int count = 0;
            for (int levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                for (int waveIndex = 0; waveIndex < levels[levelIndex].Waves.Count; waveIndex++)
                {
                    count += levels[levelIndex].Waves[waveIndex].Spawns.Count;
                }
            }

            return count;
        }

        private static int CountScheduledEnemies(params LevelDefinition[] levels)
        {
            int count = 0;
            for (int levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                for (int waveIndex = 0; waveIndex < levels[levelIndex].Waves.Count; waveIndex++)
                {
                    IReadOnlyList<SpawnDefinition> spawns =
                        levels[levelIndex].Waves[waveIndex].Spawns;
                    for (int spawnIndex = 0; spawnIndex < spawns.Count; spawnIndex++)
                    {
                        count += spawns[spawnIndex].Count;
                    }
                }
            }

            return count;
        }

        private static SpawnDefinition FindSpawn(WaveDefinition wave, string spawnId)
        {
            for (int index = 0; index < wave.Spawns.Count; index++)
            {
                if (string.Equals(wave.Spawns[index].SpawnId, spawnId, StringComparison.Ordinal))
                {
                    return wave.Spawns[index];
                }
            }

            throw new AssertionException($"Spawn '{spawnId}' was not found.");
        }

        private static int Count(LevelAdvanceReport report, LevelRuntimeEventKind kind)
        {
            int count = 0;
            for (int index = 0; index < report.Events.Count; index++)
            {
                if (report.Events[index].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static JObject FindRow(JArray rows, string key, string value)
        {
            foreach (JObject row in rows.Children<JObject>())
            {
                if (string.Equals(row[key]?.Value<string>(), value, StringComparison.Ordinal))
                {
                    return row;
                }
            }

            throw new AssertionException($"Configured row '{key}={value}' was not found.");
        }

        private static GameplayConfigService Load(string json, string source)
        {
            var service = new GameplayConfigService();
            service.Load(json, source);
            return service;
        }

        private sealed class RecordingWorld : ILevelSpawnWorld
        {
            private readonly List<LevelSpawnRequest> requests =
                new List<LevelSpawnRequest>();
            private long nextEntityId = 1L;

            public bool AcceptSpawns { get; set; } = true;

            public int Attempts { get; private set; }

            public IReadOnlyList<LevelSpawnRequest> Requests => requests;

            public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
            {
                Attempts++;
                if (!AcceptSpawns)
                {
                    entityId = 0L;
                    return false;
                }

                requests.Add(request);
                entityId = nextEntityId++;
                return true;
            }
        }
    }
}
