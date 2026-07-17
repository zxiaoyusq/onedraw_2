using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    // 定义 StanceSnapshot 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct StanceSnapshot
    {
        // 初始化 StanceSnapshot，并建立角色运行时所需的初始状态。
        private StanceSnapshot(
            string stanceId,
            string damageFormulaId,
            double damageMultiplier,
            double ghostDamageMultiplier,
            double projectileCutMultiplier,
            long strokeWidthReferencePixels,
            double switchCooldownSeconds,
            string onSwitchEffectGroupId,
            string assetKey)
        {
            StanceId = stanceId;
            DamageFormulaId = damageFormulaId;
            DamageMultiplier = damageMultiplier;
            GhostDamageMultiplier = ghostDamageMultiplier;
            ProjectileCutMultiplier = projectileCutMultiplier;
            StrokeWidthReferencePixels = strokeWidthReferencePixels;
            SwitchCooldownSeconds = switchCooldownSeconds;
            OnSwitchEffectGroupId = onSwitchEffectGroupId;
            AssetKey = assetKey;
            IsConfigured = true;
        }

        public string StanceId { get; }

        public string DamageFormulaId { get; }

        public double DamageMultiplier { get; }

        public double GhostDamageMultiplier { get; }

        public double ProjectileCutMultiplier { get; }

        public long StrokeWidthReferencePixels { get; }

        public double SwitchCooldownSeconds { get; }

        public string OnSwitchEffectGroupId { get; }

        public string AssetKey { get; }

        public bool IsConfigured { get; }

        // 处理 FromConfig 对应的角色逻辑，并返回或发布一致的状态结果。
        internal static StanceSnapshot FromConfig(StanceConfig stance)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (stance == null)
            {
                throw new ArgumentNullException(nameof(stance));
            }

            RequireText(stance.StanceId, nameof(stance.StanceId));
            RequireText(stance.DamageFormulaId, nameof(stance.DamageFormulaId));
            RequireFiniteNonNegative(
                stance.StanceId,
                nameof(stance.DamageMultiplier),
                stance.DamageMultiplier);
            RequireFiniteNonNegative(
                stance.StanceId,
                nameof(stance.GhostDamageMultiplier),
                stance.GhostDamageMultiplier);
            RequireFiniteNonNegative(
                stance.StanceId,
                nameof(stance.ProjectileCutMultiplier),
                stance.ProjectileCutMultiplier);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (stance.StrokeWidthRefPx <= 0)
            {
                throw Invalid(
                    stance.StanceId,
                    nameof(stance.StrokeWidthRefPx),
                    stance.StrokeWidthRefPx);
            }

            RequireFiniteNonNegative(
                stance.StanceId,
                nameof(stance.SwitchCooldownSec),
                stance.SwitchCooldownSec);
            RequireText(stance.OnSwitchEffectGroupId, nameof(stance.OnSwitchEffectGroupId));
            RequireText(stance.AssetKey, nameof(stance.AssetKey));

            return new StanceSnapshot(
                stance.StanceId,
                stance.DamageFormulaId,
                stance.DamageMultiplier,
                stance.GhostDamageMultiplier,
                stance.ProjectileCutMultiplier,
                stance.StrokeWidthRefPx,
                stance.SwitchCooldownSec,
                stance.OnSwitchEffectGroupId,
                stance.AssetKey);
        }

        // 处理 RequireText 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireText(string value, string field)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"Configured stance field '{field}' must be non-empty.",
                    field);
            }
        }

        // 处理 RequireFiniteNonNegative 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireFiniteNonNegative(
            string rowId,
            string field,
            double value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw Invalid(rowId, field, value);
            }
        }

        // 处理 Invalid 对应的角色逻辑，并返回或发布一致的状态结果。
        private static ArgumentOutOfRangeException Invalid(
            string rowId,
            string field,
            object value)
        {
            return new ArgumentOutOfRangeException(
                field,
                value,
                $"Configured value for '{rowId}.{field}' is outside the supported stance range.");
        }
    }

    // 定义 StanceSwitchStatus 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum StanceSwitchStatus
    {
        None = 0,
        Switched = 1,
        AlreadyActive = 2,
        CooldownActive = 3,
        PlayerDead = 4
    }

    // 定义 StanceSwitchResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct StanceSwitchResult
    {
        // 初始化 StanceSwitchResult，并建立角色运行时所需的初始状态。
        internal StanceSwitchResult(
            StanceSwitchStatus status,
            string requestedStanceId,
            in StanceSnapshot previous,
            in StanceSnapshot current,
            double requestedAt,
            double nextSwitchAvailableAt)
        {
            Status = status;
            RequestedStanceId = requestedStanceId;
            Previous = previous;
            Current = current;
            RequestedAt = requestedAt;
            NextSwitchAvailableAt = nextSwitchAvailableAt;
            RemainingCooldownSeconds = Math.Max(0d, nextSwitchAvailableAt - requestedAt);
            OnSwitchEffectGroupId = status == StanceSwitchStatus.Switched
                ? current.OnSwitchEffectGroupId
                : string.Empty;
            IsValid = true;
        }

        public StanceSwitchStatus Status { get; }

        public string RequestedStanceId { get; }

        public StanceSnapshot Previous { get; }

        public StanceSnapshot Current { get; }

        public double RequestedAt { get; }

        public double NextSwitchAvailableAt { get; }

        public double RemainingCooldownSeconds { get; }

        public string OnSwitchEffectGroupId { get; }

        public bool IsValid { get; }

        public bool DidSwitch => Status == StanceSwitchStatus.Switched;
    }

    // 定义 StanceService 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class StanceService
    {
        private readonly IConfigProvider configProvider;
        private StanceSnapshot current;
        private double nextSwitchAvailableAt;
        private double lastObservedTimestamp;
        private bool hasObservedTimestamp;

        // 初始化 StanceService，并建立角色运行时所需的初始状态。
        public StanceService(IConfigProvider configProvider, string defaultStanceId)
        {
            this.configProvider = configProvider ??
                throw new ArgumentNullException(nameof(configProvider));
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(defaultStanceId))
            {
                throw new ArgumentException(
                    "Default stance id must be non-empty.",
                    nameof(defaultStanceId));
            }

            current = Load(defaultStanceId);
            nextSwitchAvailableAt = 0d;
        }

        public StanceSnapshot Current => current;

        public double NextSwitchAvailableAt => nextSwitchAvailableAt;

        // 尝试执行 TrySwitch 对应的角色逻辑，并返回或发布一致的状态结果。
        public StanceSwitchResult TrySwitch(string stanceId, double timestamp)
        {
            StanceSnapshot requested = ValidateRequest(stanceId, timestamp);

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.Equals(current.StanceId, requested.StanceId, StringComparison.Ordinal))
            {
                return new StanceSwitchResult(
                    StanceSwitchStatus.AlreadyActive,
                    requested.StanceId,
                    current,
                    current,
                    timestamp,
                    nextSwitchAvailableAt);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (timestamp < nextSwitchAvailableAt)
            {
                return new StanceSwitchResult(
                    StanceSwitchStatus.CooldownActive,
                    requested.StanceId,
                    current,
                    current,
                    timestamp,
                    nextSwitchAvailableAt);
            }

            double nextAvailable = timestamp + requested.SwitchCooldownSeconds;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsInfinity(nextAvailable) || double.IsNaN(nextAvailable))
            {
                throw new OverflowException(
                    $"Stance '{requested.StanceId}' switch cooldown exceeds timestamp capacity.");
            }

            StanceSnapshot previous = current;
            current = requested;
            nextSwitchAvailableAt = nextAvailable;
            return new StanceSwitchResult(
                StanceSwitchStatus.Switched,
                requested.StanceId,
                previous,
                current,
                timestamp,
                nextSwitchAvailableAt);
        }

        // 处理 RejectBecausePlayerDead 对应的角色逻辑，并返回或发布一致的状态结果。
        internal StanceSwitchResult RejectBecausePlayerDead(
            string stanceId,
            double timestamp)
        {
            ValidateRequest(stanceId, timestamp);
            return new StanceSwitchResult(
                StanceSwitchStatus.PlayerDead,
                stanceId,
                current,
                current,
                timestamp,
                nextSwitchAvailableAt);
        }

        // 获取 GetRemainingCooldown 对应的角色逻辑，并返回或发布一致的状态结果。
        public double GetRemainingCooldown(double timestamp)
        {
            ValidateTimestamp(timestamp);
            return Math.Max(0d, nextSwitchAvailableAt - timestamp);
        }

        // 处理 Load 对应的角色逻辑，并返回或发布一致的状态结果。
        private StanceSnapshot Load(string stanceId)
        {
            return StanceSnapshot.FromConfig(configProvider.GetStance(stanceId));
        }

        // 校验 ValidateRequest 对应的角色逻辑，并返回或发布一致的状态结果。
        private StanceSnapshot ValidateRequest(string stanceId, double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException(
                    "Requested stance id must be non-empty.",
                    nameof(stanceId));
            }

            ValidateTimestamp(timestamp);
            StanceSnapshot requested = Load(stanceId);
            Observe(timestamp);
            return requested;
        }

        // 校验 ValidateTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ValidateTimestamp(double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    "Stance timestamp must be finite and non-negative.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (hasObservedTimestamp && timestamp < lastObservedTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    $"Stance timestamp cannot move backwards from {lastObservedTimestamp}.");
            }
        }

        // 处理 Observe 对应的角色逻辑，并返回或发布一致的状态结果。
        private void Observe(double timestamp)
        {
            lastObservedTimestamp = timestamp;
            hasObservedTimestamp = true;
        }
    }
}
