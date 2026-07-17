namespace OneStrokeDemon.Combat
{
    /// <summary>可被笔迹命中解析器识别的稳定目标合同。</summary>
    public interface IHittable
    {
        /// <summary>获取当前生命周期内非零且唯一的目标 ID。</summary>
        int HitTargetId { get; }

        /// <summary>获取目标当前是否接受笔迹命中。</summary>
        bool CanReceiveStrokeHit { get; }
    }

    /// <summary>把物理 Collider 映射到目标，并标明弱点和命中盒活动状态。</summary>
    public interface IStrokeHitbox
    {
        /// <summary>获取命中盒所属目标。</summary>
        IHittable HitTarget { get; }

        /// <summary>获取该命中盒是否代表弱点。</summary>
        bool IsWeakpoint { get; }

        /// <summary>获取该命中盒当前是否参与查询。</summary>
        bool IsStrokeHitboxActive { get; }
    }
}
