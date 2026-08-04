using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.AssetRegistry;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Tests.EditMode.T240
{
    [Category("ConfigPipeline")]
    public sealed class AssetRegistryValidationTests
    {
        private readonly List<UnityObject> temporaryObjects = new List<UnityObject>();
        private GameplayConfigService config;
        private AssetRegistrySO canonical;

        [SetUp]
        public void SetUp()
        {
            config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            canonical = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(AssetRegistryPaths.CanonicalRegistry);
            Assert.That(canonical, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UnityObject temporary in temporaryObjects)
            {
                if (temporary != null)
                {
                    UnityObject.DestroyImmediate(temporary);
                }
            }

            temporaryObjects.Clear();
        }

        [Test]
        public void CanonicalRegistryCoversEveryManifestKeyWithPersistentTypedAssets()
        {
            AssetRegistryLoadSummary summary = AssetRegistryEditorValidator.ValidateCanonical();
            AssetRegistryLoadSummary buildSummary = AssetRegistryBuildPreprocessor.ValidateForBuild();

            Assert.That(summary.ConfigHash, Is.EqualTo(config.ContentHash));
            Assert.That(buildSummary.EntryCount, Is.EqualTo(summary.EntryCount));
            Assert.That(summary.EntryCount, Is.EqualTo(78));
            Assert.That(summary.PrefabCount, Is.EqualTo(44));
            Assert.That(summary.SpriteCount, Is.EqualTo(16));
            Assert.That(summary.AudioClipCount, Is.EqualTo(17));
            Assert.That(summary.SceneCount, Is.EqualTo(1));
            Assert.That(summary.ToLogMessage(), Does.Contain("entries=78"));
            Assert.That(canonical.Entries.Select(entry => entry.AssetKey),
                Is.EquivalentTo(config.GetAssetManifest().Select(entry => entry.AssetKey)));
            Assert.That(canonical.Entries.All(entry => AssetDatabase.Contains(entry.Asset)), Is.True);

            var service = new AssetRegistryService();
            service.Load(canonical, config, "test:canonical-registry");
            Assert.That(service.GetAudioClip("audio_sfx_hit"), Is.Not.Null);
            Assert.That(service.GetSprite("bg_red_cave"), Is.Not.Null);
            Assert.That(service.GetPrefab("vfx_hit"), Is.Not.Null);
            Assert.That(service.GetScene("scene_battle").ScenePath, Is.EqualTo(AssetRegistryPaths.BattleScene));
            Assert.That(service.GetScene("scene_battle").SceneName, Is.EqualTo("Battle"));

            var manifest = config.GetAssetManifest() as IList<AssetManifestConfig>;
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => manifest.Add(manifest[0]));
        }

        [Test]
        public void EmptyNullDuplicateMissingExtraAndWrongTypeEntriesRejectWholeRegistry()
        {
            List<AssetRegistryEntry> entries = CloneCanonicalEntries();
            entries[0] = new AssetRegistryEntry(string.Empty, entries[0].Asset);
            AssertFailure(entries, "ARREG002");

            entries = CloneCanonicalEntries();
            entries[0] = new AssetRegistryEntry(entries[0].AssetKey, null);
            AssertFailure(entries, "ARREG003");

            entries = CloneCanonicalEntries();
            entries.Add(new AssetRegistryEntry(entries[0].AssetKey, entries[0].Asset));
            AssertFailure(entries, "ARREG004");

            entries = CloneCanonicalEntries();
            entries.RemoveAll(entry => entry.AssetKey == "bg_red_cave");
            AssertFailure(entries, "ARREG005");

            entries = CloneCanonicalEntries();
            entries.Add(new AssetRegistryEntry("extra_asset", entries[0].Asset));
            AssertFailure(entries, "ARREG006");

            entries = CloneCanonicalEntries();
            AudioClip audio = canonical.Entries.Single(entry => entry.AssetKey == "audio_sfx_hit").Asset as AudioClip;
            int spriteIndex = entries.FindIndex(entry => entry.AssetKey == "bg_red_cave");
            entries[spriteIndex] = new AssetRegistryEntry("bg_red_cave", audio);
            AssertFailure(entries, "ARREG007");
        }

        [Test]
        public void BuildGateConvertsInvalidRegistryIntoBuildFailure()
        {
            List<AssetRegistryEntry> entries = CloneCanonicalEntries();
            entries.RemoveAt(0);
            AssetRegistrySO invalid = CreateRegistry(entries);

            BuildFailedException exception = Assert.Throws<BuildFailedException>(() =>
                AssetRegistryBuildPreprocessor.ValidateForBuild(
                    invalid,
                    config,
                    "test:build-gate",
                    requireEnabledScenes: false));

            Assert.That(exception.Message, Does.Contain("ARREG005"));
            Assert.That(exception.Message, Does.Contain("build validation failed"));
        }

        [Test]
        public void ReplacingObjectKeepsStableConfigIdAndTypedLookup()
        {
            var texture = Track(new Texture2D(2, 2));
            var replacement = Track(Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f)));
            List<AssetRegistryEntry> entries = CloneCanonicalEntries();
            int index = entries.FindIndex(entry => entry.AssetKey == "bg_red_cave");
            entries[index] = new AssetRegistryEntry("bg_red_cave", replacement);

            var service = new AssetRegistryService();
            service.Load(CreateRegistry(entries), config, "test:replacement");

            Assert.That(service.GetSprite("bg_red_cave"), Is.SameAs(replacement));
            Assert.That(config.GetAsset("bg_red_cave").AssetKey, Is.EqualTo("bg_red_cave"));
            Assert.That(config.GetAsset("bg_red_cave").AddressOrPath,
                Is.EqualTo("Assets/_Game/Art/Backgrounds/bg_red_cave.png"));
            AssetRegistryException wrongType = Assert.Throws<AssetRegistryException>(() =>
                service.GetAudioClip("bg_red_cave"));
            Assert.That(wrongType.Code, Is.EqualTo("ARREG008"));
            AssetRegistryException unknown = Assert.Throws<AssetRegistryException>(() =>
                service.GetObject("missing_asset"));
            Assert.That(unknown.Code, Is.EqualTo("ARREG009"));
        }

        [Test]
        public void RegistrySerializedFieldsContainOnlyKeysObjectAndSceneReference()
        {
            Assert.That(SerializedFieldNames(typeof(AssetRegistrySO)), Is.EquivalentTo(new[] { "entries" }));
            Assert.That(SerializedFieldNames(typeof(AssetRegistryEntry)),
                Is.EquivalentTo(new[] { "assetKey", "asset" }));
            Assert.That(SerializedFieldNames(typeof(AssetSceneReference)),
                Is.EquivalentTo(new[] { "scenePath" }));

            FieldInfo[] entryFields = typeof(AssetRegistryEntry).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(entryFields.Single(field => field.Name == "assetKey").FieldType, Is.EqualTo(typeof(string)));
            Assert.That(entryFields.Single(field => field.Name == "asset").FieldType, Is.EqualTo(typeof(UnityObject)));
        }

        private void AssertFailure(IEnumerable<AssetRegistryEntry> entries, string code)
        {
            var service = new AssetRegistryService();
            AssetRegistryException exception = Assert.Throws<AssetRegistryException>(() =>
                service.Load(CreateRegistry(entries), config, $"test:{code}"));
            Assert.That(exception.Code, Is.EqualTo(code));
            Assert.That(service.State, Is.EqualTo(AssetRegistryServiceState.Failed));
            Assert.That(service.Summary, Is.Null);
        }

        private List<AssetRegistryEntry> CloneCanonicalEntries()
        {
            return canonical.Entries
                .Select(entry => new AssetRegistryEntry(entry.AssetKey, entry.Asset))
                .ToList();
        }

        private AssetRegistrySO CreateRegistry(IEnumerable<AssetRegistryEntry> entries)
        {
            AssetRegistrySO registry = Track(ScriptableObject.CreateInstance<AssetRegistrySO>());
            registry.ReplaceEntriesForEditor(entries);
            return registry;
        }

        private T Track<T>(T value) where T : UnityObject
        {
            temporaryObjects.Add(value);
            return value;
        }

        private static IEnumerable<string> SerializedFieldNames(Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(field => field.GetCustomAttribute<SerializeField>() != null)
                .Select(field => field.Name);
        }
    }
}
