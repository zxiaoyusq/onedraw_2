using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.Art;
using OneStrokeDemon.Editor.AssetRegistry;
using OneStrokeDemon.Presentation;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace OneStrokeDemon.Tests.EditMode.T630
{
    [Category("AssetImport")]
    public sealed class AssetImportValidationTests
    {
        private static readonly Regex FileNamePattern = new Regex(
            "^[a-z0-9]+(?:_[a-z0-9]+)*\\.png$",
            RegexOptions.CultureInvariant);

        [Test]
        public void RuntimeArtUsesOnlyRgbaPngWithTransparentForegrounds()
        {
            string[] forbidden = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".psd", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".psb", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.That(forbidden, Is.Empty, "Runtime Assets must not contain layered or lossy T630 sources.");

            IReadOnlyList<string> paths = PngPaths();
            Assert.That(paths.Count, Is.GreaterThanOrEqualTo(28));
            foreach (string path in paths)
            {
                Assert.That(FileNamePattern.IsMatch(Path.GetFileName(path)), Is.True, path);
                byte[] bytes = File.ReadAllBytes(path);
                Assert.That(bytes.Length, Is.GreaterThan(32), path);
                Assert.That(bytes.Take(8), Is.EqualTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), path);
                Assert.That(bytes[25], Is.EqualTo(6), $"{path} must use PNG RGBA color type 6.");

                if (!path.Contains("/Backgrounds/", StringComparison.Ordinal))
                {
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        Assert.That(ImageConversion.LoadImage(texture, bytes, markNonReadable: false), Is.True, path);
                        Assert.That(texture.GetPixels32().Any(pixel => pixel.a < 250), Is.True,
                            $"{path} must retain transparent pixels.");
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
            }
        }

        [Test]
        public void ImportersUseCategoryPpuPivotMeshCompressionAndSizeContract()
        {
            foreach (string path in PngPaths())
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                bool animated = path.Contains("/Animated/", StringComparison.Ordinal);
                Assert.That(
                    importer.spriteImportMode,
                    Is.EqualTo(animated ? SpriteImportMode.Multiple : SpriteImportMode.Single),
                    path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), path);
                Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.FromInput), path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.sRGBTexture, Is.True, path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.isReadable, Is.False, path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                TextureImporterCompression expectedCompression =
                    path.Contains("/Characters/Animated/Moyan/", StringComparison.Ordinal)
                        ? TextureImporterCompression.Uncompressed
                        : TextureImporterCompression.CompressedHQ;
                Assert.That(importer.textureCompression, Is.EqualTo(expectedCompression), path);

                bool background = path.Contains("/Backgrounds/", StringComparison.Ordinal);
                bool actor = path.Contains("/Characters/", StringComparison.Ordinal) ||
                    path.Contains("/Enemies/", StringComparison.Ordinal);
                bool ui = path.Contains("/UI/", StringComparison.Ordinal);
                var spriteSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(spriteSettings);
                Assert.That(spriteSettings.spriteMeshType,
                    Is.EqualTo(background || ui ? SpriteMeshType.FullRect : SpriteMeshType.Tight), path);
                Assert.That(importer.maxTextureSize,
                    Is.EqualTo(background ? 4096 : animated ? 1024 : actor || ui ? 2048 : 1024), path);

                Vector2 expectedPivot = actor ? new Vector2(0.5f, 0.08f) : new Vector2(0.5f, 0.5f);
                Sprite[] sprites = animated
                    ? AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray()
                    : new[] { AssetDatabase.LoadAssetAtPath<Sprite>(path) };
                Assert.That(sprites, Is.Not.Empty, path);
                Assert.That(sprites.All(sprite => sprite != null), Is.True, path);
                foreach (Sprite sprite in sprites)
                {
                    Vector2 normalizedPivot = new Vector2(
                        sprite.pivot.x / sprite.rect.width,
                        sprite.pivot.y / sprite.rect.height);
                    Assert.That(normalizedPivot.x, Is.EqualTo(expectedPivot.x).Within(0.01f), path);
                    Assert.That(normalizedPivot.y, Is.EqualTo(expectedPivot.y).Within(0.01f), path);
                }
            }
        }

        [Test]
        public void SpriteAtlasV2AssetsAreIncludedAndBindEveryCategory()
        {
            foreach (string path in T630ArtAssetPaths.Atlases)
            {
                SpriteAtlasAsset authoring = SpriteAtlasAsset.Load(path);
                Assert.That(authoring, Is.Not.Null, path);
                var importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.includeInBuild, Is.True, path);
                SpriteAtlasPackingSettings packing = importer.packingSettings;
                Assert.That(packing.enableRotation, Is.False, path);
                Assert.That(packing.enableTightPacking, Is.True, path);
                Assert.That(packing.padding, Is.GreaterThanOrEqualTo(4), path);

                SpriteAtlas runtime = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                Assert.That(runtime, Is.Not.Null, path);
            }

            AssertAtlasBinds(
                T630ArtAssetPaths.BackgroundAtlas,
                "Assets/_Game/Art/Backgrounds/bg_red_cave.png");
            AssertAtlasBinds(
                T630ArtAssetPaths.CharacterAtlas,
                T694MoyanAnimationAuthoring.IdleTexturePath);
            AssertAtlasBinds(
                T630ArtAssetPaths.CharacterAtlas,
                T694MoyanAnimationAuthoring.AttackTexturePath);
            AssertAtlasBinds(T630ArtAssetPaths.EnemyAtlas, T630ArtAssetPaths.SoulPuppetSprite);
            AssertAtlasBinds(T630ArtAssetPaths.UiAtlas, "Assets/_Game/Art/UI/icon_skill_ultimate.png");
            AssertAtlasBinds(T630ArtAssetPaths.UiAtlas, "Assets/_Game/Art/Sprites/icon_skill_blade_echo.png");
            AssertAtlasBinds(T630ArtAssetPaths.VfxAtlas, "Assets/_Game/Art/Sprites/proj_ghost_fire.png");
            AssertAtlasBinds(T630ArtAssetPaths.VfxAtlas, T630ArtAssetPaths.SlashArcSprite);
        }

        [Test]
        public void SortingLayersHaveStableBackToFrontOrder()
        {
            string[] names = SortingLayer.layers.Select(layer => layer.name).ToArray();
            int background = Array.IndexOf(names, "Background");
            int defaultLayer = Array.IndexOf(names, "Default");
            int actors = Array.IndexOf(names, "Actors");
            int projectiles = Array.IndexOf(names, "Projectiles");
            int vfx = Array.IndexOf(names, "VFX");

            Assert.That(background, Is.GreaterThanOrEqualTo(0));
            Assert.That(background, Is.LessThan(defaultLayer));
            Assert.That(defaultLayer, Is.LessThan(actors));
            Assert.That(actors, Is.LessThan(projectiles));
            Assert.That(projectiles, Is.LessThan(vfx));
        }

        [Test]
        public void CanonicalVisualRegistryUsesRealSpritesAndRenderablePrefabs()
        {
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            Assert.That(registry, Is.Not.Null);
            Dictionary<string, AssetRegistryEntry> entries = registry.Entries
                .ToDictionary(entry => entry.AssetKey, StringComparer.Ordinal);

            foreach (AssetManifestConfig row in config.GetAssetManifest()
                         .Where(row => row.AssetType == "Sprite" || row.AssetType == "Prefab"))
            {
                Assert.That(entries.ContainsKey(row.AssetKey), Is.True, row.AssetKey);
                Assert.That(AssetDatabase.GetAssetPath(entries[row.AssetKey].Asset),
                    Is.EqualTo(row.AddressOrPath), row.AssetKey);
                if (row.AssetType == "Sprite")
                {
                    Assert.That(entries[row.AssetKey].Asset, Is.TypeOf<Sprite>(), row.AssetKey);
                    continue;
                }

                var prefab = entries[row.AssetKey].Asset as GameObject;
                Assert.That(prefab, Is.Not.Null, row.AssetKey);
                SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.That(renderers.Length, Is.GreaterThan(0), row.AssetKey);
                Assert.That(renderers.All(renderer => renderer.sprite != null), Is.True, row.AssetKey);
                if (row.AssetKey.StartsWith("vfx_", StringComparison.Ordinal))
                {
                    Assert.That(prefab.GetComponent<VfxPoolItem>(), Is.Not.Null, row.AssetKey);
                    Assert.That(renderers.All(renderer => renderer.sortingLayerName == "VFX"), Is.True, row.AssetKey);
                }
                else
                {
                    Assert.That(prefab.GetComponents<MonoBehaviour>(), Is.Empty,
                        $"{row.AssetKey} must not serialize gameplay values into the visual Prefab.");
                    Assert.That(renderers.All(renderer => renderer.sortingLayerName == "Actors"), Is.True, row.AssetKey);
                }
            }

            AssetRegistryLoadSummary summary = AssetRegistryEditorValidator.ValidateCanonical();
            Assert.That(summary.EntryCount, Is.EqualTo(76));
        }

        private static void AssertAtlasBinds(string atlasPath, string spritePath)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) ??
                AssetDatabase.LoadAllAssetsAtPath(spritePath).OfType<Sprite>().FirstOrDefault();
            Assert.That(atlas, Is.Not.Null, atlasPath);
            Assert.That(sprite, Is.Not.Null, spritePath);
            Assert.That(atlas.CanBindTo(sprite), Is.True, $"{atlasPath} -> {spritePath}");
        }

        private static IReadOnlyList<string> PngPaths() =>
            Directory.GetFiles(T630ArtAssetPaths.ArtRoot, "*.png", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
    }
}
