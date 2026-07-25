using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.AssetRegistry;
using OneStrokeDemon.Presentation;
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
    /// 按T695预检计划生成十一帧怪物死亡特效，并完成VFX图集与Registry绑定。
    /// </summary>
    public static class T695EnemyDeathVfxAuthoring
    {
        public const string PlanPath = "artifacts/evals/T695/animation-import-plan.json";
        public const string TexturePath =
            "Assets/_Game/Art/VFX/Animated/EnemyDeath/enemy_death_sheet.png";
        public const string FrameDataPath =
            "Assets/_Game/Art/VFX/Animated/EnemyDeath/enemy_death_sheet.frames.json";
        public const string ClipPath =
            "Assets/_Game/Art/VFX/Animated/EnemyDeath/EnemyDeath.anim";
        public const string ControllerPath =
            "Assets/_Game/Art/VFX/Animated/EnemyDeath/EnemyDeath.controller";
        public const string PrefabPath =
            "Assets/_Game/Art/VFX/Animated/EnemyDeath/vfx_enemy_death.prefab";

        private const string AnimationId = "enemy-death";
        private const string StateName = "Play";
        private const string VfxAtlasPath = "Assets/_Game/Art/SpriteAtlases/VFX.spriteatlasv2";

        [MenuItem("One Stroke Demon/Art/Create or Repair T695 Enemy Death VFX")]
        public static void CreateOrRepair()
        {
            AnimationPlanItem item = LoadAndValidatePlan();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextureImportSettings(item);
            AssetDatabase.ImportAsset(
                item.textureAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            SliceTexture(item);

            Sprite[] frames = LoadFrames(item);
            AnimationClip clip = CreateOrUpdateClip(item, frames);
            AnimatorController controller = CreateOrUpdateController(clip);
            CreateOrUpdatePrefab(item, frames[0], controller);
            RebuildVfxAtlas();
            BindRegistry();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log(
                $"T695_ENEMY_DEATH_VFX_AUTHORING_PASS frames={frames.Length} " +
                $"fps={item.fps:0.##} loop={item.loop} prefab={PrefabPath}");
        }

        // 读取规范化计划并拒绝任何与已审批特效合同不一致的输入。
        private static AnimationPlanItem LoadAndValidatePlan()
        {
            string absolutePlanPath = Path.GetFullPath(PlanPath);
            if (!File.Exists(absolutePlanPath))
            {
                throw new FileNotFoundException("T695 normalized animation plan is missing.", absolutePlanPath);
            }

            AnimationPlanDocument plan =
                JsonUtility.FromJson<AnimationPlanDocument>(File.ReadAllText(absolutePlanPath));
            if (plan == null || plan.schemaVersion != 2 || plan.animationCount != 1 ||
                plan.animations == null || plan.animations.Length != 1)
            {
                throw new InvalidOperationException(
                    "T695 animation plan must contain exactly one schema-2 item.");
            }

            AnimationPlanItem item = plan.animations[0];
            if (item == null ||
                !string.Equals(item.id, AnimationId, StringComparison.Ordinal) ||
                !string.Equals(item.assetKey, ConfigIds.Assets.VfxEnemyDeath, StringComparison.Ordinal) ||
                !item.bindRegistry ||
                !string.Equals(item.textureAssetPath, TexturePath, StringComparison.Ordinal) ||
                !string.Equals(item.frameDataAssetPath, FrameDataPath, StringComparison.Ordinal) ||
                !string.Equals(item.clipAssetPath, ClipPath, StringComparison.Ordinal) ||
                !string.Equals(item.controllerAssetPath, ControllerPath, StringComparison.Ordinal) ||
                !string.Equals(item.prefabAssetPath, PrefabPath, StringComparison.Ordinal) ||
                !string.Equals(item.atlas, VfxAtlasPath, StringComparison.Ordinal) ||
                !string.Equals(item.stateName, StateName, StringComparison.Ordinal) ||
                !item.defaultState ||
                item.loop ||
                Math.Abs(item.fps - 12f) > 0.001f ||
                Math.Abs(item.pixelsPerUnit - 100f) > 0.001f ||
                item.pivot == null ||
                Math.Abs(item.pivot.x - 0.5f) > 0.001f ||
                Math.Abs(item.pivot.y - 0.5f) > 0.001f ||
                !string.Equals(item.sortingLayer, "VFX", StringComparison.Ordinal) ||
                item.frames == null ||
                item.frames.Length != 11)
            {
                throw new InvalidOperationException(
                    "T695 animation plan violates the approved enemy-death VFX contract.");
            }

            RequireSha256(item.sourceTexture, item.sourceTextureSha256);
            RequireSha256(item.frameData, item.frameDataSha256);
            return item;
        }

        // 校验项目内已落位源文件的SHA-256，避免Unity生成阶段消费漂移素材。
        private static void RequireSha256(string path, string expected)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("T695 staged source file is missing.", path);
            }

            using SHA256 sha256 = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            string actual = BitConverter.ToString(sha256.ComputeHash(stream))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"T695 staged source hash mismatch for '{path}': expected={expected} actual={actual}");
            }
        }

        // 配置多Sprite无损导入；切片数据由专用计划维护，不能被通用单图作者工具覆盖。
        private static void ConfigureTextureImportSettings(AnimationPlanItem item)
        {
            var importer = AssetImporter.GetAtPath(item.textureAssetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"TextureImporter is missing for '{item.textureAssetPath}'.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = item.pixelsPerUnit;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            AssetDatabase.WriteImportSettingsIfDirty(item.textureAssetPath);
        }

        // 按计划中的Unity左下角坐标切片，并在重复执行时保留既有Sprite GUID。
        private static void SliceTexture(AnimationPlanItem item)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(item.textureAssetPath);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            IReadOnlyDictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .ToDictionary(frame => frame.name, frame => frame.spriteID, StringComparer.Ordinal);
            var rects = new SpriteRect[item.frames.Length];
            for (int index = 0; index < item.frames.Length; index += 1)
            {
                AnimationPlanFrame frame = item.frames[index];
                if (frame.index != index + 1 || frame.unityRectBottomLeft == null)
                {
                    throw new InvalidOperationException(
                        $"T695 plan frame order is invalid at index {index}.");
                }

                rects[index] = new SpriteRect
                {
                    name = frame.spriteName,
                    rect = new Rect(
                        frame.unityRectBottomLeft.x,
                        frame.unityRectBottomLeft.y,
                        frame.unityRectBottomLeft.w,
                        frame.unityRectBottomLeft.h),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(item.pivot.x, item.pivot.y),
                    border = Vector4.zero,
                    spriteID = existingIds.TryGetValue(frame.spriteName, out GUID spriteId)
                        ? spriteId
                        : GUID.Generate(),
                };
            }

            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        // 以规范化帧名加载Sprite，避免依赖AssetDatabase返回顺序。
        private static Sprite[] LoadFrames(AnimationPlanItem item)
        {
            IReadOnlyDictionary<string, Sprite> sprites = AssetDatabase
                .LoadAllAssetsAtPath(item.textureAssetPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, StringComparer.Ordinal);
            var ordered = new Sprite[item.frames.Length];
            for (int index = 0; index < item.frames.Length; index += 1)
            {
                string spriteName = item.frames[index].spriteName;
                if (!sprites.TryGetValue(spriteName, out Sprite sprite))
                {
                    throw new InvalidOperationException(
                        $"Sprite '{spriteName}' is missing from '{item.textureAssetPath}'.");
                }

                ordered[index] = sprite;
            }

            return ordered;
        }

        // 生成12 FPS非循环序列；末帧重复一个采样间隔，确保第十一帧完整显示。
        private static AnimationClip CreateOrUpdateClip(
            AnimationPlanItem item,
            IReadOnlyList<Sprite> frames)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(item.clipAssetPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(item.clipAssetPath) };
                AssetDatabase.CreateAsset(clip, item.clipAssetPath);
            }

            clip.frameRate = item.fps;
            var keyframes = new ObjectReferenceKeyframe[frames.Count + 1];
            for (int index = 0; index < frames.Count; index += 1)
            {
                keyframes[index] = new ObjectReferenceKeyframe
                {
                    time = index / item.fps,
                    value = frames[index],
                };
            }

            keyframes[frames.Count] = new ObjectReferenceKeyframe
            {
                time = frames.Count / item.fps,
                value = frames[frames.Count - 1],
            };
            AnimationUtility.SetObjectReferenceCurve(
                clip,
                EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
                keyframes);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        // 创建仅含默认Play状态的控制器，死亡表现的生命周期仍由配置化VFX对象池负责。
        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            while (controller.parameters.Length > 0)
            {
                controller.RemoveParameter(0);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState play = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(state => string.Equals(state.name, StateName, StringComparison.Ordinal));
            if (play == null)
            {
                play = stateMachine.AddState(StateName);
            }

            foreach (ChildAnimatorState child in stateMachine.states
                         .Where(child => child.state != play)
                         .ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }

            foreach (AnimatorStateTransition transition in play.transitions.ToArray())
            {
                play.RemoveTransition(transition);
            }

            play.motion = clip;
            stateMachine.defaultState = play;
            EditorUtility.SetDirty(play);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // Prefab只保存Unity资源引用；尺寸、层级、寿命与跟随策略继续由配置表在运行时覆盖。
        private static void CreateOrUpdatePrefab(
            AnimationPlanItem item,
            Sprite firstFrame,
            RuntimeAnimatorController controller)
        {
            var root = new GameObject("vfx_enemy_death");
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = firstFrame;
                renderer.sortingLayerName = item.sortingLayer;
                renderer.sortingOrder = 0;
                Animator animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                root.AddComponent<VfxPoolItem>();
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to save enemy-death VFX Prefab '{PrefabPath}'.");
                }
            }
            finally
            {
                UnityObject.DestroyImmediate(root);
            }
        }

        // 重建VFX图集并纳入通用特效、动画特效和投射物纹理。
        private static void RebuildVfxAtlas()
        {
            UnityObject[] textures = Directory
                .GetFiles("Assets/_Game/Art", "*.png", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .Where(path =>
                    path.Contains("/VFX/Sprites/", StringComparison.Ordinal) ||
                    path.Contains("/VFX/Animated/", StringComparison.Ordinal) ||
                    Path.GetFileName(path).StartsWith("proj_", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path))
                .Cast<UnityObject>()
                .ToArray();
            if (textures.Length == 0 || textures.Any(texture => texture == null))
            {
                throw new InvalidOperationException(
                    "VFX Sprite Atlas contains a missing Texture2D input.");
            }

            var atlas = new SpriteAtlasAsset();
            atlas.SetIsVariant(false);
            atlas.Add(textures);
            SpriteAtlasAsset.Save(atlas, VfxAtlasPath);
            AssetDatabase.ImportAsset(
                VfxAtlasPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(VfxAtlasPath) as SpriteAtlasImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"SpriteAtlasImporter is missing for '{VfxAtlasPath}'.");
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

        // 先补齐新增配置键，再把死亡特效键替换为专用动画Prefab并执行完整Registry校验。
        private static void BindRegistry()
        {
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            AssetManifestConfig expected = config.GetAsset(ConfigIds.Assets.VfxEnemyDeath);
            if (!string.Equals(expected.AssetType, "Prefab", StringComparison.Ordinal) ||
                !string.Equals(expected.AddressOrPath, PrefabPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{ConfigIds.Assets.VfxEnemyDeath} must be configured as Prefab at '{PrefabPath}'.");
            }

            AssetRegistryAuthoring.CreateOrRepairCanonicalRegistry();
            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (registry == null || prefab == null)
            {
                throw new InvalidOperationException("Enemy-death Registry binding inputs are missing.");
            }

            var entries = registry.Entries
                .Select(entry => string.Equals(
                    entry.AssetKey,
                    ConfigIds.Assets.VfxEnemyDeath,
                    StringComparison.Ordinal)
                    ? new AssetRegistryEntry(entry.AssetKey, prefab)
                    : new AssetRegistryEntry(entry.AssetKey, entry.Asset))
                .ToArray();
            if (entries.Count(entry => string.Equals(
                    entry.AssetKey,
                    ConfigIds.Assets.VfxEnemyDeath,
                    StringComparison.Ordinal)) != 1)
            {
                throw new InvalidOperationException(
                    $"AssetRegistry must contain exactly one {ConfigIds.Assets.VfxEnemyDeath} entry.");
            }

            registry.ReplaceEntriesForEditor(entries);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetRegistryEditorValidator.ValidateCanonical();
        }

        [Serializable]
        private sealed class AnimationPlanDocument
        {
            public int schemaVersion;
            public int animationCount;
            public AnimationPlanItem[] animations;
        }

        [Serializable]
        private sealed class AnimationPlanItem
        {
            public string id;
            public string assetKey;
            public bool bindRegistry;
            public string sourceTexture;
            public string frameData;
            public string sourceTextureSha256;
            public string frameDataSha256;
            public string textureAssetPath;
            public string frameDataAssetPath;
            public string clipAssetPath;
            public string controllerAssetPath;
            public string prefabAssetPath;
            public string atlas;
            public string stateName;
            public bool defaultState;
            public bool loop;
            public float fps;
            public float pixelsPerUnit;
            public string sortingLayer;
            public AnimationPlanPivot pivot;
            public AnimationPlanFrame[] frames;
        }

        [Serializable]
        private sealed class AnimationPlanPivot
        {
            public float x;
            public float y;
        }

        [Serializable]
        private sealed class AnimationPlanFrame
        {
            public int index;
            public string spriteName;
            public AnimationPlanRect unityRectBottomLeft;
        }

        [Serializable]
        private sealed class AnimationPlanRect
        {
            public int x;
            public int y;
            public int w;
            public int h;
        }
    }
}
