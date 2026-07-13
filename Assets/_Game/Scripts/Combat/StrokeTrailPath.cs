using System;
using System.Collections.Generic;
using OneStrokeDemon.Input;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    public readonly struct StrokeTrailPath
    {
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

        public ulong StrokeId { get; }

        public IReadOnlyList<Vector2> Points { get; }

        public int PointCount => Points?.Count ?? 0;

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
