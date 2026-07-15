using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    public readonly struct StrokePreviewPointEvent
    {
        public StrokePreviewPointEvent(ulong strokeId, Vector2 referencePosition)
        {
            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId));
            }

            StrokeId = strokeId;
            ReferencePosition = referencePosition;
        }

        public ulong StrokeId { get; }

        public Vector2 ReferencePosition { get; }
    }

    public readonly struct StrokeCanceledEvent
    {
        public StrokeCanceledEvent(
            ulong strokeId,
            double timestamp,
            PointerCancelReason reason)
        {
            if (reason == PointerCancelReason.None)
            {
                throw new ArgumentException("A canceled stroke requires a cancellation reason.", nameof(reason));
            }

            StrokeId = strokeId;
            Timestamp = timestamp;
            Reason = reason;
        }

        public ulong StrokeId { get; }

        public double Timestamp { get; }

        public PointerCancelReason Reason { get; }
    }

    public sealed class StrokeInputCollector : IDisposable
    {
        private readonly IPointerInput pointerInput;
        private readonly StrokeSampler sampler;
        private ulong nextStrokeId;
        private int activePointerId;
        private PointerSource activeSource;
        private int previewPointCount;
        private bool awaitingPointerTerminal;
        private bool disposed;

        public StrokeInputCollector(
            IPointerInput pointerInput,
            StrokeSamplingSettings settings,
            ulong initialStrokeId = 0)
        {
            this.pointerInput = pointerInput ?? throw new ArgumentNullException(nameof(pointerInput));
            sampler = new StrokeSampler(settings);
            nextStrokeId = initialStrokeId;
            pointerInput.PointerChanged += OnPointerChanged;
        }

        public event Action<StrokeData> StrokeCompleted;

        public event Action<StrokeCanceledEvent> StrokeCanceled;

        public event Action<StrokePreviewPointEvent> StrokeStarted;

        public event Action<StrokePreviewPointEvent> StrokePointAdded;

        public bool IsCollecting => sampler.IsSampling;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            pointerInput.PointerChanged -= OnPointerChanged;
            sampler.Cancel();
            previewPointCount = 0;
            awaitingPointerTerminal = false;
            disposed = true;
        }

        private void OnPointerChanged(PointerInputEvent pointerEvent)
        {
            if (disposed)
            {
                return;
            }

            switch (pointerEvent.Phase)
            {
                case PointerPhase.Began:
                    Begin(pointerEvent);
                    break;
                case PointerPhase.Moved:
                    Move(pointerEvent);
                    break;
                case PointerPhase.Ended:
                    End(pointerEvent);
                    break;
                case PointerPhase.Canceled:
                    Cancel(pointerEvent);
                    break;
            }
        }

        private void Begin(PointerInputEvent pointerEvent)
        {
            if (sampler.IsSampling || awaitingPointerTerminal)
            {
                return;
            }

            if (nextStrokeId == ulong.MaxValue)
            {
                throw new InvalidOperationException("Stroke ID space was exhausted.");
            }

            activePointerId = pointerEvent.PointerId;
            activeSource = pointerEvent.Source;
            nextStrokeId++;
            sampler.Begin(nextStrokeId, pointerEvent.ReferencePosition, pointerEvent.Timestamp);
            previewPointCount = 1;
            StrokeStarted?.Invoke(new StrokePreviewPointEvent(
                nextStrokeId,
                pointerEvent.ReferencePosition));
        }

        private void Move(PointerInputEvent pointerEvent)
        {
            if (!MatchesActivePointer(pointerEvent) || !sampler.IsSampling)
            {
                return;
            }

            StrokeSampleResult result = sampler.AddPoint(
                pointerEvent.ReferencePosition,
                pointerEvent.Timestamp);
            if (result == StrokeSampleResult.Accepted)
            {
                PublishPreviewPoint(pointerEvent.ReferencePosition);
            }
            else if (result == StrokeSampleResult.CompletedMaximumLength ||
                result == StrokeSampleResult.CompletedMaximumPointCount)
            {
                awaitingPointerTerminal = true;
                PublishMissingPreviewPoints(sampler.CompletedStroke);
                StrokeCompleted?.Invoke(sampler.CompletedStroke);
            }
        }

        private void End(PointerInputEvent pointerEvent)
        {
            if (!MatchesActivePointer(pointerEvent))
            {
                return;
            }

            if (sampler.IsSampling)
            {
                StrokeData stroke = sampler.End(
                    pointerEvent.ReferencePosition,
                    pointerEvent.Timestamp);
                PublishMissingPreviewPoints(stroke);
                StrokeCompleted?.Invoke(stroke);
            }

            previewPointCount = 0;
            awaitingPointerTerminal = false;
        }

        private void Cancel(PointerInputEvent pointerEvent)
        {
            if (!MatchesActivePointer(pointerEvent))
            {
                return;
            }

            if (sampler.IsSampling)
            {
                ulong canceledStrokeId = nextStrokeId;
                sampler.Cancel();
                previewPointCount = 0;
                StrokeCanceled?.Invoke(new StrokeCanceledEvent(
                    canceledStrokeId,
                    pointerEvent.Timestamp,
                    pointerEvent.CancelReason));
            }

            awaitingPointerTerminal = false;
        }

        private void PublishMissingPreviewPoints(StrokeData stroke)
        {
            while (previewPointCount < stroke.PointCount)
            {
                PublishPreviewPoint(stroke.Points[previewPointCount]);
            }
        }

        private void PublishPreviewPoint(Vector2 referencePosition)
        {
            previewPointCount += 1;
            StrokePointAdded?.Invoke(new StrokePreviewPointEvent(
                nextStrokeId,
                referencePosition));
        }

        private bool MatchesActivePointer(PointerInputEvent pointerEvent)
        {
            return (sampler.IsSampling || awaitingPointerTerminal) &&
                   pointerEvent.PointerId == activePointerId &&
                   pointerEvent.Source == activeSource;
        }
    }
}
