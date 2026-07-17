using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Combat
{
    /// <summary>按外部单调时间维护命中连斩计数和超时清零。</summary>
    public sealed class ComboService
    {
        private bool hasObservedTimestamp;
        private double latestObservedTimestamp;
        private double lastHitTimestamp;
        private int count;

        /// <summary>连斩快照变化时发布。</summary>
        public event Action<ComboSnapshot> Changed;

        /// <summary>创建使用指定正有限超时秒数的连斩服务。</summary>
        public ComboService(double timeoutSeconds)
        {
            if (double.IsNaN(timeoutSeconds) || double.IsInfinity(timeoutSeconds) || timeoutSeconds <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeoutSeconds),
                    "Combo timeout must be finite and positive.");
            }

            TimeoutSeconds = timeoutSeconds;
        }

        /// <summary>获取相邻命中延续连斩的最大间隔。</summary>
        public double TimeoutSeconds { get; }

        /// <summary>获取当前不可变连斩快照。</summary>
        public ComboSnapshot Current => new ComboSnapshot(count, lastHitTimestamp);

        /// <summary>从 Global 配置读取连斩超时并创建服务。</summary>
        public static ComboService FromConfig(IConfigProvider configProvider)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            GlobalConfig row = configProvider.GetGlobal(ConfigIds.GlobalKeys.ComboTimeoutSec);
            if (!row.FloatValue.HasValue)
            {
                throw new ArgumentException(
                    $"Global '{ConfigIds.GlobalKeys.ComboTimeoutSec}' must define floatValue.",
                    nameof(configProvider));
            }

            return new ComboService(row.FloatValue.Value);
        }

        /// <summary>登记一次命中；超过超时从一开始，否则安全递增。</summary>
        public ComboSnapshot RegisterHit(double timestamp)
        {
            Observe(timestamp);
            if (count == 0 || timestamp - lastHitTimestamp > TimeoutSeconds)
            {
                count = 1;
            }
            else
            {
                count = checked(count + 1);
            }

            lastHitTimestamp = timestamp;
            ComboSnapshot snapshot = Current;
            Changed?.Invoke(snapshot);
            return snapshot;
        }

        /// <summary>观察时间推进，并在严格超过超时边界时清空活动连斩。</summary>
        public ComboSnapshot AdvanceTime(double timestamp)
        {
            Observe(timestamp);
            if (count > 0 && timestamp - lastHitTimestamp > TimeoutSeconds)
            {
                count = 0;
                lastHitTimestamp = 0d;
                Changed?.Invoke(Current);
            }

            return Current;
        }

        /// <summary>重置时间与计数，仅在状态实际变化时发布事件。</summary>
        public void Reset()
        {
            bool changed = hasObservedTimestamp || count != 0 || lastHitTimestamp != 0d;
            hasObservedTimestamp = false;
            latestObservedTimestamp = 0d;
            lastHitTimestamp = 0d;
            count = 0;
            if (changed)
            {
                Changed?.Invoke(Current);
            }
        }

        /// <summary>验证时间有限且不回退，并记录最新观察时间。</summary>
        private void Observe(double timestamp)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), "Timestamp must be finite.");
            }

            if (hasObservedTimestamp && timestamp < latestObservedTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    "Combo timestamps must be monotonic.");
            }

            hasObservedTimestamp = true;
            latestObservedTimestamp = timestamp;
        }
    }

    /// <summary>保存当前连斩数和最后命中时间。</summary>
    public readonly struct ComboSnapshot
    {
        /// <summary>创建连斩快照。</summary>
        internal ComboSnapshot(int count, double lastHitTimestamp)
        {
            Count = count;
            LastHitTimestamp = lastHitTimestamp;
        }

        /// <summary>获取当前连斩数。</summary>
        public int Count { get; }

        /// <summary>获取最后命中时间戳。</summary>
        public double LastHitTimestamp { get; }

        /// <summary>获取当前是否存在活动连斩。</summary>
        public bool IsActive => Count > 0;
    }
}
