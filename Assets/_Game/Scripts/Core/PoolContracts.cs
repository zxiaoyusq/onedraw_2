using System;
using System.Collections.Generic;

namespace OneStrokeDemon.Core
{
    public enum PoolExhaustionPolicy
    {
        Reject = 0,
        ReuseOldest = 1
    }

    public enum PoolReleaseReason
    {
        Prewarm = 0,
        Manual = 1,
        Completed = 2,
        ReusedOldest = 3,
        Restart = 4,
        ServiceDisposed = 5,
        AcquireRollback = 6
    }

    public enum PoolAcquireStatus
    {
        Acquired = 0,
        RejectedAtCapacity = 1
    }

    public enum PoolReleaseStatus
    {
        Released = 0,
        UnknownItem = 1,
        AlreadyReleased = 2,
        StaleLease = 3
    }

    public readonly struct PoolFamilyDefinition
    {
        public PoolFamilyDefinition(
            string familyId,
            int capacity,
            PoolExhaustionPolicy exhaustionPolicy)
        {
            if (string.IsNullOrWhiteSpace(familyId))
            {
                throw new ArgumentException("Pool family id must be non-empty.", nameof(familyId));
            }

            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Pool family capacity must be positive.");
            }

            if (!Enum.IsDefined(typeof(PoolExhaustionPolicy), exhaustionPolicy))
            {
                throw new ArgumentOutOfRangeException(nameof(exhaustionPolicy));
            }

            FamilyId = familyId;
            Capacity = capacity;
            ExhaustionPolicy = exhaustionPolicy;
        }

        public string FamilyId { get; }

        public int Capacity { get; }

        public PoolExhaustionPolicy ExhaustionPolicy { get; }
    }

    public readonly struct PoolDefinition
    {
        public PoolDefinition(
            string poolId,
            string familyId,
            int prewarmCount,
            Func<IPoolable> factory)
        {
            if (string.IsNullOrWhiteSpace(poolId))
            {
                throw new ArgumentException("Pool id must be non-empty.", nameof(poolId));
            }

            if (string.IsNullOrWhiteSpace(familyId))
            {
                throw new ArgumentException("Pool family id must be non-empty.", nameof(familyId));
            }

            if (prewarmCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(prewarmCount),
                    "Pool prewarm count must be non-negative.");
            }

            PoolId = poolId;
            FamilyId = familyId;
            PrewarmCount = prewarmCount;
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public string PoolId { get; }

        public string FamilyId { get; }

        public int PrewarmCount { get; }

        public Func<IPoolable> Factory { get; }
    }

    public readonly struct PoolLease : IEquatable<PoolLease>
    {
        internal PoolLease(
            string poolId,
            string familyId,
            uint generation,
            ulong activationSequence)
        {
            PoolId = poolId ?? string.Empty;
            FamilyId = familyId ?? string.Empty;
            Generation = generation;
            ActivationSequence = activationSequence;
        }

        public string PoolId { get; }

        public string FamilyId { get; }

        public uint Generation { get; }

        public ulong ActivationSequence { get; }

        public bool IsValid =>
            Generation > 0U &&
            ActivationSequence > 0UL &&
            !string.IsNullOrEmpty(PoolId) &&
            !string.IsNullOrEmpty(FamilyId);

        public bool Equals(PoolLease other)
        {
            return Generation == other.Generation &&
                ActivationSequence == other.ActivationSequence &&
                string.Equals(PoolId, other.PoolId, StringComparison.Ordinal) &&
                string.Equals(FamilyId, other.FamilyId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PoolLease other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Generation.GetHashCode();
                hash = (hash * 31) + ActivationSequence.GetHashCode();
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(PoolId ?? string.Empty);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(FamilyId ?? string.Empty);
                return hash;
            }
        }

        public static bool operator ==(PoolLease left, PoolLease right) => left.Equals(right);

        public static bool operator !=(PoolLease left, PoolLease right) => !left.Equals(right);
    }

    public readonly struct PoolReleaseContext
    {
        internal PoolReleaseContext(in PoolLease lease, PoolReleaseReason reason)
        {
            Lease = lease;
            Reason = reason;
        }

        public PoolLease Lease { get; }

        public PoolReleaseReason Reason { get; }
    }

    public interface IPoolable
    {
        bool IsPoolActive { get; }

        void AcquireFromPool(in PoolLease lease);

        void ReleaseToPool(in PoolReleaseContext context);
    }

    public readonly struct PoolAcquireResult
    {
        internal PoolAcquireResult(
            PoolAcquireStatus status,
            IPoolable item,
            in PoolLease lease,
            bool reusedOldest)
        {
            Status = status;
            Item = item;
            Lease = lease;
            ReusedOldest = reusedOldest;
        }

        public PoolAcquireStatus Status { get; }

        public IPoolable Item { get; }

        public PoolLease Lease { get; }

        public bool ReusedOldest { get; }

        public bool IsAcquired => Status == PoolAcquireStatus.Acquired && Item != null;
    }

    public readonly struct PoolReleaseResult
    {
        internal PoolReleaseResult(PoolReleaseStatus status, in PoolLease lease)
        {
            Status = status;
            Lease = lease;
        }

        public PoolReleaseStatus Status { get; }

        public PoolLease Lease { get; }

        public bool WasReleased => Status == PoolReleaseStatus.Released;
    }

    public readonly struct PoolServiceSnapshot
    {
        internal PoolServiceSnapshot(
            uint generation,
            int familyCount,
            int poolCount,
            int allocatedCount,
            int activeCount)
        {
            Generation = generation;
            FamilyCount = familyCount;
            PoolCount = poolCount;
            AllocatedCount = allocatedCount;
            ActiveCount = activeCount;
        }

        public uint Generation { get; }

        public int FamilyCount { get; }

        public int PoolCount { get; }

        public int AllocatedCount { get; }

        public int ActiveCount { get; }

        public int InactiveCount => AllocatedCount - ActiveCount;
    }

    public readonly struct PoolLeak
    {
        internal PoolLeak(IPoolable item, in PoolLease lease)
        {
            Item = item;
            Lease = lease;
        }

        public IPoolable Item { get; }

        public PoolLease Lease { get; }
    }

    public sealed class PoolLeakReport
    {
        internal PoolLeakReport(IReadOnlyList<PoolLeak> leaks)
        {
            Leaks = leaks ?? throw new ArgumentNullException(nameof(leaks));
        }

        public IReadOnlyList<PoolLeak> Leaks { get; }

        public int Count => Leaks.Count;

        public bool HasLeaks => Count > 0;
    }

    public readonly struct PoolRestartReport
    {
        internal PoolRestartReport(uint previousGeneration, uint generation, int releasedCount)
        {
            PreviousGeneration = previousGeneration;
            Generation = generation;
            ReleasedCount = releasedCount;
        }

        public uint PreviousGeneration { get; }

        public uint Generation { get; }

        public int ReleasedCount { get; }
    }
}
