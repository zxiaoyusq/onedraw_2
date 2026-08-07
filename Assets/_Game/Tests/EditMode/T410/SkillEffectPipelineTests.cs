using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Skills;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T410
{
    [Category("T410")]
    public sealed class SkillEffectPipelineTests
    {
        private readonly List<GameObject> roots = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < roots.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(roots[i]);
            }

            roots.Clear();
        }

        [Test]
        public void UltimateExecutesStableConfiguredOrderAndTargetFilters()
        {
            GameplayConfigService config = LoadConfig();
            PlayerCombatController player = CreatePlayer(config);
            player.GainEnergy(100, 0d, "test_fill");

            var normal = new FakeTarget("normal_low", SkillEnemyTier.Normal)
            {
                HealthRatio = 0.2f,
                IsInsideGestureValue = true,
            };
            var elite = new FakeTarget("elite_high", SkillEnemyTier.Elite)
            {
                HealthRatio = 0.8f,
                IsInsideGestureValue = true,
            };
            var boss = new FakeTarget("boss", SkillEnemyTier.Boss)
            {
                HealthRatio = 0.2f,
            };
            var world = new FakeWorld(normal, elite, boss)
            {
                ClearedProjectiles = 2,
            };
            var service = new SkillService(config, player);
            var request = new SkillActivationRequest(
                "skill_ultimate_seal",
                SkillTriggerTypes.Ultimate,
                "Circle",
                gestureIsValid: true,
                inputElapsedSeconds: 2.5d,
                timestamp: 1d);

            SkillActivationResult result = service.TryActivate(
                request,
                new SkillEffectContext(world, 1d));

            Assert.That(result.Status, Is.EqualTo(SkillActivationStatus.Activated));
            Assert.That(player.Current.CurrentEnergy, Is.Zero);
            Assert.That(result.Steps.Count, Is.EqualTo(5));
            Assert.That(
                EffectTypes(result.Steps),
                Is.EqualTo(new[]
                {
                    "TimeScale",
                    "ClearProjectiles",
                    "Damage",
                    "ExecuteBelowHpRatio",
                    "ApplyBuff",
                }));
            Assert.That(result.Steps[1].AffectedCount, Is.EqualTo(2));
            Assert.That(result.Steps[2].SelectedTargetCount, Is.EqualTo(3));
            Assert.That(result.Steps[2].AffectedCount, Is.EqualTo(3));
            Assert.That(result.Steps[3].SelectedTargetCount, Is.EqualTo(2));
            Assert.That(result.Steps[3].AffectedCount, Is.EqualTo(1));
            Assert.That(result.Steps[4].SelectedTargetCount, Is.EqualTo(1));
            Assert.That(result.Steps[4].AffectedCount, Is.EqualTo(1));
            Assert.That(world.VfxCount, Is.EqualTo(5));
            Assert.That(world.AudioCount, Is.EqualTo(5));
            AssertOrder(world.Operations, "world:timescale", "world:clear");
            AssertOrder(world.Operations, "world:clear", "normal_low:damage");
            AssertOrder(world.Operations, "normal_low:damage", "normal_low:execute");
            AssertOrder(world.Operations, "normal_low:execute", "boss:buff");
            Assert.That(world.Operations, Does.Contain("elite_high:execute"));
            Assert.That(world.Operations, Does.Not.Contain("boss:execute"));

            Assert.That(
                service.Executors.RegisteredEffectTypes,
                Is.EqualTo(new[]
                {
                    "ApplyBuff",
                    "ClearProjectiles",
                    "Damage",
                    "DamageMultiplier",
                    "ExecuteBelowHpRatio",
                    "Heal",
                    "IncrementCounter",
                    "Knockback",
                    "PlayVfx",
                    "RemoveArmor",
                    "RepeatStroke",
                    "TimeScale",
                }));
        }

        [Test]
        public void InvalidGestureExpiredWindowAndCooldownNeverPartiallySpendEnergy()
        {
            GameplayConfigService config = LoadConfig();
            PlayerCombatController player = CreatePlayer(config);
            player.GainEnergy(100, 0d, "test_fill");
            var world = new FakeWorld(
                new FakeTarget("normal", SkillEnemyTier.Normal)
                {
                    IsInsideGestureValue = true,
                });
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
            SkillActivationResult expired = service.TryActivate(
                new SkillActivationRequest(
                    "skill_ultimate_seal",
                    SkillTriggerTypes.Ultimate,
                    "Circle",
                    gestureIsValid: true,
                    inputElapsedSeconds: 2.5001d,
                    timestamp: 2d),
                new SkillEffectContext(world, 2d));

            Assert.That(invalid.Status, Is.EqualTo(SkillActivationStatus.GestureInvalid));
            Assert.That(expired.Status, Is.EqualTo(SkillActivationStatus.InputWindowExpired));
            Assert.That(player.Current.CurrentEnergy, Is.EqualTo(100));
            Assert.That(world.Operations, Is.Empty);

            player.TrySwitchStance("stance_talisman", 3d);
            SkillActivationResult activated = service.TryActivate(
                new SkillActivationRequest(
                    "skill_talisman_bind",
                    SkillTriggerTypes.Gesture,
                    "Circle",
                    gestureIsValid: true,
                    inputElapsedSeconds: 1d,
                    timestamp: 4d),
                new SkillEffectContext(world, 4d));
            long energyAfterFirst = player.Current.CurrentEnergy;
            SkillActivationResult coolingDown = service.TryActivate(
                new SkillActivationRequest(
                    "skill_talisman_bind",
                    SkillTriggerTypes.Gesture,
                    "Circle",
                    gestureIsValid: true,
                    inputElapsedSeconds: 1d,
                    timestamp: 5d),
                new SkillEffectContext(world, 5d));

            Assert.That(activated.Status, Is.EqualTo(SkillActivationStatus.Activated));
            Assert.That(activated.CooldownUntil, Is.EqualTo(10d));
            Assert.That(energyAfterFirst, Is.EqualTo(80));
            Assert.That(coolingDown.Status, Is.EqualTo(SkillActivationStatus.CooldownActive));
            Assert.That(player.Current.CurrentEnergy, Is.EqualTo(energyAfterFirst));
        }

        [Test]
        [Category("T699H")]
        public void TriangleGestureSkillAppliesConfiguredSlowToAllEnemyTiers()
        {
            GameplayConfigService config = LoadConfig();
            PlayerCombatController player = CreatePlayer(config);
            var normal = new FakeTarget("normal", SkillEnemyTier.Normal);
            var boss = new FakeTarget("boss", SkillEnemyTier.Boss);
            var world = new FakeWorld(normal, boss);
            var service = new SkillService(config, player);

            SkillActivationResult result = service.TryActivate(
                new SkillActivationRequest(
                    ConfigIds.Skills.SkillTriangleSlow,
                    SkillTriggerTypes.Gesture,
                    GestureType.Triangle.ToString(),
                    gestureIsValid: true,
                    inputElapsedSeconds: 1d,
                    timestamp: 1d),
                new SkillEffectContext(world, 1d));

            Assert.That(result.Status, Is.EqualTo(SkillActivationStatus.Activated));
            Assert.That(result.Steps.Count, Is.EqualTo(1));
            Assert.That(result.Steps[0].EffectType, Is.EqualTo(SkillEffectTypes.ApplyBuff));
            Assert.That(result.Steps[0].SelectedTargetCount, Is.EqualTo(2));
            Assert.That(result.Steps[0].AffectedCount, Is.EqualTo(2));
            Assert.That(normal.LastBuffId, Is.EqualTo(ConfigIds.Buffs.BuffSlow30));
            Assert.That(boss.LastBuffId, Is.EqualTo(ConfigIds.Buffs.BuffSlow30));
            Assert.That(normal.LastBuffDurationSeconds, Is.EqualTo(2f));
            Assert.That(player.Current.CurrentEnergy, Is.Zero);
        }

        [Test]
        public void ConfigConditionControlsExistingRepeatStrokeExecutor()
        {
            GameplayConfigService config = LoadConfig();
            PlayerCombatController player = CreatePlayer(config);
            var radiusTarget = new FakeTarget("radius_enemy", SkillEnemyTier.Normal);
            var world = new FakeWorld(radiusTarget);
            var service = new SkillService(config, player);

            SkillActivationResult belowThreshold = service.TryActivate(
                new SkillActivationRequest(
                    "skill_blade_echo",
                    SkillTriggerTypes.Passive,
                    "Any",
                    gestureIsValid: false,
                    inputElapsedSeconds: 0d,
                    timestamp: 1d),
                new SkillEffectContext(
                    world,
                    1d,
                    new Dictionary<string, double> { ["comboCount"] = 2d }));
            SkillActivationResult atThreshold = service.TryActivate(
                new SkillActivationRequest(
                    "skill_blade_echo",
                    SkillTriggerTypes.Passive,
                    "Any",
                    gestureIsValid: false,
                    inputElapsedSeconds: 0d,
                    timestamp: 2d),
                new SkillEffectContext(
                    world,
                    2d,
                    new Dictionary<string, double> { ["comboCount"] = 3d }));

            Assert.That(belowThreshold.Succeeded, Is.True);
            Assert.That(
                belowThreshold.Steps[0].Status,
                Is.EqualTo(SkillEffectStepStatus.ConditionNotMet));
            Assert.That(atThreshold.Succeeded, Is.True);
            Assert.That(atThreshold.Steps[0].Status, Is.EqualTo(SkillEffectStepStatus.Executed));
            Assert.That(world.RepeatCount, Is.EqualTo(1));

            StanceSwitchResult stanceSwitch = player.TrySwitchStance("stance_talisman", 3d);
            IReadOnlyList<SkillEffectStepResult> switchSteps = service.ExecuteEffectGroup(
                stanceSwitch.OnSwitchEffectGroupId,
                stanceSwitch.Current.StanceId,
                new SkillEffectContext(world, 3d));

            Assert.That(stanceSwitch.DidSwitch, Is.True);
            Assert.That(switchSteps.Count, Is.EqualTo(1));
            Assert.That(switchSteps[0].EffectType, Is.EqualTo("Knockback"));
            Assert.That(switchSteps[0].AffectedCount, Is.EqualTo(1));
            Assert.That(world.Operations, Does.Contain("radius_enemy:knockback"));
        }

        [Test]
        public void NewHealSkillNeedsOnlyConfigRowsAndCannotResurrectPlayer()
        {
            GameplayConfigService config = LoadConfigWithHealSkill();
            PlayerCombatController player = CreatePlayer(config);
            var events = new List<PlayerCombatEvent>();
            player.CombatEventPublished += events.Add;
            player.ApplyDamage(30, 0d, "enemy");
            var playerTarget = new PlayerSkillEffectTarget(player);
            var world = new FakeWorld { PrimaryTargetValue = playerTarget };
            var service = new SkillService(config, player);

            SkillActivationResult result = service.TryActivate(
                new SkillActivationRequest(
                    "skill_test_heal",
                    SkillTriggerTypes.Passive,
                    "Any",
                    gestureIsValid: false,
                    inputElapsedSeconds: 0d,
                    timestamp: 1d),
                new SkillEffectContext(world, 1d));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps[0].EffectType, Is.EqualTo("Heal"));
            Assert.That(result.Steps[0].AffectedCount, Is.EqualTo(1));
            Assert.That(player.Current.CurrentHp, Is.EqualTo(82));
            Assert.That(events[events.Count - 1].SignedAmount, Is.EqualTo(12));

            player.ApplyDamage(1000, 2d, "enemy");
            Assert.That(playerTarget.ApplyHealing(50f, "late_heal", 3d), Is.False);
            Assert.That(player.Current.CurrentHp, Is.Zero);
        }

        private GameplayConfigService LoadConfig()
        {
            var config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T410");
            return config;
        }

        private GameplayConfigService LoadConfigWithHealSkill()
        {
            JObject root = RuntimeConfigTestFixture.LoadRoot();
            var skill = (JObject)root["skills"][0].DeepClone();
            skill["skillId"] = "skill_test_heal";
            skill["triggerType"] = "Passive";
            skill["requiredStanceId"] = "stance_blade";
            skill["energyCost"] = 0;
            skill["cooldownSec"] = 0;
            skill["gestureType"] = "Any";
            skill["inputWindowSec"] = 0;
            skill["effectGroupId"] = "fx_test_heal";
            ((JArray)root["skills"]).Add(skill);
            ((JArray)root["skillEffects"]).Add(new JObject
            {
                ["effectGroupId"] = "fx_test_heal",
                ["order"] = 1,
                ["effectType"] = "Heal",
                ["targetType"] = "Target",
                ["value1"] = 12,
                ["value2"] = 0,
                ["durationSec"] = 0,
                ["buffId"] = string.Empty,
                ["vfxKey"] = string.Empty,
                ["audioKey"] = string.Empty,
                ["condition"] = string.Empty,
            });
            root["contentHash"] = GameplayConfigHash.Calculate(root);
            var config = new GameplayConfigService();
            config.Load(root.ToString(Formatting.None), "test:T410-config-only-skill");
            return config;
        }

        private PlayerCombatController CreatePlayer(IConfigProvider config)
        {
            var root = new GameObject("T410Player");
            roots.Add(root);
            PlayerCombatController player = root.AddComponent<PlayerCombatController>();
            player.Initialize(config, "player_moyan");
            return player;
        }

        private static string[] EffectTypes(IReadOnlyList<SkillEffectStepResult> steps)
        {
            var types = new string[steps.Count];
            for (int i = 0; i < steps.Count; i++)
            {
                types[i] = steps[i].EffectType;
            }

            return types;
        }

        private static void AssertOrder(
            IReadOnlyList<string> operations,
            string before,
            string after)
        {
            int beforeIndex = IndexOf(operations, before);
            int afterIndex = IndexOf(operations, after);
            Assert.That(beforeIndex, Is.GreaterThanOrEqualTo(0), before);
            Assert.That(afterIndex, Is.GreaterThan(beforeIndex), after);
        }

        private static int IndexOf(IReadOnlyList<string> values, string expected)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], expected, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private sealed class FakeWorld : ISkillEffectWorld
        {
            private readonly List<ISkillEffectTarget> targets;

            public FakeWorld(params ISkillEffectTarget[] configuredTargets)
            {
                targets = new List<ISkillEffectTarget>(configuredTargets);
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] is FakeTarget fake)
                    {
                        fake.Operations = Operations;
                    }
                }
            }

            public List<string> Operations { get; } = new List<string>();

            public int ClearedProjectiles { get; set; }

            public int RepeatCount { get; private set; }

            public int VfxCount { get; private set; }

            public int AudioCount { get; private set; }

            public ISkillEffectTarget PrimaryTargetValue { get; set; }

            public IReadOnlyList<ISkillEffectTarget> Targets => targets;

            public ISkillEffectTarget PrimaryTarget => PrimaryTargetValue;

            public int RepeatLastStroke(
                float damageMultiplier,
                float delaySeconds,
                string sourceId,
                double timestamp)
            {
                RepeatCount++;
                Operations.Add("world:repeat");
                return 1;
            }

            public int SetTimeScale(
                float scale,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                Operations.Add("world:timescale");
                return 1;
            }

            public int SetNextStrokeDamageMultiplier(
                float multiplier,
                string sourceId,
                double timestamp)
            {
                Operations.Add("world:damage-multiplier");
                return 1;
            }

            public int ClearHostileProjectiles(string sourceId, double timestamp)
            {
                Operations.Add("world:clear");
                return ClearedProjectiles;
            }

            public void PlayVfx(
                string vfxKey,
                IReadOnlyList<ISkillEffectTarget> selected,
                string sourceId,
                double timestamp)
            {
                VfxCount++;
                Operations.Add($"vfx:{vfxKey}");
            }

            public void PlayAudio(string audioKey, string sourceId, double timestamp)
            {
                AudioCount++;
                Operations.Add($"audio:{audioKey}");
            }
        }

        private sealed class FakeTarget : ISkillEffectTarget
        {
            public FakeTarget(string targetId, SkillEnemyTier tier)
            {
                TargetId = targetId;
                EnemyTier = tier;
            }

            public List<string> Operations { get; set; }

            public float HealthRatio { get; set; } = 1f;

            public bool IsInsideGestureValue { get; set; }

            public string TargetId { get; }

            public SkillTargetFaction Faction => SkillTargetFaction.Enemy;

            public SkillEnemyTier EnemyTier { get; }

            public bool IsAlive { get; private set; } = true;

            public bool IsInEffectRadius => true;

            public bool WasHitByLastStroke => true;

            public bool IsInsideGesture => IsInsideGestureValue;

            public string LastBuffId { get; private set; } = string.Empty;

            public float LastBuffDurationSeconds { get; private set; }

            public bool ApplyDamage(float amount, string sourceId, double timestamp)
            {
                Operations?.Add($"{TargetId}:damage");
                return true;
            }

            public bool ApplyHealing(float amount, string sourceId, double timestamp)
            {
                Operations?.Add($"{TargetId}:heal");
                return true;
            }

            public bool ApplyBuff(
                BuffConfig buff,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                LastBuffId = buff.BuffId;
                LastBuffDurationSeconds = durationSeconds;
                Operations?.Add($"{TargetId}:buff");
                return true;
            }

            public bool RemoveArmor(float amount, string sourceId, double timestamp)
            {
                Operations?.Add($"{TargetId}:armor");
                return true;
            }

            public bool ApplyKnockback(
                float distanceRefPixels,
                float durationSeconds,
                string sourceId,
                double timestamp)
            {
                Operations?.Add($"{TargetId}:knockback");
                return true;
            }

            public bool ExecuteBelowHpRatio(
                float threshold,
                string sourceId,
                double timestamp)
            {
                Operations?.Add($"{TargetId}:execute");
                if (HealthRatio > threshold)
                {
                    return false;
                }

                IsAlive = false;
                return true;
            }

            public bool IncrementCounter(
                float amount,
                float limit,
                string sourceId,
                double timestamp)
            {
                Operations?.Add($"{TargetId}:counter");
                return true;
            }
        }
    }
}
