using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OneStrokeDemon.Bootstrap;
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
    /// 按T694预检计划导入墨衍待机/攻击图集，并生成共享控制器、Prefab、图集与Registry绑定。
    /// </summary>
    public static class T694MoyanAnimationAuthoring
    {
        public const string PlanPath = "artifacts/evals/T694/animation-import-plan.json";
        public const string IdleTexturePath =
            "Assets/_Game/Art/Characters/Animated/Moyan/moyan_idle_sheet.png";
        public const string IdleFrameDataPath =
            "Assets/_Game/Art/Characters/Animated/Moyan/moyan_idle_sheet.frames.json";
        public const string AttackTexturePath =
            "Assets/_Game/Art/Characters/Animated/Moyan/moyan_attack_sheet.png";
        public const string AttackFrameDataPath =
            "Assets/_Game/Art/Characters/Animated/Moyan/moyan_attack_sheet.frames.json";
        public const string IdleClipPath =
            "Assets/_Game/Art/Characters/Animated/Moyan/MoyanIdle.anim";
        public const string AttackClipPath =
            "Assets/_Game/Art/Characters/Animated/Moyan/MoyanAttack.anim";
        public const string ControllerPath =
            "Assets/_Game/Art/Characters/Animated/Moyan/PlayerMoyan.controller";
        public const string PrefabPath = "Assets/_Game/Prefabs/Actors/PlayerMoyan.prefab";
        public const string AttackTriggerName = T694PlayerAnimationContract.AttackTriggerName;

        private const string IdleAnimationId = "moyan-idle";
        private const string AttackAnimationId = "moyan-attack";
        private const string CharacterAtlasPath =
            "Assets/_Game/Art/SpriteAtlases/Characters.spriteatlasv2";

        [MenuItem("One Stroke Demon/Art/Create or Repair T694 Moyan Animations")]
        public static void CreateOrRepair()
        {
            AnimationPlanDocument plan = LoadAndValidatePlan();
            AnimationPlanItem idle = plan.Require(IdleAnimationId);
            AnimationPlanItem attack = plan.Require(AttackAnimationId);

            CopySources(idle, attack);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // 先写完两张图的导入设置，再触发任一重导入，避免修复半成品时图集读到另一张压缩源图。
            ConfigureTextureImportSettings(idle);
            ConfigureTextureImportSettings(attack);
            AssetDatabase.ImportAsset(
                idle.textureAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                attack.textureAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            SliceTexture(idle);
            SliceTexture(attack);

            Sprite[] idleFrames = LoadFrames(idle);
            Sprite[] attackFrames = LoadFrames(attack);
            AnimationClip idleClip = CreateOrUpdateClip(idle, idleFrames);
            AnimationClip attackClip = CreateOrUpdateClip(attack, attackFrames);
            AnimatorController controller = CreateOrUpdateController(idleClip, attackClip);
            CreateOrUpdatePrefab(idle, idleFrames[0], controller);

            // 预检计划明确声明了旧单帧资源，只有在新Prefab完整生成后才通过AssetDatabase移除。
            if (!string.IsNullOrEmpty(idle.legacyAsset) &&
                AssetDatabase.LoadMainAssetAtPath(idle.legacyAsset) != null &&
                !AssetDatabase.DeleteAsset(idle.legacyAsset))
            {
                throw new InvalidOperationException(
                    $"Failed to delete legacy Moyan sprite '{idle.legacyAsset}'.");
            }

            RebuildCharacterAtlas();
            BindPlayerRegistry();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log(
                $"T694_MOYAN_AUTHORING_PASS idleFrames={idleFrames.Length} " +
                $"attackFrames={attackFrames.Length} fps={idle.fps:0.##} prefab={PrefabPath}");
        }

        private static AnimationPlanDocument LoadAndValidatePlan()
        {
            string absolutePlanPath = Path.GetFullPath(PlanPath);
            if (!File.Exists(absolutePlanPath))
            {
                throw new FileNotFoundException("T694 normalized animation plan is missing.", absolutePlanPath);
            }

            AnimationPlanDocument plan =
                JsonUtility.FromJson<AnimationPlanDocument>(File.ReadAllText(absolutePlanPath));
            if (plan == null || plan.schemaVersion != 1 || plan.animationCount != 2 ||
                plan.animations == null || plan.animations.Length != 2)
            {
                throw new InvalidOperationException("T694 animation plan must contain exactly two schema-1 items.");
            }

            AnimationPlanItem idle = plan.Require(IdleAnimationId);
            AnimationPlanItem attack = plan.Require(AttackAnimationId);
            RequirePlanItem(
                idle,
                ConfigIds.Assets.CharMoyanIdle,
                IdleTexturePath,
                IdleFrameDataPath,
                IdleClipPath,
                expectedFrames: 9,
                expectedLoop: true);
            RequirePlanItem(
                attack,
                ConfigIds.Assets.CharMoyanIdle,
                AttackTexturePath,
                AttackFrameDataPath,
                AttackClipPath,
                expectedFrames: 12,
                expectedLoop: false);
            if (!idle.bindRegistry || attack.bindRegistry ||
                !string.Equals(idle.controllerAssetPath, ControllerPath, StringComparison.Ordinal) ||
                !string.Equals(idle.prefabAssetPath, PrefabPath, StringComparison.Ordinal) ||
                !string.Equals(idle.atlas, CharacterAtlasPath, StringComparison.Ordinal) ||
                !string.Equals(attack.atlas, CharacterAtlasPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "T694 plan must bind only the idle identity to the shared player Prefab and Characters atlas.");
            }

            return plan;
        }

        private static void RequirePlanItem(
            AnimationPlanItem item,
            string expectedAssetKey,
            string expectedTexture,
            string expectedFrameData,
            string expectedClip,
            int expectedFrames,
            bool expectedLoop)
        {
            if (!string.Equals(item.assetKey, expectedAssetKey, StringComparison.Ordinal) ||
                !string.Equals(item.textureAssetPath, expectedTexture, StringComparison.Ordinal) ||
                !string.Equals(item.frameDataAssetPath, expectedFrameData, StringComparison.Ordinal) ||
                !string.Equals(item.clipAssetPath, expectedClip, StringComparison.Ordinal) ||
                item.frames == null || item.frames.Length != expectedFrames ||
                item.loop != expectedLoop ||
                Math.Abs(item.fps - 12f) > 0.001f ||
                Math.Abs(item.pixelsPerUnit - 100f) > 0.001f ||
                item.pivot == null ||
                Math.Abs(item.pivot.x - 0.5f) > 0.001f ||
                Math.Abs(item.pivot.y - 0.08f) > 0.001f ||
                !string.Equals(item.sortingLayer, "Actors", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"T694 plan item '{item.id}' violates the approved import contract.");
            }
        }

        private static void CopySources(params AnimationPlanItem[] items)
        {
            foreach (AnimationPlanItem item in items)
            {
                EnsureAssetFolder(Path.GetDirectoryName(item.textureAssetPath)?.Replace('\\', '/'));
                CopySource(item.sourceTexture, item.textureAssetPath);
                CopySource(item.frameData, item.frameDataAssetPath);
            }
        }

        private static void CopySource(string sourcePath, string targetAssetPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("T694 source file is missing.", sourcePath);
            }

            string destination = Path.GetFullPath(targetAssetPath);
            if (string.Equals(
                    Path.GetFullPath(sourcePath),
                    destination,
                    StringComparison.Ordinal))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException($"Target has no directory: '{targetAssetPath}'."));
            File.Copy(sourcePath, destination, overwrite: true);
        }

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
            // 角色动画进入Sprite Atlas前保持源像素无损，避免透明边缘被二次压缩并消除图集导入警告。
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            AssetDatabase.WriteImportSettingsIfDirty(item.textureAssetPath);
        }

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
                        $"T694 plan frame order is invalid for '{item.id}' at index {index}.");
                }

                string spriteName = frame.spriteName;
                rects[index] = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(
                        frame.unityRectBottomLeft.x,
                        frame.unityRectBottomLeft.y,
                        frame.unityRectBottomLeft.w,
                        frame.unityRectBottomLeft.h),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(item.pivot.x, item.pivot.y),
                    border = Vector4.zero,
                    spriteID = existingIds.TryGetValue(spriteName, out GUID spriteId)
                        ? spriteId
                        : GUID.Generate(),
                };
            }

            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

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

            // 待机在末端回到首帧；非循环攻击重复末帧一次，保证最后源帧也完整显示一帧。
            keyframes[frames.Count] = new ObjectReferenceKeyframe
            {
                time = frames.Count / item.fps,
                value = item.loop ? frames[0] : frames[frames.Count - 1],
            };
            AnimationUtility.SetObjectReferenceCurve(
                clip,
                EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
                keyframes);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = item.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateOrUpdateController(
            AnimationClip idleClip,
            AnimationClip attackClip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            for (int index = controller.parameters.Length - 1; index >= 0; index -= 1)
            {
                AnimatorControllerParameter parameter = controller.parameters[index];
                if (string.Equals(parameter.name, AttackTriggerName, StringComparison.Ordinal) &&
                    parameter.type != AnimatorControllerParameterType.Trigger)
                {
                    controller.RemoveParameter(index);
                }
            }

            if (!controller.parameters.Any(parameter =>
                    string.Equals(parameter.name, AttackTriggerName, StringComparison.Ordinal)))
            {
                controller.AddParameter(AttackTriggerName, AnimatorControllerParameterType.Trigger);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = RequireState(stateMachine, "Idle");
            AnimatorState attack = RequireState(stateMachine, "Attack");
            idle.motion = idleClip;
            attack.motion = attackClip;
            stateMachine.defaultState = idle;

            foreach (AnimatorStateTransition transition in
                     stateMachine.anyStateTransitions.Where(item => item.destinationState == attack).ToArray())
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }

            foreach (AnimatorStateTransition transition in
                     attack.transitions.Where(item => item.destinationState == idle).ToArray())
            {
                attack.RemoveTransition(transition);
            }

            AnimatorStateTransition toAttack = stateMachine.AddAnyStateTransition(attack);
            toAttack.hasExitTime = false;
            toAttack.duration = 0f;
            toAttack.canTransitionToSelf = true;
            toAttack.AddCondition(
                AnimatorConditionMode.If,
                0f,
                AttackTriggerName);
            AnimatorStateTransition toIdle = attack.AddTransition(idle);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 1f;
            toIdle.duration = 0f;
            toIdle.hasFixedDuration = true;

            EditorUtility.SetDirty(idle);
            EditorUtility.SetDirty(attack);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState RequireState(AnimatorStateMachine stateMachine, string stateName)
        {
            AnimatorState state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.name, stateName, StringComparison.Ordinal));
            return state ?? stateMachine.AddState(stateName);
        }

        private static void CreateOrUpdatePrefab(
            AnimationPlanItem idle,
            Sprite firstFrame,
            RuntimeAnimatorController controller)
        {
            var root = new GameObject("PlayerMoyan");
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = firstFrame;
                renderer.sortingLayerName = idle.sortingLayer;
                renderer.sortingOrder = 5;
                Animator animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to save Moyan Prefab '{PrefabPath}'.");
                }
            }
            finally
            {
                UnityObject.DestroyImmediate(root);
            }
        }

        private static void RebuildCharacterAtlas()
        {
            UnityObject[] textures = Directory
                .GetFiles("Assets/_Game/Art/Characters", "*.png", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path))
                .Cast<UnityObject>()
                .ToArray();
            if (textures.Length == 0 || textures.Any(texture => texture == null))
            {
                throw new InvalidOperationException(
                    "Characters Sprite Atlas contains a missing Texture2D input.");
            }

            var atlas = new SpriteAtlasAsset();
            atlas.SetIsVariant(false);
            atlas.Add(textures);
            SpriteAtlasAsset.Save(atlas, CharacterAtlasPath);
            AssetDatabase.ImportAsset(
                CharacterAtlasPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(CharacterAtlasPath) as SpriteAtlasImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"SpriteAtlasImporter is missing for '{CharacterAtlasPath}'.");
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

        private static void BindPlayerRegistry()
        {
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            AssetManifestConfig expected = config.GetAsset(ConfigIds.Assets.CharMoyanIdle);
            if (!string.Equals(expected.AssetType, "Prefab", StringComparison.Ordinal) ||
                !string.Equals(expected.AddressOrPath, PrefabPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{ConfigIds.Assets.CharMoyanIdle} must be configured as Prefab at '{PrefabPath}'.");
            }

            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (registry == null || prefab == null)
            {
                throw new InvalidOperationException("Moyan Registry binding inputs are missing.");
            }

            var entries = registry.Entries
                .Select(entry => string.Equals(
                    entry.AssetKey,
                    ConfigIds.Assets.CharMoyanIdle,
                    StringComparison.Ordinal)
                    ? new AssetRegistryEntry(entry.AssetKey, prefab)
                    : new AssetRegistryEntry(entry.AssetKey, entry.Asset))
                .ToArray();
            if (entries.Count(entry => string.Equals(
                    entry.AssetKey,
                    ConfigIds.Assets.CharMoyanIdle,
                    StringComparison.Ordinal)) != 1)
            {
                throw new InvalidOperationException(
                    $"AssetRegistry must contain exactly one {ConfigIds.Assets.CharMoyanIdle} entry.");
            }

            registry.ReplaceEntriesForEditor(entries);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetRegistryEditorValidator.ValidateCanonical();
        }

        private static void EnsureAssetFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            if (separator <= 0)
            {
                throw new InvalidOperationException($"Invalid Unity asset folder '{path}'.");
            }

            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        [Serializable]
        private sealed class AnimationPlanDocument
        {
            public int schemaVersion;
            public int animationCount;
            public AnimationPlanItem[] animations;

            public AnimationPlanItem Require(string animationId)
            {
                AnimationPlanItem[] matches = animations
                    .Where(item => item != null &&
                        string.Equals(item.id, animationId, StringComparison.Ordinal))
                    .ToArray();
                if (matches.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"T694 plan must contain exactly one '{animationId}' item.");
                }

                return matches[0];
            }
        }

        [Serializable]
        private sealed class AnimationPlanItem
        {
            public string id;
            public string assetKey;
            public bool bindRegistry;
            public string sourceTexture;
            public string frameData;
            public string textureAssetPath;
            public string frameDataAssetPath;
            public string clipAssetPath;
            public string controllerAssetPath;
            public string prefabAssetPath;
            public string legacyAsset;
            public string atlas;
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
