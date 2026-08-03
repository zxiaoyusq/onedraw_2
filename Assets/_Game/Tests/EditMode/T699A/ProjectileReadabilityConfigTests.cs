using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T699A
{
    // 验证生产弹体的速度足够可读，同时寿命覆盖右侧出生区到玩家侧的最小行程。
    [Category("T699A")]
    public sealed class ProjectileReadabilityConfigTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T699A");
        }

        [TestCase(ConfigIds.Projectiles.ProjGhostFire, 180f, 10f)]
        [TestCase(ConfigIds.Projectiles.ProjSoulShard, 210f, 9f)]
        [TestCase(ConfigIds.Projectiles.ProjSealBolt, 190f, 10f)]
        [TestCase(ConfigIds.Projectiles.ProjRockfall, 200f, 8f)]
        [TestCase(ConfigIds.Projectiles.ProjSealWave, 160f, 11f)]
        public void ProjectileSpeedIsReadableAndLifetimeReachesPlayerSide(
            string projectileId,
            float expectedSpeed,
            float expectedLifetime)
        {
            ProjectileConfig projectile = config.GetProjectile(projectileId);
            long referenceWidth = config.GetGlobal(
                ConfigIds.GlobalKeys.ReferenceWidth).IntValue.Value;

            Assert.That(projectile.SpeedRefPxSec, Is.EqualTo(expectedSpeed));
            Assert.That(projectile.LifeSec, Is.EqualTo(expectedLifetime));
            Assert.That(projectile.SpeedRefPxSec, Is.InRange(160f, 210f));
            Assert.That(
                projectile.SpeedRefPxSec * projectile.LifeSec,
                Is.GreaterThanOrEqualTo(referenceWidth * 0.8f),
                projectileId);
            Assert.That(projectile.Damage, Is.GreaterThan(0L));
            Assert.That(projectile.HitRadiusRefPx, Is.GreaterThan(0f));
            Assert.That(projectile.Cuttable, Is.True);
        }
    }
}
