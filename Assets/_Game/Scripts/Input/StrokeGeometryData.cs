using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    public sealed class StrokeGeometryData
    {
        private readonly ReadOnlyCollection<Vector2> points;

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

        public StrokeData Source { get; }

        public ulong StrokeId => Source.StrokeId;

        public IReadOnlyList<Vector2> Points => points;

        public int PointCount => points.Count;

        public int SourcePointCount => Source.PointCount;

        public float LengthReferencePixels { get; }

        public Rect BoundsReferencePixels { get; }

        public float SignedAreaReferencePixelsSquared { get; }

        public float AreaReferencePixelsSquared => Math.Abs(SignedAreaReferencePixelsSquared);

        public float ClosureDistanceReferencePixels { get; }

        public float ClosureRatio { get; }

        public float SignedCurvatureRadians { get; }

        public float TotalCurvatureRadians { get; }

        public float NormalizedCurvature => TotalCurvatureRadians / Mathf.PI;

        public double StartedAt => Source.StartedAt;

        public double EndedAt => Source.EndedAt;

        public double Duration => Source.Duration;

        public StrokeCompletionReason CompletionReason => Source.CompletionReason;
    }
}
