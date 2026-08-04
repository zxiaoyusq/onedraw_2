using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.AssetRegistry;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T250
{
    [Category("ConfigPipeline")]
    public sealed class GeneratedConfigPipelineTests
    {
        private const string JsonPath = "Assets/_Game/Config/Generated/gameplay_config.json";
        private const string HashPath = "Assets/_Game/Config/Generated/gameplay_config.hash";
        private const string ConfigIdsPath =
            "Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs";
        private static readonly Regex StableIdPattern = new Regex("^[a-z][a-z0-9_]*$");

        [Test]
        public void GeneratedHashMetadataAndAssemblyMatchRuntimeSnapshot()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            Assert.That(json, Is.Not.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(HashPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<MonoScript>(ConfigIdsPath), Is.Not.Null);
            Assert.That(
                CompilationPipeline.GetAssemblyNameFromScriptPath(ConfigIdsPath),
                Is.EqualTo("OneStrokeDemon.Config.dll"));

            var service = new GameplayConfigService();
            GameplayConfigLoadSummary summary = service.Load(json.text, "test:T250-generated-json");
            byte[] hashBytes = File.ReadAllBytes(HashPath);
            Assert.That(hashBytes, Is.EqualTo(Encoding.UTF8.GetBytes(service.ContentHash + "\n")));
            Assert.That(ConfigIds.SchemaVersion, Is.EqualTo(service.SchemaVersion));
            Assert.That(ConfigIds.ContentVersion, Is.EqualTo(service.ContentVersion));
            Assert.That(ConfigIds.ContentHash, Is.EqualTo(service.ContentHash));
            Assert.That(summary.RecordCount, Is.EqualTo(765));
            Assert.That(ConfigIds.IdSetCount, Is.EqualTo(29));
            Assert.That(ConfigIds.IdConstantCount, Is.EqualTo(382));
        }

        [Test]
        public void GeneratedConstantsCoverStableIdsAndDriveTypedConfigLookups()
        {
            Type[] idSets = typeof(ConfigIds).GetNestedTypes(BindingFlags.Public);
            FieldInfo[] idFields = idSets
                .SelectMany(type => type.GetFields(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .ToArray();
            string[] values = idFields.Select(field => (string)field.GetRawConstantValue()).ToArray();

            Assert.That(idSets.Length, Is.EqualTo(ConfigIds.IdSetCount));
            Assert.That(idFields.Length, Is.EqualTo(ConfigIds.IdConstantCount));
            Assert.That(values.All(value => StableIdPattern.IsMatch(value)), Is.True);
            foreach (Type idSet in idSets)
            {
                string[] setValues = idSet
                    .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                    .Select(field => (string)field.GetRawConstantValue())
                    .ToArray();
                Assert.That(setValues.Distinct(System.StringComparer.Ordinal).Count(), Is.EqualTo(setValues.Length));
            }

            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            Assert.That(config.GetPlayer(ConfigIds.Players.PlayerMoyan).PlayerId,
                Is.EqualTo("player_moyan"));
            Assert.That(config.GetLevel(ConfigIds.Levels.Lv001Tutorial).LevelId,
                Is.EqualTo("lv_001_tutorial"));
            Assert.That(config.GetText(ConfigIds.Texts.TextUiPause).ZhCN,
                Is.EqualTo("暂停"));
            Assert.That(config.GetEnemy(ConfigIds.Enemies.BossTombKing).EnemyId,
                Is.EqualTo("boss_tomb_king"));
            Assert.That(config.GetSkill(ConfigIds.Skills.SkillUltimateSeal).SkillId,
                Is.EqualTo("skill_ultimate_seal"));
            Assert.That(config.GetAsset(ConfigIds.Assets.SceneBattle).AssetKey,
                Is.EqualTo("scene_battle"));
            Assert.That(config.GetFeedbackCue(ConfigIds.FeedbackCues.FeedbackArmorBreak).VfxKey,
                Is.EqualTo(ConfigIds.VfxCues.VfxArmorBreak));
            Assert.That(
                config.GetStrokeTrailStyle(ConfigIds.StrokeTrailStyles.StrokeTrailLightningC).StyleId,
                Is.EqualTo("stroke_trail_lightning_c"));
            Assert.That(AssetRegistryEditorValidator.ValidateCanonical().EntryCount, Is.EqualTo(78));
        }
    }
}
