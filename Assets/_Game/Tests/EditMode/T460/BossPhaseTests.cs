using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T460
{
    [Category("T460")]
    public sealed class BossPhaseTests
    {
        private GameplayConfigService config;
        private IReadOnlyList<BossPhaseDefinition> phases;

        [SetUp]
        public void SetUp()
        {
            config = Load(RuntimeConfigTestFixture.LoadJson(), "test:T460");
            phases = BossPhaseCatalog.Create(
                config,
                ConfigIds.Enemies.BossTombKing);
        }

        [Test]
        public void TombKingProfilesMapThreeContinuousConfiguredPhases()
        {
            Assert.That(phases.Count, Is.EqualTo(3));
            Assert.That(
                PhaseIds(phases),
                Is.EqualTo(new[]
                {
                    ConfigIds.BossPhases.BossTombPhase1,
                    ConfigIds.BossPhases.BossTombPhase2,
                    ConfigIds.BossPhases.BossTombPhase3,
                }));
            Assert.That(phases[0].EnterHpRatio, Is.EqualTo(1d));
            Assert.That(phases[0].ExitHpRatio, Is.EqualTo(0.67d).Within(0.000001d));
            Assert.That(phases[1].EnterHpRatio, Is.EqualTo(0.67d).Within(0.000001d));
            Assert.That(phases[1].ExitHpRatio, Is.EqualTo(0.34d).Within(0.000001d));
            Assert.That(phases[2].EnterHpRatio, Is.EqualTo(0.34d).Within(0.000001d));
            Assert.That(phases[2].ExitHpRatio, Is.Zero);

            Assert.That(
                phases[0].Movement.SpeedReferencePixelsPerSecond,
                Is.EqualTo(20d).Within(0.00001d));
            Assert.That(
                phases[1].Movement.SpeedReferencePixelsPerSecond,
                Is.EqualTo(32d).Within(0.00001d));
            Assert.That(
                phases[2].Movement.SpeedReferencePixelsPerSecond,
                Is.EqualTo(48d).Within(0.00001d));
            Assert.That(phases[0].Attacks[0].AttackId, Is.EqualTo(ConfigIds.EnemyAttacks.AtkBossRockfall));
            Assert.That(phases[1].Attacks[0].AttackId, Is.EqualTo(ConfigIds.EnemyAttacks.AtkBossSealWave));
            Assert.That(phases[2].Attacks[0].AttackId, Is.EqualTo(ConfigIds.EnemyAttacks.AtkBossCharge));
            Assert.That(phases[0].CombatProfile.Defense.MaximumArmor, Is.EqualTo(120));
            Assert.That(phases[1].CombatProfile.Defense.MaximumArmor, Is.EqualTo(60));
            Assert.That(phases[2].CombatProfile.Defense.MaximumArmor, Is.Zero);
            Assert.That(phases[0].CombatProfile.Weakpoint.HasHitbox, Is.False);
            Assert.That(phases[1].CombatProfile.Weakpoint.RadiusReferencePixels, Is.EqualTo(90f));
            Assert.That(phases[2].CombatProfile.Weakpoint.RadiusReferencePixels, Is.EqualTo(90f));
            Assert.That(phases[0].Defense.RequiredGestureType, Is.EqualTo("Any"));
            Assert.That(phases[1].Defense.RequiredGestureType, Is.EqualTo("Any"));
            Assert.That(phases[2].Defense.RequiredGestureType, Is.EqualTo("Any"));
            Assert.That(phases[0].DescriptionZhCN, Is.Not.Empty);
            Assert.That(phases[1].OnEnterEffectGroupId, Is.EqualTo("fx_boss_phase2_enter"));
        }

        [Test]
        public void ExactThresholdsAdvanceInOrderOnceAndLargeDamageCannotSkipEvents()
        {
            var machine = new BossPhaseStateMachine(phases);
            BossPhaseTransition started = machine.Start(1d);

            Assert.That(started.Sequence, Is.EqualTo(1UL));
            Assert.That(started.CurrentPhase.BossPhaseId, Is.EqualTo(ConfigIds.BossPhases.BossTombPhase1));
            Assert.That(machine.Advance(0.670001d), Is.Empty);
            IReadOnlyList<BossPhaseTransition> phase2 = machine.Advance(0.67d);
            Assert.That(phase2.Count, Is.EqualTo(1));
            Assert.That(phase2[0].Sequence, Is.EqualTo(2UL));
            Assert.That(phase2[0].CurrentPhase.BossPhaseId, Is.EqualTo(ConfigIds.BossPhases.BossTombPhase2));
            Assert.That(machine.Advance(0.67d), Is.Empty);
            Assert.That(machine.Advance(0.9d), Is.Empty);
            IReadOnlyList<BossPhaseTransition> phase3 = machine.Advance(0.2d);
            Assert.That(phase3.Count, Is.EqualTo(1));
            Assert.That(phase3[0].Sequence, Is.EqualTo(3UL));
            Assert.That(phase3[0].CurrentPhase.BossPhaseId, Is.EqualTo(ConfigIds.BossPhases.BossTombPhase3));
            Assert.That(machine.Advance(0d), Is.Empty);

            var skipped = new BossPhaseStateMachine(phases);
            skipped.Start(1d);
            IReadOnlyList<BossPhaseTransition> caughtUp = skipped.Advance(0.2d);
            Assert.That(caughtUp.Count, Is.EqualTo(2));
            Assert.That(caughtUp[0].CurrentPhase.Order, Is.EqualTo(2));
            Assert.That(caughtUp[1].CurrentPhase.Order, Is.EqualTo(3));
        }

        [Test]
        public void RuntimeCatalogRejectsGapOverlapAndNonBossOwnership()
        {
            string gapJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                FindRow((JArray)root["bossPhases"], "bossPhaseId", ConfigIds.BossPhases.BossTombPhase2)["enterHpRatio"] = 0.66d;
            });
            string overlapJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                FindRow((JArray)root["bossPhases"], "bossPhaseId", ConfigIds.BossPhases.BossTombPhase1)["exitHpRatio"] = 0.68d;
            });
            string tierJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                FindRow((JArray)root["enemies"], "enemyId", ConfigIds.Enemies.BossTombKing)["tier"] = "Normal";
            });

            Assert.That(
                () => BossPhaseCatalog.Create(Load(gapJson, "test:T460-gap"), ConfigIds.Enemies.BossTombKing),
                Throws.ArgumentException.With.Message.Contains("continuous HP coverage"));
            Assert.That(
                () => BossPhaseCatalog.Create(Load(overlapJson, "test:T460-overlap"), ConfigIds.Enemies.BossTombKing),
                Throws.ArgumentException.With.Message.Contains("continuous HP coverage"));
            Assert.That(
                () => BossPhaseCatalog.Create(Load(tierJson, "test:T460-tier"), ConfigIds.Enemies.BossTombKing),
                Throws.ArgumentException.With.Message.Contains("tier Boss"));
        }

        [Test]
        public void ThresholdSpeedDefenseAndWeakpointChangesNeedOnlyReloadedConfig()
        {
            string changedJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                JObject phase1 = FindRow(
                    (JArray)root["bossPhases"],
                    "bossPhaseId",
                    ConfigIds.BossPhases.BossTombPhase1);
                JObject phase2 = FindRow(
                    (JArray)root["bossPhases"],
                    "bossPhaseId",
                    ConfigIds.BossPhases.BossTombPhase2);
                phase1["exitHpRatio"] = 0.6d;
                phase2["enterHpRatio"] = 0.6d;
                phase2["defenseRuleId"] = "defense_none";
                phase2["weakpointRuleId"] = "weakpoint_none";
                FindRow(
                    (JArray)root["movePatterns"],
                    "movePatternId",
                    ConfigIds.MovePatterns.MoveBossPhase2)["speedMultiplier"] = 1.05d;
            });
            IReadOnlyList<BossPhaseDefinition> changed = BossPhaseCatalog.Create(
                Load(changedJson, "test:T460-mutated"),
                ConfigIds.Enemies.BossTombKing);

            Assert.That(changed[0].ExitHpRatio, Is.EqualTo(0.6d).Within(0.000001d));
            Assert.That(changed[1].EnterHpRatio, Is.EqualTo(0.6d).Within(0.000001d));
            Assert.That(
                changed[1].Movement.SpeedReferencePixelsPerSecond,
                Is.EqualTo(42d).Within(0.00001d));
            Assert.That(changed[1].CombatProfile.Defense.MaximumArmor, Is.Zero);
            Assert.That(changed[1].CombatProfile.Weakpoint.HasHitbox, Is.False);
        }

        private static string[] PhaseIds(IReadOnlyList<BossPhaseDefinition> configured)
        {
            var ids = new string[configured.Count];
            for (int index = 0; index < configured.Count; index++)
            {
                ids[index] = configured[index].BossPhaseId;
            }

            return ids;
        }

        private static JObject FindRow(JArray rows, string key, string value)
        {
            foreach (JObject row in rows.Children<JObject>())
            {
                if (string.Equals(row[key]?.Value<string>(), value, StringComparison.Ordinal))
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
