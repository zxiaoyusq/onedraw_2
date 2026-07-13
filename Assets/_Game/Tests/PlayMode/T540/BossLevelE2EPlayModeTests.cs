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

namespace OneStrokeDemon.Tests.PlayMode.T540
{
    [Category("T540")]
    public sealed class BossLevelE2EPlayModeTests
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
        public IEnumerator MixedGateThreePhasesAndExecutionSettleVictoryWithinLimit()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            root = new GameObject("T540 Boss Level Runtime");
            PlayerCombatController player = CreatePlayer(config, "Victory Player");

            using var pool = new EnemyArchetypePool(
                config,
                AssetRegistryRuntime.Current,
                root.transform);
            var world = new BossLevelWorld(config, pool, root.transform);
            using var coordinator = new BossLevelCoordinator(
                config,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv003Boss,
                player,
                world);
            var phaseEvents = new List<BossPhaseChangedEvent>();
            coordinator.BossPhaseChanged += phaseEvents.Add;

            EnemyController boss = ReachBoss(coordinator, world);
            Assert.That(coordinator.NotifyEnemyDefeated(coordinator.BossEntityId),
                Is.False,
                "A live Boss must not satisfy BossDefeated.");
            ExecuteConfiguredBossFlow(coordinator, boss, phaseEvents);

            Assert.That(coordinator.NotifyEnemyDefeated(coordinator.BossEntityId), Is.True);
            BattleFlowAdvanceReport victory = coordinator.Advance(0d);

            Assert.That(victory.State, Is.EqualTo(BattleFlowState.Victory));
            Assert.That(coordinator.Battle.Level.State,
                Is.EqualTo(LevelRunnerState.Completed));
            Assert.That(coordinator.Battle.Level.ElapsedSeconds,
                Is.LessThanOrEqualTo(
                    coordinator.Battle.Level.Definition.DurationLimitSeconds));
            Assert.That(coordinator.BossPhases, Is.Null);
            Assert.That(world.TotalSpawned, Is.EqualTo(12));
            Assert.That(world.Actions.Count, Is.EqualTo(3));
            Assert.That(world.Actions[0].AttackId,
                Is.EqualTo(ConfigIds.EnemyAttacks.AtkBossRockfall));
            Assert.That(world.Actions[1].AttackId,
                Is.EqualTo(ConfigIds.EnemyAttacks.AtkBossSealWave));
            Assert.That(world.Actions[2].AttackId,
                Is.EqualTo(ConfigIds.EnemyAttacks.AtkBossCharge));
            Assert.That(world.PhaseEntryVfxCount, Is.EqualTo(3));

            world.ReleaseAll();
            Assert.That(world.ActiveCount, Is.Zero);
            pool.AssertNoLeaks();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DefeatStopsBossRuntimeAndFreshRetryCanWinWithoutLeaks()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            root = new GameObject("T540 Boss Retry Runtime");

            using var pool = new EnemyArchetypePool(
                config,
                AssetRegistryRuntime.Current,
                root.transform);
            PlayerCombatController firstPlayer = CreatePlayer(config, "Defeated Player");
            var firstWorld = new BossLevelWorld(config, pool, root.transform);
            var first = new BossLevelCoordinator(
                config,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv003Boss,
                firstPlayer,
                firstWorld);
            int stalePhaseEvents = 0;
            first.BossPhaseChanged += _ => stalePhaseEvents++;
            EnemyController firstBoss = ReachBoss(first, firstWorld);
            Assert.That(stalePhaseEvents, Is.EqualTo(1));

            PlayerDamageResult death = firstPlayer.ApplyDamage(
                firstPlayer.Current.CurrentHp,
                1d,
                "T540_forced_defeat");
            Assert.That(death.DeathTriggered, Is.True);
            Assert.That(first.Advance(0d).State, Is.EqualTo(BattleFlowState.Defeat));
            Assert.That(first.BossPhases, Is.Null);
            Assert.That(first.HasActiveBoss, Is.False);

            EnemyDamageSnapshot before = firstBoss.Damage;
            firstBoss.ApplyDamage(
                before.CurrentArmor + 500L,
                "T540_post_defeat_damage",
                firstBoss.State.LastTimestamp + 1d);
            Assert.That(stalePhaseEvents, Is.EqualTo(1),
                "Settled runs must not retain Boss phase subscriptions.");
            first.Dispose();
            firstWorld.ReleaseAll();
            pool.AssertNoLeaks();

            PlayerCombatController retryPlayer = CreatePlayer(config, "Retry Player");
            var retryWorld = new BossLevelWorld(config, pool, root.transform);
            using var retry = new BossLevelCoordinator(
                config,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv003Boss,
                retryPlayer,
                retryWorld);
            var retryPhases = new List<BossPhaseChangedEvent>();
            retry.BossPhaseChanged += retryPhases.Add;
            EnemyController retryBoss = ReachBoss(retry, retryWorld);
            ExecuteConfiguredBossFlow(retry, retryBoss, retryPhases);
            Assert.That(retry.NotifyEnemyDefeated(retry.BossEntityId), Is.True);
            Assert.That(retry.Advance(0d).State, Is.EqualTo(BattleFlowState.Victory));
            Assert.That(retryPhases.Count, Is.EqualTo(3));

            retryWorld.ReleaseAll();
            Assert.That(retryWorld.ActiveCount, Is.Zero);
            pool.AssertNoLeaks();
            yield return null;
        }

        private PlayerCombatController CreatePlayer(
            IConfigProvider config,
            string objectName)
        {
            var playerObject = new GameObject(objectName);
            playerObject.transform.SetParent(root.transform, false);
            PlayerCombatController player =
                playerObject.AddComponent<PlayerCombatController>();
            player.Initialize(config, ConfigIds.Players.PlayerMoyan);
            return player;
        }

        private static EnemyController ReachBoss(
            BossLevelCoordinator coordinator,
            BossLevelWorld world)
        {
            coordinator.Advance(
                coordinator.Battle.Flow.Settings.CountdownDurationSeconds);
            LevelDefinition level = coordinator.Battle.Level.Definition;
            WaveDefinition intro = level.Waves[0];
            coordinator.Advance(GetLastSpawnAt(intro));

            Assert.That(world.ActiveCount, Is.EqualTo(11));
            Assert.That(world.ActiveBossCount, Is.Zero);
            Assert.That(world.SpawnedEnemyIds, Is.EquivalentTo(new[]
            {
                ConfigIds.Enemies.EnemyFireFish,
                ConfigIds.Enemies.EnemyTalismanBat,
                ConfigIds.Enemies.EnemyStoneTurtle,
                ConfigIds.Enemies.EnemySkeletonGhost,
                ConfigIds.Enemies.EnemySoulPuppet,
            }));

            long[] introIds = world.ActiveEntityIds;
            for (int index = 0; index < introIds.Length; index++)
            {
                ActiveEnemy active = world.Get(introIds[index]);
                Assert.That(active.Request.IsBoss, Is.False);
                EnemyDamageSnapshot damage = active.Controller.Damage;
                EnemyDamageResult defeated = active.Controller.ApplyDamage(
                    checked(damage.CurrentHp + damage.CurrentArmor),
                    "T540_intro_clear",
                    active.Controller.State.LastTimestamp + 1d);
                Assert.That(defeated.DeathTriggered, Is.True, active.Request.SpawnId);
                Assert.That(coordinator.NotifyEnemyDefeated(introIds[index]), Is.True);
                world.Release(introIds[index]);
            }

            coordinator.Advance(0d);
            coordinator.Advance(intro.EndDelaySeconds);
            BattleFlowAdvanceReport bossSpawned = coordinator.Advance(
                level.Waves[1].StartDelaySeconds);
            Assert.That(bossSpawned.State, Is.EqualTo(BattleFlowState.Playing));
            Assert.That(world.ActiveCount, Is.EqualTo(1));
            Assert.That(world.ActiveBossCount, Is.EqualTo(1));
            Assert.That(coordinator.BossPhases, Is.Not.Null);
            Assert.That(coordinator.BossPhases.IsStarted, Is.True);
            Assert.That(coordinator.BossPhases.CurrentPhase.BossPhaseId,
                Is.EqualTo(ConfigIds.BossPhases.BossTombPhase1));
            return world.Get(coordinator.BossEntityId).Controller;
        }

        private static void ExecuteConfiguredBossFlow(
            BossLevelCoordinator coordinator,
            EnemyController boss,
            IList<BossPhaseChangedEvent> phaseEvents)
        {
            Assert.That(phaseEvents.Count, Is.EqualTo(1));
            double timestamp = CompleteCurrentAttack(coordinator.BossPhases, boss);
            MoveToNextPhase(boss, coordinator.BossPhases.CurrentPhase.ExitHpRatio, timestamp);

            Assert.That(phaseEvents.Count, Is.EqualTo(2));
            Assert.That(coordinator.BossPhases.CurrentPhase.BossPhaseId,
                Is.EqualTo(ConfigIds.BossPhases.BossTombPhase2));
            timestamp = CompleteCurrentAttack(coordinator.BossPhases, boss);
            MoveToNextPhase(boss, coordinator.BossPhases.CurrentPhase.ExitHpRatio, timestamp);

            Assert.That(phaseEvents.Count, Is.EqualTo(3));
            Assert.That(coordinator.BossPhases.CurrentPhase.BossPhaseId,
                Is.EqualTo(ConfigIds.BossPhases.BossTombPhase3));
            Assert.That(phaseEvents[2].Transition.CurrentPhase.DescriptionZhCN,
                Does.Contain("处决"));
            timestamp = CompleteCurrentAttack(coordinator.BossPhases, boss);

            EnemyDamageSnapshot damage = boss.Damage;
            EnemyDamageResult executed = boss.ApplyDamage(
                checked(damage.CurrentHp + damage.CurrentArmor),
                "T540_boss_execution",
                timestamp + 1d);
            Assert.That(executed.DeathTriggered, Is.True);
            Assert.That(coordinator.BossPhases.HasEnded, Is.True);
        }

        private static double CompleteCurrentAttack(
            BossPhaseController phases,
            EnemyController boss)
        {
            EnemyAttackDefinition attack = phases.Strategy.Attacks[0];
            double startedAt = boss.State.LastTimestamp + 0.1d;
            Assert.That(phases.TryBeginAttack(
                    new EnemyAttackTriggerContext(
                        cooldownReady: true,
                        targetInDistance: true,
                        hpThresholdReached: true,
                        supportTargetId: ""),
                    unitSelection: 0d,
                    startedAt),
                Is.True,
                attack.AttackId);
            double completedAt = startedAt + attack.Timeline.CooldownSeconds;
            Assert.That(phases.Tick(completedAt), Is.GreaterThanOrEqualTo(3));
            Assert.That(boss.State.State, Is.EqualTo(EnemyState.Move));
            return completedAt;
        }

        private static void MoveToNextPhase(
            EnemyController boss,
            double exitHpRatio,
            double timestamp)
        {
            EnemyDamageSnapshot damage = boss.Damage;
            long targetHp = Math.Max(
                1L,
                (long)Math.Floor(damage.MaximumHp * exitHpRatio));
            long requested = checked(
                damage.CurrentArmor + damage.CurrentHp - targetHp);
            EnemyDamageResult result = boss.ApplyDamage(
                requested,
                "T540_phase_threshold",
                timestamp + 0.1d);
            Assert.That(result.State.CurrentHp, Is.EqualTo(targetHp));
        }

        private static double GetLastSpawnAt(WaveDefinition wave)
        {
            double last = 0d;
            for (int index = 0; index < wave.Spawns.Count; index++)
            {
                SpawnDefinition spawn = wave.Spawns[index];
                last = Math.Max(
                    last,
                    spawn.SpawnTimeSeconds +
                    ((spawn.Count - 1) * spawn.IntervalSeconds));
            }

            return last;
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
            Assert.That(AssetRegistryRuntime.IsReady, Is.True);
        }

        private sealed class ActiveEnemy
        {
            public ActiveEnemy(
                in LevelSpawnRequest request,
                EnemyController controller,
                EnemySkillEffectTarget target,
                bool isPooled,
                in EnemyArchetypeSpawnResult pooledSpawn)
            {
                Request = request;
                Controller = controller;
                Target = target;
                IsPooled = isPooled;
                PooledSpawn = pooledSpawn;
            }

            public LevelSpawnRequest Request { get; }

            public EnemyController Controller { get; }

            public EnemySkillEffectTarget Target { get; }

            public bool IsPooled { get; }

            public EnemyArchetypeSpawnResult PooledSpawn { get; }
        }

        private sealed class BossLevelWorld : IBossLevelWorld
        {
            private readonly IConfigProvider config;
            private readonly EnemyArchetypePool pool;
            private readonly Transform rootTransform;
            private readonly Dictionary<long, ActiveEnemy> active =
                new Dictionary<long, ActiveEnemy>();
            private readonly HashSet<string> spawnedEnemyIds =
                new HashSet<string>(StringComparer.Ordinal);
            private readonly List<EnemyAttackAction> actions =
                new List<EnemyAttackAction>();
            private readonly List<string> vfxSources = new List<string>();
            private long nextEntityId = 1L;

            public BossLevelWorld(
                IConfigProvider configuredProvider,
                EnemyArchetypePool configuredPool,
                Transform configuredRoot)
            {
                config = configuredProvider;
                pool = configuredPool;
                rootTransform = configuredRoot;
            }

            public int ActiveCount => active.Count;

            public int TotalSpawned { get; private set; }

            public int ActiveBossCount
            {
                get
                {
                    int count = 0;
                    foreach (ActiveEnemy item in active.Values)
                    {
                        if (item.Request.IsBoss)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }

            public int PhaseEntryVfxCount
            {
                get
                {
                    int count = 0;
                    for (int index = 0; index < vfxSources.Count; index++)
                    {
                        if (vfxSources[index].StartsWith(
                                "boss_tomb_phase_",
                                StringComparison.Ordinal))
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }

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

            public IReadOnlyList<ISkillEffectTarget> Targets
            {
                get
                {
                    var targets = new List<ISkillEffectTarget>(active.Count);
                    foreach (ActiveEnemy item in active.Values)
                    {
                        targets.Add(item.Target);
                    }

                    return targets.AsReadOnly();
                }
            }

            public ISkillEffectTarget PrimaryTarget
            {
                get
                {
                    foreach (ActiveEnemy item in active.Values)
                    {
                        if (item.Request.IsBoss)
                        {
                            return item.Target;
                        }
                    }

                    return null;
                }
            }

            public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
            {
                long candidate = nextEntityId++;
                double timestamp = candidate * 10d;
                ActiveEnemy item;
                if (request.IsBoss)
                {
                    EnemyController controller = CreateBoss(
                        config,
                        request.EnemyId,
                        checked((int)candidate),
                        timestamp,
                        rootTransform);
                    item = new ActiveEnemy(
                        request,
                        controller,
                        new EnemySkillEffectTarget(controller),
                        false,
                        default);
                }
                else
                {
                    EnemyArchetypeSpawnResult spawned = pool.Spawn(
                        request.EnemyId,
                        checked((int)candidate),
                        timestamp,
                        this);
                    if (!spawned.IsSpawned)
                    {
                        entityId = 0L;
                        return false;
                    }

                    item = new ActiveEnemy(
                        request,
                        spawned.Actor.Controller,
                        new EnemySkillEffectTarget(spawned.Actor.Controller),
                        true,
                        spawned);
                }

                entityId = candidate;
                active.Add(entityId, item);
                spawnedEnemyIds.Add(request.EnemyId);
                TotalSpawned++;
                return true;
            }

            public bool TryGetEnemyController(
                long entityId,
                out EnemyController controller)
            {
                if (active.TryGetValue(entityId, out ActiveEnemy item))
                {
                    controller = item.Controller;
                    return true;
                }

                controller = null;
                return false;
            }

            public void ExecuteAttack(in EnemyAttackAction action, double timestamp)
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
                if (item.IsPooled)
                {
                    PoolReleaseResult released = pool.Release(item.PooledSpawn);
                    Assert.That(released.WasReleased, Is.True);
                }
                else
                {
                    double timestamp = item.Controller.State.LastTimestamp + 0.1d;
                    item.Controller.Release(EnemyReleaseReason.Manual, timestamp);
                    UnityEngine.Object.DestroyImmediate(item.Controller.gameObject);
                }

                active.Remove(entityId);
            }

            public void ReleaseAll()
            {
                long[] ids = ActiveEntityIds;
                for (int index = 0; index < ids.Length; index++)
                {
                    Release(ids[index]);
                }
            }

            public int RepeatLastStroke(
                float damageMultiplier,
                float delaySeconds,
                string sourceId,
                double timestamp) => 0;

            public int SetTimeScale(
                float scale,
                float durationSeconds,
                string sourceId,
                double timestamp) => 0;

            public int SetNextStrokeDamageMultiplier(
                float multiplier,
                string sourceId,
                double timestamp) => 0;

            public int ClearHostileProjectiles(string sourceId, double timestamp) => 0;

            public void PlayVfx(
                string vfxKey,
                IReadOnlyList<ISkillEffectTarget> targets,
                string sourceId,
                double timestamp)
            {
                vfxSources.Add(sourceId);
            }

            public void PlayAudio(string audioKey, string sourceId, double timestamp)
            {
            }

            private static EnemyController CreateBoss(
                IConfigProvider config,
                string enemyId,
                int hitTargetId,
                double timestamp,
                Transform parent)
            {
                var bossObject = new GameObject("T540 Configured Boss");
                bossObject.transform.SetParent(parent, false);
                bossObject.SetActive(false);
                bossObject.AddComponent<Damageable>();
                EnemyController boss = bossObject.AddComponent<EnemyController>();
                var weakpointObject = new GameObject("T540 Boss Weakpoint");
                weakpointObject.transform.SetParent(bossObject.transform, false);
                weakpointObject.AddComponent<CircleCollider2D>();
                WeakpointController weakpoint =
                    weakpointObject.AddComponent<WeakpointController>();
                boss.Spawn(config, enemyId, hitTargetId, timestamp, weakpoint);
                Assert.That(boss.CompleteSpawn(timestamp), Is.True);
                return boss;
            }
        }
    }
}
