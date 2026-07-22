using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.AssetRegistry;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Editor.Art
{
    /// <summary>
    /// 将用户提供的火鱼九帧图集确定性切片，并生成循环动画、控制器、Prefab和敌人图集绑定。
    /// </summary>
    public static class T690FireFishAnimationAuthoring
    {
        public const string SourceTexturePath =
            "Assets/_Game/Art/Enemies/Animated/FireFish/fire_fish_idle_sheet.png";
        public const string SourceFrameDataPath =
            "Assets/_Game/Art/Enemies/Animated/FireFish/fire_fish_idle_sheet.frames.json";
        public const string AnimationClipPath =
            "Assets/_Game/Art/Enemies/Animated/FireFish/FireFishIdle.anim";
        public const string AnimatorControllerPath =
            "Assets/_Game/Art/Enemies/Animated/FireFish/FireFish.controller";
        public const string PrefabPath = "Assets/_Game/Prefabs/Actors/EnemyFireFish.prefab";

        private const string LegacySpritePath = "Assets/_Game/Art/Enemies/fire_fish.png";
        private const string EnemyAtlasPath = "Assets/_Game/Art/SpriteAtlases/Enemies.spriteatlasv2";
        private const int FrameSize = 256;
        private const int Columns = 3;
        private const int Rows = 3;
        private const int FrameCount = Columns * Rows;
        private const float PixelsPerUnit = 100f;
        private const float FrameRate = 12f;
        private static readonly Vector2 ActorPivot = new Vector2(0.5f, 0.08f);

        [MenuItem("One Stroke Demon/Art/Create or Repair T690 Fire Fish Animation")]
        public static void CreateOrRepair()
        {
            RequireSources();
            ConfigureAndSliceTexture();
            Sprite[] frames = LoadFrames();
            AnimationClip clip = CreateOrUpdateClip(frames);
            AnimatorController controller = CreateOrUpdateController(clip);
            CreateOrUpdatePrefab(frames[0], controller);

            // 旧单帧资源已由动画Prefab替代；通过AssetDatabase删除以同时维护对应.meta。
            if (AssetDatabase.LoadMainAssetAtPath(LegacySpritePath) != null &&
                !AssetDatabase.DeleteAsset(LegacySpritePath))
            {
                throw new InvalidOperationException($"Failed to delete legacy fire fish sprite '{LegacySpritePath}'.");
            }

            RebuildEnemyAtlas();
            BindFireFishRegistry();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log(
                $"T690_FIRE_FISH_AUTHORING_PASS frames={frames.Length} fps={FrameRate:0.##} " +
                $"prefab={PrefabPath}");
        }

        private static void RequireSources()
        {
            if (!File.Exists(SourceTexturePath) || !File.Exists(SourceFrameDataPath))
            {
                throw new FileNotFoundException(
                    $"T690 fire fish sources are incomplete: '{SourceTexturePath}', '{SourceFrameDataPath}'.");
            }
        }

        private static void ConfigureAndSliceTexture()
        {
            AssetDatabase.ImportAsset(
                SourceTexturePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(SourceTexturePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"TextureImporter is missing for '{SourceTexturePath}'.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
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
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();

            importer = (TextureImporter)AssetImporter.GetAtPath(SourceTexturePath);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            IReadOnlyDictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .ToDictionary(item => item.name, item => item.spriteID, StringComparer.Ordinal);
            var rects = new SpriteRect[FrameCount];
            for (int index = 0; index < FrameCount; index += 1)
            {
                int sourceRow = index / Columns;
                int column = index % Columns;
                string name = $"fire_fish_idle_{index + 1:000}";
                rects[index] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(
                        column * FrameSize,
                        (Rows - sourceRow - 1) * FrameSize,
                        FrameSize,
                        FrameSize),
                    alignment = SpriteAlignment.Custom,
                    pivot = ActorPivot,
                    border = Vector4.zero,
                    spriteID = existingIds.TryGetValue(name, out GUID spriteId)
                        ? spriteId
                        : GUID.Generate(),
                };
            }

            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static Sprite[] LoadFrames()
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(SourceTexturePath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            if (frames.Length != FrameCount)
            {
                throw new InvalidOperationException(
                    $"Fire fish sprite sheet must contain {FrameCount} frames, but found {frames.Length}.");
            }

            return frames;
        }

        private static AnimationClip CreateOrUpdateClip(IReadOnlyList<Sprite> frames)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "FireFishIdle" };
                AssetDatabase.CreateAsset(clip, AnimationClipPath);
            }

            clip.frameRate = FrameRate;
            var keyframes = new ObjectReferenceKeyframe[frames.Count + 1];
            for (int index = 0; index < frames.Count; index += 1)
            {
                keyframes[index] = new ObjectReferenceKeyframe
                {
                    time = index / FrameRate,
                    value = frames[index],
                };
            }

            keyframes[frames.Count] = new ObjectReferenceKeyframe
            {
                time = frames.Count / FrameRate,
                value = frames[0],
            };
            AnimationUtility.SetObjectReferenceCurve(
                clip,
                EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
                keyframes);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(state => string.Equals(state.name, "Idle", StringComparison.Ordinal));
            if (idle == null)
            {
                idle = stateMachine.AddState("Idle");
            }

            idle.motion = clip;
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(idle);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void CreateOrUpdatePrefab(Sprite firstFrame, RuntimeAnimatorController controller)
        {
            var root = new GameObject("EnemyFireFish");
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = firstFrame;
                renderer.sortingLayerName = "Actors";
                renderer.sortingOrder = 0;
                Animator animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                {
                    throw new InvalidOperationException($"Failed to save fire fish Prefab '{PrefabPath}'.");
                }
            }
            finally
            {
                UnityObject.DestroyImmediate(root);
            }
        }

        private static void RebuildEnemyAtlas()
        {
            UnityObject[] textures = Directory
                .GetFiles("Assets/_Game/Art/Enemies", "*.png", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path))
                .Cast<UnityObject>()
                .ToArray();
            if (textures.Length == 0 || textures.Any(texture => texture == null))
            {
                throw new InvalidOperationException("Enemy Sprite Atlas contains a missing Texture2D input.");
            }

            var atlas = new SpriteAtlasAsset();
            atlas.SetIsVariant(false);
            atlas.Add(textures);
            SpriteAtlasAsset.Save(atlas, EnemyAtlasPath);
            AssetDatabase.ImportAsset(
                EnemyAtlasPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(EnemyAtlasPath) as SpriteAtlasImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"SpriteAtlasImporter is missing for '{EnemyAtlasPath}'.");
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
                maxTextureSize = 2048,
                format = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100,
                crunchedCompression = false,
                allowsAlphaSplitting = false,
            });
            importer.SaveAndReimport();
        }

        private static void BindFireFishRegistry()
        {
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            AssetManifestConfig expected = config.GetAsset("enemy_fire_fish");
            if (!string.Equals(expected.AssetType, "Prefab", StringComparison.Ordinal) ||
                !string.Equals(expected.AddressOrPath, PrefabPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"enemy_fire_fish must be configured as Prefab at '{PrefabPath}'.");
            }

            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (registry == null || prefab == null)
            {
                throw new InvalidOperationException("Fire fish Registry binding inputs are missing.");
            }

            var entries = registry.Entries
                .Select(entry => string.Equals(entry.AssetKey, "enemy_fire_fish", StringComparison.Ordinal)
                    ? new AssetRegistryEntry(entry.AssetKey, prefab)
                    : new AssetRegistryEntry(entry.AssetKey, entry.Asset))
                .ToArray();
            if (entries.Count(entry => string.Equals(
                    entry.AssetKey,
                    "enemy_fire_fish",
                    StringComparison.Ordinal)) != 1)
            {
                throw new InvalidOperationException("AssetRegistry must contain exactly one enemy_fire_fish entry.");
            }

            registry.ReplaceEntriesForEditor(entries);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetRegistryEditorValidator.ValidateCanonical();
        }
    }
}
