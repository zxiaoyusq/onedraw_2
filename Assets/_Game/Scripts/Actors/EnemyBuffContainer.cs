using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    // 定义 EnemyBuffApplyStatus 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum EnemyBuffApplyStatus
    {
        None = 0,
        Applied = 1,
        Refreshed = 2,
        Replaced = 3,
        StackAdded = 4,
        Inactive = 5
    }

    // 定义 EnemyBuffSnapshot 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyBuffSnapshot
    {
        // 初始化 EnemyBuffSnapshot，并建立角色运行时所需的初始状态。
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

    // 定义 EnemyBuffApplyResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyBuffApplyResult
    {
        // 初始化 EnemyBuffApplyResult，并建立角色运行时所需的初始状态。
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

    // 定义 EnemyBuffContainer 的角色领域数据与行为边界，供上层流程以明确契约使用。
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

        // 生成 Spawn 对应的角色逻辑，并返回或发布一致的状态结果。
        public void Spawn(double timestamp)
        {
            ValidateTimestamp(timestamp, nameof(timestamp));
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 应用 Apply 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyBuffApplyResult Apply(
            BuffConfig buff,
            double durationSeconds,
            string sourceId,
            double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (buff == null)
            {
                throw new ArgumentNullException(nameof(buff));
            }

            ValidateDuration(durationSeconds, nameof(durationSeconds));
            ObserveTimestamp(timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isActive)
            {
                return new EnemyBuffApplyResult(
                    EnemyBuffApplyStatus.Inactive,
                    default);
            }

            ValidateBuff(buff);
            double expiresAt = timestamp + durationSeconds;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(expiresAt) || double.IsInfinity(expiresAt))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationSeconds),
                    "Enemy buff expiry must remain finite.");
            }

            EnemyBuffApplyStatus status;
            int stacks;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!active.TryGetValue(buff.BuffId, out ActiveBuff existing))
            {
                stacks = 1;
                status = EnemyBuffApplyStatus.Applied;
            }
            else
            {
                // 按当前枚举或状态选择对应的角色行为分支。
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

        // 按时间推进 Tick 对应的角色逻辑，并返回或发布一致的状态结果。
        public int Tick(double timestamp)
        {
            ObserveTimestamp(timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!isActive || active.Count == 0)
            {
                return 0;
            }

            expiredIds.Clear();
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            foreach (KeyValuePair<string, ActiveBuff> pair in active)
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (timestamp >= pair.Value.ExpiresAt)
                {
                    expiredIds.Add(pair.Key);
                }
            }

            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < expiredIds.Count; index++)
            {
                active.Remove(expiredIds[index]);
            }

            int removed = expiredIds.Count;
            expiredIds.Clear();
            return removed;
        }

        // 尝试执行 TryGet 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool TryGet(string buffId, out EnemyBuffSnapshot snapshot)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (buffId != null && active.TryGetValue(buffId, out ActiveBuff buff))
            {
                snapshot = buff.Snapshot();
                return true;
            }

            snapshot = default;
            return false;
        }

        // 获取 GetIncomingDamageMultiplier 对应的角色逻辑，并返回或发布一致的状态结果。
        public double GetIncomingDamageMultiplier()
        {
            double multiplier = 1d;
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            foreach (ActiveBuff buff in active.Values)
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (string.Equals(buff.Type, "DamageTaken", StringComparison.Ordinal))
                {
                    multiplier *= 1d + (buff.Magnitude * buff.Stacks);
                }
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier < 0d)
            {
                throw new OverflowException(
                    "Configured enemy incoming-damage buffs produced an invalid multiplier.");
            }

            return multiplier;
        }

        // 获取 GetMovementMultiplier 对应的角色逻辑，并返回或发布一致的状态结果。
        public double GetMovementMultiplier()
        {
            double multiplier = 1d;
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            foreach (ActiveBuff buff in active.Values)
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (string.Equals(buff.Type, "Slow", StringComparison.Ordinal))
                {
                    multiplier *= Math.Max(0d, 1d - (buff.Magnitude * buff.Stacks));
                }
            }

            return multiplier;
        }

        // 判断是否具有 HasType 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool HasType(string type)
        {
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            foreach (ActiveBuff buff in active.Values)
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (string.Equals(buff.Type, type, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // 释放 Release 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool Release()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 处理 ObserveTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ObserveTimestamp(double timestamp)
        {
            ValidateTimestamp(timestamp, nameof(timestamp));
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 校验 ValidateBuff 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateBuff(BuffConfig buff)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 校验 ValidateDuration 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateDuration(double value, string parameterName)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Enemy buff duration must be finite and positive.");
            }
        }

        // 校验 ValidateTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateTimestamp(double value, string parameterName)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Enemy buff timestamp must be finite and non-negative.");
            }
        }

        // 定义 ActiveBuff 的角色领域数据与行为边界，供上层流程以明确契约使用。
        private readonly struct ActiveBuff
        {
            // 初始化 ActiveBuff，并建立角色运行时所需的初始状态。
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

            // 处理 Snapshot 对应的角色逻辑，并返回或发布一致的状态结果。
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
