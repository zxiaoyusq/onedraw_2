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

namespace OneStrokeDemon.Tests.PlayMode.T694
{
    /// <summary>验证生产入口中的主角待机会播放，普通有效笔势会触发攻击并自动返回待机。</summary>
    [Category("T694")]
    public sealed class MoyanAnimationProductionPlayModeTests : InputTestFixture
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
        public IEnumerator ValidProductionStrokePlaysAttackThenReturnsToIdle()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
            MainMenuCompositionRoot menu =
                Object.FindAnyObjectByType<MainMenuCompositionRoot>();
            Assert.That(menu, Is.Not.Null);
            menu.View.StartButton.onClick.Invoke();
            FindChoice(menu.View, ConfigIds.Levels.Lv001Tutorial).Button.onClick.Invoke();
            yield return WaitForScene(SceneNames.Battle);

            BattleCompositionRoot battle =
                Object.FindAnyObjectByType<BattleCompositionRoot>();
            Assert.That(battle, Is.Not.Null);
            ProductionBattleSession session = battle.CurrentSession;
            Assert.That(session, Is.Not.Null);
            yield return WaitForPlaying(session);
            yield return new WaitForSecondsRealtime(0.6f);

            Animator animator = session.Player.GetComponent<Animator>();
            SpriteRenderer renderer = session.Player.GetComponent<SpriteRenderer>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
            Sprite initialFrame = renderer.sprite;
            float idleDeadline = Time.realtimeSinceStartup + 0.5f;
            while (renderer.sprite == initialFrame && Time.realtimeSinceStartup < idleDeadline)
            {
                yield return null;
            }

            Assert.That(renderer.sprite, Is.Not.SameAs(initialFrame));
            yield return WaitForEnemy(session);
            EnemyController enemy = Object.FindAnyObjectByType<EnemyController>();
            Assert.That(enemy, Is.Not.Null);

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

            Assert.That(session.CompletedStrokeCount, Is.EqualTo(1));
            float attackDeadline = Time.realtimeSinceStartup + 0.3f;
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") &&
                   Time.realtimeSinceStartup < attackDeadline)
            {
                yield return null;
            }

            Assert.That(
                animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"),
                Is.True,
                "普通有效笔势应触发主角攻击动画。");
            string screenshotPath =
                Environment.GetEnvironmentVariable("ONEDRAW_T694_SCREENSHOT");
            if (!string.IsNullOrWhiteSpace(screenshotPath))
            {
                Assert.That(
                    SystemInfo.graphicsDeviceType,
                    Is.Not.EqualTo(GraphicsDeviceType.Null),
                    "T694视觉证据需要图形设备；不要使用-nographics。");
                CaptureBattle(screenshotPath);
                Assert.That(new FileInfo(screenshotPath).Length, Is.GreaterThan(10_000));
            }

            float idleReturnDeadline = Time.realtimeSinceStartup + 1.3f;
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") &&
                   Time.realtimeSinceStartup < idleReturnDeadline)
            {
                yield return null;
            }

            Assert.That(
                animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),
                Is.True,
                "非循环攻击播放完成后应自动返回待机。");
        }

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

        private static void CaptureBattle(string outputPath)
        {
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
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
    }
}
