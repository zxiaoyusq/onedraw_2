using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    public enum EnemyBuffApplyStatus
    {
        None = 0,
        Applied = 1,
        Refreshed = 2,
        Replaced = 3,
        StackAdded = 4,
        Inactive = 5
    }

    public readonly struct EnemyBuffSnapshot
    {
        internal EnemyBuffSnapshot(
            string buffId,
            string type,
            int stacks,
            double magnitude,
            double appliedAt,
            double expiresAt,
            string sourceId,
            bool isActive)
        {
            BuffId = buffId ?? string.Empty;
            Type = type ?? string.Empty;
            Stacks = stacks;
            Magnitude = magnitude;
            AppliedAt = appliedAt;
            ExpiresAt = expiresAt;
            SourceId = sourceId ?? string.Empty;
            IsActive = isActive;
            IsValid = true;
        }

        public string BuffId { get; }

        public string Type { get; }

        public int Stacks { get; }

        public double Magnitude { get; }

        public double AppliedAt { get; }

        public double ExpiresAt { get; }

        public string SourceId { get; }

        public bool IsActive { get; }

        public bool IsValid { get; }
    }

    public readonly struct EnemyBuffApplyResult
    {
        internal EnemyBuffApplyResult(
            EnemyBuffApplyStatus status,
            EnemyBuffSnapshot buff)
        {
            Status = status;
            Buff = buff;
            IsValid = true;
        }

        public EnemyBuffApplyStatus Status { get; }

        public EnemyBuffSnapshot Buff { get; }

        public bool IsValid { get; }

        public bool Changed =>
            Status == EnemyBuffApplyStatus.Applied ||
            Status == EnemyBuffApplyStatus.Refreshed ||
            Status == EnemyBuffApplyStatus.Replaced ||
            Status == EnemyBuffApplyStatus.StackAdded;
    }

    public sealed class EnemyBuffContainer
    {
        private readonly Dictionary<string, ActiveBuff> active =
            new Dictionary<string, ActiveBuff>(StringComparer.Ordinal);
        private readonly List<string> expiredIds = new List<string>();
        private bool isActive;
        private double lastTimestamp;
        private bool hasTimestamp;

        public bool IsActive => isActive;

        public int Count => active.Count;

        public void Spawn(double timestamp)
        {
            ValidateTimestamp(timestamp, nameof(timestamp));
            if (isActive)
            {
                throw new InvalidOperationException(
                    "Active enemy buff container must be released before reuse.");
            }

            active.Clear();
            expiredIds.Clear();
            isActive = true;
            lastTimestamp = timestamp;
            hasTimestamp = true;
        }

        public EnemyBuffApplyResult Apply(
            BuffConfig buff,
            double durationSeconds,
            string sourceId,
            double timestamp)
        {
            if (buff == null)
            {
                throw new ArgumentNullException(nameof(buff));
            }

            ValidateDuration(durationSeconds, nameof(durationSeconds));
            ObserveTimestamp(timestamp);
            if (!isActive)
            {
                return new EnemyBuffApplyResult(
                    EnemyBuffApplyStatus.Inactive,
                    default);
            }

            ValidateBuff(buff);
            double expiresAt = timestamp + durationSeconds;
            if (double.IsNaN(expiresAt) || double.IsInfinity(expiresAt))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationSeconds),
                    "Enemy buff expiry must remain finite.");
            }

            EnemyBuffApplyStatus status;
            int stacks;
            if (!active.TryGetValue(buff.BuffId, out ActiveBuff existing))
            {
                stacks = 1;
                status = EnemyBuffApplyStatus.Applied;
            }
            else
            {
                switch (buff.RefreshPolicy)
                {
                    case "Refresh":
                        stacks = existing.Stacks;
                        status = EnemyBuffApplyStatus.Refreshed;
                        break;
                    case "Replace":
                        stacks = 1;
                        status = EnemyBuffApplyStatus.Replaced;
                        break;
                    case "AddStack":
                        stacks = Math.Min(existing.Stacks + 1, checked((int)buff.MaxStacks));
                        status = stacks > existing.Stacks
                            ? EnemyBuffApplyStatus.StackAdded
                            : EnemyBuffApplyStatus.Refreshed;
                        break;
                    default:
                        throw new ArgumentException(
                            $"Buff '{buff.BuffId}' has unsupported refresh policy '{buff.RefreshPolicy}'.",
                            nameof(buff));
                }
            }

            var runtime = new ActiveBuff(
                buff.BuffId,
                buff.Type,
                stacks,
                buff.Magnitude,
                timestamp,
                expiresAt,
                sourceId ?? string.Empty);
            active[buff.BuffId] = runtime;
            return new EnemyBuffApplyResult(status, runtime.Snapshot());
        }

        public int Tick(double timestamp)
        {
            ObserveTimestamp(timestamp);
            if (!isActive || active.Count == 0)
            {
                return 0;
            }

            expiredIds.Clear();
            foreach (KeyValuePair<string, ActiveBuff> pair in active)
            {
                if (timestamp >= pair.Value.ExpiresAt)
                {
                    expiredIds.Add(pair.Key);
                }
            }

            for (int index = 0; index < expiredIds.Count; index++)
            {
                active.Remove(expiredIds[index]);
            }

            int removed = expiredIds.Count;
            expiredIds.Clear();
            return removed;
        }

        public bool TryGet(string buffId, out EnemyBuffSnapshot snapshot)
        {
            if (buffId != null && active.TryGetValue(buffId, out ActiveBuff buff))
            {
                snapshot = buff.Snapshot();
                return true;
            }

            snapshot = default;
            return false;
        }

        public double GetIncomingDamageMultiplier()
        {
            double multiplier = 1d;
            foreach (ActiveBuff buff in active.Values)
            {
                if (string.Equals(buff.Type, "DamageTaken", StringComparison.Ordinal))
                {
                    multiplier *= 1d + (buff.Magnitude * buff.Stacks);
                }
                else if (string.Equals(
                             buff.Type,
                             "DamageReduction",
                             StringComparison.Ordinal))
                {
                    multiplier *= Math.Max(
                        0d,
                        1d - (buff.Magnitude * buff.Stacks));
                }
            }

            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier < 0d)
            {
                throw new OverflowException(
                    "Configured enemy incoming-damage buffs produced an invalid multiplier.");
            }

            return multiplier;
        }

        public double GetMovementMultiplier()
        {
            double multiplier = 1d;
            foreach (ActiveBuff buff in active.Values)
            {
                if (string.Equals(buff.Type, "Slow", StringComparison.Ordinal))
                {
                    multiplier *= Math.Max(0d, 1d - (buff.Magnitude * buff.Stacks));
                }
            }

            return multiplier;
        }

        public bool HasType(string type)
        {
            foreach (ActiveBuff buff in active.Values)
            {
                if (string.Equals(buff.Type, type, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Release()
        {
            if (!isActive)
            {
                return false;
            }

            active.Clear();
            expiredIds.Clear();
            isActive = false;
            lastTimestamp = 0d;
            hasTimestamp = false;
            return true;
        }

        private void ObserveTimestamp(double timestamp)
        {
            ValidateTimestamp(timestamp, nameof(timestamp));
            if (hasTimestamp && timestamp < lastTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    $"Enemy buff timestamp cannot move backwards from {lastTimestamp}.");
            }

            lastTimestamp = timestamp;
            hasTimestamp = true;
        }

        private static void ValidateBuff(BuffConfig buff)
        {
            if (string.IsNullOrWhiteSpace(buff.BuffId) ||
                string.IsNullOrWhiteSpace(buff.Type) ||
                buff.MaxStacks <= 0L ||
                buff.MaxStacks > int.MaxValue ||
                float.IsNaN(buff.Magnitude) ||
                float.IsInfinity(buff.Magnitude) ||
                buff.Magnitude < 0f)
            {
                throw new ArgumentException(
                    $"Buff '{buff.BuffId}' contains invalid runtime values.",
                    nameof(buff));
            }
        }

        private static void ValidateDuration(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Enemy buff duration must be finite and positive.");
            }
        }

        private static void ValidateTimestamp(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Enemy buff timestamp must be finite and non-negative.");
            }
        }

        private readonly struct ActiveBuff
        {
            public ActiveBuff(
                string buffId,
                string type,
                int stacks,
                double magnitude,
                double appliedAt,
                double expiresAt,
                string sourceId)
            {
                BuffId = buffId;
                Type = type;
                Stacks = stacks;
                Magnitude = magnitude;
                AppliedAt = appliedAt;
                ExpiresAt = expiresAt;
                SourceId = sourceId;
            }

            public string BuffId { get; }

            public string Type { get; }

            public int Stacks { get; }

            public double Magnitude { get; }

            public double AppliedAt { get; }

            public double ExpiresAt { get; }

            public string SourceId { get; }

            public EnemyBuffSnapshot Snapshot()
            {
                return new EnemyBuffSnapshot(
                    BuffId,
                    Type,
                    Stacks,
                    Magnitude,
                    AppliedAt,
                    ExpiresAt,
                    SourceId,
                    true);
            }
        }
    }
}
