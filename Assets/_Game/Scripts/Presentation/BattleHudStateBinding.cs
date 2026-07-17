using System;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Levels;

namespace OneStrokeDemon.Presentation
{
    // 定义 BattleHudStateBinding 的表现层契约，隔离战斗状态与具体Unity视图实现。
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

        // 初始化 BattleHudStateBinding，并建立表现层所需的引用与初始显示状态。
        public BattleHudStateBinding(
            string configuredLevelId,
            PlayerCombatController playerController,
            ComboService comboService,
            ScoreService scoreService,
            BattleFlowStateMachine flowStateMachine,
            ResultService resultService)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(configuredLevelId))
            {
                throw new ArgumentException("Level id must be non-empty.", nameof(configuredLevelId));
            }

            player = playerController ?? throw new ArgumentNullException(nameof(playerController));
            combo = comboService ?? throw new ArgumentNullException(nameof(comboService));
            score = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
            flow = flowStateMachine ?? throw new ArgumentNullException(nameof(flowStateMachine));
            results = resultService ?? throw new ArgumentNullException(nameof(resultService));
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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

        // 更新 UpdateUltimateClock 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void UpdateUltimateClock(double currentTimestamp, double cooldownUntil)
        {
            RequireActive();
            ValidateTime(currentTimestamp, nameof(currentTimestamp));
            ValidateTime(cooldownUntil, nameof(cooldownUntil));
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (previousBucket != currentBucket || previousReady != currentReady)
            {
                Publish();
            }
        }

        // 释放 Dispose 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Dispose()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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

        // 响应 OnPlayerEvent 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void OnPlayerEvent(PlayerCombatEvent combatEvent)
        {
            timestamp = Math.Max(timestamp, combatEvent.Timestamp);
            Publish();
        }

        // 响应 OnComboChanged 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void OnComboChanged(ComboSnapshot snapshot)
        {
            Publish();
        }

        // 响应 OnScoreChanged 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void OnScoreChanged(CombatScoreSnapshot snapshot)
        {
            Publish();
        }

        // 响应 OnFlowEvent 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void OnFlowEvent(BattleFlowEvent flowEvent)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (flowEvent.EventType == BattleFlowEventType.StateChanged ||
                flowEvent.EventType == BattleFlowEventType.Settled)
            {
                Publish();
            }
        }

        // 响应 OnReceiptPublished 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void OnReceiptPublished(ResultReceipt receipt)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!string.Equals(receipt.LevelId, levelId, StringComparison.Ordinal))
            {
                return;
            }

            var rewards = new BattleHudRewardState[receipt.AppliedRewards.Count];
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
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

        // 发布 Publish 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void Publish()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!disposed)
            {
                Changed?.Invoke(Current);
            }
        }

        // 处理 RequireActive 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void RequireActive()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BattleHudStateBinding));
            }
        }

        // 处理 CooldownBucket 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static long CooldownBucket(double currentTimestamp, double cooldownUntil)
        {
            return checked((long)Math.Ceiling(Math.Max(0d, cooldownUntil - currentTimestamp)));
        }

        // 校验 ValidateTime 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void ValidateTime(double value, string parameter)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(parameter);
            }
        }
    }
}
