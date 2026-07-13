using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Skills;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T510
{
    [Category("T510")]
    public sealed class NoAdvanceBeforePlayerActionTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            PointerInputRuntime.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            PointerInputRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator FocusAndApplicationPauseCancelOnceFreezeAndRequireFullResume()
        {
            yield return LoadRuntimeConfiguration();
            var world = new RuntimeSpawnWorld();
            var coordinator = CreateCoordinator(world);
            var events = new List<BattleFlowEvent>();
            coordinator.Flow.EventPublished += events.Add;
            coordinator.Advance(coordinator.Flow.Settings.CountdownDurationSeconds);
            Assert.That(coordinator.TryBeginUltimateDrawing(), Is.True);
            coordinator.Advance(1d);
            BattleTimeSnapshot beforePause = coordinator.Flow.Time.Current;
            double levelBeforePause = coordinator.Level.ElapsedSeconds;

            Assert.That(coordinator.SetApplicationFocus(false), Is.True);
            Assert.That(coordinator.Flow.State, Is.EqualTo(BattleFlowState.Paused));
            Assert.That(coordinator.SetApplicationPaused(true), Is.True);
            BattleFlowAdvanceReport frozen = coordinator.Advance(30d);
            Assert.That(frozen.Time.FlowDeltaSeconds, Is.Zero);
            Assert.That(frozen.Time.GameplayDeltaSeconds, Is.Zero);
            Assert.That(coordinator.Level.ElapsedSeconds, Is.EqualTo(levelBeforePause));
            Assert.That(
                coordinator.Flow.Time.Current.FlowElapsedSeconds,
                Is.EqualTo(beforePause.FlowElapsedSeconds));
            Assert.That(Count(events, BattleFlowEventType.StrokeCancellationRequested), Is.EqualTo(1));
            Assert.That(Count(events, BattleFlowEventType.UltimateCanceled), Is.EqualTo(1));

            Assert.That(coordinator.SetApplicationFocus(true), Is.True);
            Assert.That(coordinator.Flow.State, Is.EqualTo(BattleFlowState.Paused));
            Assert.That(coordinator.SetApplicationPaused(false), Is.True);
            Assert.That(coordinator.Flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(coordinator.Flow.ActivePauseReasons, Is.EqualTo(BattlePauseReason.None));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimerCannotActivateUltimateButValidBoundaryGestureCan()
        {
            yield return LoadRuntimeConfiguration();
            root = new GameObject("T510 Runtime Player");
            PlayerCombatController player = root.AddComponent<PlayerCombatController>();
            player.Initialize(
                GameplayConfigRuntime.Current,
                ConfigIds.Players.PlayerMoyan);
            player.GainEnergy(100L, 0d, "test_fill");

            var coordinator = CreateCoordinator(new RuntimeSpawnWorld());
            var events = new List<BattleFlowEvent>();
            coordinator.Flow.EventPublished += events.Add;
            var world = new RuntimeSkillWorld(coordinator);
            var skills = new SkillService(GameplayConfigRuntime.Current, player);
            coordinator.Advance(coordinator.Flow.Settings.CountdownDurationSeconds);

            Assert.That(coordinator.TryBeginUltimateDrawing(), Is.True);
            coordinator.Advance(coordinator.Flow.Settings.UltimateInputWindowSeconds);
            Assert.That(coordinator.Flow.State, Is.EqualTo(BattleFlowState.UltimateDrawing));
            Assert.That(player.Current.CurrentEnergy, Is.EqualTo(100));
            Assert.That(world.CoreEffects, Is.Zero);
            coordinator.Advance(0.000001d);
            Assert.That(coordinator.Flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(Count(events, BattleFlowEventType.UltimateResolved), Is.Zero);
            Assert.That(player.Current.CurrentEnergy, Is.EqualTo(100));

            Assert.That(coordinator.TryBeginUltimateDrawing(), Is.True);
            Assert.That(coordinator.CanAcceptUltimateGestureEvent(1UL), Is.True);
            double timestamp = coordinator.Flow.Time.Current.GameplayElapsedSeconds;
            SkillActivationResult activated = skills.TryActivate(
                new SkillActivationRequest(
                    ConfigIds.Skills.SkillUltimateSeal,
                    SkillTriggerTypes.Ultimate,
                    "Circle",
                    gestureIsValid: true,
                    inputElapsedSeconds: coordinator.Flow.Settings.UltimateInputWindowSeconds,
                    timestamp),
                new SkillEffectContext(world, timestamp));

            Assert.That(activated.Status, Is.EqualTo(SkillActivationStatus.Activated));
            Assert.That(coordinator.ResolveUltimate(1UL, activated), Is.True);
            Assert.That(coordinator.Flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(player.Current.CurrentEnergy, Is.Zero);
            Assert.That(world.CoreEffects, Is.EqualTo(2));
            Assert.That(Count(events, BattleFlowEventType.UltimateResolved), Is.EqualTo(1));

            BattleFlowAdvanceReport slowed = coordinator.Advance(0.8d);
            Assert.That(slowed.Time.GameplayDeltaSeconds, Is.EqualTo(0.2d).Within(0.000001d));
            yield return null;
        }

        private static BattleFlowCoordinator CreateCoordinator(ILevelSpawnWorld world)
        {
            return new BattleFlowCoordinator(
                GameplayConfigRuntime.Current,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv001Tutorial,
                world);
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

        private static IEnumerator LoadRuntimeConfiguration()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.MainMenu &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));
            Assert.That(GameplayConfigRuntime.IsReady, Is.True);
        }

        private sealed class RuntimeSpawnWorld : ILevelSpawnWorld
        {
            private long nextEntityId = 1L;

            public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
            {
                entityId = nextEntityId++;
                return true;
            }
        }

        private sealed class RuntimeSkillWorld : ISkillEffectWorld
        {
            private readonly BattleFlowCoordinator coordinator;

            public RuntimeSkillWorld(BattleFlowCoordinator battleCoordinator)
            {
                coordinator = battleCoordinator;
            }

            public int CoreEffects { get; private set; }

            public IReadOnlyList<ISkillEffectTarget> Targets { get; } =
                Array.Empty<ISkillEffectTarget>();

            public ISkillEffectTarget PrimaryTarget => null;

            public int RepeatLastStroke(float damageMultiplier, float delaySeconds, string sourceId, double timestamp) => 0;

            public int SetTimeScale(float scale, float durationSeconds, string sourceId, double timestamp)
            {
                coordinator.ApplyGameplayScale(scale, durationSeconds);
                CoreEffects++;
                return 1;
            }

            public int SetNextStrokeDamageMultiplier(float multiplier, string sourceId, double timestamp) => 0;

            public int ClearHostileProjectiles(string sourceId, double timestamp)
            {
                CoreEffects++;
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
