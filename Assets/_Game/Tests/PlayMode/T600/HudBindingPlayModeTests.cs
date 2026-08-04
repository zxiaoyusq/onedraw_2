using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T600
{
    [Category("T600")]
    public sealed class HudBindingPlayModeTests
    {
        private BattleHudRuntime runtime;

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
        }

        [UnityTest]
        public IEnumerator RuntimeViewHonorsSafeAreaRendersStatesAndForwardsButtons()
        {
            LogAssert.Expect(
                LogType.Log,
                new Regex("CONFIG_RUNTIME_READY.*content=0\\.6\\.10-sample.*records=765"));
            LogAssert.Expect(
                LogType.Log,
                new Regex("ASSET_REGISTRY_READY.*entries=78"));
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            var source = new MutableHudSource(CreateState(
                energy: 100L,
                combo: 4,
                score: 521L));
            var commands = new RecordingCommands();
            runtime = BattleHudRuntime.Create(
                GameplayConfigRuntime.Current,
                source,
                commands,
                ConfigIds.Players.PlayerMoyan,
                BattleHudLanguage.EnUS);
            BattleHudView view = runtime.View;

            Assert.That(view.IsInitialized, Is.True);
            Assert.That(view.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(view.SafeAreaRoot.parent, Is.EqualTo(view.transform));
            Assert.That(view.HpValueText.text, Is.EqualTo("100 / 100"));
            Assert.That(view.EnergyValueText.text, Is.EqualTo("100 / 100"));
            Assert.That(view.ComboValueText.text, Is.EqualTo("4"));
            Assert.That(view.ScoreValueText.text, Is.EqualTo("521"));
            Assert.That(view.StanceValueText.text, Is.EqualTo("Demon Blade"));

            view.ApplySafeArea(
                new Rect(100f, 50f, 1720f, 980f),
                new Vector2(1920f, 1080f));
            Assert.That(view.SafeAreaRoot.anchorMin.x, Is.EqualTo(100f / 1920f).Within(0.000001f));
            Assert.That(view.SafeAreaRoot.anchorMin.y, Is.EqualTo(50f / 1080f).Within(0.000001f));
            Assert.That(view.SafeAreaRoot.anchorMax.x, Is.EqualTo(1820f / 1920f).Within(0.000001f));
            Assert.That(view.SafeAreaRoot.anchorMax.y, Is.EqualTo(1030f / 1080f).Within(0.000001f));
            for (int index = 0; index < view.SafeAreaRoot.childCount; index += 1)
            {
                Assert.That(
                    view.SafeAreaRoot.GetChild(index).GetComponent<RectTransform>(),
                    Is.Not.Null,
                    $"Safe-area child {index}");
            }

            view.UltimateButton.onClick.Invoke();
            view.PauseButton.onClick.Invoke();
            Assert.That(commands.UltimateCount, Is.EqualTo(1));
            Assert.That(commands.LastPauseRequest, Is.True);

            source.Emit(CreateState(100L, flow: BattleFlowState.Paused));
            Assert.That(view.LastRendered.PauseOverlayVisible, Is.True);
            Assert.That(view.LastRendered.MainMenuVisible, Is.True);
            view.MainMenuButton.onClick.Invoke();
            Assert.That(commands.MainMenuCount, Is.EqualTo(1));

            source.Emit(CreateState(
                100L,
                flow: BattleFlowState.Victory,
                result: new BattleHudResultState(
                    BattleSettlement.Victory,
                    4480L,
                    2,
                    new[]
                    {
                        new BattleHudRewardState(
                            RewardGrantType.ScoreToken,
                            "score_token",
                            100L),
                    },
                    canGoNext: true)));
            Assert.That(view.ResultTitleText.text, Is.EqualTo("Victory"));
            Assert.That(view.LastRendered.RewardsBody, Is.EqualTo("Demon Score: +100"));
            Assert.That(view.NextLevelButton.gameObject.activeSelf, Is.True);
            view.RestartButton.onClick.Invoke();
            view.NextLevelButton.onClick.Invoke();
            Assert.That(commands.RestartCount, Is.EqualTo(1));
            Assert.That(commands.NextCount, Is.EqualTo(1));

            GameObject root = view.gameObject;
            runtime.Dispose();
            runtime = null;
            yield return null;
            Assert.That(root == null, Is.True);
        }

        private static BattleHudState CreateState(
            long energy,
            int combo = 0,
            long score = 0L,
            BattleFlowState flow = BattleFlowState.Playing,
            BattleHudResultState result = null)
        {
            return new BattleHudState(
                ConfigIds.Levels.Lv001Tutorial,
                100L,
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
            public MutableHudSource(BattleHudState initial)
            {
                Current = initial;
            }

            public event Action<BattleHudState> Changed;

            public BattleHudState Current { get; private set; }

            public void Emit(BattleHudState state)
            {
                Current = state;
                Changed?.Invoke(state);
            }
        }

        private sealed class RecordingCommands : IBattleHudCommandSink
        {
            public bool LastPauseRequest { get; private set; }
            public int UltimateCount { get; private set; }
            public int RestartCount { get; private set; }
            public int NextCount { get; private set; }
            public int MainMenuCount { get; private set; }

            public void SetPlayerPaused(bool paused) => LastPauseRequest = paused;
            public void SwitchStance() { }
            public void BeginUltimateDrawing() => UltimateCount += 1;
            public void Restart() => RestartCount += 1;
            public void GoNext() => NextCount += 1;
            public void ReturnToMainMenu() => MainMenuCount += 1;
        }
    }
}
