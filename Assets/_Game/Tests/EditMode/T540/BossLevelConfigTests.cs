using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T540
{
    [Category("T540")]
    public sealed class BossLevelConfigTests
    {
        private GameplayConfigService config;
        private LevelDefinition level;

        [SetUp]
        public void SetUp()
        {
            config = Load(RuntimeConfigTestFixture.LoadJson(), "test:T540");
            level = LevelCatalog.Create(config, ConfigIds.Levels.Lv003Boss);
        }

        [Test]
        public void BossLevelFitsFourMinuteCapAndStagesMixedGateBeforeOnlyBoss()
        {
            Assert.That(level.DurationLimitSeconds, Is.EqualTo(240d));
            Assert.That(level.BossEnemyId, Is.EqualTo(ConfigIds.Enemies.BossTombKing));
            Assert.That(level.Waves.Count, Is.EqualTo(2));
            Assert.That(level.Waves[0].EndCondition,
                Is.EqualTo(WaveEndCondition.AllEnemiesDefeated));
            Assert.That(level.Waves[0].MaxAlive, Is.EqualTo(11));
            Assert.That(level.Waves[1].EndCondition,
                Is.EqualTo(WaveEndCondition.BossDefeated));
            Assert.That(level.Waves[1].Spawns.Count, Is.EqualTo(1));
            Assert.That(level.Waves[1].Spawns[0].IsBoss, Is.True);
            Assert.That(CountOccurrences(level.Waves[0]), Is.EqualTo(11));
            Assert.That(CountOccurrences(level.Waves[1]), Is.EqualTo(1));

            var introEnemyIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < level.Waves[0].Spawns.Count; index++)
            {
                SpawnDefinition spawn = level.Waves[0].Spawns[index];
                Assert.That(spawn.IsBoss, Is.False, spawn.SpawnId);
                introEnemyIds.Add(spawn.EnemyId);
            }

            Assert.That(introEnemyIds, Is.EquivalentTo(new[]
            {
                ConfigIds.Enemies.EnemyFireFish,
                ConfigIds.Enemies.EnemyTalismanBat,
                ConfigIds.Enemies.EnemyStoneTurtle,
                ConfigIds.Enemies.EnemySkeletonGhost,
                ConfigIds.Enemies.EnemySoulPuppet,
            }));
            Assert.That(
                FindSpawnByEnemy(
                        level.Waves[0],
                        ConfigIds.Enemies.EnemySoulPuppet)
                    .Modifier.ModifierId,
                Is.EqualTo(ConfigIds.EnemyModifiers.ModifierElite));

            LevelConfig row = config.GetLevel(ConfigIds.Levels.Lv003Boss);
            Assert.That(row.StarScore1, Is.EqualTo(8000));
            Assert.That(row.StarScore2, Is.EqualTo(12000));
            Assert.That(row.StarScore3, Is.EqualTo(17000));
        }

        [Test]
        public void ThreePhasePromptsMatchConfiguredInterruptAndExecutionFlow()
        {
            IReadOnlyList<BossPhaseDefinition> phases = BossPhaseCatalog.Create(
                config,
                ConfigIds.Enemies.BossTombKing);

            Assert.That(phases.Count, Is.EqualTo(3));
            var descriptions = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < phases.Count; index++)
            {
                BossPhaseDefinition phase = phases[index];
                Assert.That(phase.Order, Is.EqualTo(index + 1));
                Assert.That(phase.DescriptionZhCN, Is.Not.Empty);
                Assert.That(phase.DescriptionEnUS, Is.Not.Empty);
                Assert.That(descriptions.Add(phase.DescriptionZhCN), Is.True);
                Assert.That(phase.Attacks.Count, Is.EqualTo(1));
                Assert.That(phase.OnEnterEffectGroupId, Is.Not.Empty);
            }

            Assert.That(phases[0].DescriptionZhCN, Does.Contain("符钉"));
            Assert.That(phases[0].CombatProfile.Defense.DefenseRuleId,
                Is.EqualTo(ConfigIds.DefenseRules.DefenseBossPins));
            Assert.That(phases[1].DescriptionZhCN, Does.Contain("弱点"));
            Assert.That(phases[1].Attacks[0].Timeline.InterruptGestureType,
                Is.EqualTo("Any"));
            Assert.That(phases[2].DescriptionZhCN, Does.Contain("弱点"));
            Assert.That(phases[2].DescriptionZhCN, Does.Contain("处决"));
            Assert.That(phases[2].Attacks[0].Timeline.InterruptGestureType,
                Is.EqualTo("Any"));
            Assert.That(phases[2].ExitHpRatio, Is.Zero);
        }

        [Test]
        public void LevelWaveSpawnPhaseAndPromptChangesFlowThroughReloadOnly()
        {
            string changedJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                FindRow(
                    (JArray)root["levels"],
                    "levelId",
                    ConfigIds.Levels.Lv003Boss)["durationLimitSec"] = 225d;

                JObject wave = FindRow(
                    (JArray)root["waves"],
                    "waveId",
                    ConfigIds.Waves.Wave00301);
                wave["endDelaySec"] = 1.75d;
                wave["maxAlive"] = 10;

                FindRow(
                    (JArray)root["spawns"],
                    "spawnId",
                    ConfigIds.Spawns.Spawn00301A)["count"] = 2;

                FindRow(
                    (JArray)root["bossPhases"],
                    "bossPhaseId",
                    ConfigIds.BossPhases.BossTombPhase1)["exitHpRatio"] = 0.7d;
                FindRow(
                    (JArray)root["bossPhases"],
                    "bossPhaseId",
                    ConfigIds.BossPhases.BossTombPhase2)["enterHpRatio"] = 0.7d;

                FindRow(
                    (JArray)root["texts"],
                    "textKey",
                    ConfigIds.Texts.TextBossPhase3)["zhCN"] = "配置变更后的处决提示";
            });
            GameplayConfigService changed = Load(changedJson, "test:T540-mutated");
            LevelDefinition changedLevel = LevelCatalog.Create(
                changed,
                ConfigIds.Levels.Lv003Boss);
            IReadOnlyList<BossPhaseDefinition> phases = BossPhaseCatalog.Create(
                changed,
                ConfigIds.Enemies.BossTombKing);

            Assert.That(changedLevel.DurationLimitSeconds, Is.EqualTo(225d));
            Assert.That(changedLevel.Waves[0].EndDelaySeconds, Is.EqualTo(1.75d));
            Assert.That(changedLevel.Waves[0].MaxAlive, Is.EqualTo(10));
            Assert.That(
                FindSpawnByEnemy(
                        changedLevel.Waves[0],
                        ConfigIds.Enemies.EnemyFireFish).Count,
                Is.EqualTo(2));
            Assert.That(phases[0].ExitHpRatio, Is.EqualTo(0.7d).Within(0.000001d));
            Assert.That(phases[1].EnterHpRatio, Is.EqualTo(0.7d).Within(0.000001d));
            Assert.That(phases[2].DescriptionZhCN,
                Is.EqualTo("配置变更后的处决提示"));
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

        private static SpawnDefinition FindSpawnByEnemy(
            WaveDefinition wave,
            string enemyId)
        {
            for (int index = 0; index < wave.Spawns.Count; index++)
            {
                if (string.Equals(
                        wave.Spawns[index].EnemyId,
                        enemyId,
                        StringComparison.Ordinal))
                {
                    return wave.Spawns[index];
                }
            }

            throw new AssertionException($"Enemy spawn '{enemyId}' was not found.");
        }

        private static JObject FindRow(JArray rows, string key, string value)
        {
            foreach (JObject row in rows.Children<JObject>())
            {
                if (string.Equals(
                        row[key]?.Value<string>(),
                        value,
                        StringComparison.Ordinal))
                {
                    return row;
                }
            }

            throw new AssertionException(
                $"Configured row '{key}={value}' was not found.");
        }

        private static GameplayConfigService Load(string json, string source)
        {
            var service = new GameplayConfigService();
            service.Load(json, source);
            return service;
        }
    }
}
