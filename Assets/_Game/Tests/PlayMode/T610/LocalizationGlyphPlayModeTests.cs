using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T610
{
    [Category("T610")]
    public sealed class LocalizationGlyphPlayModeTests
    {
        private BattleHudRuntime runtime;
        private GameObject damageNumber;

        [SetUp]
        public void SetUp()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            runtime?.Dispose();
            runtime = null;
            if (damageNumber != null)
            {
                UnityEngine.Object.DestroyImmediate(damageNumber);
                damageNumber = null;
            }
        }

        [UnityTest]
        public IEnumerator ChineseHudAndDynamicDamageDigitsRenderWithoutMissingGlyphsOrClipping()
        {
            LogAssert.Expect(
                LogType.Log,
                new Regex("CONFIG_RUNTIME_READY.*content=0\\.6\\.0-sample.*records=715"));
            LogAssert.Expect(LogType.Log, new Regex("ASSET_REGISTRY_READY.*entries=76"));
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            var source = new MutableHudSource(CreateState(
                currentHp: 87L,
                energy: 100L,
                combo: 12,
                score: 5210L));
            runtime = BattleHudRuntime.Create(
                GameplayConfigRuntime.Current,
                source,
                new NoOpCommands(),
                ConfigIds.Players.PlayerMoyan,
                BattleHudLanguage.ZhCN);
            BattleHudView view = runtime.View;
            TMP_FontAsset primary = Resources.Load<TMP_FontAsset>(
                BattleHudViewFactory.HudFontResourcePath);
            Assert.That(primary, Is.Not.Null);

            TMP_Text damageText = CreateDamageNumber(view.SafeAreaRoot, primary);
            damageNumber = damageText.gameObject;
            yield return null;
            AssertVisibleText(view.gameObject, primary);
            AssertText(damageText, primary);

            string screenshotPath = Environment.GetEnvironmentVariable("ONEDRAW_T610_SCREENSHOT");
            if (!string.IsNullOrWhiteSpace(screenshotPath))
            {
                Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                    "Screenshot run requires a graphics device; omit -nographics.");
                CaptureHud(view, screenshotPath);
                Assert.That(new FileInfo(screenshotPath).Length, Is.GreaterThan(10_000));
            }

            source.Emit(CreateState(
                currentHp: 87L,
                energy: 100L,
                combo: 12,
                score: 5210L,
                flow: BattleFlowState.Victory,
                result: new BattleHudResultState(
                    BattleSettlement.Victory,
                    4480L,
                    2,
                    new[]
                    {
                        new BattleHudRewardState(
                            RewardGrantType.UnlockLevel,
                            ConfigIds.Levels.Lv002Cave,
                            1L),
                        new BattleHudRewardState(
                            RewardGrantType.ScoreToken,
                            "score_token",
                            100L),
                    },
                    canGoNext: true)));
            yield return null;
            AssertVisibleText(view.gameObject, primary);
            Assert.That(view.ResultTitleText.text, Is.EqualTo("胜利"));
            Assert.That(view.LastRendered.RewardsBody, Is.EqualTo(
                "解锁关卡: 百鬼回廊\n镇妖积分: +100"));
        }

        private static TMP_Text CreateDamageNumber(Transform parent, TMP_FontAsset font)
        {
            var root = new GameObject("T610DamageNumber", typeof(RectTransform));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 70f);
            rect.sizeDelta = new Vector2(620f, 110f);
            var text = root.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = "-12345  暴击";
            text.fontSize = 60f;
            text.color = new Color32(255, 215, 76, 255);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static void AssertVisibleText(GameObject root, TMP_FontAsset primary)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(includeInactive: false);
            Assert.That(texts.Length, Is.GreaterThan(0));
            foreach (TMP_Text text in texts)
            {
                if (!string.IsNullOrEmpty(text.text))
                {
                    AssertText(text, primary);
                }
            }
        }

        private static void AssertText(TMP_Text text, TMP_FontAsset primary)
        {
            Assert.That(text.font, Is.SameAs(primary), text.name);
            text.ForceMeshUpdate(ignoreActiveState: false, forceTextReparsing: true);
            Assert.That(text.isTextOverflowing, Is.False, text.name + " overflow");
            Assert.That(text.isTextTruncated, Is.False, text.name + " truncated");
            for (int index = 0; index < text.textInfo.characterCount; index += 1)
            {
                TMP_CharacterInfo info = text.textInfo.characterInfo[index];
                if (char.IsWhiteSpace(info.character))
                {
                    continue;
                }

                Assert.That(
                    primary.HasCharacter(info.character, searchFallbacks: true, tryAddCharacter: false),
                    Is.True,
                    $"{text.name} missing U+{(int)info.character:X4} '{info.character}'.");
                Assert.That(info.fontAsset, Is.Not.Null, text.name + " font asset");
                Assert.That(info.textElement, Is.Not.Null, text.name + " text element");
                Assert.That(info.textElement.unicode, Is.EqualTo((uint)info.character),
                    $"{text.name} replacement glyph at U+{(int)info.character:X4}.");
            }
        }

        private static void CaptureHud(BattleHudView view, string outputPath)
        {
            DisableNonHudRenderers(view.transform);
            Canvas canvas = view.GetComponent<Canvas>();
            var cameraRoot = new GameObject("T610ScreenshotCamera", typeof(Camera));
            Camera camera = cameraRoot.GetComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 16, 29, 255);
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            camera.targetTexture = renderTexture;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, mipChain: false);
            image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
            image.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(cameraRoot);
        }

        private static void DisableNonHudRenderers(Transform hudRoot)
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas candidate in canvases)
            {
                if (candidate.transform != hudRoot && !candidate.transform.IsChildOf(hudRoot))
                {
                    candidate.enabled = false;
                }
            }

            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.transform != hudRoot && !renderer.transform.IsChildOf(hudRoot))
                {
                    renderer.enabled = false;
                }
            }
        }

        private static BattleHudState CreateState(
            long currentHp,
            long energy,
            int combo,
            long score,
            BattleFlowState flow = BattleFlowState.Playing,
            BattleHudResultState result = null)
        {
            return new BattleHudState(
                ConfigIds.Levels.Lv001Tutorial,
                currentHp,
                100L,
                energy,
                100L,
                ConfigIds.Stances.StanceBlade,
                combo,
                score,
                flow,
                0d,
                0d,
                result);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != sceneName &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }

        private sealed class MutableHudSource : IBattleHudStateSource
        {
            public MutableHudSource(BattleHudState current)
            {
                Current = current;
            }

            public event Action<BattleHudState> Changed;

            public BattleHudState Current { get; private set; }

            public void Emit(BattleHudState state)
            {
                Current = state;
                Changed?.Invoke(state);
            }
        }

        private sealed class NoOpCommands : IBattleHudCommandSink
        {
            public void SetPlayerPaused(bool paused) { }
            public void BeginUltimateDrawing() { }
            public void Restart() { }
            public void GoNext() { }
            public void ReturnToMainMenu() { }
        }
    }
}
