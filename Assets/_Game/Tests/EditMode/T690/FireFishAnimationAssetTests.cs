using System;
using System.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.Art;
using OneStrokeDemon.Editor.AssetRegistry;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T690
{
    /// <summary>验证用户提供的火鱼九帧图集及其运行时资源绑定。</summary>
    [Category("T690")]
    public sealed class FireFishAnimationAssetTests
    {
        [Test]
        public void FireFishSheetClipPrefabAndRegistryStayConsistent()
        {
            var importer = AssetImporter.GetAtPath(
                T690FireFishAnimationAuthoring.SourceTexturePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f));

            Sprite[] frames = AssetDatabase
                .LoadAllAssetsAtPath(T690FireFishAnimationAuthoring.SourceTexturePath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(frames, Has.Length.EqualTo(9));
            for (int index = 0; index < frames.Length; index += 1)
            {
                Assert.That(frames[index].name, Is.EqualTo($"fire_fish_idle_{index + 1:000}"));
                Assert.That(frames[index].rect.width, Is.EqualTo(256f));
                Assert.That(frames[index].rect.height, Is.EqualTo(256f));
                Assert.That(frames[index].rect.x, Is.EqualTo((index % 3) * 256f));
                Assert.That(frames[index].rect.y, Is.EqualTo((2 - index / 3) * 256f));
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                T690FireFishAnimationAuthoring.AnimationClipPath);
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.frameRate, Is.EqualTo(12f));
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.True);
            EditorCurveBinding binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            Assert.That(keys, Has.Length.EqualTo(10));
            Assert.That(keys.Take(9).Select(key => key.value), Is.EqualTo(frames));
            Assert.That(keys[9].value, Is.SameAs(frames[0]));

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                T690FireFishAnimationAuthoring.PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<SpriteRenderer>().sprite, Is.SameAs(frames[0]));
            Assert.That(prefab.GetComponent<Animator>().runtimeAnimatorController, Is.Not.Null);

            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            AssetManifestConfig manifest = config.GetAsset(ConfigIds.Assets.EnemyFireFish);
            Assert.That(manifest.AssetType, Is.EqualTo("Prefab"));
            Assert.That(manifest.AddressOrPath,
                Is.EqualTo(T690FireFishAnimationAuthoring.PrefabPath));
            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            AssetRegistryEntry entry = registry.Entries.Single(item =>
                string.Equals(item.AssetKey, ConfigIds.Assets.EnemyFireFish, StringComparison.Ordinal));
            Assert.That(entry.Asset, Is.SameAs(prefab));
            Assert.That(AssetRegistryEditorValidator.ValidateCanonical().EntryCount,
                Is.EqualTo(config.GetAssetManifest().Count));
        }
    }
}
