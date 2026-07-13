using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

namespace OneStrokeDemon.Tests.PlayMode.T520
{
    [Category("T520")]
    public sealed class TutorialLevelE2EPlayModeTests
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
        public IEnumerator SixConfiguredActionsDriveTutorialToVictoryWithoutTimerShortcuts()
        {
            yield return LoadRuntimeConfiguration();
            root = new GameObject("T520 Tutorial Runtime");
            var world = new GameObjectSpawnWorld(root.transform);
            var coordinator = new TutorialLevelCoordinator(
                GameplayConfigRuntime.Current,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv001Tutorial,
                world);
            var tutorialEvents = new List<TutorialRuntimeEvent>();
            coordinator.Tutorial.EventPublished += tutorialEvents.Add;

            coordinator.Advance(coordinator.Battle.Flow.Settings.CountdownDurationSeconds);
            Assert.That(coordinator.Battle.Flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(coordinator.Tutorial.CurrentStep.Order, Is.EqualTo(1));
            Assert.That(coordinator.Battle.Level.CurrentWave.Definition.Order, Is.EqualTo(1));

            coordinator.Advance(0.9d);
            Assert.That(world.ActiveEntityIds, Has.Length.EqualTo(2));
            DefeatAll(coordinator, world);
            coordinator.Advance(5d);
            Assert.That(
                coordinator.Tutorial.State,
                Is.EqualTo(TutorialSequenceState.Active),
                "Elapsed time must not replace the configured ValidStroke action.");
            CompleteAction(
                coordinator,
                TutorialEventType.ValidStroke,
                TutorialGestureType.Horizontal);
            EnterNextWave(coordinator, expectedOrder: 2);
            Assert.That(world.ActiveEntityIds, Has.Length.EqualTo(1));

            StartStep(coordinator, TutorialEventType.EnemyWeakpointShown, 2);
            DefeatAll(coordinator, world);
            coordinator.Advance(0.5d);
            CompleteAction(coordinator, TutorialEventType.WeakpointHit);
            EnterNextWave(coordinator, expectedOrder: 3);
            Assert.That(world.ActiveEntityIds, Has.Length.EqualTo(3));

            StartStep(coordinator, TutorialEventType.WaveMultiTarget, 3);
            coordinator.Advance(0.5d);
            Assert.That(
                coordinator.NotifyGameplayEvent(new TutorialGameplayEvent(
                    TutorialEventType.StrokeHitCount,
                    value: 2L)).EventAccepted,
                Is.False);
            DefeatAll(coordinator, world);
            Assert.That(
                coordinator.NotifyGameplayEvent(new TutorialGameplayEvent(
                    TutorialEventType.StrokeHitCount,
                    value: 3L)).StepCompleted,
                Is.True);
            EnterNextWave(coordinator, expectedOrder: 4);
            coordinator.Advance(0.65d);
            Assert.That(world.ActiveEntityIds, Has.Length.EqualTo(2));

            StartStep(coordinator, TutorialEventType.ProjectileSpawned, 4);
            DefeatAll(coordinator, world);
            coordinator.Advance(
                coordinator.Tutorial.CurrentStep.MinimumDisplaySeconds);
            CompleteAction(coordinator, TutorialEventType.ProjectileCut);
            EnterNextWave(coordinator, expectedOrder: 5);
            coordinator.Advance(1.4d);
            Assert.That(world.ActiveEntityIds, Has.Length.EqualTo(3));

            StartStep(coordinator, TutorialEventType.GhostSpawned, 5);
            coordinator.Advance(0.5d);
            var playerRoot = new GameObject("T520 Player");
            playerRoot.transform.SetParent(root.transform, false);
            PlayerCombatController player = playerRoot.AddComponent<PlayerCombatController>();
            player.Initialize(GameplayConfigRuntime.Current, ConfigIds.Players.PlayerMoyan);
            StanceSwitchResult switched = player.TrySwitchStance(
                ConfigIds.Stances.StanceTalisman,
                coordinator.Battle.Flow.Time.Current.GameplayElapsedSeconds);
            Assert.That(switched.Status, Is.EqualTo(StanceSwitchStatus.Switched));
            DefeatAll(coordinator, world);
            CompleteAction(coordinator, TutorialEventType.StanceChanged);
            EnterNextWave(coordinator, expectedOrder: 6);
            Assert.That(world.ActiveEntityIds, Has.Length.EqualTo(4));

            StartStep(coordinator, TutorialEventType.UltimateReady, 6);
            coordinator.Advance(0.5d);
            player.GainEnergy(
                100L,
                coordinator.Battle.Flow.Time.Current.GameplayElapsedSeconds,
                "test_fill");
            Assert.That(coordinator.TryBeginUltimateDrawing(), Is.True);
            Assert.That(coordinator.CanAcceptUltimateGestureEvent(1UL), Is.True);
            long[] ultimateTargetIds = world.ActiveEntityIds;
            var skillWorld = new TutorialSkillWorld(
                coordinator,
                world,
                ultimateTargetIds);
            var skills = new SkillService(GameplayConfigRuntime.Current, player);
            double timestamp = coordinator.Battle.Flow.Time.Current.GameplayElapsedSeconds;
            SkillActivationResult activated = skills.TryActivate(
                new SkillActivationRequest(
                    coordinator.Battle.Flow.Settings.UltimateSkillId,
                    SkillTriggerTypes.Ultimate,
                    coordinator.Battle.Flow.Settings.UltimateGestureType,
                    gestureIsValid: true,
                    inputElapsedSeconds:
                        coordinator.Battle.Flow.Settings.UltimateInputWindowSeconds,
                    timestamp),
                new SkillEffectContext(skillWorld, timestamp));

            Assert.That(activated.Status, Is.EqualTo(SkillActivationStatus.Activated));
            Assert.That(skillWorld.DefeatedTargetCount, Is.EqualTo(4));
            Assert.That(world.ActiveEntityIds, Is.Empty);
            Assert.That(coordinator.ResolveUltimate(1UL, activated), Is.True);
            Assert.That(coordinator.Tutorial.State, Is.EqualTo(TutorialSequenceState.Completed));
            Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.False);

            BattleFlowAdvanceReport settled = coordinator.Advance(0.8d);
            Assert.That(settled.State, Is.EqualTo(BattleFlowState.Victory));
            Assert.That(coordinator.Battle.Level.State, Is.EqualTo(LevelRunnerState.Completed));
            Assert.That(world.TotalSpawned, Is.EqualTo(15));
            Assert.That(
                coordinator.Battle.Level.ElapsedSeconds,
                Is.LessThanOrEqualTo(180d));
            Assert.That(
                Count(tutorialEvents, TutorialRuntimeEventType.StepStarted),
                Is.EqualTo(6));
            Assert.That(
                Count(tutorialEvents, TutorialRuntimeEventType.StepCompleted),
                Is.EqualTo(6));
            Assert.That(
                Count(tutorialEvents, TutorialRuntimeEventType.TutorialCompleted),
                Is.EqualTo(1));
            Assert.That(player.Current.CurrentEnergy, Is.Zero);
            Assert.That(skillWorld.TimeScaleApplications, Is.EqualTo(1));
            Assert.That(skillWorld.ProjectileClears, Is.EqualTo(1));
            yield return null;
        }

        private static void StartStep(
            TutorialLevelCoordinator coordinator,
            TutorialEventType eventType,
            int expectedOrder)
        {
            Assert.That(coordinator.Tutorial.CurrentStep.Order, Is.EqualTo(expectedOrder));
            TutorialUpdateReport report = coordinator.NotifyGameplayEvent(
                new TutorialGameplayEvent(eventType));
            Assert.That(report.StepStarted, Is.True);
            Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.True);
        }

        private static void CompleteAction(
            TutorialLevelCoordinator coordinator,
            TutorialEventType eventType,
            TutorialGestureType gestureType = TutorialGestureType.Any)
        {
            TutorialUpdateReport report = coordinator.NotifyGameplayEvent(
                new TutorialGameplayEvent(eventType, gestureType: gestureType));
            Assert.That(report.StepCompleted, Is.True);
        }

        private static void EnterNextWave(
            TutorialLevelCoordinator coordinator,
            int expectedOrder)
        {
            coordinator.Advance(0d);
            coordinator.Advance(0.5d);
            coordinator.Advance(0.7d);
            Assert.That(
                coordinator.Battle.Level.CurrentWave.Definition.Order,
                Is.EqualTo(expectedOrder));
            Assert.That(
                coordinator.Tutorial.State,
                Is.EqualTo(TutorialSequenceState.WaitingForTrigger));
            Assert.That(coordinator.Battle.Level.IsProgressBlocked, Is.False);
        }

        private static void DefeatAll(
            TutorialLevelCoordinator coordinator,
            GameObjectSpawnWorld world)
        {
            long[] ids = world.ActiveEntityIds;
            for (int index = 0; index < ids.Length; index++)
            {
                Assert.That(coordinator.NotifyEnemyDefeated(ids[index]), Is.True);
                world.Release(ids[index]);
            }
        }

        private static int Count(
            IReadOnlyList<TutorialRuntimeEvent> events,
            TutorialRuntimeEventType eventType)
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
            yield return SceneManager.LoadSceneAsync(
                SceneNames.Bootstrap,
                LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.MainMenu &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));
            Assert.That(GameplayConfigRuntime.IsReady, Is.True);
        }

        private sealed class GameObjectSpawnWorld : ILevelSpawnWorld
        {
            private readonly Transform parent;
            private readonly Dictionary<long, GameObject> active =
                new Dictionary<long, GameObject>();
            private long nextEntityId = 1L;

            public GameObjectSpawnWorld(Transform configuredParent)
            {
                parent = configuredParent;
            }

            public int TotalSpawned { get; private set; }

            public long[] ActiveEntityIds
            {
                get
                {
                    var ids = new long[active.Count];
                    active.Keys.CopyTo(ids, 0);
                    Array.Sort(ids);
                    return ids;
                }
            }

            public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
            {
                entityId = nextEntityId++;
                var instance = new GameObject(
                    request.EnemyId + " #" +
                    entityId.ToString(CultureInfo.InvariantCulture));
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = new Vector3(
                    (float)request.Position.X,
                    (float)request.Position.Y,
                    0f);
                active.Add(entityId, instance);
                TotalSpawned++;
                return true;
            }

            public void Release(long entityId)
            {
                GameObject instance = active[entityId];
                active.Remove(entityId);
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private sealed class TutorialSkillWorld : ISkillEffectWorld
        {
            private readonly TutorialLevelCoordinator coordinator;
            private readonly IReadOnlyList<ISkillEffectTarget> targets;

            public TutorialSkillWorld(
                TutorialLevelCoordinator configuredCoordinator,
                GameObjectSpawnWorld spawnWorld,
                IReadOnlyList<long> entityIds)
            {
                coordinator = configuredCoordinator;
                var configuredTargets = new ISkillEffectTarget[entityIds.Count];
                for (int index = 0; index < configuredTargets.Length; index++)
                {
                    configuredTargets[index] = new TutorialSkillTarget(
                        configuredCoordinator,
                        spawnWorld,
                        entityIds[index],
                        this);
                }

                targets = Array.AsReadOnly(configuredTargets);
            }

            public int DefeatedTargetCount { get; private set; }

            public int TimeScaleApplications { get; private set; }

            public int ProjectileClears { get; private set; }

            public IReadOnlyList<ISkillEffectTarget> Targets => targets;

            public ISkillEffectTarget PrimaryTarget => null;

            public int RepeatLastStroke(
                float damageMultiplier,
                float delaySeconds,
                string sourceId,
                double timestamp)
            {
                return 0;
            }

            public int SetTimeScale(
                float scale,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                coordinator.ApplyGameplayScale(scale, durationSeconds);
                TimeScaleApplications++;
                return 1;
            }

            public int SetNextStrokeDamageMultiplier(
                float multiplier,
                string sourceId,
                double timestamp)
            {
                return 0;
            }

            public int ClearHostileProjectiles(string sourceId, double timestamp)
            {
                ProjectileClears++;
                return 1;
            }

            public void PlayVfx(
                string vfxKey,
                IReadOnlyList<ISkillEffectTarget> selectedTargets,
                string sourceId,
                double timestamp)
            {
            }

            public void PlayAudio(string audioKey, string sourceId, double timestamp)
            {
            }

            public void RecordDefeat()
            {
                DefeatedTargetCount++;
            }
        }

        private sealed class TutorialSkillTarget : ISkillEffectTarget
        {
            private readonly TutorialLevelCoordinator coordinator;
            private readonly GameObjectSpawnWorld world;
            private readonly long entityId;
            private readonly TutorialSkillWorld owner;
            private float hitPoints = 30f;

            public TutorialSkillTarget(
                TutorialLevelCoordinator configuredCoordinator,
                GameObjectSpawnWorld spawnWorld,
                long configuredEntityId,
                TutorialSkillWorld configuredOwner)
            {
                coordinator = configuredCoordinator;
                world = spawnWorld;
                entityId = configuredEntityId;
                owner = configuredOwner;
                TargetId = entityId.ToString(CultureInfo.InvariantCulture);
            }

            public string TargetId { get; }

            public SkillTargetFaction Faction => SkillTargetFaction.Enemy;

            public SkillEnemyTier EnemyTier => SkillEnemyTier.Normal;

            public bool IsAlive => hitPoints > 0f;

            public bool IsInEffectRadius => true;

            public bool WasHitByLastStroke => false;

            public bool IsInsideGesture => true;

            public bool ApplyDamage(float amount, string sourceId, double timestamp)
            {
                if (!IsAlive || amount <= 0f)
                {
                    return false;
                }

                hitPoints -= amount;
                if (hitPoints <= 0f)
                {
                    Assert.That(coordinator.NotifyEnemyDefeated(entityId), Is.True);
                    world.Release(entityId);
                    owner.RecordDefeat();
                }

                return true;
            }

            public bool ApplyHealing(float amount, string sourceId, double timestamp)
            {
                return false;
            }

            public bool ApplyBuff(
                BuffConfig buff,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                return false;
            }

            public bool RemoveArmor(float amount, string sourceId, double timestamp)
            {
                return false;
            }

            public bool ApplyKnockback(
                float distanceRefPixels,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                return false;
            }

            public bool ExecuteBelowHpRatio(
                float threshold,
                string sourceId,
                double timestamp)
            {
                return false;
            }

            public bool IncrementCounter(
                float amount,
                float limit,
                string sourceId,
                double timestamp)
            {
                return false;
            }
        }
    }
}
