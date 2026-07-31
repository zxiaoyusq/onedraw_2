using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T430
{
    [Category("T430")]
    public sealed class AttackTelegraphPlayModeTests
    {
        [UnityTest]
        public IEnumerator SupportTelegraphExecutesConfiguredShieldOnceAtActiveBoundary()
        {
            var config = new GameplayConfigService();
            config.Load(
                File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "_Game/Config/Generated/gameplay_config.json")),
                "test:T430-playmode");
            double startedAt = Time.timeAsDouble;
            EnemyController puppet = CreateEnemy(
                config,
                "enemy_soul_puppet",
                43001,
                startedAt,
                requiresWeakpoint: true);
            EnemyController ally = CreateEnemy(
                config,
                "enemy_skeleton_ghost",
                43002,
                startedAt,
                requiresWeakpoint: false);
            var playerObject = new GameObject("T430 Effect Pipeline Player");
            PlayerCombatController player = playerObject.AddComponent<PlayerCombatController>();
            player.Initialize(config, "player_moyan");
            var allyTarget = new EnemySkillEffectTarget(ally);
            var world = new StrategyWorld(config, player, allyTarget);
            using var runtime = new EnemyStrategyRuntime(puppet, config, world);
            var context = new EnemyAttackTriggerContext(
                cooldownReady: true,
                targetInDistance: false,
                hpThresholdReached: false,
                supportTargetId: allyTarget.TargetId);

            bool began = runtime.TryBeginAttack(context, 0d, startedAt);
            EnemyAttackTelegraphSnapshot opened = runtime.Telegraph;
            double executeAt = opened.ExpectedExecuteAt;

            Assert.That(began, Is.True);
            Assert.That(puppet.State.State, Is.EqualTo(EnemyState.Windup));
            Assert.That(opened.IsVisible, Is.True);
            Assert.That(opened.AttackId, Is.EqualTo("atk_puppet_shield"));
            Assert.That(opened.ActionKind, Is.EqualTo(EnemyAttackActionKind.Support));
            Assert.That(opened.InterruptGestureType, Is.EqualTo("Circle"));
            Assert.That(executeAt - startedAt, Is.EqualTo(0.8d).Within(0.000001d));

            runtime.Tick(executeAt - 0.001d);
            Assert.That(runtime.Telegraph.IsVisible, Is.True);
            Assert.That(world.ExecutionCount, Is.Zero);
            Assert.That(ally.Buffs.Count, Is.Zero);

            runtime.Tick(executeAt);
            Assert.That(puppet.State.State, Is.EqualTo(EnemyState.Attack));
            Assert.That(runtime.Telegraph.IsVisible, Is.False);
            Assert.That(runtime.Telegraph.ClosedAt, Is.EqualTo(executeAt).Within(0.000001d));
            Assert.That(world.ExecutionCount, Is.EqualTo(1));
            Assert.That(world.LastAction.Kind, Is.EqualTo(EnemyAttackActionKind.Support));
            Assert.That(world.LastAction.EffectGroupId, Is.EqualTo("fx_puppet_shield"));
            Assert.That(world.VfxCount, Is.EqualTo(1));
            Assert.That(ally.Buffs.TryGet("buff_shield_50", out EnemyBuffSnapshot shield), Is.True);
            Assert.That(shield.Magnitude, Is.EqualTo(0.5d));

            EnemyDamageResult shielded = ally.ApplyDamage(10L, "test_hit", executeAt);
            Assert.That(shielded.AppliedHpDamage, Is.EqualTo(5L));
            runtime.Tick(executeAt + 0.1d);
            Assert.That(world.ExecutionCount, Is.EqualTo(1));

            EnemyDamageResult expired = ally.ApplyDamage(
                10L,
                "test_hit_after_expiry",
                executeAt + config.GetBuff("buff_shield_50").DurationSec);
            Assert.That(expired.AppliedHpDamage, Is.EqualTo(10L));
            Assert.That(ally.Buffs.Count, Is.Zero);

            runtime.Dispose();
            Object.DestroyImmediate(puppet.gameObject);
            Object.DestroyImmediate(ally.gameObject);
            Object.DestroyImmediate(playerObject);
            yield return null;
        }

        private static EnemyController CreateEnemy(
            IConfigProvider config,
            string enemyId,
            int hitTargetId,
            double timestamp,
            bool requiresWeakpoint)
        {
            var root = new GameObject($"T430 {enemyId}");
            root.SetActive(false);
            root.AddComponent<Damageable>();
            EnemyController enemy = root.AddComponent<EnemyController>();
            WeakpointController weakpoint = null;
            if (requiresWeakpoint)
            {
                var weakpointObject = new GameObject("Weakpoint");
                weakpointObject.transform.SetParent(root.transform, false);
                weakpointObject.AddComponent<CircleCollider2D>();
                weakpoint = weakpointObject.AddComponent<WeakpointController>();
            }

            enemy.Spawn(config, enemyId, hitTargetId, timestamp, weakpoint);
            Assert.That(enemy.CompleteSpawn(timestamp), Is.True);
            return enemy;
        }

        private sealed class StrategyWorld : IEnemyAttackWorld, ISkillEffectWorld
        {
            private readonly EnemySkillEffectTarget ally;
            private readonly IReadOnlyList<ISkillEffectTarget> targets;
            private readonly SkillService skills;

            public StrategyWorld(
                IConfigProvider config,
                PlayerCombatController player,
                EnemySkillEffectTarget allyTarget)
            {
                ally = allyTarget;
                targets = new ISkillEffectTarget[] { ally };
                skills = new SkillService(config, player);
            }

            public int ExecutionCount { get; private set; }

            public int VfxCount { get; private set; }

            public EnemyAttackAction LastAction { get; private set; }

            public IReadOnlyList<ISkillEffectTarget> Targets => targets;

            public ISkillEffectTarget PrimaryTarget { get; private set; }

            public void ExecuteAttack(
                EnemyController source,
                in EnemyAttackAction action,
                double timestamp)
            {
                ExecutionCount++;
                LastAction = action;
                Assert.That(action.SupportTargetId, Is.EqualTo(ally.TargetId));
                PrimaryTarget = ally;
                IReadOnlyList<SkillEffectStepResult> steps = skills.ExecuteEffectGroup(
                    action.EffectGroupId,
                    action.AttackId,
                    new SkillEffectContext(this, timestamp));
                Assert.That(steps.Count, Is.EqualTo(1));
                Assert.That(steps[0].EffectType, Is.EqualTo(SkillEffectTypes.ApplyBuff));
                Assert.That(steps[0].AffectedCount, Is.EqualTo(1));
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
                IReadOnlyList<ISkillEffectTarget> selected,
                string sourceId,
                double timestamp)
            {
                VfxCount++;
            }

            public void PlayAudio(string audioKey, string sourceId, double timestamp)
            {
            }
        }
    }
}
