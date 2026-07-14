using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Levels = OneStrokeDemon.Levels;

namespace OneStrokeDemon.Presentation
{
    public enum BattleHudLanguage
    {
        ZhCN = 0,
        EnUS = 1,
    }

    public readonly struct BattleHudRewardState
    {
        public BattleHudRewardState(
            Levels.RewardGrantType type,
            string rewardId,
            long amount)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("Reward id must be non-empty.", nameof(rewardId));
            }

            if (amount <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Type = type;
            RewardId = rewardId;
            Amount = amount;
        }

        public Levels.RewardGrantType Type { get; }

        public string RewardId { get; }

        public long Amount { get; }
    }

    public sealed class BattleHudResultState
    {
        public BattleHudResultState(
            Levels.BattleSettlement settlement,
            long finalScore,
            int stars,
            BattleHudRewardState[] rewards,
            bool canGoNext)
        {
            if (settlement != Levels.BattleSettlement.Victory &&
                settlement != Levels.BattleSettlement.Defeat)
            {
                throw new ArgumentOutOfRangeException(nameof(settlement));
            }

            if (finalScore < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(finalScore));
            }

            if (stars < 0 || stars > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(stars));
            }

            Settlement = settlement;
            FinalScore = finalScore;
            Stars = stars;
            Rewards = rewards == null || rewards.Length == 0
                ? Array.AsReadOnly(Array.Empty<BattleHudRewardState>())
                : new ReadOnlyCollection<BattleHudRewardState>(
                    (BattleHudRewardState[])rewards.Clone());
            CanGoNext = canGoNext;
        }

        public Levels.BattleSettlement Settlement { get; }

        public long FinalScore { get; }

        public int Stars { get; }

        public IReadOnlyList<BattleHudRewardState> Rewards { get; }

        public bool CanGoNext { get; }
    }

    public readonly struct BattleHudState
    {
        public BattleHudState(
            string levelId,
            long currentHp,
            long maximumHp,
            long currentEnergy,
            long maximumEnergy,
            string stanceId,
            int comboCount,
            long liveScore,
            Levels.BattleFlowState flowState,
            double timestamp,
            double ultimateCooldownUntil,
            BattleHudResultState result)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("Level id must be non-empty.", nameof(levelId));
            }

            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException("Stance id must be non-empty.", nameof(stanceId));
            }

            if (maximumHp <= 0L || currentHp < 0L || currentHp > maximumHp)
            {
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            }

            if (maximumEnergy <= 0L || currentEnergy < 0L || currentEnergy > maximumEnergy)
            {
                throw new ArgumentOutOfRangeException(nameof(currentEnergy));
            }

            if (comboCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(comboCount));
            }

            if (liveScore < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(liveScore));
            }

            RequireFiniteNonNegative(timestamp, nameof(timestamp));
            RequireFiniteNonNegative(ultimateCooldownUntil, nameof(ultimateCooldownUntil));

            LevelId = levelId;
            CurrentHp = currentHp;
            MaximumHp = maximumHp;
            CurrentEnergy = currentEnergy;
            MaximumEnergy = maximumEnergy;
            StanceId = stanceId;
            ComboCount = comboCount;
            LiveScore = liveScore;
            FlowState = flowState;
            Timestamp = timestamp;
            UltimateCooldownUntil = ultimateCooldownUntil;
            Result = result;
            IsInitialized = true;
        }

        public string LevelId { get; }
        public long CurrentHp { get; }
        public long MaximumHp { get; }
        public long CurrentEnergy { get; }
        public long MaximumEnergy { get; }
        public string StanceId { get; }
        public int ComboCount { get; }
        public long LiveScore { get; }
        public Levels.BattleFlowState FlowState { get; }
        public double Timestamp { get; }
        public double UltimateCooldownUntil { get; }
        public BattleHudResultState Result { get; }
        public bool IsInitialized { get; }

        private static void RequireFiniteNonNegative(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(parameter);
            }
        }
    }

    public interface IBattleHudStateSource
    {
        event Action<BattleHudState> Changed;

        BattleHudState Current { get; }
    }

    public interface IBattleHudCommandSink
    {
        void SetPlayerPaused(bool paused);
        void BeginUltimateDrawing();
        void Restart();
        void GoNext();
        void ReturnToMainMenu();
    }

    public interface IBattleHudView
    {
        event Action PauseToggleRequested;
        event Action UltimateRequested;
        event Action RestartRequested;
        event Action NextLevelRequested;
        event Action MainMenuRequested;

        void Render(BattleHudViewModel model);
    }

    public sealed class BattleHudViewModel
    {
        internal BattleHudViewModel(
            string levelName,
            string hpLabel,
            string hpValue,
            float hpNormalized,
            string energyLabel,
            string energyValue,
            float energyNormalized,
            string comboLabel,
            string comboValue,
            bool comboVisible,
            string scoreLabel,
            string scoreValue,
            string stanceLabel,
            string stanceValue,
            string ultimateLabel,
            string ultimateStatus,
            float ultimateCooldownNormalized,
            bool ultimateVisible,
            bool ultimateInteractable,
            string pauseButtonText,
            bool pauseButtonVisible,
            bool pauseButtonInteractable,
            string pausedTitle,
            bool pauseOverlayVisible,
            bool resultVisible,
            string resultTitle,
            string resultScoreLabel,
            string resultScoreValue,
            string starsLabel,
            string starsValue,
            string rewardsLabel,
            string rewardsBody,
            bool rewardsVisible,
            string restartText,
            string nextLevelText,
            bool nextLevelVisible,
            string mainMenuText,
            bool mainMenuVisible)
        {
            LevelName = levelName;
            HpLabel = hpLabel;
            HpValue = hpValue;
            HpNormalized = hpNormalized;
            EnergyLabel = energyLabel;
            EnergyValue = energyValue;
            EnergyNormalized = energyNormalized;
            ComboLabel = comboLabel;
            ComboValue = comboValue;
            ComboVisible = comboVisible;
            ScoreLabel = scoreLabel;
            ScoreValue = scoreValue;
            StanceLabel = stanceLabel;
            StanceValue = stanceValue;
            UltimateLabel = ultimateLabel;
            UltimateStatus = ultimateStatus;
            UltimateCooldownNormalized = ultimateCooldownNormalized;
            UltimateVisible = ultimateVisible;
            UltimateInteractable = ultimateInteractable;
            PauseButtonText = pauseButtonText;
            PauseButtonVisible = pauseButtonVisible;
            PauseButtonInteractable = pauseButtonInteractable;
            PausedTitle = pausedTitle;
            PauseOverlayVisible = pauseOverlayVisible;
            ResultVisible = resultVisible;
            ResultTitle = resultTitle;
            ResultScoreLabel = resultScoreLabel;
            ResultScoreValue = resultScoreValue;
            StarsLabel = starsLabel;
            StarsValue = starsValue;
            RewardsLabel = rewardsLabel;
            RewardsBody = rewardsBody;
            RewardsVisible = rewardsVisible;
            RestartText = restartText;
            NextLevelText = nextLevelText;
            NextLevelVisible = nextLevelVisible;
            MainMenuText = mainMenuText;
            MainMenuVisible = mainMenuVisible;
        }

        public string LevelName { get; }
        public string HpLabel { get; }
        public string HpValue { get; }
        public float HpNormalized { get; }
        public string EnergyLabel { get; }
        public string EnergyValue { get; }
        public float EnergyNormalized { get; }
        public string ComboLabel { get; }
        public string ComboValue { get; }
        public bool ComboVisible { get; }
        public string ScoreLabel { get; }
        public string ScoreValue { get; }
        public string StanceLabel { get; }
        public string StanceValue { get; }
        public string UltimateLabel { get; }
        public string UltimateStatus { get; }
        public float UltimateCooldownNormalized { get; }
        public bool UltimateVisible { get; }
        public bool UltimateInteractable { get; }
        public string PauseButtonText { get; }
        public bool PauseButtonVisible { get; }
        public bool PauseButtonInteractable { get; }
        public string PausedTitle { get; }
        public bool PauseOverlayVisible { get; }
        public bool ResultVisible { get; }
        public string ResultTitle { get; }
        public string ResultScoreLabel { get; }
        public string ResultScoreValue { get; }
        public string StarsLabel { get; }
        public string StarsValue { get; }
        public string RewardsLabel { get; }
        public string RewardsBody { get; }
        public bool RewardsVisible { get; }
        public string RestartText { get; }
        public string NextLevelText { get; }
        public bool NextLevelVisible { get; }
        public string MainMenuText { get; }
        public bool MainMenuVisible { get; }
    }
}
