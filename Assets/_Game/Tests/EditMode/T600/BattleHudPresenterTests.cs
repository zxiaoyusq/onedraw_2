using System;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T600
{
    [Category("T600")]
    public sealed class BattleHudPresenterTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), "test:T600:hud-presenter");
        }

        [Test]
        public void ConfiguredChineseHudTracksCombatPauseAndUltimateReadiness()
        {
            var source = new MutableHudSource(CreateState(energy: 0L));
            var view = new RecordingHudView();
            var commands = new RecordingHudCommands();
            using var presenter = new BattleHudPresenter(
                config,
                source,
                view,
                commands,
                ConfigIds.Players.PlayerMoyan,
                BattleHudLanguage.ZhCN);

            Assert.That(view.Last.LevelName, Is.EqualTo("幽菌古道"));
            Assert.That(view.Last.HpLabel, Is.EqualTo("生命"));
            Assert.That(view.Last.HpValue, Is.EqualTo("100 / 100"));
            Assert.That(view.Last.EnergyLabel, Is.EqualTo("能量"));
            Assert.That(view.Last.EnergyValue, Is.EqualTo("0 / 100"));
            Assert.That(view.Last.StanceValue, Is.EqualTo("斩妖刀"));
            Assert.That(view.Last.UltimateLabel, Is.EqualTo("天地封妖令"));
            Assert.That(view.Last.UltimateStatus, Is.EqualTo("能量 0 / 100"));
            Assert.That(view.Last.UltimateInteractable, Is.False);

            view.RequestUltimate();
            Assert.That(commands.UltimateCount, Is.Zero);

            source.Emit(CreateState(energy: 100L, combo: 3, score: 521L));
            Assert.That(view.Last.ComboVisible, Is.True);
            Assert.That(view.Last.ComboValue, Is.EqualTo("3"));
            Assert.That(view.Last.ScoreValue, Is.EqualTo("521"));
            Assert.That(view.Last.UltimateStatus, Is.EqualTo("可释放"));
            Assert.That(view.Last.UltimateInteractable, Is.True);
            view.RequestUltimate();
            Assert.That(commands.UltimateCount, Is.EqualTo(1));

            source.Emit(CreateState(
                energy: 100L,
                combo: 3,
                score: 521L,
                timestamp: 2d,
                cooldownUntil: 5d));
            Assert.That(view.Last.UltimateStatus, Is.EqualTo("冷却 3"));
            Assert.That(view.Last.UltimateInteractable, Is.False);

            view.RequestPauseToggle();
            Assert.That(commands.PauseRequests, Is.EqualTo(new[] { true }));
            view.RequestStanceSwitch();
            Assert.That(commands.StanceSwitchCount, Is.EqualTo(1));
            source.Emit(CreateState(
                energy: 100L,
                flow: BattleFlowState.Paused));
            Assert.That(view.Last.PauseOverlayVisible, Is.True);
            Assert.That(view.Last.PausedTitle, Is.EqualTo("战斗已暂停"));
            Assert.That(view.Last.PauseButtonText, Is.EqualTo("继续"));
            Assert.That(view.Last.MainMenuVisible, Is.True);
            view.RequestPauseToggle();
            Assert.That(commands.PauseRequests, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void ResultPanelFormatsConfiguredVictoryRewardsAndGatesCommands()
        {
            var source = new MutableHudSource(CreateState(energy: 100L));
            var view = new RecordingHudView();
            var commands = new RecordingHudCommands();
            using var presenter = new BattleHudPresenter(
                config,
                source,
                view,
                commands,
                ConfigIds.Players.PlayerMoyan);
            var result = new BattleHudResultState(
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
                canGoNext: true);

            source.Emit(CreateState(
                energy: 100L,
                score: 2000L,
                flow: BattleFlowState.Victory,
                result: result));

            Assert.That(view.Last.ResultVisible, Is.True);
            Assert.That(view.Last.ResultTitle, Is.EqualTo("胜利"));
            Assert.That(view.Last.ResultScoreValue, Is.EqualTo("4480"));
            Assert.That(view.Last.StarsValue, Is.EqualTo("2 / 3"));
            Assert.That(view.Last.RewardsBody, Is.EqualTo(
                "解锁关卡: 百鬼回廊\n镇妖积分: +100"));
            Assert.That(view.Last.RestartText, Is.EqualTo("重新挑战"));
            Assert.That(view.Last.NextLevelVisible, Is.True);
            Assert.That(view.Last.MainMenuVisible, Is.True);
            Assert.That(view.Last.PauseButtonVisible, Is.False);
            Assert.That(view.Last.UltimateVisible, Is.False);

            view.RequestRestart();
            view.RequestNextLevel();
            view.RequestMainMenu();
            Assert.That(commands.RestartCount, Is.EqualTo(1));
            Assert.That(commands.NextCount, Is.EqualTo(1));
            Assert.That(commands.MainMenuCount, Is.EqualTo(1));

            source.Emit(CreateState(
                energy: 100L,
                flow: BattleFlowState.Defeat,
                result: new BattleHudResultState(
                    BattleSettlement.Defeat,
                    123L,
                    0,
                    Array.Empty<BattleHudRewardState>(),
                    canGoNext: false)));
            Assert.That(view.Last.ResultTitle, Is.EqualTo("败北"));
            Assert.That(view.Last.RewardsVisible, Is.False);
            Assert.That(view.Last.NextLevelVisible, Is.False);
            view.RequestNextLevel();
            Assert.That(commands.NextCount, Is.EqualTo(1));
        }

        [Test]
        public void EnglishLocaleAndDisposalRemainDeterministic()
        {
            var source = new MutableHudSource(CreateState(energy: 100L));
            var view = new RecordingHudView();
            var commands = new RecordingHudCommands();
            var presenter = new BattleHudPresenter(
                config,
                source,
                view,
                commands,
                ConfigIds.Players.PlayerMoyan,
                BattleHudLanguage.EnUS);

            Assert.That(view.Last.LevelName, Is.EqualTo("Luminous Fungi Path"));
            Assert.That(view.Last.HpLabel, Is.EqualTo("HP"));
            Assert.That(view.Last.UltimateStatus, Is.EqualTo("Ready"));
            int renderCount = view.RenderCount;

            presenter.Dispose();
            source.Emit(CreateState(energy: 0L));
            view.RequestUltimate();

            Assert.That(view.RenderCount, Is.EqualTo(renderCount));
            Assert.That(commands.UltimateCount, Is.Zero);
            Assert.That(() => presenter.Dispose(), Throws.Nothing);
        }

        private static BattleHudState CreateState(
            long energy,
            int combo = 0,
            long score = 0L,
            BattleFlowState flow = BattleFlowState.Playing,
            double timestamp = 0d,
            double cooldownUntil = 0d,
            BattleHudResultState result = null)
        {
            return new BattleHudState(
                ConfigIds.Levels.Lv001Tutorial,
                currentHp: 100L,
                maximumHp: 100L,
                currentEnergy: energy,
                maximumEnergy: 100L,
                ConfigIds.Stances.StanceBlade,
                combo,
                score,
                flow,
                timestamp,
                cooldownUntil,
                result);
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

        private sealed class RecordingHudView : IBattleHudView
        {
            public event Action PauseToggleRequested;
            public event Action StanceSwitchRequested;
            public event Action UltimateRequested;
            public event Action RestartRequested;
            public event Action NextLevelRequested;
            public event Action MainMenuRequested;

            public BattleHudViewModel Last { get; private set; }
            public int RenderCount { get; private set; }

            public void Render(BattleHudViewModel model)
            {
                Last = model;
                RenderCount += 1;
            }

            public void RequestPauseToggle() => PauseToggleRequested?.Invoke();
            public void RequestStanceSwitch() => StanceSwitchRequested?.Invoke();
            public void RequestUltimate() => UltimateRequested?.Invoke();
            public void RequestRestart() => RestartRequested?.Invoke();
            public void RequestNextLevel() => NextLevelRequested?.Invoke();
            public void RequestMainMenu() => MainMenuRequested?.Invoke();
        }

        private sealed class RecordingHudCommands : IBattleHudCommandSink
        {
            public System.Collections.Generic.List<bool> PauseRequests { get; } =
                new System.Collections.Generic.List<bool>();
            public int UltimateCount { get; private set; }
            public int StanceSwitchCount { get; private set; }
            public int RestartCount { get; private set; }
            public int NextCount { get; private set; }
            public int MainMenuCount { get; private set; }

            public void SetPlayerPaused(bool paused) => PauseRequests.Add(paused);
            public void SwitchStance() => StanceSwitchCount += 1;
            public void BeginUltimateDrawing() => UltimateCount += 1;
            public void Restart() => RestartCount += 1;
            public void GoNext() => NextCount += 1;
            public void ReturnToMainMenu() => MainMenuCount += 1;
        }
    }
}
