using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T600
{
    [Category("T600")]
    public sealed class BattleHudStateBindingTests
    {
        private GameObject playerRoot;
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T600:hud-binding");
            playerRoot = new GameObject("T600 Player");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(playerRoot);
        }

        [Test]
        public void BindingPublishesOneStateFromPlayerComboScoreFlowAndResultServices()
        {
            PlayerCombatController player = playerRoot.AddComponent<PlayerCombatController>();
            player.Initialize(config, ConfigIds.Players.PlayerMoyan);
            ComboService combo = ComboService.FromConfig(config);
            var score = new ScoreService();
            var flow = new BattleFlowStateMachine(
                BattleFlowSettingsFactory.Create(config, ConfigIds.Players.PlayerMoyan));
            flow.Advance(flow.Settings.CountdownDurationSeconds);
            var results = new ResultService(config, new MemoryStore());
            using var binding = new BattleHudStateBinding(
                ConfigIds.Levels.Lv001Tutorial,
                player,
                combo,
                score,
                flow,
                results);
            var states = new List<BattleHudState>();
            binding.Changed += states.Add;

            player.GainEnergy(100L, 1d, "test_fill");
            Assert.That(states.Count, Is.EqualTo(1));
            Assert.That(states[0].CurrentEnergy, Is.EqualTo(100L));
            Assert.That(states[0].Timestamp, Is.EqualTo(1d));

            combo.RegisterHit(1d);
            Assert.That(states.Count, Is.EqualTo(2));
            Assert.That(states[1].ComboCount, Is.EqualTo(1));

            score.Record(CreateDamageResult());
            Assert.That(states.Count, Is.EqualTo(3));
            Assert.That(states[2].LiveScore, Is.GreaterThan(0L));

            flow.SetPlayerPaused(true);
            Assert.That(states.Count, Is.EqualTo(4));
            Assert.That(states[3].FlowState, Is.EqualTo(BattleFlowState.Paused));

            results.Settle(new ResultRequest(
                "t600-binding-settlement",
                ConfigIds.Levels.Lv001Tutorial,
                BattleSettlement.Victory,
                new BattleResultMetrics(2000L, 2, 0L, 120.9d)));
            Assert.That(states.Count, Is.EqualTo(5));
            Assert.That(states[4].Result, Is.Not.Null);
            Assert.That(states[4].Result.FinalScore, Is.EqualTo(4480L));
            Assert.That(states[4].Result.Rewards.Count, Is.EqualTo(2));
            Assert.That(states[4].Result.CanGoNext, Is.True);
        }

        [Test]
        public void UltimateClockPublishesOnlyOnVisibleSecondOrReadyBoundary()
        {
            PlayerCombatController player = playerRoot.AddComponent<PlayerCombatController>();
            player.Initialize(config, ConfigIds.Players.PlayerMoyan);
            ComboService combo = ComboService.FromConfig(config);
            var score = new ScoreService();
            var flow = new BattleFlowStateMachine(
                BattleFlowSettingsFactory.Create(config, ConfigIds.Players.PlayerMoyan));
            var results = new ResultService(config, new MemoryStore());
            using var binding = new BattleHudStateBinding(
                ConfigIds.Levels.Lv001Tutorial,
                player,
                combo,
                score,
                flow,
                results);
            int publicationCount = 0;
            binding.Changed += _ => publicationCount += 1;

            binding.UpdateUltimateClock(1d, 5d);
            Assert.That(publicationCount, Is.EqualTo(1));
            Assert.That(binding.Current.UltimateCooldownUntil, Is.EqualTo(5d));
            binding.UpdateUltimateClock(1.1d, 5d);
            Assert.That(publicationCount, Is.EqualTo(1));
            binding.UpdateUltimateClock(2.1d, 5d);
            Assert.That(publicationCount, Is.EqualTo(2));
            binding.UpdateUltimateClock(5d, 5d);
            Assert.That(publicationCount, Is.EqualTo(3));

            Assert.That(
                () => binding.UpdateUltimateClock(4.9d, 5d),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void DisposedBindingUnsubscribesAndRejectsStateReads()
        {
            PlayerCombatController player = playerRoot.AddComponent<PlayerCombatController>();
            player.Initialize(config, ConfigIds.Players.PlayerMoyan);
            ComboService combo = ComboService.FromConfig(config);
            var score = new ScoreService();
            var flow = new BattleFlowStateMachine(
                BattleFlowSettingsFactory.Create(config, ConfigIds.Players.PlayerMoyan));
            var results = new ResultService(config, new MemoryStore());
            var binding = new BattleHudStateBinding(
                ConfigIds.Levels.Lv001Tutorial,
                player,
                combo,
                score,
                flow,
                results);
            int publicationCount = 0;
            binding.Changed += _ => publicationCount += 1;

            binding.Dispose();
            player.GainEnergy(10L, 1d, "after_dispose");
            combo.RegisterHit(1d);
            flow.SetPlayerPaused(true);

            Assert.That(publicationCount, Is.Zero);
            Assert.That(() => _ = binding.Current, Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => binding.Dispose(), Throws.Nothing);
        }

        private DamageResult CreateDamageResult()
        {
            DamageRuleSet rules = DamageRuleSetFactory.Create(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.DefenseRules.DefenseNone,
                ConfigIds.WeakpointRules.WeakpointNone);
            var context = new DamageContext(
                1UL,
                101,
                GestureType.Horizontal,
                ConfigIds.Stances.StanceBlade,
                false,
                1,
                1d);
            return DamageCalculator.Calculate(context, rules, new FixedRandomSource());
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            public double NextUnitInterval() => 0.5d;
        }

        private sealed class MemoryStore : IProgressSaveStore
        {
            private string payload;

            public bool TryRead(out string loaded)
            {
                loaded = payload;
                return payload != null;
            }

            public void Write(string value)
            {
                payload = value;
            }
        }
    }
}
