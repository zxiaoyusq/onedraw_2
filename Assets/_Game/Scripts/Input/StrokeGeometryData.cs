using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>保存供识别、视觉与命中共享的处理后点集及全部几何指标。</summary>
    public sealed class StrokeGeometryData
    {
        private readonly ReadOnlyCollection<Vector2> points;

        /// <summary>接管处理后点数组并创建不可变几何快照。</summary>
        internal StrokeGeometryData(
            StrokeData source,
            Vector2[] ownedPoints,
            float lengthReferencePixels,
            Rect boundsReferencePixels,
            float signedAreaReferencePixelsSquared,
            float closureDistanceReferencePixels,
            float closureRatio,
            float signedCurvatureRadians,
            float totalCurvatureRadians)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (ownedPoints == null)
            {
                throw new ArgumentNullException(nameof(ownedPoints));
            }

            points = Array.AsReadOnly(ownedPoints);
            LengthReferencePixels = lengthReferencePixels;
            BoundsReferencePixels = boundsReferencePixels;
            SignedAreaReferencePixelsSquared = signedAreaReferencePixelsSquared;
            ClosureDistanceReferencePixels = closureDistanceReferencePixels;
            ClosureRatio = closureRatio;
            SignedCurvatureRadians = signedCurvatureRadians;
            TotalCurvatureRadians = totalCurvatureRadians;
        }

        /// <summary>获取来源采样笔迹。</summary>
        public StrokeData Source { get; }

        /// <summary>获取来源笔迹 ID。</summary>
        public ulong StrokeId => Source.StrokeId;

        /// <summary>获取不可修改的处理后点集。</summary>
        public IReadOnlyList<Vector2> Points => points;

        /// <summary>获取处理后点数。</summary>
        public int PointCount => points.Count;

        /// <summary>获取处理前采样点数。</summary>
        public int SourcePointCount => Source.PointCount;

        /// <summary>获取处理后路径长度。</summary>
        public float LengthReferencePixels { get; }

        /// <summary>获取参考像素包围盒。</summary>
        public Rect BoundsReferencePixels { get; }

        /// <summary>获取带顺逆时针方向的隐式闭合面积。</summary>
        public float SignedAreaReferencePixelsSquared { get; }

        /// <summary>获取不区分方向的绝对面积。</summary>
        public float AreaReferencePixelsSquared => Math.Abs(SignedAreaReferencePixelsSquared);

        /// <summary>获取首尾直线距离。</summary>
        public float ClosureDistanceReferencePixels { get; }

        /// <summary>获取首尾距离与路径长度之比。</summary>
        public float ClosureRatio { get; }

        /// <summary>获取保留转向符号的累计转角。</summary>
        public float SignedCurvatureRadians { get; }

        /// <summary>获取累计绝对转角。</summary>
        public float TotalCurvatureRadians { get; }

        /// <summary>获取总绝对转角除以 π 的尺度无关曲率。</summary>
        public float NormalizedCurvature => TotalCurvatureRadians / Mathf.PI;

        /// <summary>获取来源起笔时间。</summary>
        public double StartedAt => Source.StartedAt;

        /// <summary>获取来源结束时间。</summary>
        public double EndedAt => Source.EndedAt;

        /// <summary>获取来源绘制持续时间。</summary>
        public double Duration => Source.Duration;

        /// <summary>获取来源起笔停留时间。</summary>
        public double InitialHoldDuration => Source.InitialHoldDuration;

        /// <summary>获取来源采样完成原因。</summary>
        public StrokeCompletionReason CompletionReason => Source.CompletionReason;
    }
}
