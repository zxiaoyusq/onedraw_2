using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T450
{
    [Category("T450")]
    public sealed class EnemyArchetypeConfigTests
    {
        private GameplayConfigService config;
        private IReadOnlyList<EnemyArchetypeDefinition> roster;

        [SetUp]
        public void SetUp()
        {
            config = Load(RuntimeConfigTestFixture.LoadJson(), "test:T450");
            roster = EnemyArchetypeCatalog.CreateCombatRoster(config);
        }

        [Test]
        public void RosterComesFromConfigAsFiveNormalEnemiesAndOneElite()
        {
            Assert.That(config.GetEnemies().Count, Is.EqualTo(7));
            Assert.That(roster.Count, Is.EqualTo(6));
            Assert.That(CountTier(EnemyTier.Normal), Is.EqualTo(5));
            Assert.That(CountTier(EnemyTier.Elite), Is.EqualTo(1));
            Assert.That(CountTier(EnemyTier.Boss), Is.Zero);

            var expectedNames = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ConfigIds.Enemies.EnemyFireFish] = "符火鱼妖",
                [ConfigIds.Enemies.EnemyWheelZombie] = "轮车僵妖",
                [ConfigIds.Enemies.EnemyStoneTurtle] = "石甲龟妖",
                [ConfigIds.Enemies.EnemySkeletonGhost] = "骷髅幽魂",
                [ConfigIds.Enemies.EnemyTalismanBat] = "飞行符蝠",
                [ConfigIds.Enemies.EnemySoulPuppet] = "摄魂道傀",
            };
            foreach (EnemyArchetypeDefinition archetype in roster)
            {
                Assert.That(
                    archetype.DisplayNameZhCN,
                    Is.EqualTo(expectedNames[archetype.Enemy.EnemyId]));
                Assert.That(archetype.DisplayNameEnUS, Is.Not.Empty);
                Assert.That(
                    archetype.AssetType,
                    Is.EqualTo("Sprite").Or.EqualTo("Prefab"));
                Assert.That(archetype.Enemy.AssetKey, Is.Not.Empty);
                Assert.That(archetype.Enemy.PoolPrewarm, Is.GreaterThan(0));
            }
        }

        [Test]
        public void EveryEnemyHasUniqueConfiguredTeachingPointAndClearTelegraph()
        {
            var teachingSignatures = new HashSet<string>(StringComparer.Ordinal);
            foreach (EnemyArchetypeDefinition archetype in roster)
            {
                Assert.That(teachingSignatures.Add(archetype.TeachingSignature), Is.True);
                Assert.That(archetype.Attacks, Is.Not.Empty);
                foreach (EnemyAttackDefinition attack in archetype.Attacks)
                {
                    Assert.That(attack.Timeline.WindupSeconds, Is.GreaterThan(0d));
                    Assert.That(
                        attack.Timeline.InterruptStartSeconds,
                        Is.LessThan(attack.Timeline.WindupSeconds));
                    Assert.That(
                        attack.Timeline.InterruptEndSeconds,
                        Is.GreaterThanOrEqualTo(attack.Timeline.WindupSeconds));
                }
            }

            EnemyArchetypeDefinition fireFish = Find(ConfigIds.Enemies.EnemyFireFish);
            Assert.That(fireFish.Attacks[0].ActionKind, Is.EqualTo(EnemyAttackActionKind.Projectile));
            ProjectileConfig ghostFire = config.GetProjectile(fireFish.Attacks[0].ProjectileId);
            Assert.That(ghostFire.Cuttable, Is.True);
            Assert.That(ghostFire.Reflectable, Is.True);

            EnemyArchetypeDefinition wheel = Find(ConfigIds.Enemies.EnemyWheelZombie);
            Assert.That(wheel.Attacks[0].ActionKind, Is.EqualTo(EnemyAttackActionKind.Charge));
            Assert.That(wheel.Attacks[0].Timeline.InterruptGestureType, Is.EqualTo("Any"));

            EnemyArchetypeDefinition turtle = Find(ConfigIds.Enemies.EnemyStoneTurtle);
            Assert.That(turtle.Defense.RequiredGestureType, Is.EqualTo("Charged"));
            Assert.That(turtle.Defense.RequiredStanceId, Is.EqualTo(ConfigIds.Stances.StanceBlade));

            EnemyArchetypeDefinition ghost = Find(ConfigIds.Enemies.EnemySkeletonGhost);
            Assert.That(ghost.Enemy.StanceVulnerability, Is.EqualTo("Talisman"));
            Assert.That(ghost.Enemy.Weakpoint.HasHitbox, Is.False);
            Assert.That(ghost.Attacks[0].Timeline.InterruptGestureType, Is.EqualTo("Any"));

            EnemyArchetypeDefinition bat = Find(ConfigIds.Enemies.EnemyTalismanBat);
            Assert.That(bat.Movement.PatternType, Is.EqualTo(EnemyMovementPatternTypes.Dive));
            Assert.That(bat.Attacks[0].Timeline.InterruptGestureType, Is.EqualTo("Any"));

            EnemyArchetypeDefinition puppet = Find(ConfigIds.Enemies.EnemySoulPuppet);
            Assert.That(puppet.Enemy.Tier, Is.EqualTo(EnemyTier.Elite));
            Assert.That(
                HasAction(puppet, EnemyAttackActionKind.Support),
                Is.True);
        }

        [Test]
        [Category("T699G")]
        public void CurrentContentKeepsOnlyChargedAndUltimateShapeRequirements()
        {
            Assert.That(
                config.GetDefenseRule(ConfigIds.DefenseRules.DefenseTurtleShell)
                    .RequiredGestureType,
                Is.EqualTo("Charged"));
            Assert.That(
                config.GetDefenseRule(ConfigIds.DefenseRules.DefenseDirectionSeal)
                    .RequiredGestureType,
                Is.EqualTo("Any"));
            Assert.That(
                config.GetDefenseRule(ConfigIds.DefenseRules.DefenseBossPins)
                    .RequiredGestureType,
                Is.EqualTo("Any"));

            IReadOnlyList<EnemyConfig> enemies = config.GetEnemies();
            for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                IReadOnlyList<EnemyAttackConfig> attacks =
                    config.GetEnemyAttacks(enemies[enemyIndex].AttackSetId);
                for (int attackIndex = 0; attackIndex < attacks.Count; attackIndex++)
                {
                    Assert.That(
                        attacks[attackIndex].GestureInterruptType,
                        Is.EqualTo("Any"),
                        attacks[attackIndex].AttackId);
                }
            }

            Assert.That(
                config.GetSkill(ConfigIds.Skills.SkillTalismanBind).GestureType,
                Is.EqualTo("Any"));
            Assert.That(
                config.GetSkill(ConfigIds.Skills.SkillUltimateSeal).GestureType,
                Is.EqualTo("Circle"));
            Assert.That(
                config.GetTutorialSteps(ConfigIds.Tutorials.TutorialLevel002)[0].GestureType,
                Is.EqualTo("Charged"));
            Assert.That(
                config.GetTutorialSteps(ConfigIds.Tutorials.TutorialLevel003)[0].GestureType,
                Is.EqualTo("Circle"));

            Assert.That(
                config.GetWeakpointRule(ConfigIds.WeakpointRules.WeakpointForeheadTalisman)
                    .InterruptAttack,
                Is.True);
            Assert.That(
                config.GetWeakpointRule(ConfigIds.WeakpointRules.WeakpointTurtleBelly)
                    .InterruptAttack,
                Is.True);
            Assert.That(
                config.GetWeakpointRule(ConfigIds.WeakpointRules.WeakpointBatDive)
                    .InterruptAttack,
                Is.True);
            Assert.That(
                config.GetWeakpointRule(ConfigIds.WeakpointRules.WeakpointBossSeal)
                    .InterruptAttack,
                Is.True);
        }

        [Test]
        public void HpSpeedAndAttackChangesFlowThroughReloadedConfigWithoutRuntimeBranches()
        {
            string changedJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                JObject enemy = FindRow(
                    (JArray)root["enemies"],
                    "enemyId",
                    ConfigIds.Enemies.EnemyFireFish);
                enemy["maxHp"] = 47;
                enemy["moveSpeedRefPxSec"] = 137;

                JObject attack = FindRow(
                    (JArray)root["enemyAttacks"],
                    "attackId",
                    ConfigIds.EnemyAttacks.AtkFireFishOrb);
                attack["damage"] = 13;
                JObject projectile = FindRow(
                    (JArray)root["projectiles"],
                    "projectileId",
                    ConfigIds.Projectiles.ProjGhostFire);
                projectile["damage"] = 13;
            });
            GameplayConfigService changed = Load(changedJson, "test:T450-mutated");
            EnemyArchetypeDefinition archetype = EnemyArchetypeCatalog.Create(
                changed,
                ConfigIds.Enemies.EnemyFireFish);

            Assert.That(archetype.Enemy.MaximumHp, Is.EqualTo(47));
            Assert.That(
                archetype.Movement.SpeedReferencePixelsPerSecond,
                Is.EqualTo(137d));
            Assert.That(archetype.Attacks[0].Damage, Is.EqualTo(13));
            Assert.That(
                changed.GetProjectile(archetype.Attacks[0].ProjectileId).Damage,
                Is.EqualTo(13));
        }

        private int CountTier(EnemyTier tier)
        {
            int count = 0;
            for (int index = 0; index < roster.Count; index++)
            {
                if (roster[index].Enemy.Tier == tier)
                {
                    count++;
                }
            }

            return count;
        }

        private EnemyArchetypeDefinition Find(string enemyId)
        {
            for (int index = 0; index < roster.Count; index++)
            {
                if (string.Equals(
                        roster[index].Enemy.EnemyId,
                        enemyId,
                        StringComparison.Ordinal))
                {
                    return roster[index];
                }
            }

            throw new AssertionException($"Enemy archetype '{enemyId}' was not found.");
        }

        private static bool HasAction(
            in EnemyArchetypeDefinition archetype,
            EnemyAttackActionKind kind)
        {
            for (int index = 0; index < archetype.Attacks.Count; index++)
            {
                if (archetype.Attacks[index].ActionKind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static JObject FindRow(JArray rows, string key, string value)
        {
            foreach (JObject row in rows.Children<JObject>())
            {
                if (string.Equals(
                        row[key]?.Value<string>(),
                        value,
                        StringComparison.Ordinal))
                {
                    return row;
                }
            }

            throw new AssertionException($"Configured row '{key}={value}' was not found.");
        }

        private static GameplayConfigService Load(string json, string source)
        {
            var service = new GameplayConfigService();
            service.Load(json, source);
            return service;
        }
    }
}
