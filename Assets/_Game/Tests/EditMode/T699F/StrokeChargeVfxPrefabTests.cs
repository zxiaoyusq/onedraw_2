using System;
using System.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.Art;
using OneStrokeDemon.Editor.AssetRegistry;
using OneStrokeDemon.Presentation;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T699F
{
    [Category("T699F")]
    public sealed class StrokeChargeVfxPrefabTests
    {
        [Test]
        public void ConfigRegistryAndPrefabExposeIndependentParticleTopology()
        {
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            StrokeTrailStyleConfig style = config.GetStrokeTrailStyle(
                ConfigIds.StrokeTrailStyles.StrokeTrailLightningC);
            Assert.That(style.ChargeVfxAssetKey, Is.EqualTo(ConfigIds.Assets.VfxStrokeCharge));

            AssetManifestConfig manifest = config.GetAsset(style.ChargeVfxAssetKey);
            Assert.That(manifest.AssetType, Is.EqualTo("Prefab"));
            Assert.That(manifest.AddressOrPath, Is.EqualTo(T699FStrokeChargeVfxAuthoring.PrefabPath));

            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            AssetRegistryEntry entry = registry.Entries.Single(candidate => string.Equals(
                candidate.AssetKey,
                style.ChargeVfxAssetKey,
                StringComparison.Ordinal));
            Assert.That(AssetDatabase.GetAssetPath(entry.Asset), Is.EqualTo(manifest.AddressOrPath));

            var prefab = entry.Asset as GameObject;
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<VfxPoolItem>(), Is.Not.Null);
            StrokeChargeVfxView view = prefab.GetComponent<StrokeChargeVfxView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.RingRenderers.Count, Is.EqualTo(StrokeChargeVfxView.RingRendererCount));
            Assert.That(view.RadialRenderers.Count, Is.EqualTo(StrokeChargeVfxView.RadialRendererCount));
            Assert.That(view.ParticleSystems.Count, Is.EqualTo(StrokeChargeVfxView.ParticleSystemCount));
            Assert.That(view.RingRenderers.All(renderer => !renderer.useWorldSpace), Is.True);
            Assert.That(view.RadialRenderers.All(renderer => !renderer.useWorldSpace), Is.True);
            Assert.That(
                view.ParticleSystems.All(system => !system.main.playOnAwake),
                Is.True);
            Assert.That(
                prefab.GetComponentsInChildren<SpriteRenderer>(true).Single().enabled,
                Is.False,
                "Compatibility Sprite must not render in the production charge effect.");
            Assert.That(AssetRegistryEditorValidator.ValidateCanonical().EntryCount, Is.EqualTo(78));
        }
    }
}
