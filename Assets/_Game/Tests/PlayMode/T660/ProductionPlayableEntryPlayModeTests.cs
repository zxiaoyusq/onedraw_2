using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Bootstrap;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OneStrokeDemon.Tests.PlayMode.T660
{
    [Category("T660")]
    public sealed class ProductionPlayableEntryPlayModeTests : InputTestFixture
    {
        [SetUp]
        public override void Setup()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            PointerInputRuntime.ResetForTests();
            BattleLaunchContext.Clear();
            PlayerPrefs.DeleteKey(PlayerPrefsProgressSaveStore.StorageKey);
            base.Setup();
        }

        [TearDown]
        public override void TearDown()
        {
            BattleLaunchContext.Clear();
            PlayerPrefs.DeleteKey(PlayerPrefsProgressSaveStore.StorageKey);
            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator BootstrapMenuClickCreatesHudAndRealStrokeDamagesEnemy()
        {
            yield return LoadBootstrapToMenu();
            MainMenuCompositionRoot menu = FindMenu();
            Assert.That(menu.View.StartButton.gameObject.activeSelf, Is.True);
            Assert.That(
                menu.View.LevelChoices.Count,
                Is.EqualTo(GameplayConfigRuntime.Current.GetLevels().Count));
            Assert.That(menu.View.LevelChoices[0].IsUnlocked, Is.True);
            var returningPlayerResults = new ResultService(
                GameplayConfigRuntime.Current,
                new PlayerPrefsProgressSaveStore());
            Assert.That(
                returningPlayerResults.MarkTutorialCompleted(
                    ConfigIds.Tutorials.TutorialLevel001),
                Is.True);

            menu.View.StartButton.onClick.Invoke();
            string screenshotPath = Environment.GetEnvironmentVariable(
                "ONEDRAW_T660_SCREENSHOT");
            if (!string.IsNullOrWhiteSpace(screenshotPath))
            {
                Assert.That(
                    SystemInfo.graphicsDeviceType,
                    Is.Not.EqualTo(GraphicsDeviceType.Null),
                    "T660 screenshot requires a graphics device; omit -nographics.");
                CaptureMenu(menu.View, screenshotPath);
                Assert.That(new FileInfo(screenshotPath).Length, Is.GreaterThan(10_000));
            }

            MainMenuLevelChoice tutorialChoice = FindChoice(
                menu.View,
                ConfigIds.Levels.Lv001Tutorial);
            Assert.That(tutorialChoice.Button.interactable, Is.True);
            tutorialChoice.Button.onClick.Invoke();
            yield return WaitForScene(SceneNames.Battle);

            BattleCompositionRoot battleRoot = FindBattle();
            ProductionBattleSession session = battleRoot.CurrentSession;
            Assert.That(session, Is.Not.Null);
            Assert.That(session.LevelId, Is.EqualTo(ConfigIds.Levels.Lv001Tutorial));
            Assert.That(session.HudView, Is.Not.Null);
            MeshRenderer graybox = FindMeshRenderer("BattleGraybox");
            Assert.That(graybox, Is.Not.Null);
            Assert.That(
                graybox.gameObject.activeSelf,
                Is.False,
                "The production Battle scene must not render its development graybox over the configured background.");
            yield return WaitForPlaying(session);
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(session.TutorialView, Is.Not.Null);
            Assert.That(session.TutorialView.OverlayVisible, Is.False);
            yield return WaitForEnemy(session);

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            EnemyController enemy = Object.FindAnyObjectByType<EnemyController>();
            Assert.That(enemy, Is.Not.Null);
            Assert.That(enemy.IsAlive, Is.True);
            Assert.That(
                session.FlowState,
                Is.EqualTo(BattleFlowState.Playing),
                $"Battle must accept input before the stroke; playerHp={session.Player.Current.CurrentHp}.");
            long hpBefore = enemy.Damage.CurrentHp;
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(enemy.transform.position);
            Assert.That(screenPoint.x, Is.InRange(0f, (float)Screen.width));
            Assert.That(screenPoint.y, Is.InRange(0f, (float)Screen.height));
            Vector2 strokeStart = new Vector2(Screen.width * 0.25f, screenPoint.y);
            Assert.That(mouse.leftButton.isPressed, Is.False);
            Assert.That(
                ((InputSystemPointerAdapter)PointerInputRuntime.Current).IsPointerActive,
                Is.False);
            Assert.That(
                new EventSystemPointerUiBlocker().IsBlocked(
                    strokeStart,
                    InputSystemPointerAdapter.MousePointerId),
                Is.False,
                $"Configured enemy stroke began over UI at {strokeStart} for enemy {screenPoint}.");
            Vector2 strokeMidpoint = Vector2.Lerp(strokeStart, screenPoint, 0.5f);
            Set(mouse.position, strokeStart, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, strokeMidpoint, queueEventOnly: true);
            yield return null;

            StrokeTrailView liveTrail = FindVisibleTrail();
            Assert.That(liveTrail, Is.Not.Null,
                "The production battle must show the sampled trail before pointer release.");
            Assert.That(liveTrail.LineRenderer.enabled, Is.True);
            Assert.That(liveTrail.LineRenderer.positionCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(liveTrail.LineRenderer.useWorldSpace, Is.True);
            StrokeTrailStyle liveTrailStyle = StrokeTrailSettingsFactory.CreateStyle(
                GameplayConfigRuntime.Current,
                session.Player.Current.StanceId,
                ConfigIds.VfxCues.VfxSlash);
            float expectedWorldWidth =
                liveTrailStyle.WidthReferencePixels *
                liveTrailStyle.OuterWidthMultiplier *
                liveTrail.ReferencePixelWorldScale;
            Assert.That(
                liveTrail.LineRenderer.startWidth,
                Is.EqualTo(expectedWorldWidth).Within(0.001f),
                "Reference-pixel trail width must be converted to world width exactly once.");

            Set(mouse.position, screenPoint, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(session.CompletedStrokeCount, Is.EqualTo(1));
            Assert.That(session.LastResolvedHitCount, Is.GreaterThan(0));
            Assert.That(enemy.Damage.CurrentHp, Is.LessThan(hpBefore));
            Assert.That(session.HudView.ScoreValueText.text, Is.Not.EqualTo("0"));

            StrokeTrailView completedTrail = FindVisibleTrail();
            Assert.That(completedTrail, Is.Not.Null,
                "The production battle must render the processed pointer stroke.");
            Assert.That(completedTrail.SourcePoints.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(completedTrail.LineRenderer.enabled, Is.True);
            Assert.That(completedTrail.LineRenderer.startWidth, Is.GreaterThan(0f));
            Assert.That(completedTrail.BodyLineRenderer.enabled, Is.True);
            Assert.That(completedTrail.CoreLineRenderer.enabled, Is.True);
            Assert.That(completedTrail.ActiveBranchCount, Is.GreaterThan(0));
            string lightningScreenshotPath = Environment.GetEnvironmentVariable(
                "ONEDRAW_T698_SCREENSHOT");
            if (!string.IsNullOrWhiteSpace(lightningScreenshotPath))
            {
                string directory = Path.GetDirectoryName(lightningScreenshotPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                ScreenCapture.CaptureScreenshot(lightningScreenshotPath);
                yield return new WaitForEndOfFrame();
                float screenshotDeadline = Time.realtimeSinceStartup + 2f;
                while (!File.Exists(lightningScreenshotPath) &&
                       Time.realtimeSinceStartup < screenshotDeadline)
                {
                    yield return null;
                }

                Assert.That(new FileInfo(lightningScreenshotPath).Length, Is.GreaterThan(10_000));
            }

            string stanceBefore = session.Player.Current.StanceId;
            session.HudView.StanceButton.onClick.Invoke();
            yield return null;
            Assert.That(session.Player.Current.StanceId, Is.Not.EqualTo(stanceBefore));
        }

        [UnityTest]
        public IEnumerator UnlockedNormalAndBossChoicesEnterTheirProductionSessions()
        {
            yield return LoadBootstrapToMenu();
            UnlockAllMvpLevels();
            yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            MainMenuCompositionRoot menu = FindMenu();
            menu.View.StartButton.onClick.Invoke();
            MainMenuLevelChoice normal = FindChoice(
                menu.View,
                ConfigIds.Levels.Lv002Cave);
            Assert.That(normal.Button.interactable, Is.True);
            normal.Button.onClick.Invoke();
            yield return WaitForScene(SceneNames.Battle);
            ProductionBattleSession normalSession = FindBattle().CurrentSession;
            Assert.That(normalSession.LevelId, Is.EqualTo(ConfigIds.Levels.Lv002Cave));
            Assert.That(normalSession.IsBossSession, Is.False);
            yield return WaitForPlaying(normalSession);
            yield return WaitForEnemy(normalSession);

            yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
            menu = FindMenu();
            menu.View.StartButton.onClick.Invoke();
            MainMenuLevelChoice boss = FindChoice(
                menu.View,
                ConfigIds.Levels.Lv003Boss);
            Assert.That(boss.Button.interactable, Is.True);
            boss.Button.onClick.Invoke();
            yield return WaitForScene(SceneNames.Battle);
            ProductionBattleSession bossSession = FindBattle().CurrentSession;
            Assert.That(bossSession.LevelId, Is.EqualTo(ConfigIds.Levels.Lv003Boss));
            Assert.That(bossSession.IsBossSession, Is.True);
            yield return WaitForPlaying(bossSession);
            yield return WaitForEnemy(bossSession);
        }

        [UnityTest]
        public IEnumerator StanceSwitchResolvesConfiguredAudioCueToRegistryAsset()
        {
            yield return LoadBootstrapToMenu();
            MainMenuCompositionRoot menu = FindMenu();
            menu.View.StartButton.onClick.Invoke();
            FindChoice(menu.View, ConfigIds.Levels.Lv001Tutorial).Button.onClick.Invoke();
            yield return WaitForScene(SceneNames.Battle);
            ProductionBattleSession session = FindBattle().CurrentSession;
            yield return WaitForPlaying(session);

            string stanceBefore = session.Player.Current.StanceId;
            session.HudView.StanceButton.onClick.Invoke();
            yield return null;

            Assert.That(session.Player.Current.StanceId, Is.Not.EqualTo(stanceBefore));
        }

        [UnityTest]
        public IEnumerator ResultRestartCreatesFreshSessionAndMainMenuReturnsToEntry()
        {
            yield return LoadBootstrapToMenu();
            MainMenuCompositionRoot menu = FindMenu();
            menu.View.StartButton.onClick.Invoke();
            FindChoice(menu.View, ConfigIds.Levels.Lv001Tutorial).Button.onClick.Invoke();
            yield return WaitForScene(SceneNames.Battle);
            BattleCompositionRoot battleRoot = FindBattle();
            ProductionBattleSession first = battleRoot.CurrentSession;
            Assert.That(first.TutorialView, Is.Not.Null);
            yield return WaitForPlaying(first);

            first.Player.ApplyDamage(
                first.Player.Current.CurrentHp,
                first.GameplayTimestamp,
                "T660_forced_result");
            first.Advance(0f);
            Assert.That(first.FlowState, Is.EqualTo(BattleFlowState.Defeat));
            Assert.That(first.HudView.LastRendered.ResultVisible, Is.True);
            first.HudView.RestartButton.onClick.Invoke();
            yield return null;

            ProductionBattleSession restarted = battleRoot.CurrentSession;
            Assert.That(restarted, Is.Not.SameAs(first));
            Assert.That(battleRoot.SessionGeneration, Is.EqualTo(2U));
            Assert.That(restarted.LevelId, Is.EqualTo(ConfigIds.Levels.Lv001Tutorial));
            yield return WaitForPlaying(restarted);
            restarted.Player.ApplyDamage(
                restarted.Player.Current.CurrentHp,
                restarted.GameplayTimestamp,
                "T660_forced_menu_result");
            restarted.Advance(0f);
            restarted.HudView.MainMenuButton.onClick.Invoke();
            yield return WaitForScene(SceneNames.MainMenu);

            Assert.That(FindMenu().View.StartButton.gameObject.activeSelf, Is.True);
        }

        private static IEnumerator LoadBootstrapToMenu()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
            Assert.That(GameplayConfigRuntime.IsReady, Is.True);
            Assert.That(AssetRegistryRuntime.IsReady, Is.True);
            Assert.That(PointerInputRuntime.IsReady, Is.True);
        }

        private static MainMenuCompositionRoot FindMenu()
        {
            MainMenuCompositionRoot root =
                Object.FindAnyObjectByType<MainMenuCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.View, Is.Not.Null);
            return root;
        }

        private static BattleCompositionRoot FindBattle()
        {
            BattleCompositionRoot root =
                Object.FindAnyObjectByType<BattleCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.CurrentSession, Is.Not.Null);
            return root;
        }

        private static MainMenuLevelChoice FindChoice(MainMenuView view, string levelId)
        {
            for (int index = 0; index < view.LevelChoices.Count; index++)
            {
                if (view.LevelChoices[index].LevelId == levelId)
                {
                    return view.LevelChoices[index];
                }
            }

            Assert.Fail($"Missing configured level choice '{levelId}'.");
            return null;
        }

        private static void UnlockAllMvpLevels()
        {
            var results = new ResultService(
                GameplayConfigRuntime.Current,
                new PlayerPrefsProgressSaveStore());
            results.Settle(new ResultRequest(
                "T660_unlock_normal",
                ConfigIds.Levels.Lv001Tutorial,
                BattleSettlement.Victory,
                new BattleResultMetrics(100000L, 0, 0L, 0d)));
            results.Settle(new ResultRequest(
                "T660_unlock_boss",
                ConfigIds.Levels.Lv002Cave,
                BattleSettlement.Victory,
                new BattleResultMetrics(100000L, 0, 0L, 0d)));
        }

        private static IEnumerator WaitForPlaying(ProductionBattleSession session)
        {
            float deadline = Time.realtimeSinceStartup + 6f;
            while (session.FlowState != BattleFlowState.Playing &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(session.FlowState, Is.EqualTo(BattleFlowState.Playing));
        }

        private static IEnumerator WaitForEnemy(ProductionBattleSession session)
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (session.ActiveEnemyCount == 0 &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(session.ActiveEnemyCount, Is.GreaterThan(0));
        }

        private static StrokeTrailView FindVisibleTrail()
        {
            StrokeTrailView[] views = Object.FindObjectsByType<StrokeTrailView>(
                FindObjectsInactive.Include);
            for (int index = 0; index < views.Length; index++)
            {
                if (views[index].IsActive)
                {
                    return views[index];
                }
            }

            return null;
        }

        private static MeshRenderer FindMeshRenderer(string objectName)
        {
            MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Include);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (string.Equals(
                    renderers[index].gameObject.name,
                    objectName,
                    StringComparison.Ordinal))
                {
                    return renderers[index];
                }
            }

            return null;
        }

        private static void CaptureMenu(MainMenuView view, string outputPath)
        {
            Canvas canvas = view.GetComponent<Canvas>();
            var cameraRoot = new GameObject("T660ScreenshotCamera", typeof(Camera));
            Camera camera = cameraRoot.GetComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 12, 20, 255);
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var renderTexture = new RenderTexture(
                1920,
                1080,
                24,
                RenderTextureFormat.ARGB32);
            renderTexture.Create();
            camera.targetTexture = renderTexture;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var image = new Texture2D(
                1920,
                1080,
                TextureFormat.RGB24,
                mipChain: false);
            image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
            image.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            RenderTexture.active = previous;
            camera.targetTexture = null;
            renderTexture.Release();
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraRoot);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 6f;
            while (SceneManager.GetActiveScene().name != sceneName &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
            yield return null;
        }
    }
}
