using System;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Levels;

namespace OneStrokeDemon.Presentation
{
    public sealed class BattleHudStateBinding : IBattleHudStateSource, IDisposable
    {
        private readonly string levelId;
        private readonly PlayerCombatController player;
        private readonly ComboService combo;
        private readonly ScoreService score;
        private readonly BattleFlowStateMachine flow;
        private readonly ResultService results;
        private double timestamp;
        private double ultimateCooldownUntil;
        private BattleHudResultState result;
        private bool disposed;

        public BattleHudStateBinding(
            string configuredLevelId,
            PlayerCombatController playerController,
            ComboService comboService,
            ScoreService scoreService,
            BattleFlowStateMachine flowStateMachine,
            ResultService resultService)
        {
            if (string.IsNullOrWhiteSpace(configuredLevelId))
            {
                throw new ArgumentException("Level id must be non-empty.", nameof(configuredLevelId));
            }

            player = playerController ?? throw new ArgumentNullException(nameof(playerController));
            combo = comboService ?? throw new ArgumentNullException(nameof(comboService));
            score = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
            flow = flowStateMachine ?? throw new ArgumentNullException(nameof(flowStateMachine));
            results = resultService ?? throw new ArgumentNullException(nameof(resultService));
            if (!player.IsInitialized)
            {
                throw new ArgumentException(
                    "Player combat controller must be initialized before HUD binding.",
                    nameof(playerController));
            }

            levelId = configuredLevelId;
            player.CombatEventPublished += OnPlayerEvent;
            combo.Changed += OnComboChanged;
            score.Changed += OnScoreChanged;
            flow.EventPublished += OnFlowEvent;
            results.ReceiptPublished += OnReceiptPublished;
        }

        public event Action<BattleHudState> Changed;

        public BattleHudState Current
        {
            get
            {
                RequireActive();
                PlayerCombatSnapshot playerState = player.Current;
                return new BattleHudState(
                    levelId,
                    playerState.CurrentHp,
                    playerState.MaximumHp,
                    playerState.CurrentEnergy,
                    playerState.MaximumEnergy,
                    playerState.StanceId,
                    combo.Current.Count,
                    score.Current.TotalScore,
                    flow.State,
                    timestamp,
                    ultimateCooldownUntil,
                    result);
            }
        }

        public void UpdateUltimateClock(double currentTimestamp, double cooldownUntil)
        {
            RequireActive();
            ValidateTime(currentTimestamp, nameof(currentTimestamp));
            ValidateTime(cooldownUntil, nameof(cooldownUntil));
            if (currentTimestamp < timestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentTimestamp),
                    "HUD timestamps must be monotonic.");
            }

            long previousBucket = CooldownBucket(timestamp, ultimateCooldownUntil);
            bool previousReady = timestamp >= ultimateCooldownUntil;
            timestamp = currentTimestamp;
            ultimateCooldownUntil = cooldownUntil;
            long currentBucket = CooldownBucket(timestamp, ultimateCooldownUntil);
            bool currentReady = timestamp >= ultimateCooldownUntil;
            if (previousBucket != currentBucket || previousReady != currentReady)
            {
                Publish();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            player.CombatEventPublished -= OnPlayerEvent;
            combo.Changed -= OnComboChanged;
            score.Changed -= OnScoreChanged;
            flow.EventPublished -= OnFlowEvent;
            results.ReceiptPublished -= OnReceiptPublished;
            Changed = null;
        }

        private void OnPlayerEvent(PlayerCombatEvent combatEvent)
        {
            timestamp = Math.Max(timestamp, combatEvent.Timestamp);
            Publish();
        }

        private void OnComboChanged(ComboSnapshot snapshot)
        {
            Publish();
        }

        private void OnScoreChanged(CombatScoreSnapshot snapshot)
        {
            Publish();
        }

        private void OnFlowEvent(BattleFlowEvent flowEvent)
        {
            if (flowEvent.EventType == BattleFlowEventType.StateChanged ||
                flowEvent.EventType == BattleFlowEventType.Settled)
            {
                Publish();
            }
        }

        private void OnReceiptPublished(ResultReceipt receipt)
        {
            if (!string.Equals(receipt.LevelId, levelId, StringComparison.Ordinal))
            {
                return;
            }

            var rewards = new BattleHudRewardState[receipt.AppliedRewards.Count];
            for (int index = 0; index < rewards.Length; index += 1)
            {
                RewardGrant reward = receipt.AppliedRewards[index];
                rewards[index] = new BattleHudRewardState(
                    reward.Type,
                    reward.RewardId,
                    reward.Amount);
            }

            result = new BattleHudResultState(
                receipt.Settlement,
                receipt.Score.FinalScore,
                receipt.Score.Stars,
                rewards,
                receipt.CanGoNext);
            Publish();
        }

        private void Publish()
        {
            if (!disposed)
            {
                Changed?.Invoke(Current);
            }
        }

        private void RequireActive()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BattleHudStateBinding));
            }
        }

        private static long CooldownBucket(double currentTimestamp, double cooldownUntil)
        {
            return checked((long)Math.Ceiling(Math.Max(0d, cooldownUntil - currentTimestamp)));
        }

        private static void ValidateTime(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(parameter);
            }
        }
    }
}
