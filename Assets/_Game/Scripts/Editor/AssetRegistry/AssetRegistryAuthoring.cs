using System;
using System.Collections.Generic;
using System.Linq;
using OneStrokeDemon.Config;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Editor.AssetRegistry
{
    public static class AssetRegistryAuthoring
    {
        [MenuItem("One Stroke Demon/Config/Create or Repair Asset Registry")]
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
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<AssetRegistrySO>();
                registry.name = "AssetRegistry";
                AssetDatabase.CreateAsset(registry, AssetRegistryPaths.CanonicalRegistry);
            }

            IReadOnlyDictionary<string, UnityObject> existing = FirstValidEntriesByKey(registry);
            var entries = new List<AssetRegistryEntry>(config.GetAssetManifest().Count);
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

        private static IReadOnlyDictionary<string, UnityObject> FirstValidEntriesByKey(AssetRegistrySO registry)
        {
            var result = new Dictionary<string, UnityObject>(StringComparer.Ordinal);
            foreach (AssetRegistryEntry entry in registry.Entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.AssetKey) && entry.Asset != null &&
                    !result.ContainsKey(entry.AssetKey))
                {
                    result.Add(entry.AssetKey, entry.Asset);
                }
            }

            return result;
        }

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

        private static Sprite EnsurePlaceholderSprite()
        {
            Sprite existing = AssetDatabase.LoadAllAssetsAtPath(AssetRegistryPaths.PlaceholderSprite)
                .OfType<Sprite>()
                .FirstOrDefault();
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

        private static AudioClip EnsurePlaceholderAudio()
        {
            AudioClip existing = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetRegistryPaths.PlaceholderAudio);
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

        private static GameObject EnsurePlaceholderPrefab(Sprite sprite)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(AssetRegistryPaths.PlaceholderPrefab);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("T240_PlaceholderPrefab");
            try
            {
                root.AddComponent<SpriteRenderer>().sprite = sprite;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AssetRegistryPaths.PlaceholderPrefab);
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

        private static AssetSceneReference EnsureSceneReference()
        {
            AssetSceneReference scene = AssetDatabase.LoadAssetAtPath<AssetSceneReference>(
                AssetRegistryPaths.BattleSceneReference);
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string name = path.Substring(path.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
