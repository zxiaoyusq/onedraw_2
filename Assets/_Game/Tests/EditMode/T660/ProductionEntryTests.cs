using NUnit.Framework;
using OneStrokeDemon.Bootstrap;
using OneStrokeDemon.Config;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T660
{
    [Category("T660")]
    public sealed class ProductionEntryTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T660:entry");
            BattleLaunchContext.Clear();
            PlayerPrefs.DeleteKey(PlayerPrefsProgressSaveStore.StorageKey);
        }

        [TearDown]
        public void TearDown()
        {
            BattleLaunchContext.Clear();
            PlayerPrefs.DeleteKey(PlayerPrefsProgressSaveStore.StorageKey);
        }

        [Test]
        public void LaunchContextAcceptsOnlyConfiguredTrimmedLevelIds()
        {
            BattleLaunchContext.Select(config, ConfigIds.Levels.Lv002Cave);

            Assert.That(BattleLaunchContext.HasSelection, Is.True);
            Assert.That(
                BattleLaunchContext.SelectedLevelId,
                Is.EqualTo(ConfigIds.Levels.Lv002Cave));
            Assert.That(
                () => BattleLaunchContext.Select(config, " missing "),
                Throws.ArgumentException);

            BattleLaunchContext.Clear();
            Assert.That(BattleLaunchContext.HasSelection, Is.False);
        }

        [Test]
        public void PlayerPrefsStoreRoundTripsTheProgressPayload()
        {
            var store = new PlayerPrefsProgressSaveStore();

            Assert.That(store.TryRead(out string missing), Is.False);
            Assert.That(missing, Is.Null);
            store.Write("{\"saveVersion\":1}");

            Assert.That(store.TryRead(out string payload), Is.True);
            Assert.That(payload, Is.EqualTo("{\"saveVersion\":1}"));
        }
    }
}
