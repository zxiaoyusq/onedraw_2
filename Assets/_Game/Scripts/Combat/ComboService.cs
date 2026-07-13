using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Combat
{
    public sealed class ComboService
    {
        private bool hasObservedTimestamp;
        private double latestObservedTimestamp;
        private double lastHitTimestamp;
        private int count;

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

        public double TimeoutSeconds { get; }

        public ComboSnapshot Current => new ComboSnapshot(count, lastHitTimestamp);

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
            return Current;
        }

        public ComboSnapshot AdvanceTime(double timestamp)
        {
            Observe(timestamp);
            if (count > 0 && timestamp - lastHitTimestamp > TimeoutSeconds)
            {
                count = 0;
                lastHitTimestamp = 0d;
            }

            return Current;
        }

        public void Reset()
        {
            hasObservedTimestamp = false;
            latestObservedTimestamp = 0d;
            lastHitTimestamp = 0d;
            count = 0;
        }

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

    public readonly struct ComboSnapshot
    {
        internal ComboSnapshot(int count, double lastHitTimestamp)
        {
            Count = count;
            LastHitTimestamp = lastHitTimestamp;
        }

        public int Count { get; }

        public double LastHitTimestamp { get; }

        public bool IsActive => Count > 0;
    }
}
