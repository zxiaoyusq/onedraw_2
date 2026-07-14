using System;
using System.Globalization;
using System.Text;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;

namespace OneStrokeDemon.Presentation
{
    public sealed class BattleHudPresenter : IDisposable
    {
        private readonly IConfigProvider configProvider;
        private readonly IBattleHudStateSource source;
        private readonly IBattleHudView view;
        private readonly IBattleHudCommandSink commands;
        private readonly BattleHudTextCatalog text;
        private readonly SkillConfig ultimateSkill;
        private BattleHudViewModel current;
        private bool disposed;

        public BattleHudPresenter(
            IConfigProvider configuredProvider,
            IBattleHudStateSource stateSource,
            IBattleHudView hudView,
            IBattleHudCommandSink commandSink,
            string playerId,
            BattleHudLanguage language = BattleHudLanguage.ZhCN)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            source = stateSource ?? throw new ArgumentNullException(nameof(stateSource));
            view = hudView ?? throw new ArgumentNullException(nameof(hudView));
            commands = commandSink ?? throw new ArgumentNullException(nameof(commandSink));
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException("Player id must be non-empty.", nameof(playerId));
            }

            PlayerConfig player = configProvider.GetPlayer(playerId);
            ultimateSkill = configProvider.GetSkill(player.UltimateSkillId);
            text = new BattleHudTextCatalog(configProvider, language);

            source.Changed += OnStateChanged;
            view.PauseToggleRequested += OnPauseToggleRequested;
            view.StanceSwitchRequested += OnStanceSwitchRequested;
            view.UltimateRequested += OnUltimateRequested;
            view.RestartRequested += OnRestartRequested;
            view.NextLevelRequested += OnNextLevelRequested;
            view.MainMenuRequested += OnMainMenuRequested;
            Present(source.Current);
        }

        public BattleHudViewModel Current => current;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            source.Changed -= OnStateChanged;
            view.PauseToggleRequested -= OnPauseToggleRequested;
            view.StanceSwitchRequested -= OnStanceSwitchRequested;
            view.UltimateRequested -= OnUltimateRequested;
            view.RestartRequested -= OnRestartRequested;
            view.NextLevelRequested -= OnNextLevelRequested;
            view.MainMenuRequested -= OnMainMenuRequested;
        }

        private void OnStateChanged(BattleHudState state)
        {
            Present(state);
        }

        private void Present(in BattleHudState state)
        {
            if (!state.IsInitialized)
            {
                throw new ArgumentException("HUD state must be initialized.", nameof(state));
            }

            LevelConfig level = configProvider.GetLevel(state.LevelId);
            StanceConfig stance = configProvider.GetStance(state.StanceId);
            double cooldownRemaining = Math.Max(
                0d,
                state.UltimateCooldownUntil - state.Timestamp);
            bool correctStance = string.IsNullOrEmpty(ultimateSkill.RequiredStanceId) ||
                                 string.Equals(
                                     ultimateSkill.RequiredStanceId,
                                     state.StanceId,
                                     StringComparison.Ordinal);
            bool enoughEnergy = state.CurrentEnergy >= ultimateSkill.EnergyCost;
            bool playerAlive = state.CurrentHp > 0L;
            bool terminal = state.Result != null ||
                            state.FlowState == BattleFlowState.Victory ||
                            state.FlowState == BattleFlowState.Defeat;
            bool paused = state.FlowState == BattleFlowState.Paused;
            bool ultimateVisible = !terminal;
            bool ultimateInteractable = state.FlowState == BattleFlowState.Playing &&
                                        playerAlive &&
                                        correctStance &&
                                        enoughEnergy &&
                                        cooldownRemaining <= 0d;

            string ultimateStatus = BuildUltimateStatus(
                state,
                cooldownRemaining,
                correctStance,
                enoughEnergy,
                terminal);
            BattleHudResultState result = state.Result;
            bool resultVisible = result != null;
            string rewardsBody = resultVisible
                ? BuildRewardBody(result)
                : string.Empty;

            current = new BattleHudViewModel(
                text.Get(level.DisplayNameKey),
                text.Hp,
                Ratio(state.CurrentHp, state.MaximumHp),
                Normalize(state.CurrentHp, state.MaximumHp),
                text.Energy,
                Ratio(state.CurrentEnergy, state.MaximumEnergy),
                Normalize(state.CurrentEnergy, state.MaximumEnergy),
                text.Combo,
                Integer(state.ComboCount),
                state.ComboCount > 0,
                text.Score,
                Integer(state.LiveScore),
                text.Stance,
                text.Get(stance.DisplayNameKey),
                state.FlowState == BattleFlowState.Playing && playerAlive && !terminal,
                text.Get(ultimateSkill.DisplayNameKey),
                ultimateStatus,
                NormalizeCooldown(cooldownRemaining, ultimateSkill.CooldownSec),
                ultimateVisible,
                ultimateInteractable,
                paused ? text.Resume : text.Pause,
                !terminal,
                CanTogglePause(state.FlowState) && !terminal,
                text.Paused,
                paused && !resultVisible,
                resultVisible,
                resultVisible
                    ? (result.Settlement == BattleSettlement.Victory
                        ? text.Victory
                        : text.Defeat)
                    : string.Empty,
                text.Score,
                resultVisible ? Integer(result.FinalScore) : string.Empty,
                text.Stars,
                resultVisible ? $"{result.Stars.ToString(CultureInfo.InvariantCulture)} / 3" : string.Empty,
                text.Rewards,
                rewardsBody,
                resultVisible && result.Rewards.Count > 0,
                text.Restart,
                text.NextLevel,
                resultVisible && result.CanGoNext,
                text.MainMenu,
                paused || resultVisible);
            view.Render(current);
        }

        private void OnStanceSwitchRequested()
        {
            if (current != null && current.StanceInteractable)
            {
                commands.SwitchStance();
            }
        }

        private string BuildUltimateStatus(
            in BattleHudState state,
            double cooldownRemaining,
            bool correctStance,
            bool enoughEnergy,
            bool terminal)
        {
            if (terminal || state.CurrentHp == 0L)
            {
                return string.Empty;
            }

            if (cooldownRemaining > 0d)
            {
                long displayed = checked((long)Math.Ceiling(cooldownRemaining));
                return $"{text.Cooldown} {Integer(displayed)}";
            }

            if (!correctStance)
            {
                StanceConfig required = configProvider.GetStance(ultimateSkill.RequiredStanceId);
                return $"{text.Stance} {text.Get(required.DisplayNameKey)}";
            }

            if (!enoughEnergy)
            {
                return $"{text.Energy} {Ratio(state.CurrentEnergy, ultimateSkill.EnergyCost)}";
            }

            return state.FlowState == BattleFlowState.Playing
                ? text.Ready
                : string.Empty;
        }

        private string BuildRewardBody(BattleHudResultState result)
        {
            if (result.Rewards.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int index = 0; index < result.Rewards.Count; index += 1)
            {
                if (index > 0)
                {
                    builder.Append('\n');
                }

                BattleHudRewardState reward = result.Rewards[index];
                switch (reward.Type)
                {
                    case RewardGrantType.UnlockLevel:
                        LevelConfig unlocked = configProvider.GetLevel(reward.RewardId);
                        builder.Append(text.RewardLevel)
                            .Append(": ")
                            .Append(text.Get(unlocked.DisplayNameKey));
                        break;
                    case RewardGrantType.UnlockFeature:
                        builder.Append(text.RewardFeature);
                        break;
                    case RewardGrantType.ScoreToken:
                        builder.Append(text.RewardScoreToken)
                            .Append(": +")
                            .Append(Integer(reward.Amount));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(result),
                            reward.Type,
                            "Unsupported HUD reward type.");
                }
            }

            return builder.ToString();
        }

        private void OnPauseToggleRequested()
        {
            if (current != null && current.PauseButtonInteractable)
            {
                commands.SetPlayerPaused(source.Current.FlowState != BattleFlowState.Paused);
            }
        }

        private void OnUltimateRequested()
        {
            if (current != null && current.UltimateInteractable)
            {
                commands.BeginUltimateDrawing();
            }
        }

        private void OnRestartRequested()
        {
            if (current != null && current.ResultVisible)
            {
                commands.Restart();
            }
        }

        private void OnNextLevelRequested()
        {
            if (current != null && current.ResultVisible && current.NextLevelVisible)
            {
                commands.GoNext();
            }
        }

        private void OnMainMenuRequested()
        {
            if (current != null && current.MainMenuVisible)
            {
                commands.ReturnToMainMenu();
            }
        }

        private static bool CanTogglePause(BattleFlowState state)
        {
            return state == BattleFlowState.Countdown ||
                   state == BattleFlowState.Playing ||
                   state == BattleFlowState.UltimateDrawing ||
                   state == BattleFlowState.Paused;
        }

        private static float Normalize(long current, long maximum)
        {
            return maximum <= 0L ? 0f : (float)((double)current / maximum);
        }

        private static float NormalizeCooldown(double remaining, double configuredCooldown)
        {
            if (configuredCooldown <= 0d)
            {
                return 0f;
            }

            return (float)Math.Min(1d, Math.Max(0d, remaining / configuredCooldown));
        }

        private static string Ratio(long current, long maximum)
        {
            return $"{Integer(current)} / {Integer(maximum)}";
        }

        private static string Integer(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal sealed class BattleHudTextCatalog
    {
        private readonly IConfigProvider configProvider;
        private readonly BattleHudLanguage language;

        public BattleHudTextCatalog(
            IConfigProvider configuredProvider,
            BattleHudLanguage configuredLanguage)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            language = configuredLanguage;
            Hp = Get(ConfigIds.Texts.TextUiHp);
            Energy = Get(ConfigIds.Texts.TextUiEnergy);
            Combo = Get(ConfigIds.Texts.TextUiCombo);
            Score = Get(ConfigIds.Texts.TextUiScore);
            Stance = Get(ConfigIds.Texts.TextUiStance);
            Cooldown = Get(ConfigIds.Texts.TextUiCooldown);
            Ready = Get(ConfigIds.Texts.TextUiReady);
            Pause = Get(ConfigIds.Texts.TextUiPause);
            Resume = Get(ConfigIds.Texts.TextUiResume);
            Paused = Get(ConfigIds.Texts.TextUiPaused);
            Victory = Get(ConfigIds.Texts.TextUiVictory);
            Defeat = Get(ConfigIds.Texts.TextUiDefeat);
            Stars = Get(ConfigIds.Texts.TextUiStars);
            Rewards = Get(ConfigIds.Texts.TextUiRewards);
            RewardLevel = Get(ConfigIds.Texts.TextUiRewardLevel);
            RewardFeature = Get(ConfigIds.Texts.TextUiRewardFeature);
            RewardScoreToken = Get(ConfigIds.Texts.TextUiRewardScoreToken);
            Restart = Get(ConfigIds.Texts.TextUiRestart);
            NextLevel = Get(ConfigIds.Texts.TextUiNextLevel);
            MainMenu = Get(ConfigIds.Texts.TextUiMainMenu);
        }

        public string Hp { get; }
        public string Energy { get; }
        public string Combo { get; }
        public string Score { get; }
        public string Stance { get; }
        public string Cooldown { get; }
        public string Ready { get; }
        public string Pause { get; }
        public string Resume { get; }
        public string Paused { get; }
        public string Victory { get; }
        public string Defeat { get; }
        public string Stars { get; }
        public string Rewards { get; }
        public string RewardLevel { get; }
        public string RewardFeature { get; }
        public string RewardScoreToken { get; }
        public string Restart { get; }
        public string NextLevel { get; }
        public string MainMenu { get; }

        public string Get(string textKey)
        {
            TextConfig configured = configProvider.GetText(textKey);
            string value = language == BattleHudLanguage.ZhCN
                ? configured.ZhCN
                : configured.EnUS;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"HUD text '{textKey}' is empty for language '{language}'.");
            }

            return value;
        }
    }
}
