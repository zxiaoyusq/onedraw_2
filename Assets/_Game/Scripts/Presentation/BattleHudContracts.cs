using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Levels = OneStrokeDemon.Levels;

namespace OneStrokeDemon.Presentation
{
    // 定义 BattleHudLanguage 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public enum BattleHudLanguage
    {
        ZhCN = 0,
        EnUS = 1,
    }

    // 定义 BattleHudRewardState 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public readonly struct BattleHudRewardState
    {
        // 初始化 BattleHudRewardState，并建立表现层所需的引用与初始显示状态。
        public BattleHudRewardState(
            Levels.RewardGrantType type,
            string rewardId,
            long amount)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("Reward id must be non-empty.", nameof(rewardId));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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

    // 定义 BattleHudResultState 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class BattleHudResultState
    {
        // 初始化 BattleHudResultState，并建立表现层所需的引用与初始显示状态。
        public BattleHudResultState(
            Levels.BattleSettlement settlement,
            long finalScore,
            int stars,
            BattleHudRewardState[] rewards,
            bool canGoNext)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (settlement != Levels.BattleSettlement.Victory &&
                settlement != Levels.BattleSettlement.Defeat)
            {
                throw new ArgumentOutOfRangeException(nameof(settlement));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (finalScore < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(finalScore));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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

    // 定义 BattleHudState 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public readonly struct BattleHudState
    {
        // 初始化 BattleHudState，并建立表现层所需的引用与初始显示状态。
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
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("Level id must be non-empty.", nameof(levelId));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException("Stance id must be non-empty.", nameof(stanceId));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (maximumHp <= 0L || currentHp < 0L || currentHp > maximumHp)
            {
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (maximumEnergy <= 0L || currentEnergy < 0L || currentEnergy > maximumEnergy)
            {
                throw new ArgumentOutOfRangeException(nameof(currentEnergy));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (comboCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(comboCount));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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

        // 处理 RequireFiniteNonNegative 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void RequireFiniteNonNegative(double value, string parameter)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(parameter);
            }
        }
    }

    // 定义 IBattleHudStateSource 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public interface IBattleHudStateSource
    {
        event Action<BattleHudState> Changed;

        BattleHudState Current { get; }
    }

    // 定义 IBattleHudCommandSink 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public interface IBattleHudCommandSink
    {
        void SetPlayerPaused(bool paused);
        void SwitchStance();
        void BeginUltimateDrawing();
        void Restart();
        void GoNext();
        void ReturnToMainMenu();
    }

    // 定义 IBattleHudView 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public interface IBattleHudView
    {
        event Action PauseToggleRequested;
        event Action StanceSwitchRequested;
        event Action UltimateRequested;
        event Action RestartRequested;
        event Action NextLevelRequested;
        event Action MainMenuRequested;

        void Render(BattleHudViewModel model);
    }

    // 定义 BattleHudViewModel 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class BattleHudViewModel
    {
        // 初始化 BattleHudViewModel，并建立表现层所需的引用与初始显示状态。
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
            bool stanceInteractable,
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
            StanceInteractable = stanceInteractable;
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
        public bool StanceInteractable { get; }
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
