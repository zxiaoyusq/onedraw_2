using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T510
{
    [Category("T510")]
    public sealed class NoAdvanceBeforePlayerActionTests
    {
        [Test]
        public void LargeTimeAdvanceCannotCrossConfiguredPlayerConfirmedGate()
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
            var config = new GameplayConfigService();
            config.Load(json, "test:T510-player-gate");
            var world = new RecordingSpawnWorld();
            var coordinator = new BattleFlowCoordinator(
                config,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv001Tutorial,
                world);

            Assert.That(coordinator.ConfirmPlayerAction(), Is.False);
            coordinator.Advance(102d);
            Assert.That(coordinator.Flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(coordinator.Level.CurrentWave.State, Is.EqualTo(WaveRunnerState.Waiting));
            Assert.That(world.Requests, Is.Empty);

            Assert.That(coordinator.ConfirmPlayerAction(), Is.True);
            coordinator.Advance(0.499d);
            Assert.That(coordinator.Level.CurrentWave.State, Is.EqualTo(WaveRunnerState.Waiting));
            coordinator.Advance(0.001d);
            Assert.That(coordinator.Level.CurrentWave.State, Is.EqualTo(WaveRunnerState.Running));
            Assert.That(world.Requests, Is.Empty);
            coordinator.Advance(0.2d);
            Assert.That(world.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void UltimateTimerOnlyCancelsAndNeverCreatesSuccess()
        {
            var config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T510-ultimate-timeout");
            var flow = new BattleFlowStateMachine(
                BattleFlowSettingsFactory.Create(
                    config,
                    ConfigIds.Players.PlayerMoyan));
            var events = new List<BattleFlowEvent>();
            flow.EventPublished += events.Add;
            flow.Advance(flow.Settings.CountdownDurationSeconds);

            Assert.That(flow.TryBeginUltimateDrawing(), Is.True);
            flow.Advance(flow.Settings.UltimateInputWindowSeconds);
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.UltimateDrawing));
            flow.Advance(0.000001d);

            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(Count(events, BattleFlowEventType.UltimateResolved), Is.Zero);
            Assert.That(Count(events, BattleFlowEventType.UltimateCanceled), Is.EqualTo(1));
            Assert.That(
                FindCancelReason(events),
                Is.EqualTo(UltimateCancelReason.InputWindowExpired));
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

        private static int Count(
            IReadOnlyList<BattleFlowEvent> events,
            BattleFlowEventType eventType)
        {
            int count = 0;
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].EventType == eventType)
                {
                    count++;
                }
            }

            return count;
        }

        private static UltimateCancelReason FindCancelReason(
            IReadOnlyList<BattleFlowEvent> events)
        {
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].EventType == BattleFlowEventType.UltimateCanceled)
                {
                    return events[index].UltimateCancelReason;
                }
            }

            return UltimateCancelReason.None;
        }

        private sealed class RecordingSpawnWorld : ILevelSpawnWorld
        {
            private long nextEntityId = 1L;

            public List<LevelSpawnRequest> Requests { get; } =
                new List<LevelSpawnRequest>();

            public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
            {
                Requests.Add(request);
                entityId = nextEntityId++;
                return true;
            }
        }
    }
}
