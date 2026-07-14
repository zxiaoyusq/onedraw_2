using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Rendering;

namespace OneStrokeDemon.Tests.PlayMode.T650
{
    [Category("T650")]
    public sealed class TutorialSkipPlayModeTests
    {
        private GameObject root;
        private BattleHudView hud;
        private TutorialOverlayRuntime tutorialRuntime;

        [SetUp]
        public void SetUp()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            tutorialRuntime?.Dispose();
            tutorialRuntime = null;
            if (hud != null)
            {
                UnityEngine.Object.DestroyImmediate(hud.gameObject);
                hud = null;
            }

            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                root = null;
            }

            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator SkipAndRestartReleaseAllGatesWhileReviewRemainsAvailable()
        {
            LogAssert.Expect(
                LogType.Log,
                new Regex("CONFIG_RUNTIME_READY.*content=0\\.6\\.2-sample.*records=742"));
            LogAssert.Expect(
                LogType.Log,
                new Regex("ASSET_REGISTRY_READY.*entries=76"));
            yield return SceneManager.LoadSceneAsync(
                SceneNames.Bootstrap,
                LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            root = new GameObject("T650 Tutorial Runtime");
            hud = BattleHudViewFactory.Create(
                GameplayConfigRuntime.Current,
                root.transform);
            var store = new MemoryStore();
            var progress = new ResultService(GameplayConfigRuntime.Current, store);
            var firstWorld = new RecordingSpawnWorld();
            var firstCoordinator = CreateCoordinator(firstWorld);
            tutorialRuntime = TutorialOverlayRuntime.Create(
                GameplayConfigRuntime.Current,
                firstCoordinator,
                progress,
                hud,
                BattleHudLanguage.ZhCN);

            Assert.That(tutorialRuntime.View.OverlayVisible, Is.False);
            Assert.That(tutorialRuntime.View.ReviewButton.gameObject.activeSelf, Is.True);
            firstCoordinator.Advance(
                firstCoordinator.Battle.Flow.Settings.CountdownDurationSeconds);
            Assert.That(tutorialRuntime.View.OverlayVisible, Is.True);
            Assert.That(tutorialRuntime.View.PromptText.text, Is.EqualTo("划过妖怪即可攻击"));
            Assert.That(
                tutorialRuntime.View.GestureGraphic.GestureType,
                Is.EqualTo(TutorialGestureType.Any));
            Assert.That(
                tutorialRuntime.View.ResolvedHighlightTarget,
                Is.EqualTo(hud.SafeAreaRoot));
            Assert.That(tutorialRuntime.View.SkipButton.gameObject.activeSelf, Is.True);
            Assert.That(tutorialRuntime.View.SkipButton.GetComponentInChildren<TMPro.TMP_Text>().text,
                Is.EqualTo("继续战斗"));
            string screenshotPath = Environment.GetEnvironmentVariable(
                "ONEDRAW_T650_SCREENSHOT");
            if (!string.IsNullOrWhiteSpace(screenshotPath))
            {
                Assert.That(
                    SystemInfo.graphicsDeviceType,
                    Is.Not.EqualTo(GraphicsDeviceType.Null),
                    "Tutorial screenshot run requires a graphics device; omit -nographics.");
                CaptureHud(hud, screenshotPath);
                Assert.That(new FileInfo(screenshotPath).Length, Is.GreaterThan(10_000));
            }

            tutorialRuntime.View.SkipButton.onClick.Invoke();
            Assert.That(firstCoordinator.Tutorial.State,
                Is.EqualTo(TutorialSequenceState.Completed));
            Assert.That(firstCoordinator.Battle.Level.IsProgressBlocked, Is.False);
            Assert.That(tutorialRuntime.View.OverlayVisible, Is.False);
            Assert.That(tutorialRuntime.View.ReviewButton.gameObject.activeSelf, Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));

            tutorialRuntime.View.ReviewButton.onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            Assert.That(tutorialRuntime.View.OverlayVisible, Is.True);
            Assert.That(tutorialRuntime.View.LastRendered.IsReview, Is.True);
            Assert.That(tutorialRuntime.View.SkipButton.gameObject.activeSelf, Is.False);
            Assert.That(tutorialRuntime.View.PromptText.isTextOverflowing, Is.False);
            tutorialRuntime.View.ReviewButton.onClick.Invoke();
            Assert.That(tutorialRuntime.View.OverlayVisible, Is.False);

            DriveToVictory(firstCoordinator, firstWorld);
            Assert.That(firstCoordinator.Battle.Flow.State, Is.EqualTo(BattleFlowState.Victory));
            Assert.That(firstWorld.TotalSpawned, Is.EqualTo(15));

            tutorialRuntime.Dispose();
            tutorialRuntime = null;
            yield return null;

            var restartedWorld = new RecordingSpawnWorld();
            var restartedCoordinator = CreateCoordinator(restartedWorld);
            tutorialRuntime = TutorialOverlayRuntime.Create(
                GameplayConfigRuntime.Current,
                restartedCoordinator,
                progress,
                hud,
                BattleHudLanguage.ZhCN);

            Assert.That(restartedCoordinator.Tutorial.State,
                Is.EqualTo(TutorialSequenceState.Completed));
            Assert.That(restartedCoordinator.Battle.Level.IsProgressBlocked, Is.False);
            Assert.That(tutorialRuntime.View.OverlayVisible, Is.False);
            Assert.That(tutorialRuntime.View.ReviewButton.gameObject.activeSelf, Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            tutorialRuntime.View.ReviewButton.onClick.Invoke();
            Assert.That(tutorialRuntime.View.PromptText.text, Is.EqualTo("划过妖怪即可攻击"));
            tutorialRuntime.View.ReviewButton.onClick.Invoke();

            restartedCoordinator.Advance(
                restartedCoordinator.Battle.Flow.Settings.CountdownDurationSeconds);
            Assert.That(restartedCoordinator.Battle.Flow.State,
                Is.EqualTo(BattleFlowState.Playing));
            Assert.That(restartedCoordinator.Battle.Level.IsProgressBlocked, Is.False);
            DriveToVictory(restartedCoordinator, restartedWorld);
            Assert.That(restartedCoordinator.Battle.Flow.State,
                Is.EqualTo(BattleFlowState.Victory));
            Assert.That(restartedWorld.TotalSpawned, Is.EqualTo(15));
            Assert.That(store.WriteCount, Is.EqualTo(1));
            yield return null;
        }

        private static void DriveToVictory(
            TutorialLevelCoordinator coordinator,
            RecordingSpawnWorld world)
        {
            for (int iteration = 0;
                 iteration < 1200 &&
                 coordinator.Battle.Flow.State != BattleFlowState.Victory;
                 iteration += 1)
            {
                coordinator.Advance(0.25d);
                long[] ids = world.ActiveEntityIds;
                for (int index = 0; index < ids.Length; index += 1)
                {
                    Assert.That(coordinator.NotifyEnemyDefeated(ids[index]), Is.True);
                    world.Release(ids[index]);
                }
            }

            Assert.That(
                coordinator.Battle.Flow.State,
                Is.EqualTo(BattleFlowState.Victory),
                "Skipped tutorial battle did not settle within the configured 180-second level limit.");
        }

        private static TutorialLevelCoordinator CreateCoordinator(
            RecordingSpawnWorld world)
        {
            return new TutorialLevelCoordinator(
                GameplayConfigRuntime.Current,
                ConfigIds.Players.PlayerMoyan,
                ConfigIds.Levels.Lv001Tutorial,
                world);
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

        private static void CaptureHud(BattleHudView view, string outputPath)
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include);
            foreach (Canvas candidate in canvases)
            {
                if (candidate.transform != view.transform &&
                    !candidate.transform.IsChildOf(view.transform))
                {
                    candidate.enabled = false;
                }
            }

            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.transform != view.transform &&
                    !renderer.transform.IsChildOf(view.transform))
                {
                    renderer.enabled = false;
                }
            }

            Canvas canvas = view.GetComponent<Canvas>();
            var cameraRoot = new GameObject("T650ScreenshotCamera", typeof(Camera));
            Camera camera = cameraRoot.GetComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 16, 29, 255);
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
            RenderTexture.active = previous;
            camera.targetTexture = null;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(cameraRoot);
        }

        private sealed class RecordingSpawnWorld : ILevelSpawnWorld
        {
            private readonly HashSet<long> active = new HashSet<long>();
            private long nextEntityId = 1L;

            public int TotalSpawned { get; private set; }

            public long[] ActiveEntityIds
            {
                get
                {
                    var ids = new long[active.Count];
                    active.CopyTo(ids);
                    Array.Sort(ids);
                    return ids;
                }
            }

            public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
            {
                entityId = nextEntityId++;
                active.Add(entityId);
                TotalSpawned += 1;
                return true;
            }

            public void Release(long entityId)
            {
                Assert.That(active.Remove(entityId), Is.True);
            }
        }

        private sealed class MemoryStore : IProgressSaveStore
        {
            public string Payload { get; private set; }

            public int WriteCount { get; private set; }

            public bool TryRead(out string payload)
            {
                payload = Payload;
                return payload != null;
            }

            public void Write(string payload)
            {
                Payload = payload;
                WriteCount += 1;
            }
        }
    }
}
