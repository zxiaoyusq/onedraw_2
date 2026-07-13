using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Tests.EditMode.T230
{
    [Category("ConfigPipeline")]
    public sealed class RuntimeConfigLoadTests
    {
        private static readonly IReadOnlyDictionary<string, Type> RowTypes =
            new Dictionary<string, Type>
            {
                ["GlobalRow"] = typeof(GlobalConfig),
                ["PlayersRow"] = typeof(PlayerConfig),
                ["StancesRow"] = typeof(StanceConfig),
                ["StrokeRulesRow"] = typeof(StrokeRuleConfig),
                ["DamageFormulasRow"] = typeof(DamageFormulaConfig),
                ["DefenseRulesRow"] = typeof(DefenseRuleConfig),
                ["WeakpointRulesRow"] = typeof(WeakpointRuleConfig),
                ["MovePatternsRow"] = typeof(MovePatternConfig),
                ["EnemiesRow"] = typeof(EnemyConfig),
                ["EnemyAttacksRow"] = typeof(EnemyAttackConfig),
                ["ProjectilesRow"] = typeof(ProjectileConfig),
                ["BuffsRow"] = typeof(BuffConfig),
                ["SkillsRow"] = typeof(SkillConfig),
                ["SkillEffectsRow"] = typeof(SkillEffectConfig),
                ["LevelsRow"] = typeof(LevelConfig),
                ["WavesRow"] = typeof(WaveConfig),
                ["SpawnPointsRow"] = typeof(SpawnPointConfig),
                ["EnemyModifiersRow"] = typeof(EnemyModifierConfig),
                ["SpawnsRow"] = typeof(SpawnConfig),
                ["BossPhasesRow"] = typeof(BossPhaseConfig),
                ["RewardsRow"] = typeof(RewardConfig),
                ["TutorialsRow"] = typeof(TutorialConfig),
                ["TextsRow"] = typeof(TextConfig),
                ["AudioCuesRow"] = typeof(AudioCueConfig),
                ["VfxCuesRow"] = typeof(VfxCueConfig),
                ["AssetManifestRow"] = typeof(AssetManifestConfig),
                ["EnumsRow"] = typeof(EnumConfig),
                ["FieldDictionaryRow"] = typeof(FieldDictionaryConfig),
            };

        [Test]
        public void GeneratedSnapshotLoadsOnceAndBuildsReadOnlyIndexes()
        {
            var service = new GameplayConfigService();

            GameplayConfigLoadSummary summary = service.Load(
                RuntimeConfigTestFixture.LoadJson(),
                RuntimeConfigTestFixture.Source);

            Assert.That(service.State, Is.EqualTo(GameplayConfigServiceState.Ready));
            Assert.That(summary.SchemaVersion, Is.EqualTo(1));
            Assert.That(summary.ContentVersion, Is.EqualTo("0.1.1-sample"));
            Assert.That(summary.ContentHash, Is.EqualTo("16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c"));
            Assert.That(summary.TableCount, Is.EqualTo(28));
            Assert.That(summary.RecordCount, Is.EqualTo(645));
            Assert.That(summary.PrimaryIndexCount, Is.GreaterThan(0));
            Assert.That(summary.GroupIndexCount, Is.GreaterThan(0));
            Assert.That(summary.ToLogMessage(), Does.Contain("source=test:generated-gameplay-config"));
            Assert.That(summary.ToLogMessage(), Does.Contain("records=645"));

            Assert.That(service.GetGlobal("reference_width").IntValue, Is.EqualTo(1920));
            Assert.That(service.GetEnemy("boss_tomb_king").Tier, Is.EqualTo("Boss"));
            Assert.That(service.GetLevel("lv_003_boss").BossEnemyId, Is.EqualTo("boss_tomb_king"));
            Assert.That(service.GetEnemyAttacks("attackset_boss_phase1"), Is.Not.Empty);
            Assert.That(service.GetSkillEffects("fx_ultimate_seal"), Is.Not.Empty);
            Assert.That(service.GetWaves("lv_001_tutorial").Count, Is.EqualTo(3));
            Assert.That(service.GetBossPhases("boss_tomb_king").Count, Is.EqualTo(3));
            Assert.That(service.GetRewards("reward_level_001"), Is.Not.Empty);
            Assert.That(service.GetTutorialSteps("tutorial_level_001"), Is.Not.Empty);
            Assert.That(service.GetText("text_level_001").ZhCN, Is.Not.Empty);
            Assert.That(service.GetAsset("boss_tomb_armor_king").AssetType, Is.EqualTo("Prefab"));

            IReadOnlyList<WaveConfig> waves = service.GetWaves("lv_001_tutorial");
            var mutableWaves = waves as IList<WaveConfig>;
            Assert.That(mutableWaves, Is.Not.Null);
            Assert.That(mutableWaves.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutableWaves.Add(waves[0]));

            GameplayConfigException secondLoad = Assert.Throws<GameplayConfigException>(() =>
                service.Load(RuntimeConfigTestFixture.LoadJson(), "test:second-load"));
            Assert.That(secondLoad.Code, Is.EqualTo("CFGRT001"));
            Assert.That(service.State, Is.EqualTo(GameplayConfigServiceState.Ready));
        }

        [Test]
        public void UnknownLookupFailsWithTableFieldAndRequestedId()
        {
            var service = new GameplayConfigService();
            service.Load(RuntimeConfigTestFixture.LoadJson(), RuntimeConfigTestFixture.Source);

            GameplayConfigException exception = Assert.Throws<GameplayConfigException>(() =>
                service.GetEnemy("enemy_missing"));

            Assert.That(exception.Code, Is.EqualTo("CFGRT007"));
            Assert.That(exception.Context, Is.EqualTo("Enemies.enemyId"));
            Assert.That(exception.Message, Does.Contain("enemy_missing"));
            Assert.That(exception.Source, Is.EqualTo(RuntimeConfigTestFixture.Source));
        }

        [Test]
        public void RuntimeDtosExactlyMatchFrozenJsonSchemaAndExposeNoPublicSetters()
        {
            JObject schema = JObject.Parse(System.IO.File.ReadAllText(RuntimeConfigTestFixture.SchemaPath));
            var definitions = (JObject)schema["$defs"];
            Assert.That(definitions.Properties().Select(property => property.Name), Is.EquivalentTo(RowTypes.Keys));

            foreach (KeyValuePair<string, Type> rowType in RowTypes)
            {
                var expectedProperties = (JObject)definitions[rowType.Key]["properties"];
                IReadOnlyDictionary<string, PropertyInfo> actualProperties = JsonProperties(rowType.Value);
                Assert.That(actualProperties.Keys, Is.EquivalentTo(expectedProperties.Properties().Select(property => property.Name)), rowType.Key);

                foreach (JProperty expectedProperty in expectedProperties.Properties())
                {
                    PropertyInfo actual = actualProperties[expectedProperty.Name];
                    Assert.That(actual.PropertyType, Is.EqualTo(ExpectedClrType((JObject)expectedProperty.Value)), $"{rowType.Key}.{expectedProperty.Name}");
                    JsonPropertyAttribute attribute = actual.GetCustomAttribute<JsonPropertyAttribute>();
                    bool nullable = IsNullable((JObject)expectedProperty.Value);
                    Assert.That(attribute.Required, Is.EqualTo(nullable ? Required.AllowNull : Required.Always), $"{rowType.Key}.{expectedProperty.Name}");
                    Assert.That(actual.GetSetMethod(nonPublic: true).IsPublic, Is.False, $"{rowType.Key}.{expectedProperty.Name}");
                }
            }

            IReadOnlyDictionary<string, PropertyInfo> rootProperties = JsonProperties(typeof(GameplayConfigDocument));
            var schemaRootProperties = (JObject)schema["properties"];
            Assert.That(rootProperties.Keys, Is.EquivalentTo(schemaRootProperties.Properties().Select(property => property.Name)));
            foreach (JProperty schemaProperty in schemaRootProperties.Properties())
            {
                PropertyInfo actual = rootProperties[schemaProperty.Name];
                JToken reference = schemaProperty.Value["items"]?["$ref"];
                if (reference != null)
                {
                    string definitionName = reference.Value<string>().Split('/').Last();
                    Assert.That(actual.PropertyType, Is.EqualTo(RowTypes[definitionName].MakeArrayType()), schemaProperty.Name);
                }
            }
        }

        private static IReadOnlyDictionary<string, PropertyInfo> JsonProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(property => new
                {
                    Property = property,
                    Attribute = property.GetCustomAttribute<JsonPropertyAttribute>(),
                })
                .Where(item => item.Attribute != null)
                .ToDictionary(item => item.Attribute.PropertyName, item => item.Property, StringComparer.Ordinal);
        }

        private static Type ExpectedClrType(JObject property)
        {
            JToken typeToken = property["type"];
            bool nullable = typeToken.Type == JTokenType.Array && typeToken.Values<string>().Contains("null");
            string jsonType = typeToken.Type == JTokenType.Array
                ? typeToken.Values<string>().Single(value => value != "null")
                : typeToken.Value<string>();
            Type result = jsonType switch
            {
                "string" => typeof(string),
                "integer" => typeof(long),
                "number" => typeof(float),
                "boolean" => typeof(bool),
                _ => throw new InvalidOperationException($"Unsupported JSON type {jsonType}."),
            };
            return nullable && result.IsValueType ? typeof(Nullable<>).MakeGenericType(result) : result;
        }

        private static bool IsNullable(JObject property)
        {
            return property["type"].Type == JTokenType.Array && property["type"].Values<string>().Contains("null");
        }
    }
}
