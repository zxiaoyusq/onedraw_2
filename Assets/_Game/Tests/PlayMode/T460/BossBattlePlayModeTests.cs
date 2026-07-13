using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T460
{
    [Category("T460")]
    public sealed class BossBattlePlayModeTests
    {
        [UnityTest]
        public IEnumerator TombKingRunsThreeConfiguredProfilesEffectsAndAttacksOnce()
        {
            var config = new GameplayConfigService();
            config.Load(
                File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "_Game/Config/Generated/gameplay_config.json")),
                "test:T460-playmode");
            double startedAt = Time.timeAsDouble;
            EnemyController boss = CreateBoss(config, startedAt);
            var playerObject = new GameObject("T460 Player");
            PlayerCombatController player = playerObject.AddComponent<PlayerCombatController>();
            player.Initialize(config, ConfigIds.Players.PlayerMoyan);
            var target = new EnemySkillEffectTarget(boss);
            var world = new BossWorld(target);
            var skills = new SkillService(config, player);
            world.Bind(skills);
            var phaseEvents = new List<BossPhaseChangedEvent>();
            int controllerPhaseEvents = 0;
            boss.CombatEventPublished += combatEvent =>
            {
                if (combatEvent.EventType == EnemyCombatEventType.PhaseChanged)
                {
                    controllerPhaseEvents++;
                }
            };
            var phases = new BossPhaseController(
                config,
                boss,
                world,
                skills,
                world);
            phases.PhaseChanged += phaseEvents.Add;

            BossPhaseChangedEvent phase1 = phases.Start(startedAt);
            AssertPhase(
                phase1,
                ConfigIds.BossPhases.BossTombPhase1,
                ConfigIds.EnemyAttackSets.AttacksetBossPhase1,
                ConfigIds.MovePatterns.MoveBossGround,
                "defense_boss_pins",
                "weakpoint_none",
                expectedArmor: 120,
                expectedSpeed: 20d);
            Assert.That(boss.Weakpoint.IsWindowOpen, Is.False);
            ExecuteAttack(
                phases,
                new EnemyAttackTriggerContext(true, false, false, string.Empty),
                startedAt,
                startedAt + 0.8d,
                startedAt + 3.2d);

            EnemyDamageResult toPhase2 = boss.ApplyDamage(
                516L,
                "player_phase2_threshold",
                startedAt + 3.3d);
            Assert.That(toPhase2.State.CurrentHp, Is.EqualTo(804L));
            Assert.That(phaseEvents.Count, Is.EqualTo(2));
            AssertPhase(
                phaseEvents[1],
                ConfigIds.BossPhases.BossTombPhase2,
                ConfigIds.EnemyAttackSets.AttacksetBossPhase2,
                ConfigIds.MovePatterns.MoveBossPhase2,
                "defense_direction_seal",
                "weakpoint_boss_seal",
                expectedArmor: 60,
                expectedSpeed: 32d);
            Assert.That(phases.TryBeginAttack(
                new EnemyAttackTriggerContext(true, false, false, string.Empty),
                0d,
                startedAt + 3.3d), Is.True);
            phases.Tick(startedAt + 3.75d);
            Assert.That(boss.Weakpoint.IsWindowOpen, Is.True);
            phases.Tick(startedAt + 4.0501d);
            phases.Tick(startedAt + 6.1d);

            EnemyDamageResult toPhase3 = boss.ApplyDamage(
                456L,
                "player_phase3_threshold",
                startedAt + 6.2d);
            Assert.That(toPhase3.State.CurrentHp, Is.EqualTo(408L));
            Assert.That(phaseEvents.Count, Is.EqualTo(3));
            AssertPhase(
                phaseEvents[2],
                ConfigIds.BossPhases.BossTombPhase3,
                ConfigIds.EnemyAttackSets.AttacksetBossPhase3,
                ConfigIds.MovePatterns.MoveBossPhase3,
                "defense_none",
                "weakpoint_boss_seal",
                expectedArmor: 0,
                expectedSpeed: 48d);
            ExecuteAttack(
                phases,
                new EnemyAttackTriggerContext(false, false, true, string.Empty),
                startedAt + 6.2d,
                startedAt + 7.3d,
                startedAt + 10.2d);
            phases.ObserveCurrentHp(startedAt + 10.2d);

            Assert.That(phaseEvents.Count, Is.EqualTo(3));
            Assert.That(controllerPhaseEvents, Is.EqualTo(3));
            Assert.That(world.Actions.Count, Is.EqualTo(3));
            Assert.That(world.Actions[0].AttackId, Is.EqualTo(ConfigIds.EnemyAttacks.AtkBossRockfall));
            Assert.That(world.Actions[0].Kind, Is.EqualTo(EnemyAttackActionKind.Projectile));
            Assert.That(world.Actions[1].AttackId, Is.EqualTo(ConfigIds.EnemyAttacks.AtkBossSealWave));
            Assert.That(world.Actions[1].Kind, Is.EqualTo(EnemyAttackActionKind.Projectile));
            Assert.That(world.Actions[2].AttackId, Is.EqualTo(ConfigIds.EnemyAttacks.AtkBossCharge));
            Assert.That(world.Actions[2].Kind, Is.EqualTo(EnemyAttackActionKind.Charge));
            Assert.That(world.CountVfxSources("boss_tomb_phase_"), Is.EqualTo(3));
            Assert.That(world.CountVfxSources("atk_boss_"), Is.EqualTo(3));

            phases.Dispose();
            boss.Release(EnemyReleaseReason.Manual, startedAt + 10.2d);
            UnityEngine.Object.DestroyImmediate(boss.gameObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
            yield return null;
        }

        private static EnemyController CreateBoss(
            IConfigProvider config,
            double timestamp)
        {
            var root = new GameObject("T460 Tomb King");
            root.SetActive(false);
            root.AddComponent<Damageable>();
            EnemyController boss = root.AddComponent<EnemyController>();
            var weakpointObject = new GameObject("Configured Weakpoint");
            weakpointObject.transform.SetParent(root.transform, false);
            weakpointObject.AddComponent<CircleCollider2D>();
            WeakpointController weakpoint =
                weakpointObject.AddComponent<WeakpointController>();
            boss.Spawn(
                config,
                ConfigIds.Enemies.BossTombKing,
                46001,
                timestamp,
                weakpoint);
            Assert.That(boss.CompleteSpawn(timestamp), Is.True);
            return boss;
        }

        private static void ExecuteAttack(
            BossPhaseController phases,
            in EnemyAttackTriggerContext context,
            double beginAt,
            double executeAt,
            double cycleEndAt)
        {
            Assert.That(phases.TryBeginAttack(context, 0d, beginAt), Is.True);
            Assert.That(phases.Strategy.Telegraph.IsVisible, Is.True);
            phases.Tick(executeAt + 0.0001d);
            Assert.That(phases.Strategy.Telegraph.IsVisible, Is.False);
            phases.Tick(cycleEndAt);
            Assert.That(phases.Strategy.Telegraph.IsVisible, Is.False);
            Assert.That(phases.Strategy.ActiveAction.IsConfigured, Is.False);
        }

        private static void AssertPhase(
            in BossPhaseChangedEvent phaseEvent,
            string phaseId,
            string attackSetId,
            string movementPatternId,
            string defenseRuleId,
            string weakpointRuleId,
            long expectedArmor,
            double expectedSpeed)
        {
            Assert.That(phaseEvent.Transition.CurrentPhase.BossPhaseId, Is.EqualTo(phaseId));
            Assert.That(phaseEvent.Transition.CurrentPhase.CombatProfile.AttackSetId, Is.EqualTo(attackSetId));
            Assert.That(phaseEvent.Transition.CurrentPhase.Movement.MovePatternId, Is.EqualTo(movementPatternId));
            Assert.That(phaseEvent.Transition.CurrentPhase.CombatProfile.Defense.DefenseRuleId, Is.EqualTo(defenseRuleId));
            Assert.That(phaseEvent.Transition.CurrentPhase.CombatProfile.Weakpoint.WeakpointRuleId, Is.EqualTo(weakpointRuleId));
            Assert.That(phaseEvent.ProfileResult.CurrentArmor, Is.EqualTo(expectedArmor));
            Assert.That(
                phaseEvent.Transition.CurrentPhase.Movement.SpeedReferencePixelsPerSecond,
                Is.EqualTo(expectedSpeed).Within(0.00001d));
            Assert.That(phaseEvent.EntryEffectSteps.Count, Is.EqualTo(1));
            Assert.That(phaseEvent.EntryEffectSteps[0].EffectType, Is.EqualTo(SkillEffectTypes.PlayVfx));
        }

        private sealed class BossWorld : IEnemyAttackWorld, ISkillEffectWorld
        {
            private readonly EnemySkillEffectTarget bossTarget;
            private readonly IReadOnlyList<ISkillEffectTarget> targets;
            private readonly List<string> vfxSources = new List<string>();
            private SkillService skills;

            public BossWorld(EnemySkillEffectTarget configuredBossTarget)
            {
                bossTarget = configuredBossTarget;
                targets = new ISkillEffectTarget[] { bossTarget };
            }

            public List<EnemyAttackAction> Actions { get; } =
                new List<EnemyAttackAction>();

            public IReadOnlyList<ISkillEffectTarget> Targets => targets;

            public ISkillEffectTarget PrimaryTarget => bossTarget;

            public void Bind(SkillService configuredSkills)
            {
                skills = configuredSkills;
            }

            public void ExecuteAttack(in EnemyAttackAction action, double timestamp)
            {
                Actions.Add(action);
                skills.ExecuteEffectGroup(
                    action.EffectGroupId,
                    action.AttackId,
                    new SkillEffectContext(this, timestamp));
            }

            public int CountVfxSources(string prefix)
            {
                int count = 0;
                for (int index = 0; index < vfxSources.Count; index++)
                {
                    if (vfxSources[index].StartsWith(prefix, StringComparison.Ordinal))
                    {
                        count++;
                    }
                }

                return count;
            }

            public int RepeatLastStroke(float damageMultiplier, float delaySeconds, string sourceId, double timestamp) => 0;

            public int SetTimeScale(float scale, float durationSeconds, string sourceId, double timestamp) => 0;

            public int SetNextStrokeDamageMultiplier(float multiplier, string sourceId, double timestamp) => 0;

            public int ClearHostileProjectiles(string sourceId, double timestamp) => 0;

            public void PlayVfx(
                string vfxKey,
                IReadOnlyList<ISkillEffectTarget> selected,
                string sourceId,
                double timestamp)
            {
                vfxSources.Add(sourceId);
            }

            public void PlayAudio(string audioKey, string sourceId, double timestamp)
            {
            }
        }
    }
}
