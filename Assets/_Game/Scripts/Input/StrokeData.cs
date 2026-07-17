using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>一笔采样结束的原因。</summary>
    public enum StrokeCompletionReason
    {
        PointerEnded,
        MaximumLength,
        MaximumPointCount
    }

    /// <summary>保存一笔完成后冻结的参考像素采样点与时间信息。</summary>
    public sealed class StrokeData
    {
        private readonly ReadOnlyCollection<Vector2> points;

        /// <summary>接管点数组并创建只读笔迹快照。</summary>
        internal StrokeData(
            ulong strokeId,
            Vector2[] ownedPoints,
            float totalLengthReferencePixels,
            double startedAt,
            double endedAt,
            double initialHoldDuration,
            StrokeCompletionReason completionReason)
        {
            if (ownedPoints == null)
            {
                throw new ArgumentNullException(nameof(ownedPoints));
            }

            StrokeId = strokeId;
            points = Array.AsReadOnly(ownedPoints);
            TotalLengthReferencePixels = totalLengthReferencePixels;
            StartedAt = startedAt;
            EndedAt = endedAt;
            InitialHoldDuration = Math.Max(0d, initialHoldDuration);
            CompletionReason = completionReason;
        }

        /// <summary>获取单调非零笔迹 ID。</summary>
        public ulong StrokeId { get; }

        /// <summary>获取不可修改的采样点序列。</summary>
        public IReadOnlyList<Vector2> Points => points;

        /// <summary>获取采样点数量。</summary>
        public int PointCount => points.Count;

        /// <summary>获取采样阶段累计路径长度。</summary>
        public float TotalLengthReferencePixels { get; }

        /// <summary>获取起笔时间戳。</summary>
        public double StartedAt { get; }

        /// <summary>获取笔迹完成时间戳。</summary>
        public double EndedAt { get; }

        /// <summary>获取非负绘制持续时间。</summary>
        public double Duration => Math.Max(0d, EndedAt - StartedAt);

        /// <summary>获取起笔到首次有效移动的停留时间。</summary>
        public double InitialHoldDuration { get; }

        /// <summary>获取采样完成原因。</summary>
        public StrokeCompletionReason CompletionReason { get; }
    }
}
