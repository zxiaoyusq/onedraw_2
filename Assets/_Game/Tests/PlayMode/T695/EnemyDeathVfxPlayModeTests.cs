using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OneStrokeDemon.Tests.PlayMode.T695
{
    /// <summary>验证怪物死亡特效固定在死亡点、池复用重播首帧，并由生产致死笔势触发。</summary>
    [Category("T695")]
    public sealed class EnemyDeathVfxPlayModeTests : InputTestFixture
    {
        private GameObject galleryRoot;
        private CombatFeedbackRuntime galleryRuntime;

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
            galleryRuntime?.Dispose();
            galleryRuntime = null;
            if (galleryRoot != null)
            {
                Object.DestroyImmediate(galleryRoot);
            }

            BattleLaunchContext.Clear();
            PlayerPrefs.DeleteKey(PlayerPrefsProgressSaveStore.StorageKey);
            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator RuntimeSnapshotsDeathPositionsAndReusedItemsRestartAtFirstFrame()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            galleryRoot = new GameObject("T695 Enemy Death VFX Gallery");
            galleryRoot.layer = LayerMask.NameToLayer("Ignore Raycast");
            Assert.That(galleryRoot.layer, Is.GreaterThanOrEqualTo(0));
            CreateGlobalLight(galleryRoot.transform);
            Camera camera = CreateCamera(galleryRoot.transform);
            galleryRuntime = CombatFeedbackRuntime.Create(
                GameplayConfigRuntime.Current,
                AssetRegistryRuntime.Current,
                new BattleTimeSource(),
                camera,
                galleryRoot.transform);
            var service = new CombatFeedbackService(galleryRuntime.Settings, galleryRuntime);
            var target = new GameObject("T695 Death Target", typeof(SpriteRenderer));
            target.layer = galleryRoot.layer;
            target.transform.SetParent(galleryRoot.transform, false);
            SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
            targetRenderer.sprite = AssetRegistryRuntime.Current.GetSprite(
                ConfigIds.Assets.EnemySkeletonGhost);
            galleryRuntime.RegisterTarget(69501, target.transform, targetRenderer);

            for (int index = 0; index < 6; index += 1)
            {
                target.transform.position = new Vector3(-4f + (index * 1.6f), 0.2f, 0f);
                service.HandleEnemyDeath(69501, $"T695_first_{index}", index + 1d);
            }

            VfxPoolItem[] firstRound = FindActiveDeathVfx();
            Assert.That(firstRound, Has.Length.EqualTo(6));
            var firstRoundSet = new HashSet<VfxPoolItem>(firstRound);
            foreach (VfxPoolItem item in firstRound)
            {
                Assert.That(item.FollowsTarget, Is.False);
                Assert.That(item.GetComponent<SpriteRenderer>().sprite.name,
                    Is.EqualTo("enemy_death_001"));
                Assert.That(
                    item.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Play"),
                    Is.True);
            }

            Vector3[] snapshots = firstRound
                .Select(item => item.transform.position)
                .OrderBy(position => position.x)
                .ToArray();
            target.transform.position = new Vector3(100f, 100f, 0f);
            galleryRuntime.Advance(0.05f);
            yield return null;
            Assert.That(
                firstRound.Select(item => item.transform.position).OrderBy(position => position.x),
                Is.EqualTo(snapshots));

            VfxPoolItem[] visualSequence = firstRound
                .OrderBy(item => item.transform.position.x)
                .ToArray();
            for (int index = 0; index < visualSequence.Length; index += 1)
            {
                visualSequence[index].GetComponent<Animator>().Update(index * 0.12f);
            }

            string screenshotPath = Environment.GetEnvironmentVariable("ONEDRAW_T695_SCREENSHOT");
            if (!string.IsNullOrWhiteSpace(screenshotPath))
            {
                Assert.That(
                    SystemInfo.graphicsDeviceType,
                    Is.Not.EqualTo(GraphicsDeviceType.Null),
                    "T695视觉证据需要图形设备；不要使用-nographics。");
                Capture(camera, screenshotPath);
                Assert.That(new FileInfo(screenshotPath).Length, Is.GreaterThan(10_000));
            }

            galleryRuntime.Advance(1f);
            Assert.That(galleryRuntime.ActiveVfxCount, Is.Zero);
            Assert.That(firstRound.All(item => !item.gameObject.activeSelf), Is.True);

            for (int index = 0; index < 6; index += 1)
            {
                target.transform.position = new Vector3(-4f + (index * 1.6f), -0.4f, 0f);
                service.HandleEnemyDeath(69501, $"T695_second_{index}", 10d + index);
            }

            VfxPoolItem[] secondRound = FindActiveDeathVfx();
            Assert.That(secondRound, Has.Length.EqualTo(6));
            Assert.That(secondRound.All(firstRoundSet.Contains), Is.True,
                "第二轮应只复用首轮的六个预热对象。");
            Assert.That(
                secondRound.All(item =>
                    item.GetComponent<SpriteRenderer>().sprite.name == "enemy_death_001"),
                Is.True,
                "每个复用对象都必须重新采样动画首帧。");
            Assert.That(galleryRuntime.ActiveDamageNumberCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProductionLethalStrokePublishesDeathVfxBeforeEnemyRelease()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
            MainMenuCompositionRoot menu = Object.FindAnyObjectByType<MainMenuCompositionRoot>();
            Assert.That(menu, Is.Not.Null);
            menu.View.StartButton.onClick.Invoke();
            FindChoice(menu.View, ConfigIds.Levels.Lv001Tutorial).Button.onClick.Invoke();
            yield return WaitForScene(SceneNames.Battle);

            BattleCompositionRoot battle = Object.FindAnyObjectByType<BattleCompositionRoot>();
            Assert.That(battle, Is.Not.Null);
            ProductionBattleSession session = battle.CurrentSession;
            Assert.That(session, Is.Not.Null);
            yield return WaitForPlaying(session);
            yield return WaitForEnemy(session);
            EnemyController enemy = Object.FindObjectsByType<EnemyController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .First(controller => controller.IsAlive);
            long softenDamage = enemy.Damage.CurrentHp - 1L;
            Assert.That(softenDamage, Is.GreaterThan(0L));
            enemy.ApplyDamage(
                softenDamage,
                "T695_test_soften",
                Math.Max(Time.timeAsDouble, enemy.State.LastTimestamp));
            Assert.That(enemy.Damage.CurrentHp, Is.EqualTo(1L));
            int enemiesBefore = session.ActiveEnemyCount;

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Vector2 target = Camera.main.WorldToScreenPoint(enemy.transform.position);
            Vector2 start = new Vector2(Screen.width * 0.25f, target.y);
            Assert.That(
                new EventSystemPointerUiBlocker().IsBlocked(
                    start,
                    InputSystemPointerAdapter.MousePointerId),
                Is.False);
            Set(mouse.position, start, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, Vector2.Lerp(start, target, 0.5f), queueEventOnly: true);
            yield return null;
            Set(mouse.position, target, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(session.LastResolvedHitCount, Is.GreaterThan(0));
            Assert.That(session.ActiveEnemyCount, Is.EqualTo(enemiesBefore - 1));
            VfxPoolItem deathVfx = FindActiveDeathVfx().Single();
            Assert.That(deathVfx.IsPlaying, Is.True);
            Assert.That(deathVfx.FollowsTarget, Is.False);
            Assert.That(deathVfx.transform.position, Is.Not.EqualTo(Vector3.zero));
            Vector3 fixedPosition = deathVfx.transform.position;
            yield return null;
            Assert.That(deathVfx.transform.position, Is.EqualTo(fixedPosition));
        }

        private static VfxPoolItem[] FindActiveDeathVfx() =>
            Object.FindObjectsByType<VfxPoolItem>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(item =>
                    item.gameObject.activeInHierarchy &&
                    string.Equals(
                        item.VfxKey,
                        ConfigIds.VfxCues.VfxEnemyDeath,
                        StringComparison.Ordinal))
                .ToArray();

        private static MainMenuLevelChoice FindChoice(MainMenuView view, string levelId)
        {
            for (int index = 0; index < view.LevelChoices.Count; index += 1)
            {
                if (view.LevelChoices[index].LevelId == levelId)
                {
                    return view.LevelChoices[index];
                }
            }

            Assert.Fail($"Missing configured level choice '{levelId}'.");
            return null;
        }

        private static Camera CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("T695 VFX Camera", typeof(Camera));
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 16, 29, 255);
            camera.orthographic = true;
            camera.orthographicSize = 3.2f;
            camera.cullingMask = 1 << parent.gameObject.layer;
            camera.enabled = false;
            return camera;
        }

        // 专项相机只渲染Gallery层，因此配套创建同层Global Light 2D照亮Lit特效。
        private static void CreateGlobalLight(Transform parent)
        {
            var lightObject = new GameObject("T695 VFX Global Light", typeof(Light2D));
            lightObject.layer = parent.gameObject.layer;
            lightObject.transform.SetParent(parent, false);
            Light2D light = lightObject.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
            light.targetSortingLayers = new[] { SortingLayer.NameToID("VFX") };
        }

        private static void Capture(Camera camera, string outputPath)
        {
            var renderTexture = new RenderTexture(
                1920,
                1080,
                24,
                RenderTextureFormat.ARGB32);
            renderTexture.Create();
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            camera.Render();
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
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(renderTexture);
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
    }
}
