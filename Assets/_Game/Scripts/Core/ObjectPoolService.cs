using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OneStrokeDemon.Core
{
    public sealed class ObjectPoolService : IDisposable
    {
        private sealed class FamilyState
        {
            internal FamilyState(in PoolFamilyDefinition definition)
            {
                Definition = definition;
            }

            internal PoolFamilyDefinition Definition { get; }

            internal int ActiveCount { get; set; }
        }

        private sealed class PoolState
        {
            internal PoolState(in PoolDefinition definition, FamilyState family)
            {
                Definition = definition;
                Family = family;
                Inactive = new List<Entry>(definition.PrewarmCount);
            }

            internal PoolDefinition Definition { get; }

            internal FamilyState Family { get; }

            internal List<Entry> Inactive { get; }

            internal int AllocatedCount { get; set; }
        }

        private sealed class Entry
        {
            internal Entry(IPoolable item, PoolState pool)
            {
                Item = item;
                Pool = pool;
            }

            internal IPoolable Item { get; }

            internal PoolState Pool { get; }

            internal PoolLease Lease { get; set; }

            internal bool IsActive { get; set; }
        }

        private sealed class ReferenceComparer : IEqualityComparer<IPoolable>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(IPoolable left, IPoolable right) => ReferenceEquals(left, right);

            public int GetHashCode(IPoolable value) => RuntimeHelpers.GetHashCode(value);
        }

        private readonly Dictionary<string, FamilyState> families =
            new Dictionary<string, FamilyState>(StringComparer.Ordinal);
        private readonly Dictionary<string, PoolState> pools =
            new Dictionary<string, PoolState>(StringComparer.Ordinal);
        private readonly Dictionary<IPoolable, Entry> entries =
            new Dictionary<IPoolable, Entry>(ReferenceComparer.Instance);
        private readonly List<Entry> activeEntries = new List<Entry>();
        private uint generation = 1U;
        private ulong nextActivationSequence = 1UL;
        private bool disposed;

        public uint Generation => generation;

        public void RegisterFamily(in PoolFamilyDefinition definition)
        {
            ThrowIfDisposed();
            if (families.ContainsKey(definition.FamilyId))
            {
                throw new InvalidOperationException(
                    $"Pool family '{definition.FamilyId}' is already registered.");
            }

            families.Add(definition.FamilyId, new FamilyState(definition));
        }

        public void RegisterPool(in PoolDefinition definition)
        {
            ThrowIfDisposed();
            if (pools.ContainsKey(definition.PoolId))
            {
                throw new InvalidOperationException(
                    $"Pool '{definition.PoolId}' is already registered.");
            }

            if (!families.TryGetValue(definition.FamilyId, out FamilyState family))
            {
                throw new InvalidOperationException(
                    $"Pool family '{definition.FamilyId}' must be registered before pool '{definition.PoolId}'.");
            }

            var pool = new PoolState(definition, family);
            pools.Add(definition.PoolId, pool);
            try
            {
                for (int index = 0; index < definition.PrewarmCount; index++)
                {
                    Entry entry = CreateEntry(pool);
                    entry.Item.ReleaseToPool(
                        new PoolReleaseContext(default, PoolReleaseReason.Prewarm));
                    ValidateReleased(entry.Item, definition.PoolId);
                    pool.Inactive.Add(entry);
                }
            }
            catch
            {
                pools.Remove(definition.PoolId);
                RemovePoolEntries(pool);
                throw;
            }
        }

        public PoolAcquireResult Acquire(string poolId)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(poolId))
            {
                throw new ArgumentException("Pool id must be non-empty.", nameof(poolId));
            }

            if (!pools.TryGetValue(poolId, out PoolState pool))
            {
                throw new KeyNotFoundException($"Pool '{poolId}' is not registered.");
            }

            bool reusedOldest = false;
            if (pool.Family.ActiveCount >= pool.Family.Definition.Capacity)
            {
                if (pool.Family.Definition.ExhaustionPolicy == PoolExhaustionPolicy.Reject)
                {
                    return new PoolAcquireResult(
                        PoolAcquireStatus.RejectedAtCapacity,
                        null,
                        default,
                        false);
                }

                Entry oldest = FindOldestActive(pool.Family);
                if (oldest == null)
                {
                    throw new InvalidOperationException(
                        $"Pool family '{pool.Family.Definition.FamilyId}' reports capacity without an active item.");
                }

                ReleaseEntry(oldest, PoolReleaseReason.ReusedOldest);
                reusedOldest = true;
            }

            Entry acquired = TakeInactive(pool) ?? CreateEntry(pool);
            var lease = new PoolLease(
                pool.Definition.PoolId,
                pool.Definition.FamilyId,
                generation,
                NextActivationSequence());
            acquired.Lease = lease;
            acquired.IsActive = true;
            pool.Family.ActiveCount++;
            activeEntries.Add(acquired);
            try
            {
                acquired.Item.AcquireFromPool(lease);
                if (!acquired.Item.IsPoolActive)
                {
                    throw new InvalidOperationException(
                        $"Pool item acquired from '{poolId}' did not enter an active pool state.");
                }
            }
            catch
            {
                RollBackAcquire(acquired);
                throw;
            }

            return new PoolAcquireResult(
                PoolAcquireStatus.Acquired,
                acquired.Item,
                lease,
                reusedOldest);
        }

        public PoolReleaseResult Release(
            IPoolable item,
            in PoolLease lease,
            PoolReleaseReason reason = PoolReleaseReason.Manual)
        {
            ThrowIfDisposed();
            ValidateConcreteReleaseReason(reason);
            if (item == null || !entries.TryGetValue(item, out Entry entry))
            {
                return new PoolReleaseResult(PoolReleaseStatus.UnknownItem, lease);
            }

            if (!entry.IsActive)
            {
                return new PoolReleaseResult(PoolReleaseStatus.AlreadyReleased, lease);
            }

            if (lease != entry.Lease)
            {
                return new PoolReleaseResult(PoolReleaseStatus.StaleLease, lease);
            }

            PoolLease releasedLease = entry.Lease;
            ReleaseEntry(entry, reason);
            return new PoolReleaseResult(PoolReleaseStatus.Released, releasedLease);
        }

        public PoolRestartReport Restart()
        {
            ThrowIfDisposed();
            uint previousGeneration = generation;
            int releasedCount = activeEntries.Count;
            ReleaseAllActive(PoolReleaseReason.Restart);
            generation = generation == uint.MaxValue ? 1U : generation + 1U;
            return new PoolRestartReport(previousGeneration, generation, releasedCount);
        }

        public PoolLeakReport DetectLeaks()
        {
            ThrowIfDisposed();
            var leaks = new List<PoolLeak>(activeEntries.Count);
            for (int index = 0; index < activeEntries.Count; index++)
            {
                Entry entry = activeEntries[index];
                leaks.Add(new PoolLeak(entry.Item, entry.Lease));
            }

            return new PoolLeakReport(leaks);
        }

        public void AssertNoLeaks()
        {
            PoolLeakReport report = DetectLeaks();
            if (report.HasLeaks)
            {
                PoolLease first = report.Leaks[0].Lease;
                throw new InvalidOperationException(
                    $"Object pool has {report.Count} active lease(s); first leak is '{first.PoolId}' sequence {first.ActivationSequence}.");
            }
        }

        public PoolServiceSnapshot GetSnapshot()
        {
            ThrowIfDisposed();
            return new PoolServiceSnapshot(
                generation,
                families.Count,
                pools.Count,
                entries.Count,
                activeEntries.Count);
        }

        public int GetPoolAllocatedCount(string poolId)
        {
            ThrowIfDisposed();
            if (!pools.TryGetValue(poolId, out PoolState pool))
            {
                throw new KeyNotFoundException($"Pool '{poolId}' is not registered.");
            }

            return pool.AllocatedCount;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            ReleaseAllActive(PoolReleaseReason.ServiceDisposed);
            disposed = true;
        }

        private Entry CreateEntry(PoolState pool)
        {
            IPoolable item = pool.Definition.Factory();
            if (item == null)
            {
                throw new InvalidOperationException(
                    $"Pool '{pool.Definition.PoolId}' factory returned null.");
            }

            if (entries.ContainsKey(item))
            {
                throw new InvalidOperationException(
                    $"Pool '{pool.Definition.PoolId}' factory returned an item already owned by this service.");
            }

            if (item.IsPoolActive)
            {
                throw new InvalidOperationException(
                    $"Pool '{pool.Definition.PoolId}' factory returned an active item.");
            }

            var entry = new Entry(item, pool);
            entries.Add(item, entry);
            pool.AllocatedCount++;
            return entry;
        }

        private void RemovePoolEntries(PoolState pool)
        {
            var remove = new List<IPoolable>();
            foreach (KeyValuePair<IPoolable, Entry> pair in entries)
            {
                if (ReferenceEquals(pair.Value.Pool, pool))
                {
                    remove.Add(pair.Key);
                }
            }

            for (int index = 0; index < remove.Count; index++)
            {
                entries.Remove(remove[index]);
            }
        }

        private static Entry TakeInactive(PoolState pool)
        {
            int lastIndex = pool.Inactive.Count - 1;
            if (lastIndex < 0)
            {
                return null;
            }

            Entry entry = pool.Inactive[lastIndex];
            pool.Inactive.RemoveAt(lastIndex);
            return entry;
        }

        private Entry FindOldestActive(FamilyState family)
        {
            Entry oldest = null;
            for (int index = 0; index < activeEntries.Count; index++)
            {
                Entry candidate = activeEntries[index];
                if (!ReferenceEquals(candidate.Pool.Family, family))
                {
                    continue;
                }

                if (oldest == null ||
                    candidate.Lease.ActivationSequence < oldest.Lease.ActivationSequence)
                {
                    oldest = candidate;
                }
            }

            return oldest;
        }

        private void ReleaseEntry(Entry entry, PoolReleaseReason reason)
        {
            PoolLease lease = entry.Lease;
            entry.Item.ReleaseToPool(new PoolReleaseContext(lease, reason));
            ValidateReleased(entry.Item, entry.Pool.Definition.PoolId);
            entry.IsActive = false;
            entry.Lease = default;
            entry.Pool.Family.ActiveCount--;
            activeEntries.Remove(entry);
            entry.Pool.Inactive.Add(entry);
        }

        private void RollBackAcquire(Entry entry)
        {
            PoolLease lease = entry.Lease;
            try
            {
                entry.Item.ReleaseToPool(
                    new PoolReleaseContext(lease, PoolReleaseReason.AcquireRollback));
            }
            finally
            {
                entry.IsActive = false;
                entry.Lease = default;
                entry.Pool.Family.ActiveCount--;
                activeEntries.Remove(entry);
                entry.Pool.Inactive.Add(entry);
            }
        }

        private void ReleaseAllActive(PoolReleaseReason reason)
        {
            while (activeEntries.Count > 0)
            {
                ReleaseEntry(activeEntries[activeEntries.Count - 1], reason);
            }
        }

        private ulong NextActivationSequence()
        {
            ulong sequence = nextActivationSequence;
            nextActivationSequence = nextActivationSequence == ulong.MaxValue
                ? 1UL
                : nextActivationSequence + 1UL;
            return sequence;
        }

        private static void ValidateReleased(IPoolable item, string poolId)
        {
            if (item.IsPoolActive)
            {
                throw new InvalidOperationException(
                    $"Pool item released to '{poolId}' retained an active pool state.");
            }
        }

        private static void ValidateConcreteReleaseReason(PoolReleaseReason reason)
        {
            if (reason == PoolReleaseReason.Prewarm ||
                reason == PoolReleaseReason.ReusedOldest ||
                reason == PoolReleaseReason.Restart ||
                reason == PoolReleaseReason.ServiceDisposed ||
                reason == PoolReleaseReason.AcquireRollback ||
                reason == PoolReleaseReason.Manual ||
                reason == PoolReleaseReason.Completed)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ObjectPoolService));
            }
        }
    }
}
