using System;

namespace OneStrokeDemon.Actors
{
    public readonly struct EnemyAttackTelegraphSnapshot
    {
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

        public void Open(
            in EnemyAttackAction action,
            in EnemyAttackTimeline timeline,
            double timestamp)
        {
            if (!action.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy attack action must be configured.",
                    nameof(action));
            }

            if (!timeline.IsConfigured ||
                !string.Equals(action.AttackId, timeline.AttackId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Telegraph timeline must match the configured attack action.",
                    nameof(timeline));
            }

            ValidateTimestamp(timestamp);
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

        public bool Close(double timestamp)
        {
            ValidateTimestamp(timestamp);
            if (!isVisible)
            {
                return false;
            }

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

        private static ulong NextSequence(ulong current)
        {
            ulong next = current + 1UL;
            if (next == 0UL)
            {
                throw new OverflowException("Enemy telegraph sequence is exhausted.");
            }

            return next;
        }

        private static void ValidateTimestamp(double timestamp)
        {
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
