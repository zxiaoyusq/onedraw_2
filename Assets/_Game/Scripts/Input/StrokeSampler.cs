using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    public enum StrokeSamplingState
    {
        Idle,
        Sampling,
        Completed,
        Canceled
    }

    public enum StrokeSampleResult
    {
        Accepted,
        IgnoredBelowMinimumDistance,
        IgnoredNotSampling,
        CompletedMaximumLength,
        CompletedMaximumPointCount
    }

    public sealed class StrokeSampler
    {
        private readonly Vector2[] pointBuffer;
        private readonly float minimumPointDistanceSquared;
        private int pointCount;
        private ulong strokeId;
        private float totalLength;
        private double startedAt;
        private StrokeData completedStroke;

        public StrokeSampler(StrokeSamplingSettings settings)
        {
            Settings = settings;
            pointBuffer = new Vector2[settings.MaximumPointCount];
            minimumPointDistanceSquared =
                settings.MinimumPointDistanceReferencePixels *
                settings.MinimumPointDistanceReferencePixels;
        }

        public StrokeSamplingSettings Settings { get; }

        public StrokeSamplingState State { get; private set; }

        public bool IsSampling => State == StrokeSamplingState.Sampling;

        public StrokeData CompletedStroke => completedStroke;

        public void Begin(ulong newStrokeId, Vector2 referencePosition, double timestamp)
        {
            if (State == StrokeSamplingState.Sampling)
            {
                throw new InvalidOperationException("Cannot begin a new stroke while another stroke is sampling.");
            }

            if (newStrokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newStrokeId), "Stroke IDs must be non-zero.");
            }

            ValidatePoint(referencePosition, nameof(referencePosition));
            ValidateTimestamp(timestamp, nameof(timestamp));

            strokeId = newStrokeId;
            pointBuffer[0] = referencePosition;
            pointCount = 1;
            totalLength = 0f;
            startedAt = timestamp;
            completedStroke = null;
            State = StrokeSamplingState.Sampling;
        }

        public StrokeSampleResult AddPoint(Vector2 referencePosition, double timestamp)
        {
            if (State != StrokeSamplingState.Sampling)
            {
                return StrokeSampleResult.IgnoredNotSampling;
            }

            ValidatePoint(referencePosition, nameof(referencePosition));
            ValidateTimestamp(timestamp, nameof(timestamp));

            Vector2 lastPoint = pointBuffer[pointCount - 1];
            Vector2 segment = referencePosition - lastPoint;
            float segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared < minimumPointDistanceSquared)
            {
                return StrokeSampleResult.IgnoredBelowMinimumDistance;
            }

            float segmentLength = Mathf.Sqrt(segmentLengthSquared);
            float remainingLength = Settings.MaximumStrokeLengthReferencePixels - totalLength;
            if (segmentLength >= remainingLength)
            {
                Vector2 cutoffPoint = lastPoint + segment * (remainingLength / segmentLength);
                Append(cutoffPoint, Settings.MaximumStrokeLengthReferencePixels);
                Complete(timestamp, StrokeCompletionReason.MaximumLength);
                return StrokeSampleResult.CompletedMaximumLength;
            }

            Append(referencePosition, totalLength + segmentLength);
            if (pointCount == pointBuffer.Length)
            {
                Complete(timestamp, StrokeCompletionReason.MaximumPointCount);
                return StrokeSampleResult.CompletedMaximumPointCount;
            }

            return StrokeSampleResult.Accepted;
        }

        public StrokeData End(Vector2 referencePosition, double timestamp)
        {
            if (State == StrokeSamplingState.Completed)
            {
                return completedStroke;
            }

            if (State != StrokeSamplingState.Sampling)
            {
                throw new InvalidOperationException("Only an active stroke can end.");
            }

            StrokeSampleResult result = AddPoint(referencePosition, timestamp);
            if (result != StrokeSampleResult.CompletedMaximumLength &&
                result != StrokeSampleResult.CompletedMaximumPointCount)
            {
                Complete(timestamp, StrokeCompletionReason.PointerEnded);
            }

            return completedStroke;
        }

        public bool Cancel()
        {
            if (State != StrokeSamplingState.Sampling)
            {
                return false;
            }

            State = StrokeSamplingState.Canceled;
            completedStroke = null;
            pointCount = 0;
            totalLength = 0f;
            return true;
        }

        private void Append(Vector2 point, float newTotalLength)
        {
            pointBuffer[pointCount] = point;
            pointCount++;
            totalLength = newTotalLength;
        }

        private void Complete(double timestamp, StrokeCompletionReason completionReason)
        {
            var frozenPoints = new Vector2[pointCount];
            Array.Copy(pointBuffer, frozenPoints, pointCount);
            completedStroke = new StrokeData(
                strokeId,
                frozenPoints,
                totalLength,
                startedAt,
                timestamp,
                completionReason);
            State = StrokeSamplingState.Completed;
        }

        private static void ValidatePoint(Vector2 point, string parameterName)
        {
            if (float.IsNaN(point.x) || float.IsInfinity(point.x) ||
                float.IsNaN(point.y) || float.IsInfinity(point.y))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Stroke points must be finite.");
            }
        }

        private static void ValidateTimestamp(double timestamp, string parameterName)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Stroke timestamps must be finite.");
            }
        }
    }
}
