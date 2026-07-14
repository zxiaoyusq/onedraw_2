using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.AssetRegistry;
using OneStrokeDemon.Presentation;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Editor.Art
{
    public static class T630ArtAssetPaths
    {
        public const string ArtRoot = "Assets/_Game/Art";
        public const string AtlasRoot = ArtRoot + "/SpriteAtlases";
        public const string BackgroundAtlas = AtlasRoot + "/Backgrounds.spriteatlasv2";
        public const string CharacterAtlas = AtlasRoot + "/Characters.spriteatlasv2";
        public const string EnemyAtlas = AtlasRoot + "/Enemies.spriteatlasv2";
        public const string UiAtlas = AtlasRoot + "/UI.spriteatlasv2";
        public const string VfxAtlas = AtlasRoot + "/VFX.spriteatlasv2";
        public const string SoulPuppetSprite = ArtRoot + "/Enemies/soul_puppet.png";
        public const string TombArmorKingSprite = ArtRoot + "/Enemies/tomb_armor_king.png";
        public const string GhostFlameSprite = ArtRoot + "/VFX/Sprites/vfx_ghost_flame.png";
        public const string ImpactGlowSprite = ArtRoot + "/VFX/Sprites/vfx_impact_glow.png";
        public const string SlashArcSprite = ArtRoot + "/VFX/Sprites/vfx_slash_arc.png";
        public const string SmokeBurstSprite = ArtRoot + "/VFX/Sprites/vfx_smoke_burst.png";

        public static readonly string[] Atlases =
        {
            BackgroundAtlas,
            CharacterAtlas,
            EnemyAtlas,
            UiAtlas,
            VfxAtlas,
        };
    }

    public static class T630ArtAssetAuthoring
    {
        private const float PixelsPerUnit = 100f;

        private static readonly (string Name, int UniqueId)[] RequiredSortingLayers =
        {
            ("Background", 630001001),
            ("Default", 0),
            ("Actors", 630001002),
            ("Projectiles", 630001003),
            ("VFX", 1424202891),
        };

        [MenuItem("One Stroke Demon/Art/Create or Repair T630 Prototype Assets")]
        public static void CreateOrRepairPrototypeAssets()
        {
            RequireGeneratedActorSources();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureSortingLayers();
            ConfigurePngImporters();
            CreateSpriteAtlases();
            CreateActorPrefabs();
            CreateVfxPrefabs();
            BindCanonicalRegistry();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            AssetRegistryLoadSummary summary = AssetRegistryEditorValidator.ValidateCanonical();
            Debug.Log(
                $"T630_ART_AUTHORING_PASS png={EnumeratePngPaths().Count} atlases={T630ArtAssetPaths.Atlases.Length} " +
                summary.ToLogMessage());
        }

        private static void RequireGeneratedActorSources()
        {
            foreach (string path in new[]
                     {
                         T630ArtAssetPaths.SoulPuppetSprite,
                         T630ArtAssetPaths.TombArmorKingSprite,
                     })
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(
                        "T630 generated actor PNG is missing; generation and alpha validation must finish first.",
                        path);
                }
            }
        }

        private static void EnsureSortingLayers()
        {
            UnityObject tagManagerObject = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")
                .FirstOrDefault();
            if (tagManagerObject == null)
            {
                throw new InvalidOperationException("Unable to load ProjectSettings/TagManager.asset.");
            }

            var serialized = new SerializedObject(tagManagerObject);
            SerializedProperty layers = serialized.FindProperty("m_SortingLayers");
            if (layers == null || !layers.isArray)
            {
                throw new InvalidOperationException("TagManager m_SortingLayers was not found.");
            }

            var existing = new List<(string Name, int UniqueId, bool Locked)>();
            for (int index = 0; index < layers.arraySize; index += 1)
            {
                SerializedProperty item = layers.GetArrayElementAtIndex(index);
                existing.Add((
                    item.FindPropertyRelative("name").stringValue,
                    item.FindPropertyRelative("uniqueID").intValue,
                    item.FindPropertyRelative("locked").boolValue));
            }

            var ordered = new List<(string Name, int UniqueId, bool Locked)>();
            foreach ((string name, int uniqueId) in RequiredSortingLayers)
            {
                int existingIndex = existing.FindIndex(item => item.Name == name);
                ordered.Add(existingIndex >= 0
                    ? existing[existingIndex]
                    : (name, uniqueId, false));
            }

            ordered.AddRange(existing.Where(item =>
                RequiredSortingLayers.All(required => required.Name != item.Name)));
            if (ordered.Select(item => item.UniqueId).Distinct().Count() != ordered.Count)
            {
                throw new InvalidOperationException("Sorting Layer unique IDs are not unique.");
            }

            layers.arraySize = ordered.Count;
            for (int index = 0; index < ordered.Count; index += 1)
            {
                SerializedProperty item = layers.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("name").stringValue = ordered[index].Name;
                item.FindPropertyRelative("uniqueID").intValue = ordered[index].UniqueId;
                item.FindPropertyRelative("locked").boolValue = ordered[index].Locked;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tagManagerObject);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigurePngImporters()
        {
            foreach (string path in EnumeratePngPaths())
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"TextureImporter is missing for '{path}'.");
                }

                bool isBackground = path.StartsWith(T630ArtAssetPaths.ArtRoot + "/Backgrounds/", StringComparison.Ordinal);
                bool isActor = path.StartsWith(T630ArtAssetPaths.ArtRoot + "/Characters/", StringComparison.Ordinal) ||
                    path.StartsWith(T630ArtAssetPaths.ArtRoot + "/Enemies/", StringComparison.Ordinal);
                bool isUi = path.StartsWith(T630ArtAssetPaths.ArtRoot + "/UI/", StringComparison.Ordinal);
                int maxSize = isBackground ? 4096 : isActor || isUi ? 2048 : 1024;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                var spriteSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(spriteSettings);
                spriteSettings.spriteMeshType = isBackground || isUi ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                spriteSettings.spriteAlignment = (int)(isActor ? SpriteAlignment.Custom : SpriteAlignment.Center);
                spriteSettings.spritePivot = isActor ? new Vector2(0.5f, 0.08f) : new Vector2(0.5f, 0.5f);
                importer.SetTextureSettings(spriteSettings);
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.compressionQuality = 100;
                importer.maxTextureSize = maxSize;
                importer.SaveAndReimport();
            }
        }

        private static void CreateSpriteAtlases()
        {
            EnsureAssetFolder(T630ArtAssetPaths.AtlasRoot);
            CreateSpriteAtlas(
                T630ArtAssetPaths.BackgroundAtlas,
                EnumeratePngPaths().Where(path => path.Contains("/Backgrounds/")),
                4096);
            CreateSpriteAtlas(
                T630ArtAssetPaths.CharacterAtlas,
                EnumeratePngPaths().Where(path => path.Contains("/Characters/")),
                2048);
            CreateSpriteAtlas(
                T630ArtAssetPaths.EnemyAtlas,
                EnumeratePngPaths().Where(path => path.Contains("/Enemies/")),
                2048);
            CreateSpriteAtlas(
                T630ArtAssetPaths.UiAtlas,
                EnumeratePngPaths().Where(path =>
                    path.Contains("/UI/") || Path.GetFileName(path).StartsWith("icon_", StringComparison.Ordinal)),
                2048);
            CreateSpriteAtlas(
                T630ArtAssetPaths.VfxAtlas,
                EnumeratePngPaths().Where(path =>
                    path.Contains("/VFX/Sprites/") || Path.GetFileName(path).StartsWith("proj_", StringComparison.Ordinal)),
                2048);
        }

        private static void CreateSpriteAtlas(
            string atlasPath,
            IEnumerable<string> spritePaths,
            int maxTextureSize)
        {
            UnityObject[] textures = spritePaths
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path))
                .Cast<UnityObject>()
                .ToArray();
            if (textures.Length == 0 || textures.Any(texture => texture == null))
            {
                throw new InvalidOperationException($"Sprite Atlas '{atlasPath}' has missing Texture2D inputs.");
            }

            var atlas = new SpriteAtlasAsset();
            atlas.SetIsVariant(false);
            atlas.Add(textures);
            SpriteAtlasAsset.Save(atlas, atlasPath);
            AssetDatabase.ImportAsset(
                atlasPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"SpriteAtlasImporter is missing for '{atlasPath}'.");
            }

            importer.includeInBuild = true;
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableAlphaDilation = true,
                enableRotation = false,
                enableTightPacking = true,
                padding = 4,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                anisoLevel = 0,
                filterMode = FilterMode.Bilinear,
                generateMipMaps = false,
                readable = false,
            };
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                overridden = false,
                maxTextureSize = maxTextureSize,
                format = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100,
                crunchedCompression = false,
                allowsAlphaSplitting = false,
            });
            importer.SaveAndReimport();
        }

        private static void CreateActorPrefabs()
        {
            CreateSpritePrefab(
                "BossTombArmorKing",
                T630ArtAssetPaths.TombArmorKingSprite,
                "Assets/_Game/Prefabs/Actors/BossTombArmorKing.prefab",
                "Actors");
            CreateSpritePrefab(
                "EnemySoulPuppet",
                T630ArtAssetPaths.SoulPuppetSprite,
                "Assets/_Game/Prefabs/Actors/EnemySoulPuppet.prefab",
                "Actors");
        }

        private static void CreateVfxPrefabs()
        {
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            foreach (AssetManifestConfig row in config.GetAssetManifest()
                         .Where(row => row.AssetType == "Prefab" &&
                             row.AssetKey.StartsWith("vfx_", StringComparison.Ordinal)))
            {
                var root = new GameObject(ToPascalCase(row.AssetKey));
                try
                {
                    SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                    renderer.sprite = LoadRequiredSprite(SelectVfxSprite(row.AssetKey));
                    renderer.sortingLayerName = "VFX";
                    renderer.sortingOrder = 0;
                    root.AddComponent<VfxPoolItem>();
                    GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, row.AddressOrPath);
                    if (prefab == null)
                    {
                        throw new InvalidOperationException($"Failed to save VFX Prefab '{row.AddressOrPath}'.");
                    }
                }
                finally
                {
                    UnityObject.DestroyImmediate(root);
                }
            }
        }

        private static string SelectVfxSprite(string assetKey)
        {
            if (ContainsAny(assetKey, "slash", "cut", "wave", "dive", "echo"))
            {
                return T630ArtAssetPaths.SlashArcSprite;
            }

            if (ContainsAny(assetKey, "ghost", "puppet", "slow", "vulnerable", "bind", "burn"))
            {
                return T630ArtAssetPaths.GhostFlameSprite;
            }

            if (ContainsAny(assetKey, "hit", "critical", "stun", "switch", "burst"))
            {
                return T630ArtAssetPaths.ImpactGlowSprite;
            }

            return T630ArtAssetPaths.SmokeBurstSprite;
        }

        private static bool ContainsAny(string value, params string[] fragments) =>
            fragments.Any(fragment => value.Contains(fragment, StringComparison.Ordinal));

        private static void CreateSpritePrefab(
            string name,
            string spritePath,
            string prefabPath,
            string sortingLayer)
        {
            var root = new GameObject(name);
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadRequiredSprite(spritePath);
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = 0;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Failed to save actor Prefab '{prefabPath}'.");
                }
            }
            finally
            {
                UnityObject.DestroyImmediate(root);
            }
        }

        private static void BindCanonicalRegistry()
        {
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            if (registry == null)
            {
                throw new InvalidOperationException("Canonical AssetRegistry is missing.");
            }

            IReadOnlyDictionary<string, UnityObject> existing = registry.Entries
                .Where(entry => entry != null && entry.Asset != null)
                .GroupBy(entry => entry.AssetKey, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Asset, StringComparer.Ordinal);
            var replacement = new List<AssetRegistryEntry>();
            foreach (AssetManifestConfig row in config.GetAssetManifest())
            {
                UnityObject asset = row.AssetType switch
                {
                    "Sprite" => AssetDatabase.LoadAssetAtPath<Sprite>(row.AddressOrPath),
                    "Prefab" => AssetDatabase.LoadAssetAtPath<GameObject>(row.AddressOrPath),
                    "AudioClip" or "Scene" => existing.TryGetValue(row.AssetKey, out UnityObject retained)
                        ? retained
                        : null,
                    _ => null,
                };
                if (asset == null)
                {
                    throw new InvalidOperationException(
                        $"T630 cannot bind '{row.AssetKey}' ({row.AssetType}) at '{row.AddressOrPath}'.");
                }

                replacement.Add(new AssetRegistryEntry(row.AssetKey, asset));
            }

            registry.ReplaceEntriesForEditor(replacement);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }

        private static Sprite LoadRequiredSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Sprite is missing at '{path}'.");
            }

            return sprite;
        }

        private static IReadOnlyList<string> EnumeratePngPaths()
        {
            if (!Directory.Exists(T630ArtAssetPaths.ArtRoot))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(T630ArtAssetPaths.ArtRoot, "*.png", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string name = path.Substring(path.LastIndexOf('/') + 1);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string ToPascalCase(string value) => string.Concat(
            value.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
    }

}
