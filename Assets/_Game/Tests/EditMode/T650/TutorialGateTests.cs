using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T650
{
    [Category("T650")]
    public sealed class TutorialGateTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T650");
        }

        [Test]
        public void DirectorShowsConfiguredPromptAndOnlyGameplayActionAdvancesGate()
        {
            var coordinator = CreateCoordinator();
            var view = new RecordingView();
            var progress = new ResultService(config, new MemoryStore());
            using var director = new TutorialDirector(
                config,
                coordinator,
                progress,
                view,
                BattleHudLanguage.ZhCN);

            Assert.That(view.Last.OverlayVisible, Is.False);
            Assert.That(view.Last.ReviewVisible, Is.True);
            coordinator.Advance(
                coordinator.Battle.Flow.Settings.CountdownDurationSeconds);

            TutorialStepDefinition step = coordinator.Tutorial.CurrentStep;
            Assert.That(coordinator.Tutorial.State, Is.EqualTo(TutorialSequenceState.Active));
            Assert.That(view.Last.OverlayVisible, Is.True);
            Assert.That(view.Last.Prompt,
                Is.EqualTo(config.GetText(step.TextKey).ZhCN));
            Assert.That(view.Last.SkipText,
                Is.EqualTo(config.GetText(ConfigIds.Texts.TextUiTutorialSkip).ZhCN));
            Assert.That(view.Last.ReviewText,
                Is.EqualTo(config.GetText(ConfigIds.Texts.TextUiTutorialReview).ZhCN));
            Assert.That(view.Last.HighlightTarget, Is.EqualTo("BattleArea"));
            Assert.That(view.Last.GestureType, Is.EqualTo(TutorialGestureType.Any));
            Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.True);

            coordinator.Advance(60d);
            Assert.That(coordinator.Tutorial.CurrentStep.Order, Is.EqualTo(1));
            Assert.That(coordinator.Tutorial.State, Is.EqualTo(TutorialSequenceState.Active));
            Assert.That(coordinator.NotifyGameplayEvent(new TutorialGameplayEvent(
                TutorialEventType.WeakpointHit)).Changed, Is.False);

            TutorialUpdateReport completed = coordinator.NotifyGameplayEvent(
                new TutorialGameplayEvent(
                    TutorialEventType.ValidStroke,
                    gestureType: TutorialGestureType.Horizontal));
            Assert.That(completed.StepCompleted, Is.True);
            Assert.That(coordinator.Tutorial.State,
                Is.EqualTo(TutorialSequenceState.WaitingForTrigger));
            Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.False);
            Assert.That(view.Last.OverlayVisible, Is.False);
            Assert.That(view.Last.ReviewVisible, Is.True);

            view.RequestReview();
            Assert.That(view.Last.OverlayVisible, Is.True);
            Assert.That(view.Last.IsReview, Is.True);
            Assert.That(view.Last.SkipVisible, Is.False);
            Assert.That(coordinator.Tutorial.State,
                Is.EqualTo(TutorialSequenceState.WaitingForTrigger));
            view.RequestReview();
            Assert.That(view.Last.OverlayVisible, Is.False);
        }

        [Test]
        public void ExplicitSkipMarksOnceWithoutPublishingFakeStepCompletions()
        {
            var store = new MemoryStore();
            var progress = new ResultService(config, store);
            var coordinator = CreateCoordinator();
            var events = new List<TutorialRuntimeEvent>();
            coordinator.Tutorial.EventPublished += events.Add;
            var view = new RecordingView();
            using (var director = new TutorialDirector(
                       config,
                       coordinator,
                       progress,
                       view,
                       BattleHudLanguage.EnUS))
            {
                coordinator.Advance(
                    coordinator.Battle.Flow.Settings.CountdownDurationSeconds);
                view.RequestSkip();

                Assert.That(coordinator.Tutorial.State,
                    Is.EqualTo(TutorialSequenceState.Completed));
                Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.False);
                Assert.That(progress.IsTutorialCompleted("tutorial_level_001"), Is.True);
                Assert.That(store.WriteCount, Is.EqualTo(1));
                Assert.That(Count(events, TutorialRuntimeEventType.TutorialSkipped), Is.EqualTo(1));
                Assert.That(Count(events, TutorialRuntimeEventType.TutorialCompleted), Is.EqualTo(1));
                Assert.That(Count(events, TutorialRuntimeEventType.StepCompleted), Is.Zero);
                Assert.That(view.Last.ReviewVisible, Is.True);
                Assert.That(view.Last.ReviewText, Is.EqualTo("Review Hint"));

                view.RequestSkip();
                Assert.That(store.WriteCount, Is.EqualTo(1));
            }

            var restarted = CreateCoordinator();
            var restartedEvents = new List<TutorialRuntimeEvent>();
            restarted.Tutorial.EventPublished += restartedEvents.Add;
            var restartedView = new RecordingView();
            using var restartedDirector = new TutorialDirector(
                config,
                restarted,
                progress,
                restartedView,
                BattleHudLanguage.EnUS);

            Assert.That(restarted.Tutorial.State,
                Is.EqualTo(TutorialSequenceState.Completed));
            Assert.That(restarted.Battle.Level.IsProgressBlocked, Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(Count(
                restartedEvents,
                TutorialRuntimeEventType.TutorialSkipped), Is.EqualTo(1));
            restartedView.RequestReview();
            Assert.That(restartedView.Last.Prompt, Is.EqualTo(
                config.GetText(ConfigIds.Texts.TextTutorialSwipe).EnUS));
        }

        [Test]
        public void TutorialMarkerRoundTripsThroughBuiltInVersionOneMigration()
        {
            var store = new MemoryStore
            {
                Payload =
                    "{\"version\":1,\"revision\":0,\"scoreTokens\":0," +
                    "\"levels\":[],\"unlockedLevelIds\":[\"lv_001_tutorial\"]," +
                    "\"unlockedFeatureIds\":[],\"appliedSettlementIds\":[]}",
            };
            var migrated = new ResultService(config, store);

            Assert.That(migrated.LoadResult.Status, Is.EqualTo(ProgressLoadStatus.Migrated));
            Assert.That(store.Payload, Does.Contain("\"version\":2"));
            Assert.That(store.Payload, Does.Contain("\"completedTutorialIds\":[]"));
            Assert.That(migrated.MarkTutorialCompleted("tutorial_level_001"), Is.True);
            Assert.That(migrated.MarkTutorialCompleted("tutorial_level_001"), Is.False);

            var reloaded = new ResultService(config, store);
            Assert.That(reloaded.LoadResult.Status, Is.EqualTo(ProgressLoadStatus.Loaded));
            Assert.That(reloaded.IsTutorialCompleted("tutorial_level_001"), Is.True);
            Assert.That(reloaded.Current.CompletedTutorialIds,
                Is.EqualTo(new[] { "tutorial_level_001" }));
        }

        private TutorialLevelCoordinator CreateCoordinator()
        {
            return new TutorialLevelCoordinator(
                config,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv001Tutorial,
                new AcceptingSpawnWorld());
        }

        private static int Count(
            IReadOnlyList<TutorialRuntimeEvent> events,
            TutorialRuntimeEventType eventType)
        {
            int count = 0;
            for (int index = 0; index < events.Count; index += 1)
            {
                if (events[index].EventType == eventType)
                {
                    count += 1;
                }
            }

            return count;
        }

        private sealed class RecordingView : ITutorialOverlayView
        {
            public event Action SkipRequested;

            public event Action ReviewRequested;

            public TutorialOverlayState Last { get; private set; }

            public void Render(TutorialOverlayState state) => Last = state;

            public void RequestSkip() => SkipRequested?.Invoke();

            public void RequestReview() => ReviewRequested?.Invoke();
        }

        private sealed class MemoryStore : IProgressSaveStore
        {
            public string Payload { get; set; }

            public int WriteCount { get; private set; }

            public bool TryRead(out string payload)
            {
                payload = Payload;
                return payload != null;
            }

            public void Write(string payload)
            {
                Payload = payload;
                WriteCount += 1;
            }
        }

        private sealed class AcceptingSpawnWorld : ILevelSpawnWorld
        {
            private long nextEntityId = 1L;

            public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
            {
                entityId = nextEntityId++;
                return true;
            }
        }
    }
}
