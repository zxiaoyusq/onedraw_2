using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T550
{
    [Category("T550")]
    public sealed class SaveMigrationTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), RuntimeConfigTestFixture.Source);
        }

        [Test]
        public void MissingSaveStartsAtConfiguredLevelGraphRoot()
        {
            var service = new ResultService(config, new MemoryProgressStore());

            Assert.That(service.LoadResult.Status, Is.EqualTo(ProgressLoadStatus.Missing));
            Assert.That(service.Current.Revision, Is.Zero);
            Assert.That(service.Current.UnlockedLevelIds,
                Is.EqualTo(new[] { ConfigIds.Levels.Lv001Tutorial }));
            Assert.That(service.Current.IsLevelUnlocked(ConfigIds.Levels.Lv002Cave), Is.False);
        }

        [Test]
        public void SavedProgressRoundTripsDeterministically()
        {
            var store = new MemoryProgressStore();
            var first = new ResultService(config, store);
            first.Settle(new ResultRequest(
                "settlement-roundtrip",
                ConfigIds.Levels.Lv001Tutorial,
                BattleSettlement.Victory,
                new BattleResultMetrics(2000L, 2, 0L, 120.9d)));
            string firstPayload = store.Payload;

            var second = new ResultService(config, store);
            string encodedAgain = new ProgressSaveCodec().Encode(second.Current);

            Assert.That(second.LoadResult.Status, Is.EqualTo(ProgressLoadStatus.Loaded));
            Assert.That(second.Current.Revision, Is.EqualTo(1L));
            Assert.That(second.Current.ScoreTokens, Is.EqualTo(100L));
            Assert.That(second.Current.IsLevelUnlocked(ConfigIds.Levels.Lv002Cave), Is.True);
            Assert.That(second.Current.HasAppliedSettlement("settlement-roundtrip"), Is.True);
            Assert.That(encodedAgain, Is.EqualTo(firstPayload));
            Assert.That(store.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void MalformedJsonFallsBackWithoutOverwritingSource()
        {
            var store = new MemoryProgressStore { Payload = "{not-json" };

            var service = new ResultService(config, store);

            Assert.That(service.LoadResult.Status, Is.EqualTo(ProgressLoadStatus.RecoveredCorrupt));
            Assert.That(service.Current.UnlockedLevelIds,
                Is.EqualTo(new[] { ConfigIds.Levels.Lv001Tutorial }));
            Assert.That(store.Payload, Is.EqualTo("{not-json"));
            Assert.That(store.WriteCount, Is.Zero);
        }

        [Test]
        public void FutureVersionFallsBackAsIncompatible()
        {
            var store = new MemoryProgressStore { Payload = "{\"version\":99}" };

            var service = new ResultService(config, store);

            Assert.That(service.LoadResult.Status,
                Is.EqualTo(ProgressLoadStatus.RecoveredIncompatible));
            Assert.That(service.LoadResult.Diagnostic, Is.EqualTo("future_version_99"));
            Assert.That(service.Current.Revision, Is.Zero);
        }

        [Test]
        public void VersionZeroUsesRegisteredMigration()
        {
            var store = new MemoryProgressStore
            {
                Payload = "{\"version\":0,\"legacyTokens\":7}",
            };

            var service = new ResultService(
                config,
                store,
                new IProgressSaveMigration[] { new VersionZeroMigration() });

            Assert.That(service.LoadResult.Status, Is.EqualTo(ProgressLoadStatus.Migrated));
            Assert.That(service.Current.ScoreTokens, Is.EqualTo(7L));
            Assert.That(service.Current.IsLevelUnlocked(ConfigIds.Levels.Lv001Tutorial), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(JObject.Parse(store.Payload).Value<int>("version"), Is.EqualTo(2));
            Assert.That(service.Current.CompletedTutorialIds, Is.Empty);
        }

        [Test]
        public void UnknownCatalogIdsAreRejectedAndRecovered()
        {
            var store = new MemoryProgressStore
            {
                Payload =
                    "{\"version\":1,\"revision\":2,\"scoreTokens\":5," +
                    "\"levels\":[{\"levelId\":\"lv_missing\",\"bestScore\":10," +
                    "\"bestStars\":1,\"clearCount\":1}]," +
                    "\"unlockedLevelIds\":[\"lv_001_tutorial\",\"lv_missing\"]," +
                    "\"unlockedFeatureIds\":[],\"appliedSettlementIds\":[]}",
            };

            var service = new ResultService(config, store);

            Assert.That(service.LoadResult.Status, Is.EqualTo(ProgressLoadStatus.RecoveredCorrupt));
            Assert.That(service.LoadResult.Diagnostic, Is.EqualTo("save_catalog_mismatch"));
            Assert.That(service.Current.ScoreTokens, Is.Zero);
            Assert.That(service.Current.UnlockedLevelIds,
                Is.EqualTo(new[] { ConfigIds.Levels.Lv001Tutorial }));
        }

        private sealed class VersionZeroMigration : IProgressSaveMigration
        {
            public int SourceVersion => 0;

            public int TargetVersion => 1;

            public JObject Migrate(JObject source)
            {
                return new JObject
                {
                    ["version"] = 1,
                    ["revision"] = 0,
                    ["scoreTokens"] = source.Value<long>("legacyTokens"),
                    ["levels"] = new JArray(),
                    ["unlockedLevelIds"] = new JArray(ConfigIds.Levels.Lv001Tutorial),
                    ["unlockedFeatureIds"] = new JArray(),
                    ["appliedSettlementIds"] = new JArray(),
                };
            }
        }
    }
}
