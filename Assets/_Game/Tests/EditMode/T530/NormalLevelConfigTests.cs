using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T530
{
    [Category("T530")]
    public sealed class NormalLevelConfigTests
    {
        private GameplayConfigService config;
        private LevelDefinition cave;

        [SetUp]
        public void SetUp()
        {
            config = Load(RuntimeConfigTestFixture.LoadJson(), "test:T530");
            cave = LevelCatalog.Create(config, ConfigIds.Levels.Lv002Cave);
        }

        [Test]
        public void CaveUsesEightWavesAllSixArchetypesAndThreeEliteSupportEncounters()
        {
            Assert.That(cave.Waves.Count, Is.EqualTo(8));
            Assert.That(CountSpawnRows(cave), Is.EqualTo(23));
            Assert.That(CountOccurrences(cave), Is.EqualTo(45));
            Assert.That(cave.DurationLimitSeconds, Is.EqualTo(210d));

            var enemyIds = new HashSet<string>(StringComparer.Ordinal);
            var eliteOrders = new List<int>();
            for (int waveIndex = 0; waveIndex < cave.Waves.Count; waveIndex++)
            {
                WaveDefinition wave = cave.Waves[waveIndex];
                Assert.That(wave.Order, Is.EqualTo(waveIndex + 1));
                Assert.That(wave.EndCondition, Is.EqualTo(WaveEndCondition.AllEnemiesDefeated));
                for (int spawnIndex = 0; spawnIndex < wave.Spawns.Count; spawnIndex++)
                {
                    SpawnDefinition spawn = wave.Spawns[spawnIndex];
                    enemyIds.Add(spawn.EnemyId);
                    Assert.That(spawn.IsBoss, Is.False, spawn.SpawnId);
                    if (string.Equals(
                            spawn.Modifier.ModifierId,
                            ConfigIds.EnemyModifiers.ModifierElite,
                            StringComparison.Ordinal))
                    {
                        Assert.That(
                            spawn.EnemyId,
                            Is.EqualTo(ConfigIds.Enemies.EnemySoulPuppet));
                        eliteOrders.Add(wave.Order);
                    }
                }
            }

            Assert.That(
                enemyIds,
                Is.EquivalentTo(new[]
                {
                    ConfigIds.Enemies.EnemyFireFish,
                    ConfigIds.Enemies.EnemyWheelZombie,
                    ConfigIds.Enemies.EnemyStoneTurtle,
                    ConfigIds.Enemies.EnemySkeletonGhost,
                    ConfigIds.Enemies.EnemyTalismanBat,
                    ConfigIds.Enemies.EnemySoulPuppet,
                }));
            Assert.That(eliteOrders, Is.EqualTo(new[] { 5, 7, 8 }));
        }

        [Test]
        public void ConfiguredDurabilityRisesAcrossFourTwoWaveActs()
        {
            var actAverages = new double[4];
            for (int actIndex = 0; actIndex < actAverages.Length; actIndex++)
            {
                double first = EffectiveDurability(cave.Waves[actIndex * 2]);
                double second = EffectiveDurability(cave.Waves[(actIndex * 2) + 1]);
                actAverages[actIndex] = (first + second) * 0.5d;
            }

            Assert.That(actAverages[1], Is.GreaterThan(actAverages[0]));
            Assert.That(actAverages[2], Is.GreaterThan(actAverages[1]));
            Assert.That(actAverages[3], Is.GreaterThan(actAverages[2]));

            LevelConfig row = config.GetLevel(ConfigIds.Levels.Lv002Cave);
            Assert.That(row.StarScore1, Is.EqualTo(6500));
            Assert.That(row.StarScore2, Is.EqualTo(9500));
            Assert.That(row.StarScore3, Is.EqualTo(13000));
        }

        [Test]
        public void EveryWaveFitsCapacityAndStagesConflictingHardStancesAfterCooldown()
        {
            long globalCapacity = config.GetGlobal("max_active_enemies").IntValue.Value;
            double switchCooldown = Math.Max(
                config.GetStance(ConfigIds.Stances.StanceBlade).SwitchCooldownSec,
                config.GetStance(ConfigIds.Stances.StanceTalisman).SwitchCooldownSec);
            var configuredGestures = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<StrokeRuleConfig> strokeRules = config.GetStrokeRules();
            for (int index = 0; index < strokeRules.Count; index++)
            {
                configuredGestures.Add(strokeRules[index].GestureType);
            }

            for (int waveIndex = 0; waveIndex < cave.Waves.Count; waveIndex++)
            {
                WaveDefinition wave = cave.Waves[waveIndex];
                Assert.That(CountOccurrences(wave), Is.LessThanOrEqualTo(wave.MaxAlive));
                Assert.That(wave.MaxAlive, Is.LessThanOrEqualTo(globalCapacity));
                var firstHardStanceAt = new Dictionary<string, double>(StringComparer.Ordinal);
                for (int spawnIndex = 0; spawnIndex < wave.Spawns.Count; spawnIndex++)
                {
                    SpawnDefinition spawn = wave.Spawns[spawnIndex];
                    EnemyArchetypeDefinition archetype = EnemyArchetypeCatalog.Create(
                        config,
                        spawn.EnemyId);
                    Assert.That(
                        configuredGestures,
                        Does.Contain(archetype.Defense.RequiredGestureType),
                        spawn.SpawnId);
                    RecordFirst(
                        firstHardStanceAt,
                        archetype.Defense.RequiredStanceId,
                        spawn.SpawnTimeSeconds);
                    for (int attackIndex = 0; attackIndex < archetype.Attacks.Count; attackIndex++)
                    {
                        EnemyAttackDefinition attack = archetype.Attacks[attackIndex];
                        Assert.That(
                            configuredGestures,
                            Does.Contain(attack.Timeline.InterruptGestureType),
                            attack.AttackId);
                        if (!string.IsNullOrEmpty(attack.ProjectileId))
                        {
                            RecordFirst(
                                firstHardStanceAt,
                                config.GetProjectile(attack.ProjectileId).RequiredStanceId,
                                spawn.SpawnTimeSeconds);
                        }
                    }
                }

                if (firstHardStanceAt.TryGetValue(
                        ConfigIds.Stances.StanceBlade,
                        out double bladeAt) &&
                    firstHardStanceAt.TryGetValue(
                        ConfigIds.Stances.StanceTalisman,
                        out double talismanAt))
                {
                    Assert.That(
                        Math.Abs(bladeAt - talismanAt),
                        Is.GreaterThanOrEqualTo(switchCooldown),
                        wave.WaveId);
                }
            }
        }

        [Test]
        public void TimingAndPopulationChangesFlowThroughReloadedConfigOnly()
        {
            string changedJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                JObject wave = FindRow(
                    (JArray)root["waves"],
                    "waveId",
                    ConfigIds.Waves.Wave00202);
                wave["startDelaySec"] = 1.25d;
                wave["maxAlive"] = 4;

                JObject spawn = FindRow(
                    (JArray)root["spawns"],
                    "spawnId",
                    ConfigIds.Spawns.Spawn00202A);
                spawn["spawnTimeSec"] = 0.4d;
                spawn["count"] = 2;
            });
            GameplayConfigService changed = Load(changedJson, "test:T530-mutated");
            WaveDefinition wave = LevelCatalog.Create(
                changed,
                ConfigIds.Levels.Lv002Cave).Waves[1];

            Assert.That(wave.StartDelaySeconds, Is.EqualTo(1.25d));
            Assert.That(wave.MaxAlive, Is.EqualTo(4));
            SpawnDefinition spawn = FindSpawn(wave, ConfigIds.Spawns.Spawn00202A);
            Assert.That(spawn.SpawnTimeSeconds, Is.EqualTo(0.4d).Within(0.000001d));
            Assert.That(spawn.Count, Is.EqualTo(2));
        }

        private double EffectiveDurability(WaveDefinition wave)
        {
            double total = 0d;
            for (int index = 0; index < wave.Spawns.Count; index++)
            {
                SpawnDefinition spawn = wave.Spawns[index];
                EnemyArchetypeDefinition archetype = EnemyArchetypeCatalog.Create(
                    config,
                    spawn.EnemyId);
                double perEnemy =
                    (archetype.Enemy.MaximumHp * spawn.Modifier.HpMultiplier) +
                    archetype.Defense.ArmorHp;
                total += perEnemy * spawn.Count;
            }

            return total;
        }

        private static void RecordFirst(
            IDictionary<string, double> firstByStance,
            string stanceId,
            double spawnTime)
        {
            if (string.IsNullOrEmpty(stanceId))
            {
                return;
            }

            if (!firstByStance.TryGetValue(stanceId, out double current) || spawnTime < current)
            {
                firstByStance[stanceId] = spawnTime;
            }
        }

        private static int CountSpawnRows(LevelDefinition level)
        {
            int count = 0;
            for (int index = 0; index < level.Waves.Count; index++)
            {
                count += level.Waves[index].Spawns.Count;
            }

            return count;
        }

        private static int CountOccurrences(LevelDefinition level)
        {
            int count = 0;
            for (int index = 0; index < level.Waves.Count; index++)
            {
                count += CountOccurrences(level.Waves[index]);
            }

            return count;
        }

        private static int CountOccurrences(WaveDefinition wave)
        {
            int count = 0;
            for (int index = 0; index < wave.Spawns.Count; index++)
            {
                count += wave.Spawns[index].Count;
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
    }
}
