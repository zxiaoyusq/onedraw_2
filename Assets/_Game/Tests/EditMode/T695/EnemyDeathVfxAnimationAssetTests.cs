using System;
using System.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.Art;
using OneStrokeDemon.Editor.AssetRegistry;
using OneStrokeDemon.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D;

namespace OneStrokeDemon.Tests.EditMode.T695
{
    /// <summary>验证怪物死亡图集、动画资产、配置和Registry保持同一份资源契约。</summary>
    [Category("T695")]
    public sealed class EnemyDeathVfxAnimationAssetTests
    {
        [Test]
        public void EnemyDeathSheetClipControllerPrefabConfigAtlasAndRegistryStayConsistent()
        {
            var importer = AssetImporter.GetAtPath(
                T695EnemyDeathVfxAuthoring.TexturePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f));
            Assert.That(
                importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.maxTextureSize, Is.EqualTo(1024));
            Assert.That(importer.mipmapEnabled, Is.False);

            Sprite[] frames = AssetDatabase
                .LoadAllAssetsAtPath(T695EnemyDeathVfxAuthoring.TexturePath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(frames, Has.Length.EqualTo(11));
            for (int index = 0; index < frames.Length; index += 1)
            {
                Assert.That(frames[index].name, Is.EqualTo($"enemy_death_{index + 1:000}"));
                Assert.That(frames[index].rect.width, Is.EqualTo(256f));
                Assert.That(frames[index].rect.height, Is.EqualTo(256f));
                Assert.That(frames[index].rect.x, Is.EqualTo((index % 4) * 256f));
                Assert.That(frames[index].rect.y, Is.EqualTo((2 - index / 4) * 256f));
                Assert.That(frames[index].pivot.x, Is.EqualTo(128f).Within(0.01f));
                Assert.That(frames[index].pivot.y, Is.EqualTo(128f).Within(0.01f));
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                T695EnemyDeathVfxAuthoring.ClipPath);
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.frameRate, Is.EqualTo(12f));
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.False);
            EditorCurveBinding binding =
                AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            ObjectReferenceKeyframe[] keys =
                AnimationUtility.GetObjectReferenceCurve(clip, binding);
            Assert.That(keys, Has.Length.EqualTo(12));
            Assert.That(keys.Take(11).Select(key => key.value), Is.EqualTo(frames));
            Assert.That(keys[11].value, Is.SameAs(frames[10]));
            Assert.That(keys[11].time, Is.EqualTo(11f / 12f).Within(0.0001f));

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                T695EnemyDeathVfxAuthoring.ControllerPath);
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.parameters, Is.Empty);
            AnimatorStateMachine stateMachine = controller.layers.Single().stateMachine;
            AnimatorState play = stateMachine.states.Select(child => child.state).Single();
            Assert.That(play.name, Is.EqualTo("Play"));
            Assert.That(play.motion, Is.SameAs(clip));
            Assert.That(stateMachine.defaultState, Is.SameAs(play));
            Assert.That(stateMachine.anyStateTransitions, Is.Empty);
            Assert.That(play.transitions, Is.Empty);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                T695EnemyDeathVfxAuthoring.PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.SameAs(frames[0]));
            Assert.That(renderer.sortingLayerName, Is.EqualTo("VFX"));
            Assert.That(prefab.GetComponent<VfxPoolItem>(), Is.Not.Null);
            Assert.That(
                prefab.GetComponent<Animator>().runtimeAnimatorController,
                Is.SameAs(controller));

            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            VfxCueConfig cue = config.GetVfxCue(ConfigIds.VfxCues.VfxEnemyDeath);
            Assert.That(cue.AssetKey, Is.EqualTo(ConfigIds.Assets.VfxEnemyDeath));
            Assert.That(cue.LifeSec, Is.EqualTo(0.92f).Within(0.0001f));
            Assert.That(cue.PoolPrewarm, Is.EqualTo(6L));
            Assert.That(cue.FollowTarget, Is.False);
            Assert.That(cue.SortingLayer, Is.EqualTo("VFX"));
            Assert.That(cue.SortingOrder, Is.EqualTo(40L));
            FeedbackCueConfig feedback = config.GetFeedbackCue(
                ConfigIds.FeedbackCues.FeedbackEnemyDeath);
            Assert.That(feedback.VfxKey, Is.EqualTo(ConfigIds.VfxCues.VfxEnemyDeath));
            AssetManifestConfig manifest = config.GetAsset(ConfigIds.Assets.VfxEnemyDeath);
            Assert.That(manifest.AssetType, Is.EqualTo("Prefab"));
            Assert.That(
                manifest.AddressOrPath,
                Is.EqualTo(T695EnemyDeathVfxAuthoring.PrefabPath));

            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            AssetRegistryEntry entry = registry.Entries.Single(item =>
                string.Equals(
                    item.AssetKey,
                    ConfigIds.Assets.VfxEnemyDeath,
                    StringComparison.Ordinal));
            Assert.That(entry.Asset, Is.SameAs(prefab));
            Assert.That(
                AssetRegistryEditorValidator.ValidateCanonical().EntryCount,
                Is.EqualTo(77));

            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(
                T630ArtAssetPaths.VfxAtlas);
            Assert.That(atlas, Is.Not.Null);
            Assert.That(atlas.CanBindTo(frames[0]), Is.True);
            Assert.That(atlas.CanBindTo(frames[10]), Is.True);
        }
    }
}
