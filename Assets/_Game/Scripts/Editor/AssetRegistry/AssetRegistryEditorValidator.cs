using System;
using System.Linq;
using OneStrokeDemon.Config;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace OneStrokeDemon.Editor.AssetRegistry
{
    public static class AssetRegistryEditorValidator
    {
        [MenuItem("One Stroke Demon/Config/Validate Asset Registry")]
        public static void ValidateCanonicalFromMenu()
        {
            AssetRegistryLoadSummary summary = ValidateCanonical();
            Debug.Log($"ASSET_REGISTRY_VALIDATION_PASS path={AssetRegistryPaths.CanonicalRegistry} " +
                summary.ToLogMessage());
        }

        public static GameplayConfigService LoadCanonicalConfig()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetRegistryPaths.GeneratedConfig);
            if (json == null)
            {
                throw Failure(
                    "ARVAL005",
                    "Generated gameplay configuration TextAsset is missing.",
                    AssetRegistryPaths.GeneratedConfig);
            }

            var service = new GameplayConfigService();
            service.Load(json.text, $"TextAsset:{AssetRegistryPaths.GeneratedConfig}");
            return service;
        }

        public static AssetRegistryLoadSummary ValidateCanonical()
        {
            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            if (registry == null)
            {
                throw Failure(
                    "ARVAL005",
                    "Canonical AssetRegistrySO is missing.",
                    AssetRegistryPaths.CanonicalRegistry);
            }

            return Validate(
                registry,
                LoadCanonicalConfig(),
                $"AssetRegistry:{AssetRegistryPaths.CanonicalRegistry}",
                requireEnabledScenes: true);
        }

        public static AssetRegistryLoadSummary Validate(
            AssetRegistrySO registry,
            IConfigProvider config,
            string source,
            bool requireEnabledScenes)
        {
            var runtime = new AssetRegistryService();
            AssetRegistryLoadSummary summary = runtime.Load(registry, config, source);

            foreach (AssetRegistryEntry entry in registry.Entries)
            {
                string assetPath = AssetDatabase.GetAssetPath(entry.Asset);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    throw Failure(
                        "ARVAL001",
                        $"Registry object '{entry.AssetKey}' is not a persistent project asset.",
                        entry.AssetKey,
                        source);
                }

                AssetManifestConfig expected = config.GetAsset(entry.AssetKey);
                if (expected.AssetType == "Prefab" && !PrefabUtility.IsPartOfPrefabAsset(entry.Asset))
                {
                    throw Failure(
                        "ARVAL002",
                        $"Registry object '{entry.AssetKey}' must be a prefab asset.",
                        entry.AssetKey,
                        source);
                }

                if (expected.AssetType == "Scene")
                {
                    ValidateSceneReference((AssetSceneReference)entry.Asset, entry.AssetKey, source, requireEnabledScenes);
                }
            }

            return summary;
        }

        private static void ValidateSceneReference(
            AssetSceneReference sceneReference,
            string assetKey,
            string source,
            bool requireEnabledScenes)
        {
            if (string.IsNullOrEmpty(sceneReference.ScenePath) ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneReference.ScenePath) == null)
            {
                throw Failure(
                    "ARVAL003",
                    $"Scene reference '{assetKey}' does not point to a valid scene asset.",
                    assetKey,
                    source);
            }

            if (requireEnabledScenes && !EditorBuildSettings.scenes.Any(scene =>
                    scene.enabled && string.Equals(scene.path, sceneReference.ScenePath, StringComparison.Ordinal)))
            {
                throw Failure(
                    "ARVAL004",
                    $"Scene reference '{assetKey}' is not enabled in Build Settings.",
                    assetKey,
                    source);
            }
        }

        private static AssetRegistryException Failure(
            string code,
            string message,
            string context,
            string source = "AssetRegistryEditorValidator")
        {
            return new AssetRegistryException(code, message, source, context);
        }
    }

    public sealed class AssetRegistryBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            ValidateForBuild();
        }

        public static AssetRegistryLoadSummary ValidateForBuild()
        {
            try
            {
                return AssetRegistryEditorValidator.ValidateCanonical();
            }
            catch (Exception exception) when (!(exception is BuildFailedException))
            {
                throw new BuildFailedException($"Asset registry build validation failed: {exception.Message}");
            }
        }

        public static AssetRegistryLoadSummary ValidateForBuild(
            AssetRegistrySO registry,
            IConfigProvider config,
            string source,
            bool requireEnabledScenes)
        {
            try
            {
                return AssetRegistryEditorValidator.Validate(
                    registry,
                    config,
                    source,
                    requireEnabledScenes);
            }
            catch (Exception exception) when (!(exception is BuildFailedException))
            {
                throw new BuildFailedException($"Asset registry build validation failed: {exception.Message}");
            }
        }
    }
}
