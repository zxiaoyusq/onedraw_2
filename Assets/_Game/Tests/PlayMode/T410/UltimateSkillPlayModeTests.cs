using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T410
{
    [Category("T410")]
    public sealed class UltimateSkillPlayModeTests
    {
        [UnityTest]
        public IEnumerator UltimateWaitsForValidGestureEventThenRunsConfiguredWorldChain()
        {
            var config = new GameplayConfigService();
            config.Load(
                File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "_Game/Config/Generated/gameplay_config.json")),
                "test:T410-playmode");
            var root = new GameObject("T410UltimatePlayer");
            PlayerCombatController player = root.AddComponent<PlayerCombatController>();
            player.Initialize(config, "player_moyan");
            player.GainEnergy(100, 0d, "test_fill");

            var normal = new RuntimeTarget("normal", SkillEnemyTier.Normal, 0.2f);
            var boss = new RuntimeTarget("boss", SkillEnemyTier.Boss, 0.8f);
            var world = new RuntimeWorld(normal, boss);
            var service = new SkillService(config, player);

            SkillActivationResult invalid = service.TryActivate(
                new SkillActivationRequest(
                    "skill_ultimate_seal",
                    SkillTriggerTypes.Ultimate,
                    "Circle",
                    gestureIsValid: false,
                    inputElapsedSeconds: 1d,
                    timestamp: 1d),
                new SkillEffectContext(world, 1d));

            Assert.That(invalid.Status, Is.EqualTo(SkillActivationStatus.GestureInvalid));
            Assert.That(player.Current.CurrentEnergy, Is.EqualTo(100));
            Assert.That(world.CoreOperations, Is.Empty);
            yield return null;

            SkillActivationResult activated = service.TryActivate(
                new SkillActivationRequest(
                    "skill_ultimate_seal",
                    SkillTriggerTypes.Ultimate,
                    "Circle",
                    gestureIsValid: true,
                    inputElapsedSeconds: 2.5d,
                    timestamp: 2d),
                new SkillEffectContext(world, 2d));
            yield return null;

            Assert.That(activated.Status, Is.EqualTo(SkillActivationStatus.Activated));
            Assert.That(player.Current.CurrentEnergy, Is.Zero);
            Assert.That(
                world.CoreOperations,
                Is.EqualTo(new[]
                {
                    "timescale",
                    "clear",
                    "normal:damage",
                    "boss:damage",
                    "normal:execute",
                    "boss:buff",
                }));
            Assert.That(world.TimeScale, Is.EqualTo(0.25f));
            Assert.That(world.TimeScaleDuration, Is.EqualTo(0.8f));
            Assert.That(world.ClearedProjectiles, Is.EqualTo(2));
            Assert.That(normal.DamageReceived, Is.EqualTo(50f));
            Assert.That(normal.WasExecuted, Is.True);
            Assert.That(boss.AppliedBuffId, Is.EqualTo("buff_vulnerable"));
            Assert.That(boss.AppliedBuffDuration, Is.EqualTo(2f));
            Assert.That(world.VfxCount, Is.EqualTo(5));
            Assert.That(world.AudioCount, Is.EqualTo(5));

            Object.Destroy(root);
            yield return null;
        }

        private sealed class RuntimeWorld : ISkillEffectWorld
        {
            private readonly IReadOnlyList<ISkillEffectTarget> targets;

            public RuntimeWorld(params ISkillEffectTarget[] configuredTargets)
            {
                targets = configuredTargets;
                for (int i = 0; i < configuredTargets.Length; i++)
                {
                    ((RuntimeTarget)configuredTargets[i]).Operations = CoreOperations;
                }
            }

            public List<string> CoreOperations { get; } = new List<string>();

            public float TimeScale { get; private set; }

            public float TimeScaleDuration { get; private set; }

            public int ClearedProjectiles { get; private set; }

            public int VfxCount { get; private set; }

            public int AudioCount { get; private set; }

            public IReadOnlyList<ISkillEffectTarget> Targets => targets;

            public ISkillEffectTarget PrimaryTarget => null;

            public int RepeatLastStroke(
                float damageMultiplier,
                float delaySeconds,
                string sourceId,
                double timestamp)
            {
                CoreOperations.Add("repeat");
                return 1;
            }

            public int SetTimeScale(
                float scale,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                TimeScale = scale;
                TimeScaleDuration = durationSeconds;
                CoreOperations.Add("timescale");
                return 1;
            }

            public int SetNextStrokeDamageMultiplier(
                float multiplier,
                string sourceId,
                double timestamp)
            {
                CoreOperations.Add("damage-multiplier");
                return 1;
            }

            public int ClearHostileProjectiles(string sourceId, double timestamp)
            {
                ClearedProjectiles = 2;
                CoreOperations.Add("clear");
                return ClearedProjectiles;
            }

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
                AudioCount++;
            }
        }

        private sealed class RuntimeTarget : ISkillEffectTarget
        {
            private readonly float hpRatio;

            public RuntimeTarget(string targetId, SkillEnemyTier tier, float configuredHpRatio)
            {
                TargetId = targetId;
                EnemyTier = tier;
                hpRatio = configuredHpRatio;
            }

            public List<string> Operations { get; set; }

            public float DamageReceived { get; private set; }

            public bool WasExecuted { get; private set; }

            public string AppliedBuffId { get; private set; } = string.Empty;

            public float AppliedBuffDuration { get; private set; }

            public string TargetId { get; }

            public SkillTargetFaction Faction => SkillTargetFaction.Enemy;

            public SkillEnemyTier EnemyTier { get; }

            public bool IsAlive => true;

            public bool IsInEffectRadius => true;

            public bool WasHitByLastStroke => true;

            public bool IsInsideGesture => true;

            public bool ApplyDamage(float amount, string sourceId, double timestamp)
            {
                DamageReceived += amount;
                Operations.Add($"{TargetId}:damage");
                return true;
            }

            public bool ApplyHealing(float amount, string sourceId, double timestamp)
            {
                Operations.Add($"{TargetId}:heal");
                return true;
            }

            public bool ApplyBuff(
                BuffConfig buff,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                AppliedBuffId = buff.BuffId;
                AppliedBuffDuration = durationSeconds;
                Operations.Add($"{TargetId}:buff");
                return true;
            }

            public bool RemoveArmor(float amount, string sourceId, double timestamp)
            {
                Operations.Add($"{TargetId}:armor");
                return true;
            }

            public bool ApplyKnockback(
                float distanceRefPixels,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                Operations.Add($"{TargetId}:knockback");
                return true;
            }

            public bool ExecuteBelowHpRatio(
                float threshold,
                string sourceId,
                double timestamp)
            {
                Operations.Add($"{TargetId}:execute");
                WasExecuted = hpRatio <= threshold;
                return WasExecuted;
            }

            public bool IncrementCounter(
                float amount,
                float limit,
                string sourceId,
                double timestamp)
            {
                Operations.Add($"{TargetId}:counter");
                return true;
            }
        }
    }
}
