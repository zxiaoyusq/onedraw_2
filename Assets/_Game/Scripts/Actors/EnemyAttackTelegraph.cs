using System;

namespace OneStrokeDemon.Actors
{
    // 定义 EnemyAttackTelegraphSnapshot 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyAttackTelegraphSnapshot
    {
        // 初始化 EnemyAttackTelegraphSnapshot，并建立角色运行时所需的初始状态。
        internal EnemyAttackTelegraphSnapshot(
            ulong sequence,
            string attackId,
            EnemyAttackActionKind actionKind,
            string interruptGestureType,
            string effectGroupId,
            double openedAt,
            double expectedExecuteAt,
            double closedAt,
            bool isVisible)
        {
            Sequence = sequence;
            AttackId = attackId ?? string.Empty;
            ActionKind = actionKind;
            InterruptGestureType = interruptGestureType ?? string.Empty;
            EffectGroupId = effectGroupId ?? string.Empty;
            OpenedAt = openedAt;
            ExpectedExecuteAt = expectedExecuteAt;
            ClosedAt = closedAt;
            IsVisible = isVisible;
            IsValid = true;
        }

        public ulong Sequence { get; }

        public string AttackId { get; }

        public EnemyAttackActionKind ActionKind { get; }

        public string InterruptGestureType { get; }

        public string EffectGroupId { get; }

        public double OpenedAt { get; }

        public double ExpectedExecuteAt { get; }

        public double ClosedAt { get; }

        public bool IsVisible { get; }

        public bool IsValid { get; }
    }

    // 定义 EnemyAttackTelegraph 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class EnemyAttackTelegraph
    {
        private ulong sequence;
        private string attackId = string.Empty;
        private EnemyAttackActionKind actionKind;
        private string interruptGestureType = string.Empty;
        private string effectGroupId = string.Empty;
        private double openedAt;
        private double expectedExecuteAt;
        private double closedAt;
        private bool isVisible;

        public EnemyAttackTelegraphSnapshot Current =>
            new EnemyAttackTelegraphSnapshot(
                sequence,
                attackId,
                actionKind,
                interruptGestureType,
                effectGroupId,
                openedAt,
                expectedExecuteAt,
                closedAt,
                isVisible);

        // 处理 Open 对应的角色逻辑，并返回或发布一致的状态结果。
        public void Open(
            in EnemyAttackAction action,
            in EnemyAttackTimeline timeline,
            double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!action.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy attack action must be configured.",
                    nameof(action));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!timeline.IsConfigured ||
                !string.Equals(action.AttackId, timeline.AttackId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Telegraph timeline must match the configured attack action.",
                    nameof(timeline));
            }

            ValidateTimestamp(timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (isVisible)
            {
                throw new InvalidOperationException(
                    $"Attack telegraph '{attackId}' must close before another opens.");
            }

            sequence = NextSequence(sequence);
            attackId = action.AttackId;
            actionKind = action.Kind;
            interruptGestureType = timeline.InterruptGestureType;
            effectGroupId = action.EffectGroupId;
            openedAt = timestamp;
            expectedExecuteAt = timestamp + timeline.WindupSeconds;
            closedAt = 0d;
            isVisible = true;
        }

        // 处理 Close 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool Close(double timestamp)
        {
            ValidateTimestamp(timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isVisible)
            {
                return false;
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (timestamp < openedAt)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    "Telegraph close timestamp cannot precede its open timestamp.");
            }

            sequence = NextSequence(sequence);
            closedAt = timestamp;
            isVisible = false;
            return true;
        }

        // 处理 NextSequence 对应的角色逻辑，并返回或发布一致的状态结果。
        private static ulong NextSequence(ulong current)
        {
            ulong next = current + 1UL;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (next == 0UL)
            {
                throw new OverflowException("Enemy telegraph sequence is exhausted.");
            }

            return next;
        }

        // 校验 ValidateTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateTimestamp(double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(timestamp) ||
                double.IsInfinity(timestamp) ||
                timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    "Telegraph timestamp must be finite and non-negative.");
            }
        }
    }
}
