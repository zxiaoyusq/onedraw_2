using System;
using System.Linq;
using NUnit.Framework;
using OneStrokeDemon.Bootstrap;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.Art;
using OneStrokeDemon.Editor.AssetRegistry;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T694
{
    /// <summary>验证墨衍待机/攻击图集、状态机、Prefab和配置注册表保持同一份资源契约。</summary>
    [Category("T694")]
    public sealed class MoyanAnimationAssetTests
    {
        [Test]
        public void MoyanSheetsClipsControllerPrefabAndRegistryStayConsistent()
        {
            Sprite[] idleFrames = AssertSheet(
                T694MoyanAnimationAuthoring.IdleTexturePath,
                "moyan_idle",
                columns: 3,
                rows: 3,
                expectedFrameCount: 9);
            Sprite[] attackFrames = AssertSheet(
                T694MoyanAnimationAuthoring.AttackTexturePath,
                "moyan_attack",
                columns: 4,
                rows: 3,
                expectedFrameCount: 12);

            AnimationClip idleClip = AssertClip(
                T694MoyanAnimationAuthoring.IdleClipPath,
                idleFrames,
                loop: true);
            AnimationClip attackClip = AssertClip(
                T694MoyanAnimationAuthoring.AttackClipPath,
                attackFrames,
                loop: false);
            AssertController(idleClip, attackClip);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                T694MoyanAnimationAuthoring.PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.SameAs(idleFrames[0]));
            Assert.That(renderer.sortingLayerName, Is.EqualTo("Actors"));
            Assert.That(renderer.sortingOrder, Is.EqualTo(5));
            Assert.That(
                prefab.GetComponent<Animator>().runtimeAnimatorController,
                Is.SameAs(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    T694MoyanAnimationAuthoring.ControllerPath)));

            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            AssetManifestConfig manifest = config.GetAsset(ConfigIds.Assets.CharMoyanIdle);
            Assert.That(manifest.AssetType, Is.EqualTo("Prefab"));
            Assert.That(
                manifest.AddressOrPath,
                Is.EqualTo(T694MoyanAnimationAuthoring.PrefabPath));
            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            AssetRegistryEntry entry = registry.Entries.Single(item =>
                string.Equals(
                    item.AssetKey,
                    ConfigIds.Assets.CharMoyanIdle,
                    StringComparison.Ordinal));
            Assert.That(entry.Asset, Is.SameAs(prefab));
            Assert.That(
                AssetRegistryEditorValidator.ValidateCanonical().EntryCount,
                Is.EqualTo(config.GetAssetManifest().Count));
            Assert.That(
                AssetDatabase.LoadMainAssetAtPath(
                    "Assets/_Game/Art/Characters/Moyan/moyan_idle.png"),
                Is.Null,
                "新动画Prefab完成注册后，旧单帧资源不应继续充当第二份主角资源。");
        }

        private static Sprite[] AssertSheet(
            string texturePath,
            string spritePrefix,
            int columns,
            int rows,
            int expectedFrameCount)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f));
            Assert.That(
                importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.maxTextureSize, Is.EqualTo(1024));
            Assert.That(importer.mipmapEnabled, Is.False);

            Sprite[] frames = AssetDatabase
                .LoadAllAssetsAtPath(texturePath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(frames, Has.Length.EqualTo(expectedFrameCount));
            for (int index = 0; index < frames.Length; index += 1)
            {
                Assert.That(frames[index].name, Is.EqualTo($"{spritePrefix}_{index + 1:000}"));
                Assert.That(frames[index].rect.width, Is.EqualTo(256f));
                Assert.That(frames[index].rect.height, Is.EqualTo(256f));
                Assert.That(frames[index].rect.x, Is.EqualTo((index % columns) * 256f));
                Assert.That(frames[index].rect.y, Is.EqualTo((rows - 1 - index / columns) * 256f));
                Assert.That(frames[index].pivot.x, Is.EqualTo(128f).Within(0.01f));
                Assert.That(frames[index].pivot.y, Is.EqualTo(20.48f).Within(0.01f));
            }

            return frames;
        }

        private static AnimationClip AssertClip(
            string clipPath,
            Sprite[] frames,
            bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.frameRate, Is.EqualTo(12f));
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.EqualTo(loop));
            EditorCurveBinding binding =
                AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            ObjectReferenceKeyframe[] keys =
                AnimationUtility.GetObjectReferenceCurve(clip, binding);
            Assert.That(keys, Has.Length.EqualTo(frames.Length + 1));
            Assert.That(keys.Take(frames.Length).Select(key => key.value), Is.EqualTo(frames));
            Assert.That(
                keys[frames.Length].value,
                Is.SameAs(loop ? frames[0] : frames[frames.Length - 1]));
            Assert.That(
                keys[frames.Length].time,
                Is.EqualTo(frames.Length / 12f).Within(0.0001f));
            return clip;
        }

        private static void AssertController(AnimationClip idleClip, AnimationClip attackClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                T694MoyanAnimationAuthoring.ControllerPath);
            Assert.That(controller, Is.Not.Null);
            Assert.That(
                controller.parameters.Single(parameter =>
                    parameter.name == T694PlayerAnimationContract.AttackTriggerName).type,
                Is.EqualTo(AnimatorControllerParameterType.Trigger));

            AnimatorStateMachine stateMachine = controller.layers.Single().stateMachine;
            AnimatorState idle = stateMachine.states
                .Select(child => child.state)
                .Single(state => state.name == "Idle");
            AnimatorState attack = stateMachine.states
                .Select(child => child.state)
                .Single(state => state.name == "Attack");
            Assert.That(stateMachine.defaultState, Is.SameAs(idle));
            Assert.That(idle.motion, Is.SameAs(idleClip));
            Assert.That(attack.motion, Is.SameAs(attackClip));

            AnimatorStateTransition enterAttack = stateMachine.anyStateTransitions
                .Single(transition => transition.destinationState == attack);
            Assert.That(enterAttack.hasExitTime, Is.False);
            Assert.That(enterAttack.duration, Is.Zero);
            Assert.That(
                enterAttack.conditions.Single().parameter,
                Is.EqualTo(T694PlayerAnimationContract.AttackTriggerName));
            AnimatorStateTransition returnIdle = attack.transitions
                .Single(transition => transition.destinationState == idle);
            Assert.That(returnIdle.hasExitTime, Is.True);
            Assert.That(returnIdle.exitTime, Is.EqualTo(1f));
            Assert.That(returnIdle.duration, Is.Zero);
        }
    }
}
