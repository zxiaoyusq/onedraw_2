using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T520
{
    [Category("T520")]
    public sealed class TutorialFlowTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T520");
        }

        [Test]
        public void TutorialLevelUsesSixConfiguredEventDrivenStepsAndWaves()
        {
            TutorialDefinition definition = TutorialDefinitionFactory.Create(
                config,
                ConfigIds.Levels.Lv001Tutorial);

            Assert.That(definition.TutorialId, Is.EqualTo("tutorial_level_001"));
            Assert.That(definition.Steps, Has.Count.EqualTo(6));
            Assert.That(config.GetLevel(definition.LevelId).DurationLimitSec, Is.EqualTo(180f));
            Assert.That(config.GetWaves(definition.LevelId), Has.Count.EqualTo(6));

            Assert.That(
                EventTypes(definition, trigger: true),
                Is.EqualTo(new[]
                {
                    TutorialEventType.BattleReady,
                    TutorialEventType.EnemyWeakpointShown,
                    TutorialEventType.WaveMultiTarget,
                    TutorialEventType.ProjectileSpawned,
                    TutorialEventType.GhostSpawned,
                    TutorialEventType.UltimateReady,
                }));
            Assert.That(
                EventTypes(definition, trigger: false),
                Is.EqualTo(new[]
                {
                    TutorialEventType.ValidStroke,
                    TutorialEventType.WeakpointHit,
                    TutorialEventType.StrokeHitCount,
                    TutorialEventType.ProjectileCut,
                    TutorialEventType.StanceChanged,
                    TutorialEventType.UltimateSucceeded,
                }));
            Assert.That(definition.Steps[2].Completion.MinimumValue, Is.EqualTo(3));
            Assert.That(
                definition.Steps[5].GestureType,
                Is.EqualTo(TutorialGestureType.Circle));
            Assert.That(
                definition.Steps,
                Has.All.Matches<TutorialStepDefinition>(step =>
                    step.BlockProgress &&
                    step.MinimumDisplaySeconds >= 0.4d &&
                    !string.IsNullOrWhiteSpace(step.TextKey) &&
                    !string.IsNullOrWhiteSpace(step.HighlightTarget)));
        }

        [Test]
        public void SequenceRejectsWrongEventsAndTimerCannotCompleteAnAction()
        {
            var sequence = new TutorialSequence(TutorialDefinitionFactory.Create(
                config,
                ConfigIds.Levels.Lv001Tutorial));
            var events = new List<TutorialRuntimeEvent>();
            sequence.EventPublished += events.Add;

            TutorialUpdateReport wrongTrigger = sequence.Notify(
                new TutorialGameplayEvent(TutorialEventType.EnemyWeakpointShown));
            Assert.That(wrongTrigger.Changed, Is.False);
            Assert.That(sequence.State, Is.EqualTo(TutorialSequenceState.WaitingForTrigger));
            Assert.That(sequence.IsProgressBlocked, Is.False);

            TutorialUpdateReport started = sequence.Notify(
                new TutorialGameplayEvent(TutorialEventType.BattleReady));
            Assert.That(started.StepStarted, Is.True);
            Assert.That(sequence.IsProgressBlocked, Is.True);

            TutorialUpdateReport timerOnly = sequence.Advance(60d);
            Assert.That(timerOnly.Changed, Is.False);
            Assert.That(sequence.State, Is.EqualTo(TutorialSequenceState.Active));
            Assert.That(sequence.CurrentStep.Order, Is.EqualTo(1));

            TutorialUpdateReport futureCompletion = sequence.Notify(
                new TutorialGameplayEvent(TutorialEventType.WeakpointHit));
            Assert.That(futureCompletion.EventAccepted, Is.False);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].EventType, Is.EqualTo(TutorialRuntimeEventType.StepStarted));
        }

        [Test]
        public void ValidCompletionLatchesUntilMinimumDisplayAndThresholdIsInclusive()
        {
            var sequence = new TutorialSequence(TutorialDefinitionFactory.Create(
                config,
                ConfigIds.Levels.Lv001Tutorial));

            sequence.Notify(new TutorialGameplayEvent(TutorialEventType.BattleReady));
            TutorialUpdateReport early = sequence.Notify(
                new TutorialGameplayEvent(TutorialEventType.ValidStroke));
            Assert.That(early.EventAccepted, Is.True);
            Assert.That(early.StepCompleted, Is.False);
            Assert.That(sequence.CompletionObserved, Is.True);
            Assert.That(sequence.Advance(0.499d).StepCompleted, Is.False);

            TutorialUpdateReport minimumReached = sequence.Advance(0.001d);
            Assert.That(minimumReached.StepCompleted, Is.True);
            Assert.That(minimumReached.CompletedStepOrder, Is.EqualTo(1));
            Assert.That(sequence.State, Is.EqualTo(TutorialSequenceState.WaitingForTrigger));
            Assert.That(sequence.IsProgressBlocked, Is.False);

            Assert.That(
                sequence.Notify(new TutorialGameplayEvent(TutorialEventType.WeakpointHit)).Changed,
                Is.False);
            Assert.That(
                sequence.Notify(new TutorialGameplayEvent(
                    TutorialEventType.EnemyWeakpointShown)).StepStarted,
                Is.True);
            sequence.Advance(0.5d);
            Assert.That(
                sequence.Notify(new TutorialGameplayEvent(
                    TutorialEventType.WeakpointHit)).StepCompleted,
                Is.True);

            sequence.Notify(new TutorialGameplayEvent(TutorialEventType.WaveMultiTarget));
            sequence.Advance(0.5d);
            Assert.That(
                sequence.Notify(new TutorialGameplayEvent(
                    TutorialEventType.StrokeHitCount,
                    value: 2L)).EventAccepted,
                Is.False);
            Assert.That(
                sequence.Notify(new TutorialGameplayEvent(
                    TutorialEventType.StrokeHitCount,
                    value: 3L)).StepCompleted,
                Is.True);
        }

        [Test]
        public void CoordinatorBlocksWaveCompletionOnlyWhileCurrentActionIsActive()
        {
            var world = new RecordingSpawnWorld();
            var coordinator = new TutorialLevelCoordinator(
                config,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv001Tutorial,
                world);

            coordinator.Advance(coordinator.Battle.Flow.Settings.CountdownDurationSeconds);
            Assert.That(coordinator.Tutorial.State, Is.EqualTo(TutorialSequenceState.Active));
            Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.True);
            coordinator.Advance(0.9d);
            Assert.That(world.ActiveEntityIds, Has.Length.EqualTo(2));
            DefeatAll(coordinator, world);

            coordinator.Advance(20d);
            Assert.That(coordinator.Tutorial.CurrentStep.Order, Is.EqualTo(1));
            Assert.That(coordinator.Battle.Level.CurrentWave.Definition.Order, Is.EqualTo(1));
            Assert.That(coordinator.Battle.Level.CurrentWave.State, Is.EqualTo(WaveRunnerState.Running));

            Assert.That(
                coordinator.NotifyGameplayEvent(new TutorialGameplayEvent(
                    TutorialEventType.ValidStroke)).StepCompleted,
                Is.True);
            Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.False);
            coordinator.Advance(0d);
            coordinator.Advance(0.5d);
            coordinator.Advance(0.7d);
            Assert.That(coordinator.Battle.Level.CurrentWave.Definition.Order, Is.EqualTo(2));
            Assert.That(world.ActiveEntityIds, Has.Length.EqualTo(1));
            Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.False);

            Assert.That(
                coordinator.NotifyGameplayEvent(new TutorialGameplayEvent(
                    TutorialEventType.WaveMultiTarget)).Changed,
                Is.False);
            Assert.That(
                coordinator.NotifyGameplayEvent(new TutorialGameplayEvent(
                    TutorialEventType.EnemyWeakpointShown)).StepStarted,
                Is.True);
            Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.True);
        }

        [Test]
        public void UnsupportedOrMalformedTutorialProtocolsFailBeforeRuntimeUse()
        {
            string unsupported = RuntimeConfigTestFixture.MutateAndRehash(root =>
                ((JArray)root["tutorials"])[0]["triggerEvent"] = "TimerElapsed");
            AssertRejected(unsupported, "unsupported event");

            string malformed = RuntimeConfigTestFixture.MutateAndRehash(root =>
                ((JArray)root["tutorials"])[2]["completeEvent"] =
                    "StrokeHitCount>=0");
            AssertRejected(malformed, "positive integer");
        }

        private static TutorialEventType[] EventTypes(
            TutorialDefinition definition,
            bool trigger)
        {
            var values = new TutorialEventType[definition.Steps.Count];
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = trigger
                    ? definition.Steps[index].Trigger.EventType
                    : definition.Steps[index].Completion.EventType;
            }

            return values;
        }

        private static void DefeatAll(
            TutorialLevelCoordinator coordinator,
            RecordingSpawnWorld world)
        {
            long[] ids = world.ActiveEntityIds;
            for (int index = 0; index < ids.Length; index++)
            {
                Assert.That(coordinator.NotifyEnemyDefeated(ids[index]), Is.True);
                world.Release(ids[index]);
            }
        }

        private static void AssertRejected(string json, string messagePart)
        {
            var service = new GameplayConfigService();
            service.Load(json, "test:T520-invalid");
            Assert.That(
                () => TutorialDefinitionFactory.Create(
                    service,
                    ConfigIds.Levels.Lv001Tutorial),
                Throws.ArgumentException.With.Message.Contains(messagePart));
        }

        private sealed class RecordingSpawnWorld : ILevelSpawnWorld
        {
            private readonly HashSet<long> active = new HashSet<long>();
            private long nextEntityId = 1L;

            public long[] ActiveEntityIds
            {
                get
                {
                    var ids = new long[active.Count];
                    active.CopyTo(ids);
                    Array.Sort(ids);
                    return ids;
                }
            }

            public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
            {
                entityId = nextEntityId++;
                active.Add(entityId);
                return true;
            }

            public void Release(long entityId)
            {
                Assert.That(active.Remove(entityId), Is.True);
            }
        }
    }
}
