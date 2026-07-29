using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor;
using OneStrokeDemon.Editor.AssetRegistry;
using OneStrokeDemon.Presentation;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode
{
    [Category("T698")]
    public sealed class T698StrokeTrailStyleTests
    {
        [Test]
        public void CanonicalConfigAndPrefabResolveLightningStyleC()
        {
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            StanceConfig blade = config.GetStance(ConfigIds.Stances.StanceBlade);
            StrokeTrailStyleConfig row = config.GetStrokeTrailStyle(blade.StrokeTrailStyleId);
            StrokeTrailStyle style = StrokeTrailSettingsFactory.CreateStyle(
                config,
                blade.StanceId,
                ConfigIds.VfxCues.VfxSlash);
            VfxCueConfig cue = config.GetVfxCue(ConfigIds.VfxCues.VfxSlash);
            AssetManifestConfig asset = config.GetAsset(cue.AssetKey);

            Assert.That(row.StyleId, Is.EqualTo(ConfigIds.StrokeTrailStyles.StrokeTrailLightningC));
            Assert.That(style.StyleId, Is.EqualTo(row.StyleId));
            Assert.That(
                style.OuterColor,
                Is.EqualTo((Color)new Color32(0x39, 0xD5, 0xFF, 0xFF)));
            Assert.That(
                style.BodyColor,
                Is.EqualTo((Color)new Color32(0x9A, 0xF0, 0xFF, 0xFF)));
            Assert.That(style.CoreColor, Is.EqualTo(Color.white));
            Assert.That(style.OuterWidthMultiplier, Is.EqualTo(1.75f));
            Assert.That(style.BranchSegmentCount, Is.EqualTo(3));
            Assert.That(asset.AddressOrPath, Is.EqualTo(T698StrokeTrailVfxAuthoring.PrefabPath));

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset.AddressOrPath);
            Assert.That(prefab, Is.Not.Null);
            StrokeTrailView view = prefab.GetComponent<StrokeTrailView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.LineRenderer, Is.SameAs(prefab.GetComponent<LineRenderer>()));
            Assert.That(view.BodyLineRenderer, Is.Not.Null);
            Assert.That(view.CoreLineRenderer, Is.Not.Null);
            Assert.That(
                view.BranchLineRenderers.Count,
                Is.EqualTo(StrokeTrailView.BranchRendererCapacity));
        }

        [Test]
        public void BranchLayoutIsDeterministicAndDoesNotMutateTheSharedPath()
        {
            var path = new[]
            {
                Vector2.zero,
                new Vector2(240f, 30f),
                new Vector2(480f, -20f),
                new Vector2(760f, 40f),
            };
            Vector2[] original = (Vector2[])path.Clone();
            var first = new Vector2[9];
            var second = new Vector2[9];

            int count = LightningBranchLayout.CountBranches(path, 120f, 12);
            bool firstWritten = LightningBranchLayout.TryWriteBranch(
                698,
                2,
                path,
                120f,
                64f,
                18f,
                3,
                first);
            bool secondWritten = LightningBranchLayout.TryWriteBranch(
                698,
                2,
                path,
                120f,
                64f,
                18f,
                3,
                second);

            Assert.That(count, Is.EqualTo(6));
            Assert.That(firstWritten, Is.True);
            Assert.That(secondWritten, Is.True);
            for (int index = 0; index <= 3; index++)
            {
                Assert.That(second[index], Is.EqualTo(first[index]));
            }

            Assert.That(path, Is.EqualTo(original));
        }
    }
}
