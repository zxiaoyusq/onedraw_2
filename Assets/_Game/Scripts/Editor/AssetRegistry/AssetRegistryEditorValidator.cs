using System;
using System.Linq;
using OneStrokeDemon.Config;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace OneStrokeDemon.Editor.AssetRegistry
{
    // 定义 AssetRegistryEditorValidator 的编辑器工具职责，集中管理资源生成、验证或构建入口。
    public static class AssetRegistryEditorValidator
    {
        [MenuItem("One Stroke Demon/Config/Validate Asset Registry")]
        // 校验 ValidateCanonicalFromMenu 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public static void ValidateCanonicalFromMenu()
        {
            AssetRegistryLoadSummary summary = ValidateCanonical();
            Debug.Log($"ASSET_REGISTRY_VALIDATION_PASS path={AssetRegistryPaths.CanonicalRegistry} " +
                summary.ToLogMessage());
        }

        // 加载 LoadCanonicalConfig 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public static GameplayConfigService LoadCanonicalConfig()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetRegistryPaths.GeneratedConfig);
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
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

        // 校验 ValidateCanonical 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public static AssetRegistryLoadSummary ValidateCanonical()
        {
            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
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

        // 校验 Validate 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public static AssetRegistryLoadSummary Validate(
            AssetRegistrySO registry,
            IConfigProvider config,
            string source,
            bool requireEnabledScenes)
        {
            var runtime = new AssetRegistryService();
            AssetRegistryLoadSummary summary = runtime.Load(registry, config, source);

            // 逐项处理资源或配置条目，保证生成与验证顺序稳定。
            foreach (AssetRegistryEntry entry in registry.Entries)
            {
                string assetPath = AssetDatabase.GetAssetPath(entry.Asset);
                // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
                if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    throw Failure(
                        "ARVAL001",
                        $"Registry object '{entry.AssetKey}' is not a persistent project asset.",
                        entry.AssetKey,
                        source);
                }

                AssetManifestConfig expected = config.GetAsset(entry.AssetKey);
                // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
                if (expected.AssetType == "Prefab" && !PrefabUtility.IsPartOfPrefabAsset(entry.Asset))
                {
                    throw Failure(
                        "ARVAL002",
                        $"Registry object '{entry.AssetKey}' must be a prefab asset.",
                        entry.AssetKey,
                        source);
                }

                // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
                if (expected.AssetType == "Scene")
                {
                    ValidateSceneReference((AssetSceneReference)entry.Asset, entry.AssetKey, source, requireEnabledScenes);
                }
            }

            return summary;
        }

        // 校验 ValidateSceneReference 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static void ValidateSceneReference(
            AssetSceneReference sceneReference,
            string assetKey,
            string source,
            bool requireEnabledScenes)
        {
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (string.IsNullOrEmpty(sceneReference.ScenePath) ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneReference.ScenePath) == null)
            {
                throw Failure(
                    "ARVAL003",
                    $"Scene reference '{assetKey}' does not point to a valid scene asset.",
                    assetKey,
                    source);
            }

            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
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

        // 处理 Failure 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static AssetRegistryException Failure(
            string code,
            string message,
            string context,
            string source = "AssetRegistryEditorValidator")
        {
            return new AssetRegistryException(code, message, source, context);
        }
    }

    // 定义 AssetRegistryBuildPreprocessor 的编辑器工具职责，集中管理资源生成、验证或构建入口。
    public sealed class AssetRegistryBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        // 响应 OnPreprocessBuild 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            ValidateForBuild();
        }

        // 校验 ValidateForBuild 对应的编辑器流程，并保持资源写入与校验结果可追踪。
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

        // 校验 ValidateForBuild 对应的编辑器流程，并保持资源写入与校验结果可追踪。
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
