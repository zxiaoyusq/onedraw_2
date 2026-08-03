using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using OneStrokeDemon.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T530
{
    [Category("T530")]
    public sealed class NormalLevelE2EPlayModeTests
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
        public IEnumerator EightConfiguredWavesExerciseMixedRosterAndSettleWithinLimit()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            root = new GameObject("T530 Normal Level Runtime");
            var playerObject = new GameObject("T530 Player");
            playerObject.transform.SetParent(root.transform, false);
            PlayerCombatController player = playerObject.AddComponent<PlayerCombatController>();
            player.Initialize(config, ConfigIds.Players.PlayerMoyan);

            using var pool = new EnemyArchetypePool(
                config,
                AssetRegistryRuntime.Current,
                root.transform);
            var world = new PooledNormalLevelWorld(pool);
            var coordinator = new BattleFlowCoordinator(
                config,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv002Cave,
                world);
            LevelDefinition level = coordinator.Level.Definition;
            var attackedEnemyIds = new HashSet<string>(StringComparer.Ordinal);
            int waveStartedEvents = 0;
            int waveCompletedEvents = 0;
            int levelCompletedEvents = 0;

            BattleFlowAdvanceReport countdown = coordinator.Advance(
                coordinator.Flow.Settings.CountdownDurationSeconds);
            CountEvents(
                countdown.Level.Events,
                ref waveStartedEvents,
                ref waveCompletedEvents,
                ref levelCompletedEvents);
            Assert.That(coordinator.Flow.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(level.Waves.Count, Is.EqualTo(8));

            for (int waveIndex = 0; waveIndex < level.Waves.Count; waveIndex++)
            {
                WaveDefinition wave = level.Waves[waveIndex];
                Assert.That(coordinator.Level.CurrentWave.Definition.WaveId,
                    Is.EqualTo(wave.WaveId));

                double lastSpawnAt = GetLastSpawnAt(wave);
                BattleFlowAdvanceReport spawned = coordinator.Advance(
                    wave.StartDelaySeconds + lastSpawnAt);
                CountEvents(
                    spawned.Level.Events,
                    ref waveStartedEvents,
                    ref waveCompletedEvents,
                    ref levelCompletedEvents);

                Assert.That(world.ActiveCount, Is.EqualTo(CountOccurrences(wave)));
                Assert.That(world.ActiveCount, Is.LessThanOrEqualTo(wave.MaxAlive));
                Assert.That(world.AllActiveRequestsAreValidAndNormalized(), Is.True);
                ExerciseFirstAttackForEachArchetype(world, attackedEnemyIds);
                ExerciseConfiguredPlayerMechanics(wave.Order, config, player, coordinator);
                DefeatAndReleaseAll(coordinator, world);

                CountEvents(
                    coordinator.Advance(0d).Level.Events,
                    ref waveStartedEvents,
                    ref waveCompletedEvents,
                    ref levelCompletedEvents);
                BattleFlowAdvanceReport completed = coordinator.Advance(wave.EndDelaySeconds);
                CountEvents(
                    completed.Level.Events,
                    ref waveStartedEvents,
                    ref waveCompletedEvents,
                    ref levelCompletedEvents);

                if (waveIndex + 1 < level.Waves.Count)
                {
                    Assert.That(completed.State, Is.EqualTo(BattleFlowState.Playing));
                    Assert.That(coordinator.Level.CurrentWaveIndex, Is.EqualTo(waveIndex + 1));
                }
                else
                {
                    Assert.That(completed.State, Is.EqualTo(BattleFlowState.Victory));
                }
            }

            Assert.That(coordinator.Level.State, Is.EqualTo(LevelRunnerState.Completed));
            Assert.That(coordinator.Flow.State, Is.EqualTo(BattleFlowState.Victory));
            Assert.That(world.TotalSpawned, Is.EqualTo(45));
            Assert.That(world.SpawnedEnemyIds,
                Is.EquivalentTo(new[]
                {
                    ConfigIds.Enemies.EnemyFireFish,
                    ConfigIds.Enemies.EnemyWheelZombie,
                    ConfigIds.Enemies.EnemyTalismanBat,
                    ConfigIds.Enemies.EnemyStoneTurtle,
                    ConfigIds.Enemies.EnemySkeletonGhost,
                    ConfigIds.Enemies.EnemySoulPuppet,
                }));
            Assert.That(world.CountEliteRequests(), Is.EqualTo(3));
            Assert.That(attackedEnemyIds, Is.EquivalentTo(world.SpawnedEnemyIds));
            Assert.That(world.Actions.Count, Is.EqualTo(6));
            Assert.That(world.CountActions(EnemyAttackActionKind.Projectile), Is.EqualTo(2));
            Assert.That(world.CountActions(EnemyAttackActionKind.Charge), Is.EqualTo(1));
            Assert.That(world.CountActions(EnemyAttackActionKind.Melee), Is.EqualTo(2));
            Assert.That(world.CountActions(EnemyAttackActionKind.Support), Is.EqualTo(1));
            Assert.That(waveStartedEvents, Is.EqualTo(8));
            Assert.That(waveCompletedEvents, Is.EqualTo(8));
            Assert.That(levelCompletedEvents, Is.EqualTo(1));
            Assert.That(coordinator.Level.ElapsedSeconds,
                Is.LessThanOrEqualTo(level.DurationLimitSeconds));
            Assert.That(player.Current.StanceId,
                Is.EqualTo(ConfigIds.Stances.StanceTalisman));
            Assert.That(world.ActiveCount, Is.Zero);
            pool.AssertNoLeaks();
            Assert.That(pool.Snapshot.ActiveCount, Is.Zero);
            yield return null;
        }

        private static void ExerciseFirstAttackForEachArchetype(
            PooledNormalLevelWorld world,
            ISet<string> attackedEnemyIds)
        {
            long[] ids = world.ActiveEntityIds;
            for (int index = 0; index < ids.Length; index++)
            {
                ActiveEnemy active = world.Get(ids[index]);
                if (!attackedEnemyIds.Add(active.Request.EnemyId))
                {
                    continue;
                }

                var trigger = new EnemyAttackTriggerContext(
                    cooldownReady: true,
                    targetInDistance: true,
                    hpThresholdReached: true,
                    supportTargetId: "ally_" + ids[index].ToString(CultureInfo.InvariantCulture));
                Assert.That(
                    active.Spawned.Actor.TryBeginAttack(
                        trigger,
                        unitSelection: 0d,
                        active.SpawnTimestamp),
                    Is.True,
                    active.Request.EnemyId);
                EnemyAttackTelegraphSnapshot telegraph =
                    active.Spawned.Actor.Strategy.Telegraph;
                Assert.That(telegraph.IsVisible, Is.True);
                Assert.That(active.Spawned.Actor.Tick(telegraph.ExpectedExecuteAt),
                    Is.GreaterThanOrEqualTo(1));
                Assert.That(active.Spawned.Actor.Controller.State.State,
                    Is.EqualTo(EnemyState.Attack));
            }
        }

        private static void ExerciseConfiguredPlayerMechanics(
            int waveOrder,
            IConfigProvider config,
            PlayerCombatController player,
            BattleFlowCoordinator coordinator)
        {
            var enemyOwner = new ProjectileOwner(ProjectileFaction.Enemy, 53001);
            var playerOwner = new ProjectileOwner(ProjectileFaction.Player, 101);
            if (waveOrder == 1)
            {
                ProjectileStrokeResolution reflected = ProjectileCutResolver.Resolve(
                    ProjectileRuleSetFactory.Create(
                        config,
                        ConfigIds.Projectiles.ProjGhostFire),
                    ProjectileOwnership.FromInitialOwner(enemyOwner),
                    player.Current.StanceId,
                    playerOwner);
                Assert.That(reflected.Outcome, Is.EqualTo(ProjectileStrokeOutcome.Reflected));
            }
            else if (waveOrder == 3)
            {
                EnemyDefenseEvaluation shell = new DefenseRuleService(config).Evaluate(
                    ConfigIds.DefenseRules.DefenseTurtleShell,
                    "Charged",
                    player.Current.StanceId);
                Assert.That(shell.Matches, Is.True);
                Assert.That(shell.ConfiguredDamageMultiplier, Is.GreaterThan(0d));
            }
            else if (waveOrder == 4)
            {
                ProjectileRuleSet soulShard = ProjectileRuleSetFactory.Create(
                    config,
                    ConfigIds.Projectiles.ProjSoulShard);
                ProjectileOwnership ownership = ProjectileOwnership.FromInitialOwner(enemyOwner);
                Assert.That(
                    ProjectileCutResolver.Resolve(
                        soulShard,
                        ownership,
                        player.Current.StanceId,
                        playerOwner).Outcome,
                    Is.EqualTo(ProjectileStrokeOutcome.RequiredStanceMismatch));

                StanceSwitchResult switched = player.TrySwitchStance(
                    ConfigIds.Stances.StanceTalisman,
                    coordinator.Flow.Time.Current.GameplayElapsedSeconds);
                Assert.That(switched.Status, Is.EqualTo(StanceSwitchStatus.Switched));
                Assert.That(
                    ProjectileCutResolver.Resolve(
                        soulShard,
                        ownership,
                        player.Current.StanceId,
                        playerOwner).Outcome,
                    Is.EqualTo(ProjectileStrokeOutcome.Cut));
            }
        }

        private static void DefeatAndReleaseAll(
            BattleFlowCoordinator coordinator,
            PooledNormalLevelWorld world)
        {
            long[] ids = world.ActiveEntityIds;
            for (int index = 0; index < ids.Length; index++)
            {
                ActiveEnemy active = world.Get(ids[index]);
                EnemyDamageSnapshot damage = active.Spawned.Actor.Controller.Damage;
                double timestamp = active.Spawned.Actor.Controller.State.LastTimestamp + 10d;
                EnemyDamageResult defeated = active.Spawned.Actor.Controller.ApplyDamage(
                    checked(damage.CurrentHp + damage.CurrentArmor),
                    "T530_player_stroke",
                    timestamp);
                Assert.That(defeated.DeathTriggered, Is.True, active.Request.SpawnId);
                Assert.That(coordinator.NotifyEnemyDefeated(ids[index]), Is.True);
                world.Release(ids[index]);
            }
        }

        private static double GetLastSpawnAt(WaveDefinition wave)
        {
            double last = 0d;
            for (int index = 0; index < wave.Spawns.Count; index++)
            {
                SpawnDefinition spawn = wave.Spawns[index];
                last = Math.Max(
                    last,
                    spawn.SpawnTimeSeconds + ((spawn.Count - 1) * spawn.IntervalSeconds));
            }

            return last;
        }

        private static int CountOccurrences(WaveDefinition wave)
        {
            int count = 0;
            for (int index = 0; index < wave.Spawns.Count; index++)
            {
                count += wave.Spawns[index].Count;
            }

            return count;
        }

        private static void CountEvents(
            IReadOnlyList<LevelRuntimeEvent> events,
            ref int waveStarted,
            ref int waveCompleted,
            ref int levelCompleted)
        {
            for (int index = 0; index < events.Count; index++)
            {
                switch (events[index].Kind)
                {
                    case LevelRuntimeEventKind.WaveStarted:
                        waveStarted++;
                        break;
                    case LevelRuntimeEventKind.WaveCompleted:
                        waveCompleted++;
                        break;
                    case LevelRuntimeEventKind.LevelCompleted:
                        levelCompleted++;
                        break;
                }
            }
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
            Assert.That(AssetRegistryRuntime.IsReady, Is.True);
        }

        private sealed class ActiveEnemy
        {
            public ActiveEnemy(
                in LevelSpawnRequest request,
                in EnemyArchetypeSpawnResult spawned,
                double spawnTimestamp)
            {
                Request = request;
                Spawned = spawned;
                SpawnTimestamp = spawnTimestamp;
            }

            public LevelSpawnRequest Request { get; }

            public EnemyArchetypeSpawnResult Spawned { get; }

            public double SpawnTimestamp { get; }
        }

        private sealed class PooledNormalLevelWorld : ILevelSpawnWorld, IEnemyAttackWorld
        {
            private readonly EnemyArchetypePool pool;
            private readonly Dictionary<long, ActiveEnemy> active =
                new Dictionary<long, ActiveEnemy>();
            private readonly List<LevelSpawnRequest> spawnLog =
                new List<LevelSpawnRequest>();
            private readonly List<EnemyAttackAction> actions =
                new List<EnemyAttackAction>();
            private readonly HashSet<string> spawnedEnemyIds =
                new HashSet<string>(StringComparer.Ordinal);
            private long nextEntityId = 1L;

            public PooledNormalLevelWorld(EnemyArchetypePool configuredPool)
            {
                pool = configuredPool;
            }

            public int ActiveCount => active.Count;

            public int TotalSpawned => spawnLog.Count;

            public IReadOnlyList<EnemyAttackAction> Actions => actions;

            public IReadOnlyCollection<string> SpawnedEnemyIds => spawnedEnemyIds;

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
                long candidateId = nextEntityId++;
                double timestamp = candidateId * 10d;
                EnemyArchetypeSpawnResult spawned = pool.Spawn(
                    request.EnemyId,
                    checked((int)candidateId),
                    timestamp,
                    this);
                if (!spawned.IsSpawned)
                {
                    entityId = 0L;
                    return false;
                }

                spawned.Actor.transform.localPosition = new Vector3(
                    (float)request.Position.X,
                    (float)request.Position.Y,
                    0f);
                entityId = candidateId;
                active.Add(entityId, new ActiveEnemy(request, spawned, timestamp));
                spawnLog.Add(request);
                spawnedEnemyIds.Add(request.EnemyId);
                return true;
            }

            public void ExecuteAttack(
                EnemyController source,
                in EnemyAttackAction action,
                double timestamp)
            {
                actions.Add(action);
            }

            public ActiveEnemy Get(long entityId)
            {
                return active[entityId];
            }

            public void Release(long entityId)
            {
                ActiveEnemy item = active[entityId];
                PoolReleaseResult released = pool.Release(item.Spawned);
                Assert.That(released.WasReleased, Is.True);
                active.Remove(entityId);
            }

            public bool AllActiveRequestsAreValidAndNormalized()
            {
                foreach (ActiveEnemy item in active.Values)
                {
                    if (!item.Request.IsValid || !item.Request.Position.IsNormalized)
                    {
                        return false;
                    }
                }

                return true;
            }

            public int CountEliteRequests()
            {
                int count = 0;
                for (int index = 0; index < spawnLog.Count; index++)
                {
                    if (string.Equals(
                            spawnLog[index].Modifier.ModifierId,
                            ConfigIds.EnemyModifiers.ModifierElite,
                            StringComparison.Ordinal))
                    {
                        count++;
                    }
                }

                return count;
            }

            public int CountActions(EnemyAttackActionKind kind)
            {
                int count = 0;
                for (int index = 0; index < actions.Count; index++)
                {
                    if (actions[index].Kind == kind)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
