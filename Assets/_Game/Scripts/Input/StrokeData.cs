using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    public enum StrokeCompletionReason
    {
        PointerEnded,
        MaximumLength,
        MaximumPointCount
    }

    public sealed class StrokeData
    {
        private readonly ReadOnlyCollection<Vector2> points;

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

        public ulong StrokeId { get; }

        public IReadOnlyList<Vector2> Points => points;

        public int PointCount => points.Count;

        public float TotalLengthReferencePixels { get; }

        public double StartedAt { get; }

        public double EndedAt { get; }

        public double Duration => Math.Max(0d, EndedAt - StartedAt);

        public double InitialHoldDuration { get; }

        public StrokeCompletionReason CompletionReason { get; }
    }
}
