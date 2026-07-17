using System;
using System.Collections.Generic;

namespace OneStrokeDemon.Core
{
    /// <summary>
    /// 对象池所属家族达到共享容量上限时的处理策略。
    /// </summary>
    public enum PoolExhaustionPolicy
    {
        Reject = 0,
        ReuseOldest = 1
    }

    /// <summary>
    /// 对象被送回池中的原因，供池对象选择正确的重置路径。
    /// </summary>
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

    /// <summary>
    /// 一次获取请求的结果状态。
    /// </summary>
    public enum PoolAcquireStatus
    {
        Acquired = 0,
        RejectedAtCapacity = 1
    }

    /// <summary>
    /// 一次释放请求的结果状态。
    /// </summary>
    public enum PoolReleaseStatus
    {
        Released = 0,
        UnknownItem = 1,
        AlreadyReleased = 2,
        StaleLease = 3
    }

    /// <summary>
    /// 定义一组共享活动容量和耗尽策略的对象池家族。
    /// </summary>
    public readonly struct PoolFamilyDefinition
    {
        /// <summary>
        /// 创建对象池家族定义，并立即校验ID、容量和策略。
        /// </summary>
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

        /// <summary>家族的稳定标识。</summary>
        public string FamilyId { get; }

        /// <summary>该家族允许同时活动的最大对象数。</summary>
        public int Capacity { get; }

        /// <summary>容量耗尽时使用的策略。</summary>
        public PoolExhaustionPolicy ExhaustionPolicy { get; }
    }

    /// <summary>
    /// 定义一个具体对象池的ID、家族归属、预热数量和实例工厂。
    /// </summary>
    public readonly struct PoolDefinition
    {
        /// <summary>
        /// 创建具体池定义，并拒绝空ID、负预热数或空工厂。
        /// </summary>
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

        /// <summary>具体池的稳定标识。</summary>
        public string PoolId { get; }

        /// <summary>池所属的家族ID。</summary>
        public string FamilyId { get; }

        /// <summary>注册时预先创建并回收的对象数。</summary>
        public int PrewarmCount { get; }

        /// <summary>池需要扩容时创建新对象的工厂。</summary>
        public Func<IPoolable> Factory { get; }
    }

    /// <summary>
    /// 标识某个对象当前激活周期的不可变租约，用于拒绝旧持有者的延迟释放。
    /// </summary>
    public readonly struct PoolLease : IEquatable<PoolLease>
    {
        /// <summary>
        /// 由对象池服务创建租约；外部代码不能伪造新的激活周期。
        /// </summary>
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

        /// <summary>租约对应的具体池ID。</summary>
        public string PoolId { get; }

        /// <summary>租约对应的家族ID。</summary>
        public string FamilyId { get; }

        /// <summary>服务重开世代，重开后旧世代租约失效。</summary>
        public uint Generation { get; }

        /// <summary>本世代内的单调激活序号，也用于确定最旧活动对象。</summary>
        public ulong ActivationSequence { get; }

        /// <summary>租约是否包含完整且非零的身份信息。</summary>
        public bool IsValid =>
            Generation > 0U &&
            ActivationSequence > 0UL &&
            !string.IsNullOrEmpty(PoolId) &&
            !string.IsNullOrEmpty(FamilyId);

        /// <summary>
        /// 按池ID、家族ID、世代和激活序号比较两份租约。
        /// </summary>
        public bool Equals(PoolLease other)
        {
            return Generation == other.Generation &&
                ActivationSequence == other.ActivationSequence &&
                string.Equals(PoolId, other.PoolId, StringComparison.Ordinal) &&
                string.Equals(FamilyId, other.FamilyId, StringComparison.Ordinal);
        }

        /// <summary>与装箱后的租约对象比较是否相等。</summary>
        public override bool Equals(object obj)
        {
            return obj is PoolLease other && Equals(other);
        }

        /// <summary>
        /// 使用与相等性一致的四个字段生成确定性哈希码。
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                // 所有字符串都使用Ordinal比较器，不受运行环境区域设置影响。
                int hash = 17;
                hash = (hash * 31) + Generation.GetHashCode();
                hash = (hash * 31) + ActivationSequence.GetHashCode();
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(PoolId ?? string.Empty);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(FamilyId ?? string.Empty);
                return hash;
            }
        }

        /// <summary>判断两份租约是否标识同一激活周期。</summary>
        public static bool operator ==(PoolLease left, PoolLease right) => left.Equals(right);

        /// <summary>判断两份租约是否来自不同激活周期。</summary>
        public static bool operator !=(PoolLease left, PoolLease right) => !left.Equals(right);
    }

    /// <summary>
    /// 传递给池对象的释放上下文，同时说明被释放的租约和原因。
    /// </summary>
    public readonly struct PoolReleaseContext
    {
        /// <summary>由对象池服务构造释放上下文。</summary>
        internal PoolReleaseContext(in PoolLease lease, PoolReleaseReason reason)
        {
            Lease = lease;
            Reason = reason;
        }

        /// <summary>本次结束的激活租约；预热时可为空租约。</summary>
        public PoolLease Lease { get; }

        /// <summary>对象被回收的原因。</summary>
        public PoolReleaseReason Reason { get; }
    }

    /// <summary>
    /// 所有可池化对象必须实现的生命周期合同。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>对象当前是否处于被租出的活动状态。</summary>
        bool IsPoolActive { get; }

        /// <summary>开始一次新的租用周期，并应完整初始化运行态。</summary>
        void AcquireFromPool(in PoolLease lease);

        /// <summary>结束当前周期，清除所有可能泄漏到下次使用的状态。</summary>
        void ReleaseToPool(in PoolReleaseContext context);
    }

    /// <summary>
    /// 对象池获取结果，同时携带对象、租约与是否触发最旧复用。
    /// </summary>
    public readonly struct PoolAcquireResult
    {
        /// <summary>由对象池服务组装获取结果。</summary>
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

        /// <summary>获取状态。</summary>
        public PoolAcquireStatus Status { get; }

        /// <summary>成功获取的对象；容量拒绝时为空。</summary>
        public IPoolable Item { get; }

        /// <summary>对象本次激活的租约。</summary>
        public PoolLease Lease { get; }

        /// <summary>本次获取前是否回收了同家族最旧对象。</summary>
        public bool ReusedOldest { get; }

        /// <summary>请求是否成功且返回了非空对象。</summary>
        public bool IsAcquired => Status == PoolAcquireStatus.Acquired && Item != null;
    }

    /// <summary>
    /// 对象池释放请求的不可变结果。
    /// </summary>
    public readonly struct PoolReleaseResult
    {
        /// <summary>由对象池服务组装释放结果。</summary>
        internal PoolReleaseResult(PoolReleaseStatus status, in PoolLease lease)
        {
            Status = status;
            Lease = lease;
        }

        /// <summary>释放状态。</summary>
        public PoolReleaseStatus Status { get; }

        /// <summary>调用方提交或成功释放的租约。</summary>
        public PoolLease Lease { get; }

        /// <summary>该请求是否真正结束了一个活动租约。</summary>
        public bool WasReleased => Status == PoolReleaseStatus.Released;
    }

    /// <summary>
    /// 某一时刻对象池服务的数量快照。
    /// </summary>
    public readonly struct PoolServiceSnapshot
    {
        /// <summary>由对象池服务根据当前内部计数创建快照。</summary>
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

        /// <summary>当前服务世代。</summary>
        public uint Generation { get; }

        /// <summary>已注册家族数量。</summary>
        public int FamilyCount { get; }

        /// <summary>已注册具体池数量。</summary>
        public int PoolCount { get; }

        /// <summary>服务当前拥有的对象总数。</summary>
        public int AllocatedCount { get; }

        /// <summary>当前持有活动租约的对象数。</summary>
        public int ActiveCount { get; }

        /// <summary>已分配但可立即复用的对象数。</summary>
        public int InactiveCount => AllocatedCount - ActiveCount;
    }

    /// <summary>
    /// 一条未释放池租约记录。
    /// </summary>
    public readonly struct PoolLeak
    {
        /// <summary>由泄漏检测器组装活动对象与租约。</summary>
        internal PoolLeak(IPoolable item, in PoolLease lease)
        {
            Item = item;
            Lease = lease;
        }

        /// <summary>仍处于活动状态的池对象。</summary>
        public IPoolable Item { get; }

        /// <summary>对象当前未释放的租约。</summary>
        public PoolLease Lease { get; }
    }

    /// <summary>
    /// 汇总检测时仍活动的全部对象池租约。
    /// </summary>
    public sealed class PoolLeakReport
    {
        /// <summary>创建只读泄漏报告。</summary>
        internal PoolLeakReport(IReadOnlyList<PoolLeak> leaks)
        {
            Leaks = leaks ?? throw new ArgumentNullException(nameof(leaks));
        }

        /// <summary>检测到的活动租约列表。</summary>
        public IReadOnlyList<PoolLeak> Leaks { get; }

        /// <summary>泄漏记录数量。</summary>
        public int Count => Leaks.Count;

        /// <summary>是否至少存在一条未释放租约。</summary>
        public bool HasLeaks => Count > 0;
    }

    /// <summary>
    /// 对象池整体重开操作的世代变化与回收数量。
    /// </summary>
    public readonly struct PoolRestartReport
    {
        /// <summary>由对象池服务创建重开报告。</summary>
        internal PoolRestartReport(uint previousGeneration, uint generation, int releasedCount)
        {
            PreviousGeneration = previousGeneration;
            Generation = generation;
            ReleasedCount = releasedCount;
        }

        /// <summary>重开前的世代。</summary>
        public uint PreviousGeneration { get; }

        /// <summary>重开后的新世代。</summary>
        public uint Generation { get; }

        /// <summary>重开过程回收的活动对象数。</summary>
        public int ReleasedCount { get; }
    }
}
