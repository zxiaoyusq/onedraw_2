using System;
using System.Collections.Generic;
using System.Linq;
using OneStrokeDemon.Config;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Editor.AssetRegistry
{
    // 定义 AssetRegistryAuthoring 的编辑器工具职责，集中管理资源生成、验证或构建入口。
    public static class AssetRegistryAuthoring
    {
        [MenuItem("One Stroke Demon/Config/Create or Repair Asset Registry")]
        // 创建 CreateOrRepairCanonicalRegistry 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public static void CreateOrRepairCanonicalRegistry()
        {
            EnsureFolder(AssetRegistryPaths.PlaceholderFolder);

            Sprite placeholderSprite = EnsurePlaceholderSprite();
            AudioClip placeholderAudio = EnsurePlaceholderAudio();
            GameObject placeholderPrefab = EnsurePlaceholderPrefab(placeholderSprite);
            AssetSceneReference sceneReference = EnsureSceneReference();
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();

            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<AssetRegistrySO>();
                registry.name = "AssetRegistry";
                AssetDatabase.CreateAsset(registry, AssetRegistryPaths.CanonicalRegistry);
            }

            IReadOnlyDictionary<string, UnityObject> existing = FirstValidEntriesByKey(registry);
            var entries = new List<AssetRegistryEntry>(config.GetAssetManifest().Count);
            // 逐项处理资源或配置条目，保证生成与验证顺序稳定。
            foreach (AssetManifestConfig expected in config.GetAssetManifest())
            {
                UnityObject asset = existing.TryGetValue(expected.AssetKey, out UnityObject current) &&
                    MatchesExpectedType(current, expected.AssetType)
                    ? current
                    : PlaceholderFor(
                        expected.AssetType,
                        placeholderSprite,
                        placeholderAudio,
                        placeholderPrefab,
                        sceneReference);
                entries.Add(new AssetRegistryEntry(expected.AssetKey, asset));
            }

            registry.ReplaceEntriesForEditor(entries);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssetRegistryLoadSummary summary = AssetRegistryEditorValidator.ValidateCanonical();
            Selection.activeObject = registry;
            Debug.Log($"ASSET_REGISTRY_AUTHORING_PASS path={AssetRegistryPaths.CanonicalRegistry} " +
                $"{summary.ToLogMessage()}");
        }

        // 处理 FirstValidEntriesByKey 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static IReadOnlyDictionary<string, UnityObject> FirstValidEntriesByKey(AssetRegistrySO registry)
        {
            var result = new Dictionary<string, UnityObject>(StringComparer.Ordinal);
            // 逐项处理资源或配置条目，保证生成与验证顺序稳定。
            foreach (AssetRegistryEntry entry in registry.Entries)
            {
                // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
                if (entry != null && !string.IsNullOrEmpty(entry.AssetKey) && entry.Asset != null &&
                    !result.ContainsKey(entry.AssetKey))
                {
                    result.Add(entry.AssetKey, entry.Asset);
                }
            }

            return result;
        }

        // 处理 PlaceholderFor 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static UnityObject PlaceholderFor(
            string assetType,
            Sprite sprite,
            AudioClip audio,
            GameObject prefab,
            AssetSceneReference scene)
        {
            return assetType switch
            {
                "Sprite" => sprite,
                "AudioClip" => audio,
                "Prefab" => prefab,
                "Scene" => scene,
                _ => throw new InvalidOperationException($"Unsupported AssetManifest type '{assetType}'."),
            };
        }

        // 处理 MatchesExpectedType 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static bool MatchesExpectedType(UnityObject asset, string assetType)
        {
            return assetType switch
            {
                "Sprite" => asset is Sprite,
                "AudioClip" => asset is AudioClip,
                "Prefab" => asset is GameObject && PrefabUtility.IsPartOfPrefabAsset(asset),
                "Scene" => asset is AssetSceneReference,
                _ => false,
            };
        }

        // 确保存在 EnsurePlaceholderSprite 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static Sprite EnsurePlaceholderSprite()
        {
            Sprite existing = AssetDatabase.LoadAllAssetsAtPath(AssetRegistryPaths.PlaceholderSprite)
                .OfType<Sprite>()
                .FirstOrDefault();
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (existing != null)
            {
                return existing;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "T240_PlaceholderTexture",
            };
            texture.SetPixels(new[]
            {
                Color.magenta,
                Color.black,
                Color.black,
                Color.magenta,
            });
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            AssetDatabase.CreateAsset(texture, AssetRegistryPaths.PlaceholderSprite);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
            sprite.name = "T240_PlaceholderSprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetRegistryPaths.PlaceholderSprite, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAllAssetsAtPath(AssetRegistryPaths.PlaceholderSprite)
                .OfType<Sprite>()
                .Single();
        }

        // 确保存在 EnsurePlaceholderAudio 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static AudioClip EnsurePlaceholderAudio()
        {
            AudioClip existing = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetRegistryPaths.PlaceholderAudio);
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (existing != null)
            {
                return existing;
            }

            AudioClip audio = AudioClip.Create(
                "T240_PlaceholderAudio",
                lengthSamples: 441,
                channels: 1,
                frequency: 44100,
                stream: false);
            AssetDatabase.CreateAsset(audio, AssetRegistryPaths.PlaceholderAudio);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetRegistryPaths.PlaceholderAudio);
        }

        // 确保存在 EnsurePlaceholderPrefab 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static GameObject EnsurePlaceholderPrefab(Sprite sprite)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(AssetRegistryPaths.PlaceholderPrefab);
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("T240_PlaceholderPrefab");
            try
            {
                root.AddComponent<SpriteRenderer>().sprite = sprite;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AssetRegistryPaths.PlaceholderPrefab);
                // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
                if (prefab == null)
                {
                    throw new InvalidOperationException("Unity failed to save the T240 placeholder prefab.");
                }

                return prefab;
            }
            finally
            {
                UnityObject.DestroyImmediate(root);
            }
        }

        // 确保存在 EnsureSceneReference 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static AssetSceneReference EnsureSceneReference()
        {
            AssetSceneReference scene = AssetDatabase.LoadAssetAtPath<AssetSceneReference>(
                AssetRegistryPaths.BattleSceneReference);
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (scene == null)
            {
                scene = ScriptableObject.CreateInstance<AssetSceneReference>();
                scene.name = "BattleSceneReference";
                AssetDatabase.CreateAsset(scene, AssetRegistryPaths.BattleSceneReference);
            }

            scene.SetScenePathForEditor(AssetRegistryPaths.BattleScene);
            EditorUtility.SetDirty(scene);
            return scene;
        }

        // 确保存在 EnsureFolder 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static void EnsureFolder(string path)
        {
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string name = path.Substring(path.LastIndexOf('/') + 1);
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
