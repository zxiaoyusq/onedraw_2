using System;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    /// <summary>表示单个轨迹段查询到的目标、弱点和段内接触位置。</summary>
    public readonly struct StrokeHitCandidate
    {
        /// <summary>创建段参数位于零到一之间的候选命中。</summary>
        public StrokeHitCandidate(
            IHittable target,
            bool isWeakpoint,
            float segmentParameter)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            if (float.IsNaN(segmentParameter) ||
                float.IsInfinity(segmentParameter) ||
                segmentParameter < 0f ||
                segmentParameter > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(segmentParameter),
                    "Segment parameter must be finite and normalized.");
            }

            IsWeakpoint = isWeakpoint;
            SegmentParameter = segmentParameter;
        }

        /// <summary>获取候选目标。</summary>
        public IHittable Target { get; }

        /// <summary>获取此次 Collider 接触是否命中弱点。</summary>
        public bool IsWeakpoint { get; }

        /// <summary>获取沿当前段归一化的首次接触参数。</summary>
        public float SegmentParameter { get; }
    }

    /// <summary>以预分配结果缓冲查询一段笔迹胶囊命中的端口。</summary>
    public interface IStrokeHitQuery
    {
        /// <summary>查询参考像素段与半径，返回写入结果缓冲的候选数量。</summary>
        int QuerySegment(
            Vector2 startReferencePixels,
            Vector2 endReferencePixels,
            float radiusReferencePixels,
            StrokeHitCandidate[] results);
    }
}
