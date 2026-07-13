using System;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T550
{
    [Category("T550")]
    public sealed class ResultTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = LoadConfig(RuntimeConfigTestFixture.LoadJson());
        }

        [Test]
        public void VictoryScoreUsesConfiguredReflectNoDamageAndRemainingTimeValues()
        {
            ResultScoreSettings settings = ResultScoreSettingsFactory.Create(config);
            LevelConfig level = config.GetLevel(ConfigIds.Levels.Lv001Tutorial);
            var metrics = new BattleResultMetrics(
                combatScore: 2000L,
                reflectedProjectileCount: 2,
                playerDamageTaken: 0L,
                gameplayElapsedSeconds: 120.9d);

            ResultScoreBreakdown result = ResultScoring.Calculate(
                settings,
                level,
                BattleSettlement.Victory,
                metrics);

            Assert.That(settings.ScorePerReflect, Is.EqualTo(150L));
            Assert.That(settings.NoDamageBonus, Is.EqualTo(1000L));
            Assert.That(settings.ScorePerRemainingSecond, Is.EqualTo(20L));
            Assert.That(result.CombatScore, Is.EqualTo(2000L));
            Assert.That(result.ReflectedProjectileScore, Is.EqualTo(300L));
            Assert.That(result.NoDamageScore, Is.EqualTo(1000L));
            Assert.That(result.RemainingWholeSeconds, Is.EqualTo(59L));
            Assert.That(result.RemainingTimeScore, Is.EqualTo(1180L));
            Assert.That(result.FinalScore, Is.EqualTo(4480L));
            Assert.That(result.Stars, Is.EqualTo(2));
        }

        [Test]
        public void DefeatKeepsCombatScoreButDoesNotGrantVictoryBonusesOrStars()
        {
            var metrics = new BattleResultMetrics(2000L, 9, 0L, 1d);

            ResultScoreBreakdown result = ResultScoring.Calculate(
                ResultScoreSettingsFactory.Create(config),
                config.GetLevel(ConfigIds.Levels.Lv001Tutorial),
                BattleSettlement.Defeat,
                metrics);

            Assert.That(result.FinalScore, Is.EqualTo(2000L));
            Assert.That(result.ReflectedProjectileScore, Is.Zero);
            Assert.That(result.NoDamageScore, Is.Zero);
            Assert.That(result.RemainingTimeScore, Is.Zero);
            Assert.That(result.Stars, Is.Zero);
        }

        [Test]
        public void VictoryAppliesConfiguredRewardsAndUnlocksNextLevel()
        {
            var store = new MemoryProgressStore();
            var service = new ResultService(config, store);

            ResultReceipt result = service.Settle(CreateTutorialVictory("settlement-result-1"));

            Assert.That(result.Status, Is.EqualTo(SettlementApplyStatus.Applied));
            Assert.That(result.Score.FinalScore, Is.EqualTo(4480L));
            Assert.That(result.Score.Stars, Is.EqualTo(2));
            Assert.That(result.AppliedRewards.Count, Is.EqualTo(2));
            Assert.That(result.AppliedRewards[0].Type, Is.EqualTo(RewardGrantType.UnlockLevel));
            Assert.That(result.AppliedRewards[0].RewardId, Is.EqualTo(ConfigIds.Levels.Lv002Cave));
            Assert.That(result.AppliedRewards[1].Type, Is.EqualTo(RewardGrantType.ScoreToken));
            Assert.That(result.AppliedRewards[1].Amount, Is.EqualTo(100L));
            Assert.That(result.Progress.ScoreTokens, Is.EqualTo(100L));
            Assert.That(result.Progress.IsLevelUnlocked(ConfigIds.Levels.Lv002Cave), Is.True);
            Assert.That(result.CanGoNext, Is.True);
            Assert.That(result.NextLevelId, Is.EqualTo(ConfigIds.Levels.Lv002Cave));
            Assert.That(result.Progress.TryGetLevel(ConfigIds.Levels.Lv001Tutorial, out LevelProgress level), Is.True);
            Assert.That(level.BestScore, Is.EqualTo(4480L));
            Assert.That(level.BestStars, Is.EqualTo(2));
            Assert.That(level.ClearCount, Is.EqualTo(1L));
            Assert.That(store.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateSettlementDoesNotWriteOrApplyRewardsAgain()
        {
            var store = new MemoryProgressStore();
            var service = new ResultService(config, store);
            ResultRequest request = CreateTutorialVictory("settlement-idempotent");
            ResultReceipt first = service.Settle(request);

            ResultReceipt duplicate = service.Settle(request);

            Assert.That(first.Status, Is.EqualTo(SettlementApplyStatus.Applied));
            Assert.That(duplicate.Status, Is.EqualTo(SettlementApplyStatus.Duplicate));
            Assert.That(duplicate.AppliedRewards, Is.Empty);
            Assert.That(duplicate.Progress.Revision, Is.EqualTo(1L));
            Assert.That(duplicate.Progress.ScoreTokens, Is.EqualTo(100L));
            Assert.That(duplicate.Progress.TryGetLevel(
                ConfigIds.Levels.Lv001Tutorial,
                out LevelProgress level), Is.True);
            Assert.That(level.ClearCount, Is.EqualTo(1L));
            Assert.That(store.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void FailedSaveDoesNotPublishPartiallyAppliedProgress()
        {
            var store = new MemoryProgressStore { ThrowOnWrite = true };
            var service = new ResultService(config, store);

            Assert.Throws<InvalidOperationException>(() =>
                service.Settle(CreateTutorialVictory("settlement-write-failure")));

            Assert.That(service.Current.Revision, Is.Zero);
            Assert.That(service.Current.ScoreTokens, Is.Zero);
            Assert.That(service.Current.HasAppliedSettlement("settlement-write-failure"), Is.False);
            Assert.That(service.Current.TryGetLevel(
                ConfigIds.Levels.Lv001Tutorial,
                out _), Is.False);
        }

        [Test]
        public void ChangedGlobalValuesChangeScoreWithoutCodeChanges()
        {
            string json = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                foreach (var row in root["global"])
                {
                    switch ((string)row["key"])
                    {
                        case "result_score_per_reflect":
                            row["intValue"] = 10;
                            break;
                        case "result_score_no_damage_bonus":
                            row["intValue"] = 20;
                            break;
                        case "result_score_per_remaining_second":
                            row["intValue"] = 30;
                            break;
                    }
                }
            });
            GameplayConfigService changed = LoadConfig(json);
            var metrics = new BattleResultMetrics(100L, 2, 0L, 177.1d);

            ResultScoreBreakdown result = ResultScoring.Calculate(
                ResultScoreSettingsFactory.Create(changed),
                changed.GetLevel(ConfigIds.Levels.Lv001Tutorial),
                BattleSettlement.Victory,
                metrics);

            Assert.That(result.RemainingWholeSeconds, Is.EqualTo(2L));
            Assert.That(result.FinalScore, Is.EqualTo(200L));
        }

        private static ResultRequest CreateTutorialVictory(string settlementId)
        {
            return new ResultRequest(
                settlementId,
                ConfigIds.Levels.Lv001Tutorial,
                BattleSettlement.Victory,
                new BattleResultMetrics(2000L, 2, 0L, 120.9d));
        }

        private static GameplayConfigService LoadConfig(string json)
        {
            var service = new GameplayConfigService();
            service.Load(json, RuntimeConfigTestFixture.Source);
            return service;
        }
    }

    internal sealed class MemoryProgressStore : IProgressSaveStore
    {
        public string Payload { get; set; }

        public int WriteCount { get; private set; }

        public bool ThrowOnWrite { get; set; }

        public bool TryRead(out string payload)
        {
            payload = Payload;
            return Payload != null;
        }

        public void Write(string payload)
        {
            if (ThrowOnWrite)
            {
                throw new InvalidOperationException("simulated_write_failure");
            }

            Payload = payload;
            WriteCount += 1;
        }
    }
}
