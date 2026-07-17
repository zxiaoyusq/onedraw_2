using System;
using System.Collections.Generic;
using OneStrokeDemon.Input;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    /// <summary>把命中所用的处理后点集作为只读轨迹表现路径传递。</summary>
    public readonly struct StrokeTrailPath
    {
        /// <summary>创建至少含两个点且带非零笔迹 ID 的轨迹路径。</summary>
        public StrokeTrailPath(ulong strokeId, IReadOnlyList<Vector2> points)
        {
            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId), "Stroke id must be positive.");
            }

            Points = points ?? throw new ArgumentNullException(nameof(points));
            if (points.Count < 2)
            {
                throw new ArgumentException("A stroke trail needs at least two points.", nameof(points));
            }

            StrokeId = strokeId;
        }

        /// <summary>获取笔迹 ID。</summary>
        public ulong StrokeId { get; }

        /// <summary>获取与几何/命中共享的只读点集。</summary>
        public IReadOnlyList<Vector2> Points { get; }

        /// <summary>获取点数；默认结构返回零。</summary>
        public int PointCount => Points?.Count ?? 0;

        /// <summary>直接从几何快照创建同点集轨迹，避免视觉与命中路径分叉。</summary>
        public static StrokeTrailPath FromGeometry(StrokeGeometryData geometry)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException(nameof(geometry));
            }

            return new StrokeTrailPath(geometry.StrokeId, geometry.Points);
        }
    }
}
