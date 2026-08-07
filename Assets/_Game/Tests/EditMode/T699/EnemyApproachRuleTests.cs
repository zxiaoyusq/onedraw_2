using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T699
{
    // 验证生产敌人的右侧出生、向左推进与独立移动时钟规则。
    [Category("T699")]
    public sealed class EnemyApproachRuleTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T699");
        }

        [Test]
        public void EveryMvpEnemySpawnsOnRightAndMovesTowardPlayerSide()
        {
            double referenceWidth = config.GetGlobal(
                ConfigIds.GlobalKeys.ReferenceWidth).IntValue.Value;
            for (int index = 0; index < config.GetEnemies().Count; index++)
            {
                EnemyConfig enemy = config.GetEnemies()[index];
                EnemyMovementDefinition movement = EnemyMovementDefinitionFactory.Create(
                    config,
                    enemy.EnemyId);
                Assert.That(
                    movement.StartXReferencePixels,
                    Is.GreaterThanOrEqualTo(referenceWidth * 0.5d),
                    enemy.EnemyId);
                Assert.That(
                    movement.EndXReferencePixels,
                    Is.LessThan(movement.StartXReferencePixels),
                    enemy.EnemyId);
                Assert.That(enemy.ContactDamage, Is.GreaterThan(0L), enemy.EnemyId);
            }

            string[] levelIds =
            {
                ConfigIds.Levels.Lv001Tutorial,
                ConfigIds.Levels.Lv002Cave,
                ConfigIds.Levels.Lv003Boss,
            };
            for (int levelIndex = 0; levelIndex < levelIds.Length; levelIndex++)
            {
                LevelDefinition level = LevelCatalog.Create(config, levelIds[levelIndex]);
                for (int waveIndex = 0; waveIndex < level.Waves.Count; waveIndex++)
                {
                    WaveDefinition wave = level.Waves[waveIndex];
                    for (int spawnIndex = 0; spawnIndex < wave.Spawns.Count; spawnIndex++)
                    {
                        SpawnPointDefinition point = wave.Spawns[spawnIndex].SpawnPoint;
                        Assert.That(
                            point.NormalizedX - point.JitterX,
                            Is.GreaterThanOrEqualTo(0.5d),
                            point.SpawnPointId);
                        Assert.That(point.Facing, Is.EqualTo(SpawnFacing.Left));
                    }
                }
            }
        }

        [Test]
        public void MovementAgeStartsAtZeroForLateSpawn()
        {
            var clock = new EnemyMovementAgeClock();

            Assert.That(clock.GetElapsedSeconds(42d), Is.Zero);
            Assert.That(clock.GetElapsedSeconds(42.25d), Is.EqualTo(0.25d));
            Assert.That(clock.GetElapsedSeconds(41d), Is.Zero);
            Assert.That(clock.HasOrigin, Is.True);
        }

        [Test]
        [Category("T699H")]
        public void ScaledMovementClockSlowsOnlyFutureTravelWithoutPositionJumps()
        {
            var clock = new EnemyMovementAgeClock();

            Assert.That(clock.GetScaledElapsedSeconds(10d, 1d), Is.Zero);
            Assert.That(clock.GetScaledElapsedSeconds(11d, 1d), Is.EqualTo(1d));
            Assert.That(clock.GetScaledElapsedSeconds(12d, 0.7d), Is.EqualTo(1.7d).Within(0.000001d));
            Assert.That(clock.GetScaledElapsedSeconds(13d, 0.7d), Is.EqualTo(2.4d).Within(0.000001d));
            Assert.That(clock.GetScaledElapsedSeconds(14d, 1d), Is.EqualTo(3.4d).Within(0.000001d));
            Assert.That(clock.GetScaledElapsedSeconds(9d, 1d), Is.EqualTo(3.4d).Within(0.000001d));
        }
    }
}
