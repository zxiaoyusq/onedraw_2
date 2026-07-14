using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Editor.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T610
{
    [Category("T610")]
    public sealed class LocalizationGlyphTests
    {
        private const string CommonPunctuation = "，。！？；：“”‘’（）《》【】、—…·";

        [Test]
        public void CharacterInventoryExactlyCoversConfigurationAndDynamicUiText()
        {
            string actualText = File.ReadAllText(Absolute(LocalizationFontAssetAuthoring.CharacterSetPath))
                .TrimEnd('\r', '\n');
            var actual = new HashSet<char>(actualText);
            var expected = new HashSet<char>();
            for (int codePoint = 0x20; codePoint <= 0x7E; codePoint += 1)
            {
                expected.Add((char)codePoint);
            }

            expected.Add('\u00A0');
            expected.UnionWith(CommonPunctuation);
            JObject root = JObject.Parse(File.ReadAllText(Absolute(
                "Assets/_Game/Config/Generated/gameplay_config.json")));
            foreach (JObject row in root["texts"].Values<JObject>())
            {
                expected.UnionWith(row.Value<string>("zhCN"));
            }

            Assert.That(actualText.Length, Is.EqualTo(299));
            Assert.That(actual.Count, Is.EqualTo(actualText.Length), "Character list contains duplicates.");
            Assert.That(actual, Is.EquivalentTo(expected));
            Assert.That("-1234567890 +98765 暴击".All(actual.Contains), Is.True);
        }

        [Test]
        public void StaticPrimaryAndChineseFallbackCoverEveryListedGlyphWithinAtlasBudget()
        {
            TMP_FontAsset primary = LoadFont(LocalizationFontAssetAuthoring.PrimaryFontPath);
            TMP_FontAsset fallback = LoadFont(LocalizationFontAssetAuthoring.ChineseFallbackFontPath);
            string characters = File.ReadAllText(Absolute(LocalizationFontAssetAuthoring.CharacterSetPath))
                .TrimEnd('\r', '\n');

            Assert.That(primary.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
            Assert.That(fallback.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
            Assert.That(primary.isMultiAtlasTexturesEnabled, Is.False);
            Assert.That(fallback.isMultiAtlasTexturesEnabled, Is.False);
            Assert.That(primary.atlasTextureCount, Is.EqualTo(1));
            Assert.That(fallback.atlasTextureCount, Is.EqualTo(1));
            Assert.That(primary.atlasTexture.width, Is.EqualTo(512));
            Assert.That(primary.atlasTexture.height, Is.EqualTo(512));
            Assert.That(fallback.atlasTexture.width, Is.EqualTo(1024));
            Assert.That(fallback.atlasTexture.height, Is.EqualTo(1024));
            Assert.That(primary.characterTable.Count, Is.EqualTo(96));
            Assert.That(fallback.characterTable.Count, Is.EqualTo(203));
            Assert.That(primary.fallbackFontAssetTable, Is.EqualTo(new[] { fallback }));

            foreach (char character in characters)
            {
                Assert.That(
                    primary.HasCharacter(character, searchFallbacks: true, tryAddCharacter: false),
                    Is.True,
                    $"Missing U+{(int)character:X4} '{character}'.");
                if (character <= 0x7E || character == '\u00A0')
                {
                    Assert.That(primary.HasCharacter(character), Is.True, $"Primary U+{(int)character:X4}");
                }
                else
                {
                    Assert.That(fallback.HasCharacter(character), Is.True, $"Fallback U+{(int)character:X4}");
                }
            }

            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(
                LocalizationFontAssetAuthoring.TmpSettingsPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(TMP_Settings.defaultFontAsset, Is.SameAs(primary));
            Assert.That(TMP_Settings.fallbackFontAssets, Is.EqualTo(new[] { fallback }));
            Assert.That(Resources.Load<TMP_FontAsset>("Fonts/OneStrokeDemon UI Latin SDF"),
                Is.SameAs(primary));
        }

        [Test]
        public void DeliveredSubsetIsPinnedRenamedLicensedAndSmall()
        {
            string fontPath = Absolute(LocalizationFontAssetAuthoring.SourceFontPath);
            var file = new FileInfo(fontPath);
            Assert.That(file.Length, Is.EqualTo(126168));
            Assert.That(file.Length, Is.LessThan(200_000));
            Assert.That(Sha256(fontPath), Is.EqualTo(
                "9de334f2650055fa13b55c14200a55b5d87486c7f4e0ba5a3d1a23efeff8c0e4"));
            Assert.That(AssetDatabase.LoadAssetAtPath<Font>(
                LocalizationFontAssetAuthoring.SourceFontPath).name,
                Is.EqualTo("OneStrokeDemonUI-Regular"));

            string license = File.ReadAllText(Absolute("Assets/_Game/Art/UI/Fonts/OFL.txt"));
            Assert.That(license, Does.Contain("SIL OPEN FONT LICENSE Version 1.1"));
            string provenance = File.ReadAllText(Absolute(
                "Assets/_Game/Art/UI/Fonts/FONT_SOURCE.md"));
            Assert.That(provenance, Does.Contain(
                "2894aab31764f10f29c421bdfd2340d3b382d384"));
            Assert.That(provenance, Does.Contain("One Stroke Demon UI"));
        }

        private static TMP_FontAsset LoadFont(string path)
        {
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private static string Absolute(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using SHA256 algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(stream))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
    }
}
