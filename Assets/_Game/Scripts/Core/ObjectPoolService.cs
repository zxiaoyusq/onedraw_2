using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OneStrokeDemon.Core
{
    /// <summary>
    /// 管理多个具体池及其共享家族容量，并用世代化租约保证回收和复用安全。
    /// </summary>
    public sealed class ObjectPoolService : IDisposable
    {
        /// <summary>记录池家族定义及其当前共享活动计数。</summary>
        private sealed class FamilyState
        {
            /// <summary>从已校验的家族定义创建运行状态。</summary>
            internal FamilyState(in PoolFamilyDefinition definition)
            {
                Definition = definition;
            }

            internal PoolFamilyDefinition Definition { get; }

            internal int ActiveCount { get; set; }
        }

        /// <summary>记录具体池、所属家族、非活动栈与累计分配数。</summary>
        private sealed class PoolState
        {
            /// <summary>创建具体池状态，并按预热数为非活动列表预留空间。</summary>
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

        /// <summary>把一个可池化对象与所属池、当前租约及活动状态绑定。</summary>
        private sealed class Entry
        {
            /// <summary>创建由本服务拥有的对象条目。</summary>
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

        /// <summary>
        /// 按对象引用而不是业务相等性比较IPoolable，防止两个实例被误视为同一池对象。
        /// </summary>
        private sealed class ReferenceComparer : IEqualityComparer<IPoolable>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();

            /// <summary>仅当两个参数是同一对象引用时返回true。</summary>
            public bool Equals(IPoolable left, IPoolable right) => ReferenceEquals(left, right);

            /// <summary>获取不受对象自定义GetHashCode影响的运行时引用哈希。</summary>
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

        /// <summary>当前对象池世代；每次重开后变更，使旧租约失效。</summary>
        public uint Generation => generation;

        /// <summary>
        /// 注册一个共享容量的池家族。
        /// </summary>
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

        /// <summary>
        /// 注册具体对象池，并按定义创建、重置所有预热对象。
        /// </summary>
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
                // 预热对象也必须走一次释放合同，保证初始进入池时已处于干净状态。
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
                // 预热中任一对象失败时撤销整个池，避免对外暴露半注册状态。
                pools.Remove(definition.PoolId);
                RemovePoolEntries(pool);
                throw;
            }
        }

        /// <summary>
        /// 从指定池取得一个对象并创建新租约；家族容量满时按配置拒绝或复用最旧对象。
        /// </summary>
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
                // Reject不创建、不激活、也不回收任何对象，因此池状态保持不变。
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

                // 先完整释放家族中的最旧租约，再从请求的目标池取对象。
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
                // 先在服务内记录租约，再让对象进入活动态；异常时可依此精确回滚。
                acquired.Item.AcquireFromPool(lease);
                if (!acquired.Item.IsPoolActive)
                {
                    throw new InvalidOperationException(
                        $"Pool item acquired from '{poolId}' did not enter an active pool state.");
                }
            }
            catch
            {
                // 对象初始化失败不能占用家族容量或留下活动租约。
                RollBackAcquire(acquired);
                throw;
            }

            return new PoolAcquireResult(
                PoolAcquireStatus.Acquired,
                acquired.Item,
                lease,
                reusedOldest);
        }

        /// <summary>
        /// 用精确租约释放对象；未知、重复或过期请求只返回状态，不改动当前对象。
        /// </summary>
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
                // 对象可能已被回收并重新租出，旧持有者不得释放新周期。
                return new PoolReleaseResult(PoolReleaseStatus.StaleLease, lease);
            }

            PoolLease releasedLease = entry.Lease;
            ReleaseEntry(entry, reason);
            return new PoolReleaseResult(PoolReleaseStatus.Released, releasedLease);
        }

        /// <summary>
        /// 回收全部活动对象并推进世代，使重开前发出的所有租约失效。
        /// </summary>
        public PoolRestartReport Restart()
        {
            ThrowIfDisposed();
            uint previousGeneration = generation;
            int releasedCount = activeEntries.Count;
            ReleaseAllActive(PoolReleaseReason.Restart);
            // 0作为无效世代，因此溢出时回绕1而不是0。
            generation = generation == uint.MaxValue ? 1U : generation + 1U;
            return new PoolRestartReport(previousGeneration, generation, releasedCount);
        }

        /// <summary>
        /// 快照当前所有活动租约，用于任务结束、重开和测试时检查泄漏。
        /// </summary>
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

        /// <summary>
        /// 断言当前不存在活动租约，否则报告首个泄漏的池ID与序号。
        /// </summary>
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

        /// <summary>
        /// 返回家族、池、已分配对象和活动租约的当前数量快照。
        /// </summary>
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

        /// <summary>
        /// 查询指定具体池自注册以来已创建的对象数。
        /// </summary>
        public int GetPoolAllocatedCount(string poolId)
        {
            ThrowIfDisposed();
            if (!pools.TryGetValue(poolId, out PoolState pool))
            {
                throw new KeyNotFoundException($"Pool '{poolId}' is not registered.");
            }

            return pool.AllocatedCount;
        }

        /// <summary>
        /// 幂等地回收全部活动对象并终止服务，后续操作将拒绝。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            ReleaseAllActive(PoolReleaseReason.ServiceDisposed);
            disposed = true;
        }

        /// <summary>
        /// 通过池工厂创建新对象，校验它未被占用或重复归属，然后纳入服务所有权。
        /// </summary>
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

        /// <summary>
        /// 注册预热失败时，从全局所有权索引中删除该池已创建的对象。
        /// </summary>
        private void RemovePoolEntries(PoolState pool)
        {
            // 不在遍历Dictionary时直接删除，先收集键以避免修改枚举器。
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

        /// <summary>
        /// 从非活动列表末尾以O(1)方式取出可复用条目，空池时返回null。
        /// </summary>
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

        /// <summary>
        /// 在指定家族的活动条目中查找激活序号最小的对象。
        /// </summary>
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

        /// <summary>
        /// 完整结束一个活动租约，同步对象状态、家族计数、活动索引和非活动列表。
        /// </summary>
        private void ReleaseEntry(Entry entry, PoolReleaseReason reason)
        {
            PoolLease lease = entry.Lease;
            entry.Item.ReleaseToPool(new PoolReleaseContext(lease, reason));
            // 只有对象确认已离开活动态，服务才更新容量与列表账本。
            ValidateReleased(entry.Item, entry.Pool.Definition.PoolId);
            entry.IsActive = false;
            entry.Lease = default;
            entry.Pool.Family.ActiveCount--;
            activeEntries.Remove(entry);
            entry.Pool.Inactive.Add(entry);
        }

        /// <summary>
        /// 对象AcquireFromPool失败时回滚服务账本，即使对象的释放逻辑再次抛异常也会清除租约。
        /// </summary>
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
                // finally保证服务内部容量不会因用户对象异常而永久泄漏。
                entry.IsActive = false;
                entry.Lease = default;
                entry.Pool.Family.ActiveCount--;
                activeEntries.Remove(entry);
                entry.Pool.Inactive.Add(entry);
            }
        }

        /// <summary>
        /// 以逆序持续回收活动列表末尾，直到所有租约结束。
        /// </summary>
        private void ReleaseAllActive(PoolReleaseReason reason)
        {
            while (activeEntries.Count > 0)
            {
                ReleaseEntry(activeEntries[activeEntries.Count - 1], reason);
            }
        }

        /// <summary>
        /// 取得下一个非零激活序号，溢出时安全回绕1。
        /// </summary>
        private ulong NextActivationSequence()
        {
            ulong sequence = nextActivationSequence;
            nextActivationSequence = nextActivationSequence == ulong.MaxValue
                ? 1UL
                : nextActivationSequence + 1UL;
            return sequence;
        }

        /// <summary>
        /// 验证池对象在ReleaseToPool返回后已明确进入非活动状态。
        /// </summary>
        private static void ValidateReleased(IPoolable item, string poolId)
        {
            if (item.IsPoolActive)
            {
                throw new InvalidOperationException(
                    $"Pool item released to '{poolId}' retained an active pool state.");
            }
        }

        /// <summary>
        /// 拒绝未知枚举值，避免池对象收到无法解释的释放原因。
        /// </summary>
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

        /// <summary>
        /// 保证已终止的服务不再被注册、获取、释放或查询。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ObjectPoolService));
            }
        }
    }
}
