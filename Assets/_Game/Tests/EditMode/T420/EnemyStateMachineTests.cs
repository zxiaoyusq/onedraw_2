using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Skills;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T420
{
    [Category("T420")]
    public sealed class EnemyStateMachineTests
    {
        private readonly List<GameObject> roots = new List<GameObject>();
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T420");
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
        public void DefinitionMapsEnemyDefenseAndWeakpointWithoutInspectorValues()
        {
            EnemyDefinition turtle = EnemyDefinitionFactory.Create(
                config,
                ConfigIds.Enemies.EnemyStoneTurtle);

            Assert.That(turtle.EnemyId, Is.EqualTo("enemy_stone_turtle"));
            Assert.That(turtle.Tier, Is.EqualTo(EnemyTier.Normal));
            Assert.That(turtle.MaximumHp, Is.EqualTo(100));
            Assert.That(turtle.MovePatternId, Is.EqualTo("move_ground_left"));
            Assert.That(turtle.MoveSpeedReferencePixelsPerSecond, Is.EqualTo(55f));
            Assert.That(turtle.AttackSetId, Is.EqualTo("attackset_stone_turtle"));
            Assert.That(turtle.Defense.DefenseRuleId, Is.EqualTo("defense_turtle_shell"));
            Assert.That(turtle.Defense.MaximumArmor, Is.EqualTo(40));
            Assert.That(turtle.Defense.BreakEffectGroupId, Is.EqualTo("fx_break_armor"));
            Assert.That(turtle.Weakpoint.WeakpointRuleId,
                Is.EqualTo("weakpoint_turtle_belly"));
            Assert.That(turtle.Weakpoint.WindowStartSeconds,
                Is.EqualTo(0.55d).Within(0.000001d));
            Assert.That(turtle.Weakpoint.WindowEndSeconds,
                Is.EqualTo(1.2d).Within(0.000001d));
            Assert.That(turtle.Weakpoint.RadiusReferencePixels, Is.EqualTo(70f));
            Assert.That(turtle.Weakpoint.InterruptsAttack, Is.True);
            Assert.That(turtle.ContactDamage, Is.EqualTo(18));
            Assert.That(turtle.ScoreValue, Is.EqualTo(260));
            Assert.That(turtle.PoolPrewarm, Is.EqualTo(4));
        }

        [Test]
        public void AttackTimelineCrossesEveryStateAtConfiguredInclusiveBoundaries()
        {
            EnemyAttackTimeline attack = EnemyAttackTimelineFactory.Create(
                config,
                "attackset_talisman_bat",
                "atk_bat_dive");
            var machine = new EnemyStateMachine();
            var transitions = new List<EnemyStateTransition>();
            machine.Transitioned += transitions.Add;

            machine.Spawn(10d);
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Spawn));
            Assert.That(machine.CompleteSpawn(10d), Is.True);
            Assert.That(machine.BeginAttack(attack, 10d), Is.True);
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Windup));
            Assert.That(attack.WindupSeconds,
                Is.EqualTo(0.45d).Within(0.000001d));
            Assert.That(attack.ActiveSeconds,
                Is.EqualTo(0.55d).Within(0.000001d));
            Assert.That(attack.RecoverySeconds, Is.EqualTo(1.4d).Within(0.000001d));

            double attackBoundary = 10d + attack.WindupSeconds;
            double recoveryBoundary = attackBoundary + attack.ActiveSeconds;
            double moveBoundary = 10d + attack.CooldownSeconds;
            machine.Tick(attackBoundary - 0.000001d);
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Windup));
            machine.Tick(attackBoundary);
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Attack));
            machine.Tick(recoveryBoundary);
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Recovery));
            machine.Tick(moveBoundary - 0.000001d);
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Recovery));
            machine.Tick(moveBoundary);
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Move));

            Assert.That(
                StateNames(transitions),
                Is.EqualTo(new[]
                {
                    "Spawn", "Move", "Windup", "Attack", "Recovery", "Move",
                }));
            Assert.That(transitions[3].Timestamp, Is.EqualTo(attackBoundary));
            Assert.That(transitions[4].Timestamp, Is.EqualTo(recoveryBoundary));
            Assert.That(transitions[5].Timestamp, Is.EqualTo(moveBoundary));
        }

        [Test]
        public void InterruptRequiresConfiguredWindowAndAcceptsAnyOrdinaryGesture()
        {
            EnemyAttackTimeline attack = EnemyAttackTimelineFactory.Create(
                config,
                "attackset_talisman_bat",
                "atk_bat_dive");
            var machine = new EnemyStateMachine();
            machine.Spawn(0d);
            machine.CompleteSpawn(0d);
            machine.BeginAttack(attack, 0d);

            double boundaryTime = attack.InterruptStartSeconds;
            EnemyInterruptResult early = machine.TryInterrupt(
                "Diagonal",
                boundaryTime - 0.000001d);
            EnemyInterruptResult boundary = machine.TryInterrupt("Horizontal", boundaryTime);
            EnemyInterruptResult repeated = machine.TryInterrupt("Diagonal", boundaryTime);

            Assert.That(early.Status, Is.EqualTo(EnemyInterruptStatus.OutsideWindow));
            Assert.That(boundary.Status, Is.EqualTo(EnemyInterruptStatus.Interrupted));
            Assert.That(boundary.AttackId, Is.EqualTo("atk_bat_dive"));
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Stun));
            Assert.That(repeated.Status, Is.EqualTo(EnemyInterruptStatus.AlreadyStunned));
            Assert.That(machine.RecoverFromStun(boundaryTime + 0.1d), Is.True);
            Assert.That(machine.RecoverFromStun(boundaryTime + 0.1d), Is.False);
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Move));
        }

        [Test]
        public void DeathAndReleaseAreIdempotentAndReuseResetsTheClock()
        {
            var machine = new EnemyStateMachine();
            int diedTransitions = 0;
            int releaseTransitions = 0;
            machine.Transitioned += transition =>
            {
                if (transition.Reason == EnemyTransitionReason.Killed) diedTransitions++;
                if (transition.Reason == EnemyTransitionReason.Released) releaseTransitions++;
            };
            machine.Spawn(100d);
            machine.CompleteSpawn(100d);

            Assert.That(machine.TryKill(101d), Is.EqualTo(EnemyKillStatus.Killed));
            Assert.That(machine.TryKill(101d), Is.EqualTo(EnemyKillStatus.AlreadyDead));
            Assert.That(machine.Release(102d), Is.EqualTo(EnemyReleaseStatus.Released));
            Assert.That(machine.Release(0d), Is.EqualTo(EnemyReleaseStatus.AlreadyReleased));
            Assert.That(diedTransitions, Is.EqualTo(1));
            Assert.That(releaseTransitions, Is.EqualTo(1));

            machine.Spawn(0d);
            Assert.That(machine.Current.State, Is.EqualTo(EnemyState.Spawn));
            Assert.That(machine.Current.LastTimestamp, Is.Zero);
            Assert.That(machine.Current.TransitionSequence, Is.EqualTo(5));
        }

        [Test]
        public void DamageModelConsumesArmorThenHpAndResetsAcrossReuse()
        {
            EnemyDefinition turtle = EnemyDefinitionFactory.Create(
                config,
                ConfigIds.Enemies.EnemyStoneTurtle);
            var model = new EnemyDamageModel();
            model.Spawn(turtle, 7001);

            EnemyDamageResult first = model.ApplyDamage(30);
            EnemyDamageResult breakHit = model.ApplyDamage(15);
            EnemyDamageResult lethal = model.ApplyDamage(200);
            EnemyDamageResult repeated = model.ApplyDamage(1);
            EnemyHealingResult lateHeal = model.Heal(10);

            Assert.That(first.AppliedArmorDamage, Is.EqualTo(30));
            Assert.That(first.AppliedHpDamage, Is.Zero);
            Assert.That(first.State.CurrentArmor, Is.EqualTo(10));
            Assert.That(breakHit.ArmorBroken, Is.True);
            Assert.That(breakHit.AppliedArmorDamage, Is.EqualTo(10));
            Assert.That(breakHit.AppliedHpDamage, Is.EqualTo(5));
            Assert.That(breakHit.State.CurrentHp, Is.EqualTo(95));
            Assert.That(lethal.Status, Is.EqualTo(EnemyDamageStatus.Killed));
            Assert.That(lethal.DeathTriggered, Is.True);
            Assert.That(repeated.Status, Is.EqualTo(EnemyDamageStatus.AlreadyDead));
            Assert.That(repeated.DeathTriggered, Is.False);
            Assert.That(lateHeal.Status, Is.EqualTo(EnemyHealingStatus.AlreadyDead));
            Assert.That(model.Release(), Is.True);
            Assert.That(model.Release(), Is.False);

            EnemyDefinition fish = EnemyDefinitionFactory.Create(
                config,
                ConfigIds.Enemies.EnemyFireFish);
            model.Spawn(fish, 7002);
            Assert.That(model.Current.CurrentHp, Is.EqualTo(30));
            Assert.That(model.Current.CurrentArmor, Is.Zero);
            Assert.That(model.Current.HitTargetId, Is.EqualTo(7002));
        }

        [Test]
        public void ControllerAppliesResolvedWeakpointDamageAndPublishesOneDeathAndRelease()
        {
            EnemyController enemy = CreateEnemy(
                ConfigIds.Enemies.BossTombKing,
                8001,
                0d);
            var events = new List<EnemyCombatEvent>();
            enemy.CombatEventPublished += events.Add;
            Assert.That(enemy.CompleteSpawn(0d), Is.True);
            Assert.That(enemy.BeginAttack(ConfigIds.EnemyAttacks.AtkBossRockfall, 0d), Is.True);
            enemy.Tick(0.5d);

            DamageRuleSet rules = DamageRuleSetFactory.CreateForEnemy(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.Enemies.BossTombKing);
            var context = new DamageContext(
                1UL,
                8001,
                GestureType.Diagonal,
                ConfigIds.Stances.StanceBlade,
                isWeakpoint: true,
                comboCount: 1,
                timestamp: 0.5d);
            DamageResult damage = DamageCalculator.Calculate(
                context,
                rules,
                new FixedRandom(0.99d));

            EnemyHitResolution hit = enemy.ApplyStrokeDamage(
                damage,
                "Diagonal",
                0.5d,
                "stroke:1");

            Assert.That(damage.Damage, Is.EqualTo(72));
            Assert.That(hit.Damage.AppliedArmorDamage, Is.EqualTo(72));
            Assert.That(hit.Interrupt.Status, Is.EqualTo(EnemyInterruptStatus.Interrupted));
            Assert.That(enemy.State.State, Is.EqualTo(EnemyState.Stun));
            Assert.That(enemy.Damage.CurrentArmor, Is.EqualTo(48));

            Assert.That(enemy.RecoverFromStun(0.6d), Is.True);
            EnemyDamageResult killed = enemy.ApplyDamage(5000, "test_lethal", 1d);
            EnemyDamageResult repeated = enemy.ApplyDamage(1, "test_repeat", 1d);
            EnemyReleaseSnapshot released = enemy.Release(EnemyReleaseReason.Manual, 1d);
            EnemyReleaseSnapshot repeatedRelease = enemy.Release(EnemyReleaseReason.Manual, 1d);

            Assert.That(killed.DeathTriggered, Is.True);
            Assert.That(repeated.Status, Is.EqualTo(EnemyDamageStatus.AlreadyDead));
            Assert.That(CountEvents(events, EnemyCombatEventType.Interrupted), Is.EqualTo(1));
            Assert.That(CountEvents(events, EnemyCombatEventType.Died), Is.EqualTo(1));
            Assert.That(CountEvents(events, EnemyCombatEventType.Released), Is.EqualTo(1));
            Assert.That(released.IsValid, Is.True);
            Assert.That(released.StateBeforeRelease, Is.EqualTo(EnemyState.Dead));
            Assert.That(repeatedRelease.IsValid, Is.False);
        }

        [Test]
        public void SkillAdapterAppliesConfiguredBuffDamageArmorKnockbackAndCounter()
        {
            EnemyController enemy = CreateEnemy(
                ConfigIds.Enemies.BossTombKing,
                8101,
                0d);
            enemy.CompleteSpawn(0d);
            var events = new List<EnemyCombatEvent>();
            enemy.CombatEventPublished += events.Add;
            var target = new EnemySkillEffectTarget(enemy);
            target.SetSelectionFlags(true, true, true);
            BuffConfig vulnerable = config.GetBuff(ConfigIds.Buffs.BuffVulnerable);

            Assert.That(target.EnemyTier, Is.EqualTo(SkillEnemyTier.Boss));
            Assert.That(target.IsInEffectRadius, Is.True);
            Assert.That(target.WasHitByLastStroke, Is.True);
            Assert.That(target.IsInsideGesture, Is.True);
            Assert.That(target.ApplyBuff(vulnerable, 2f, "skill_ultimate_seal", 0.1d), Is.True);
            Assert.That(target.ApplyDamage(10f, "skill_test", 0.2d), Is.True);
            Assert.That(enemy.Damage.CurrentArmor, Is.EqualTo(107));
            Assert.That(target.RemoveArmor(7f, "fx_break", 0.3d), Is.True);
            Assert.That(enemy.Damage.CurrentArmor, Is.EqualTo(100));
            Assert.That(target.ApplyKnockback(80f, 0f, "fx_switch", 0.4d), Is.True);
            Assert.That(target.IncrementCounter(1f, 4f, "fx_break_boss_pin", 0.5d), Is.True);
            Assert.That(enemy.TryGetCounter("fx_break_boss_pin", out double counter), Is.True);
            Assert.That(counter, Is.EqualTo(1d));
            Assert.That(CountEvents(events, EnemyCombatEventType.BuffApplied), Is.EqualTo(1));
            Assert.That(CountEvents(events, EnemyCombatEventType.KnockbackRequested), Is.EqualTo(1));
            Assert.That(CountEvents(events, EnemyCombatEventType.CounterChanged), Is.EqualTo(1));
        }

        private EnemyController CreateEnemy(
            string enemyId,
            int hitTargetId,
            double timestamp)
        {
            var root = new GameObject("T420 Enemy");
            roots.Add(root);
            root.SetActive(false);
            root.AddComponent<Damageable>();
            EnemyController controller = root.AddComponent<EnemyController>();
            controller.enabled = false;
            var weakpointObject = new GameObject("Weakpoint");
            weakpointObject.transform.SetParent(root.transform, false);
            WeakpointController weakpoint = weakpointObject.AddComponent<WeakpointController>();
            controller.Spawn(config, enemyId, hitTargetId, timestamp, weakpoint);
            return controller;
        }

        private static string[] StateNames(IReadOnlyList<EnemyStateTransition> transitions)
        {
            var names = new string[transitions.Count];
            for (int index = 0; index < transitions.Count; index++)
            {
                names[index] = transitions[index].CurrentState.ToString();
            }

            return names;
        }

        private static int CountEvents(
            IReadOnlyList<EnemyCombatEvent> events,
            EnemyCombatEventType type)
        {
            int count = 0;
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].EventType == type) count++;
            }

            return count;
        }

        private sealed class FixedRandom : IRandomSource
        {
            private readonly double value;

            public FixedRandom(double configuredValue)
            {
                value = configuredValue;
            }

            public double NextUnitInterval()
            {
                return value;
            }
        }
    }
}
