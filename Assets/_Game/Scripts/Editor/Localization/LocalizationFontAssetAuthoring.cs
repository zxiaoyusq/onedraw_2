using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace OneStrokeDemon.Editor.Localization
{
    public static class LocalizationFontAssetAuthoring
    {
        public const string SourceFontPath =
            "Assets/_Game/Art/UI/Fonts/OneStrokeDemonUI-Regular.ttf";
        public const string CharacterSetPath =
            "Assets/_Game/Art/UI/Fonts/OneStrokeDemonUI.charset.txt";
        public const string PrimaryFontPath =
            "Assets/_Game/Art/UI/Fonts/Resources/Fonts/OneStrokeDemon UI Latin SDF.asset";
        public const string ChineseFallbackFontPath =
            "Assets/_Game/Art/UI/Fonts/Resources/Fonts/OneStrokeDemon UI Chinese SDF.asset";
        public const string TmpSettingsPath =
            "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("One Stroke Demon/Localization/Rebuild TMP Font Assets")]
        public static void RebuildFromMenu()
        {
            BuildAssets();
        }

        public static void BuildForCommandLine()
        {
            BuildAssets();
        }

        public static void BuildAssets()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                throw new InvalidOperationException(
                    $"T610 source font is missing or not imported: {SourceFontPath}");
            }

            string characterText = File.ReadAllText(CharacterSetPath).TrimEnd('\r', '\n');
            uint[] allCharacters = EnumerateCodePoints(characterText).Distinct().OrderBy(value => value).ToArray();
            uint[] primaryCharacters = allCharacters
                .Where(value => value >= 0x20 && (value <= 0x7E || value == 0xA0))
                .ToArray();
            uint[] fallbackCharacters = allCharacters
                .Where(value => value > 0x7E && value != 0xA0)
                .ToArray();
            if (primaryCharacters.Length != 96 || fallbackCharacters.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Unexpected T610 character partition: primary={primaryCharacters.Length}, " +
                    $"fallback={fallbackCharacters.Length}.");
            }

            EnsureTmpEssentialResources();
            EnsureDirectory(Path.GetDirectoryName(PrimaryFontPath));
            TMP_FontAsset fallback = BuildStaticFontAsset(
                sourceFont,
                ChineseFallbackFontPath,
                "OneStrokeDemon UI Chinese SDF",
                fallbackCharacters,
                samplingPointSize: 48,
                atlasPadding: 4,
                atlasWidth: 1024,
                atlasHeight: 1024);
            TMP_FontAsset primary = BuildStaticFontAsset(
                sourceFont,
                PrimaryFontPath,
                "OneStrokeDemon UI Latin SDF",
                primaryCharacters,
                samplingPointSize: 42,
                atlasPadding: 4,
                atlasWidth: 512,
                atlasHeight: 512);
            primary.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(primary);

            ConfigureTmpSettings(primary, fallback);
            PruneUnusedTmpEssentialAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log(
                $"T610_FONT_ASSETS_READY primary={primaryCharacters.Length} " +
                $"fallback={fallbackCharacters.Length} total={allCharacters.Length} " +
                "atlases=512x512+1024x1024 static=true multiAtlas=false");
        }

        private static TMP_FontAsset BuildStaticFontAsset(
            Font sourceFont,
            string assetPath,
            string assetName,
            uint[] characters,
            int samplingPointSize,
            int atlasPadding,
            int atlasWidth,
            int atlasHeight)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null &&
                !AssetDatabase.DeleteAsset(assetPath))
            {
                throw new InvalidOperationException($"Unable to replace TMP font asset: {assetPath}");
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                samplingPointSize,
                atlasPadding,
                GlyphRenderMode.SDFAA,
                atlasWidth,
                atlasHeight,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: false);
            if (fontAsset == null)
            {
                throw new InvalidOperationException($"Unable to create TMP font asset: {assetPath}");
            }

            fontAsset.name = assetName;
            fontAsset.atlasTexture.name = assetName + " Atlas";
            fontAsset.material.name = assetName + " Material";
            bool added = fontAsset.TryAddCharacters(characters, out uint[] missingCharacters);
            if (!added || (missingCharacters != null && missingCharacters.Length > 0))
            {
                string missing = missingCharacters == null
                    ? "unknown"
                    : string.Join(", ", missingCharacters.Select(value => $"U+{value:X4}"));
                UnityEngine.Object.DestroyImmediate(fontAsset.material);
                UnityEngine.Object.DestroyImmediate(fontAsset.atlasTexture);
                UnityEngine.Object.DestroyImmediate(fontAsset);
                throw new InvalidOperationException(
                    $"TMP atlas {assetPath} cannot fit or render all requested characters: {missing}");
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.isMultiAtlasTexturesEnabled = false;
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static void ConfigureTmpSettings(TMP_FontAsset primary, TMP_FontAsset fallback)
        {
            TMP_Settings settings = EnsureTmpSettingsExists();

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("assetVersion").stringValue = "2";
            serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = primary;
            serialized.FindProperty("m_defaultFontAssetPath").stringValue = "Fonts/";
            serialized.FindProperty("m_defaultFontSize").floatValue = 36f;
            serialized.FindProperty("m_missingGlyphCharacter").intValue = 0;
            serialized.FindProperty("m_warningsDisabled").boolValue = false;
            SerializedProperty fallbacks = serialized.FindProperty("m_fallbackFontAssets");
            fallbacks.arraySize = 1;
            fallbacks.GetArrayElementAtIndex(0).objectReferenceValue = fallback;
            serialized.FindProperty("m_defaultSpriteAsset").objectReferenceValue = null;
            serialized.FindProperty("m_defaultSpriteAssetPath").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void PruneUnusedTmpEssentialAssets()
        {
            AssetDatabase.DeleteAsset("Assets/TextMesh Pro/Fonts");
            AssetDatabase.DeleteAsset("Assets/TextMesh Pro/Resources/Fonts & Materials");

            const string shaderRoot = "Assets/TextMesh Pro/Shaders";
            var retained = new HashSet<string>(StringComparer.Ordinal)
            {
                shaderRoot + "/TMP_SDF-Mobile.shader",
                shaderRoot + "/TMPro_Properties.cginc",
            };
            string[] shaderGuids = AssetDatabase.FindAssets(string.Empty, new[] { shaderRoot });
            foreach (string guid in shaderGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.IsValidFolder(assetPath) && !retained.Contains(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
        }

        private static TMP_Settings EnsureTmpSettingsExists()
        {
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings != null)
            {
                return settings;
            }

            throw new InvalidOperationException(
                $"TMP Essential Resources did not provide settings at {TmpSettingsPath}.");
        }

        private static void EnsureTmpEssentialResources()
        {
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            Shader distanceField = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (settings == null || distanceField == null)
            {
                UnityEditor.PackageManager.PackageInfo package =
                    UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
                if (package == null)
                {
                    throw new InvalidOperationException(
                        "Unable to locate the installed uGUI package for TMP Essential Resources.");
                }

                string packagePath = Path.Combine(
                    package.resolvedPath,
                    "Package Resources",
                    "TMP Essential Resources.unitypackage");
                if (!File.Exists(packagePath))
                {
                    throw new FileNotFoundException(
                        "TMP Essential Resources package is missing.",
                        packagePath);
                }

                AssetDatabase.ImportPackage(packagePath, interactive: false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
                distanceField = Shader.Find("TextMeshPro/Mobile/Distance Field");
            }

            if (settings == null || distanceField == null)
            {
                throw new InvalidOperationException(
                    "TMP Essential Resources were imported but settings or the mobile SDF shader is unavailable.");
            }
        }

        private static IEnumerable<uint> EnumerateCodePoints(string text)
        {
            for (int index = 0; index < text.Length; index += 1)
            {
                char current = text[index];
                if (char.IsHighSurrogate(current) &&
                    index + 1 < text.Length &&
                    char.IsLowSurrogate(text[index + 1]))
                {
                    yield return (uint)char.ConvertToUtf32(current, text[index + 1]);
                    index += 1;
                    continue;
                }

                if (!char.IsSurrogate(current))
                {
                    yield return current;
                }
            }
        }

        private static void EnsureDirectory(string assetDirectory)
        {
            if (string.IsNullOrEmpty(assetDirectory) || AssetDatabase.IsValidFolder(assetDirectory))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetDirectory)?.Replace('\\', '/');
            string name = Path.GetFileName(assetDirectory);
            EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
