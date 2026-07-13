using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Skills;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T510
{
    [Category("T510")]
    public sealed class BattleFlowTests
    {
        private readonly List<GameObject> roots = new List<GameObject>();
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T510");
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < roots.Count; index++)
            {
                UnityEngine.Object.DestroyImmediate(roots[index]);
            }

            roots.Clear();
        }

        [Test]
        public void SettingsReadCountdownLifecycleAndUltimateWindowFromConfiguration()
        {
            BattleFlowSettings settings = BattleFlowSettingsFactory.Create(
                config,
                ConfigIds.Players.PlayerMoyan);

            Assert.That(settings.CountdownDurationSeconds, Is.EqualTo(2d));
            Assert.That(settings.PauseOnFocusLost, Is.True);
            Assert.That(
                settings.UltimateSkillId,
                Is.EqualTo(ConfigIds.Skills.SkillUltimateSeal));
            Assert.That(settings.UltimateInputWindowSeconds, Is.EqualTo(2.5d));
        }

        [Test]
        public void CountdownSplitsLargeDeltaAndUnifiedScaleDrivesLevelClock()
        {
            var world = new RecordingSpawnWorld();
            var coordinator = new BattleFlowCoordinator(
                config,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv001Tutorial,
                world);

            BattleFlowAdvanceReport before = coordinator.Advance(1.999d);
            Assert.That(before.State, Is.EqualTo(BattleFlowState.Countdown));
            Assert.That(before.Time.GameplayDeltaSeconds, Is.Zero);
            Assert.That(coordinator.Level.State, Is.EqualTo(LevelRunnerState.Ready));
            Assert.That(coordinator.Level.ElapsedSeconds, Is.Zero);

            BattleFlowAdvanceReport boundary = coordinator.Advance(0.001d);
            Assert.That(boundary.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(boundary.Time.GameplayDeltaSeconds, Is.Zero.Within(0.000001d));
            Assert.That(coordinator.Level.State, Is.EqualTo(LevelRunnerState.Running));
            Assert.That(coordinator.Flow.Time.Current.FlowElapsedSeconds, Is.EqualTo(2d).Within(0.000001d));

            coordinator.ApplyGameplayScale(0.25d, 0.8d);
            BattleFlowAdvanceReport scaled = coordinator.Advance(1d);
            Assert.That(
                scaled.Time.GameplayUnscaledDeltaSeconds,
                Is.EqualTo(1d).Within(0.000001d));
            Assert.That(scaled.Time.GameplayDeltaSeconds, Is.EqualTo(0.4d).Within(0.000001d));
            Assert.That(coordinator.Level.ElapsedSeconds, Is.EqualTo(0.4d).Within(0.000001d));
            Assert.That(coordinator.Flow.Time.Current.GameplayScale, Is.EqualTo(1d));
            Assert.That(world.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void FocusAndApplicationPauseNestFreezeAndResumeTheSameWay()
        {
            BattleFlowStateMachine countdown = CreateFlow();
            countdown.Advance(1d);
            countdown.SetApplicationFocus(false);
            countdown.Advance(60d);
            countdown.SetApplicationFocus(true);
            countdown.Advance(0.999d);
            Assert.That(countdown.State, Is.EqualTo(BattleFlowState.Countdown));
            countdown.Advance(0.001d);
            Assert.That(countdown.State, Is.EqualTo(BattleFlowState.Playing));

            BattleFlowStateMachine flow = CreateFlow();
            var events = new List<BattleFlowEvent>();
            flow.EventPublished += events.Add;
            flow.Advance(flow.Settings.CountdownDurationSeconds);
            Assert.That(flow.TryBeginUltimateDrawing(), Is.True);
            flow.Advance(1d);
            BattleTimeSnapshot beforePause = flow.Time.Current;

            Assert.That(flow.SetApplicationFocus(false), Is.True);
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Paused));
            Assert.That(flow.ActivePauseReasons, Is.EqualTo(BattlePauseReason.FocusLost));
            Assert.That(Count(events, BattleFlowEventType.UltimateCanceled), Is.EqualTo(1));
            Assert.That(Count(events, BattleFlowEventType.StrokeCancellationRequested), Is.EqualTo(1));

            Assert.That(flow.SetApplicationPaused(true), Is.True);
            Assert.That(
                flow.ActivePauseReasons,
                Is.EqualTo(BattlePauseReason.FocusLost | BattlePauseReason.ApplicationPaused));
            Assert.That(Count(events, BattleFlowEventType.StrokeCancellationRequested), Is.EqualTo(1));
            BattleTimeSlice frozen = flow.Advance(60d);
            Assert.That(frozen.FlowDeltaSeconds, Is.Zero);
            Assert.That(frozen.GameplayDeltaSeconds, Is.Zero);
            Assert.That(flow.Time.Current.FlowElapsedSeconds, Is.EqualTo(beforePause.FlowElapsedSeconds));
            Assert.That(flow.Time.Current.GameplayElapsedSeconds, Is.EqualTo(beforePause.GameplayElapsedSeconds));

            Assert.That(flow.SetApplicationFocus(true), Is.True);
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Paused));
            Assert.That(flow.SetApplicationPaused(false), Is.True);
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(flow.ActivePauseReasons, Is.EqualTo(BattlePauseReason.None));
        }

        [Test]
        public void UltimateRequiresSkillServiceResultAndInclusiveBoundaryRemainsValid()
        {
            BattleFlowStateMachine flow = CreateFlow();
            flow.Advance(flow.Settings.CountdownDurationSeconds);
            PlayerCombatController player = CreatePlayer();
            player.GainEnergy(100L, 0d, "test_fill");
            var world = new FlowSkillWorld(flow.Time);
            var skills = new SkillService(config, player);

            Assert.That(flow.TryBeginUltimateDrawing(), Is.True);
            flow.Advance(flow.Settings.UltimateInputWindowSeconds);
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.UltimateDrawing));
            Assert.That(flow.CanAcceptUltimateGestureEvent(1UL), Is.True);

            SkillActivationResult invalid = ActivateUltimate(
                skills,
                world,
                gestureIsValid: false,
                flow.Settings.UltimateInputWindowSeconds,
                flow.Time.Current.GameplayElapsedSeconds);
            Assert.That(invalid.Status, Is.EqualTo(SkillActivationStatus.GestureInvalid));
            Assert.That(flow.ResolveUltimate(1UL, invalid), Is.True);
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(player.Current.CurrentEnergy, Is.EqualTo(100));
            Assert.That(world.CoreEffectCount, Is.Zero);

            Assert.That(flow.TryBeginUltimateDrawing(), Is.True);
            Assert.That(flow.CanAcceptUltimateGestureEvent(2UL), Is.True);
            SkillActivationResult activated = ActivateUltimate(
                skills,
                world,
                gestureIsValid: true,
                flow.Settings.UltimateInputWindowSeconds,
                flow.Time.Current.GameplayElapsedSeconds);
            Assert.That(activated.Status, Is.EqualTo(SkillActivationStatus.Activated));
            Assert.That(flow.ResolveUltimate(2UL, activated), Is.True);
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(player.Current.CurrentEnergy, Is.Zero);
            Assert.That(world.CoreEffectCount, Is.EqualTo(2));
            Assert.That(flow.LastUltimateGestureEventId, Is.EqualTo(2UL));

            Assert.That(flow.TryBeginUltimateDrawing(), Is.True);
            Assert.That(flow.CanAcceptUltimateGestureEvent(2UL), Is.False);
            Assert.That(
                () => flow.ResolveUltimate(2UL, activated),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(flow.CancelUltimateDrawing(), Is.True);

            BattleTimeSlice slowed = flow.Advance(0.8d);
            Assert.That(slowed.GameplayDeltaSeconds, Is.EqualTo(0.2d).Within(0.000001d));
            Assert.That(flow.Time.Current.GameplayScale, Is.EqualTo(1d));
        }

        [Test]
        public void SameFrameDefeatWinsAndSettlementPublishesExactlyOnce()
        {
            BattleFlowStateMachine flow = CreateFlow();
            var events = new List<BattleFlowEvent>();
            flow.EventPublished += events.Add;
            flow.Advance(flow.Settings.CountdownDurationSeconds);

            Assert.That(
                flow.ResolveOutcome(new BattleOutcomeFacts(
                    playerDied: true,
                    levelCompleted: true,
                    durationLimitReached: true)),
                Is.True);
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Defeat));
            Assert.That(Count(events, BattleFlowEventType.Settled), Is.EqualTo(1));
            Assert.That(LastSettlement(events), Is.EqualTo(BattleSettlement.Defeat));

            Assert.That(
                flow.ResolveOutcome(new BattleOutcomeFacts(false, true, false)),
                Is.False);
            flow.Advance(100d);
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Defeat));
            Assert.That(Count(events, BattleFlowEventType.Settled), Is.EqualTo(1));

            BattleFlowStateMachine victory = CreateFlow();
            victory.Advance(victory.Settings.CountdownDurationSeconds);
            Assert.That(
                victory.ResolveOutcome(new BattleOutcomeFacts(false, true, false)),
                Is.True);
            Assert.That(victory.State, Is.EqualTo(BattleFlowState.Victory));
        }

        [Test]
        public void InvalidDeltasAndCombinedPauseReasonsFailExplicitly()
        {
            BattleFlowStateMachine flow = CreateFlow();

            Assert.That(() => flow.Advance(double.NaN), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => flow.SetPauseReason(
                    BattlePauseReason.FocusLost | BattlePauseReason.ApplicationPaused,
                    true),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => flow.Time.ApplyGameplayScale(-0.1d, 1d),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private BattleFlowStateMachine CreateFlow()
        {
            return new BattleFlowStateMachine(
                BattleFlowSettingsFactory.Create(
                    config,
                    ConfigIds.Players.PlayerMoyan));
        }

        private PlayerCombatController CreatePlayer()
        {
            var root = new GameObject("T510 Player");
            roots.Add(root);
            PlayerCombatController player = root.AddComponent<PlayerCombatController>();
            player.Initialize(config, ConfigIds.Players.PlayerMoyan);
            return player;
        }

        private static SkillActivationResult ActivateUltimate(
            SkillService skills,
            ISkillEffectWorld world,
            bool gestureIsValid,
            double inputElapsedSeconds,
            double timestamp)
        {
            return skills.TryActivate(
                new SkillActivationRequest(
                    ConfigIds.Skills.SkillUltimateSeal,
                    SkillTriggerTypes.Ultimate,
                    "Circle",
                    gestureIsValid,
                    inputElapsedSeconds,
                    timestamp),
                new SkillEffectContext(world, timestamp));
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

        private static BattleSettlement LastSettlement(
            IReadOnlyList<BattleFlowEvent> events)
        {
            for (int index = events.Count - 1; index >= 0; index--)
            {
                if (events[index].EventType == BattleFlowEventType.Settled)
                {
                    return events[index].Settlement;
                }
            }

            return BattleSettlement.None;
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

        private sealed class FlowSkillWorld : ISkillEffectWorld
        {
            private readonly BattleTimeSource time;

            public FlowSkillWorld(BattleTimeSource battleTime)
            {
                time = battleTime;
            }

            public int CoreEffectCount { get; private set; }

            public IReadOnlyList<ISkillEffectTarget> Targets { get; } =
                Array.Empty<ISkillEffectTarget>();

            public ISkillEffectTarget PrimaryTarget => null;

            public int RepeatLastStroke(float damageMultiplier, float delaySeconds, string sourceId, double timestamp) => 0;

            public int SetTimeScale(float scale, float durationSeconds, string sourceId, double timestamp)
            {
                time.ApplyGameplayScale(scale, durationSeconds);
                CoreEffectCount++;
                return 1;
            }

            public int SetNextStrokeDamageMultiplier(float multiplier, string sourceId, double timestamp) => 0;

            public int ClearHostileProjectiles(string sourceId, double timestamp)
            {
                CoreEffectCount++;
                return 0;
            }

            public void PlayVfx(string vfxKey, IReadOnlyList<ISkillEffectTarget> targets, string sourceId, double timestamp)
            {
            }

            public void PlayAudio(string audioKey, string sourceId, double timestamp)
            {
            }
        }
    }
}
