using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T310
{
    [Category("StrokeSampling")]
    public sealed class StrokeSamplerBoundaryTests
    {
        [Test]
        public void ShortJitterIsFilteredAndThresholdDistanceIsAccepted()
        {
            var sampler = new StrokeSampler(new StrokeSamplingSettings(5f, 100f, 10));
            sampler.Begin(1, Vector2.zero, 1d);

            Assert.That(
                sampler.AddPoint(new Vector2(3f, 0f), 2d),
                Is.EqualTo(StrokeSampleResult.IgnoredBelowMinimumDistance));
            Assert.That(
                sampler.AddPoint(new Vector2(5f, 0f), 3d),
                Is.EqualTo(StrokeSampleResult.Accepted));

            StrokeData stroke = sampler.End(new Vector2(7f, 0f), 4d);

            Assert.That(stroke.PointCount, Is.EqualTo(2));
            AssertVector(stroke.Points[0], Vector2.zero);
            AssertVector(stroke.Points[1], new Vector2(5f, 0f));
            Assert.That(stroke.TotalLengthReferencePixels, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(stroke.CompletionReason, Is.EqualTo(StrokeCompletionReason.PointerEnded));
        }

        [Test]
        public void SegmentCrossingMaximumLengthIsClippedAtExactPathDistance()
        {
            var sampler = new StrokeSampler(new StrokeSamplingSettings(1f, 10f, 10));
            sampler.Begin(7, Vector2.zero, 1d);
            Assert.That(
                sampler.AddPoint(new Vector2(6f, 0f), 2d),
                Is.EqualTo(StrokeSampleResult.Accepted));

            StrokeSampleResult result = sampler.AddPoint(new Vector2(6f, 8f), 3d);
            StrokeData stroke = sampler.CompletedStroke;

            Assert.That(result, Is.EqualTo(StrokeSampleResult.CompletedMaximumLength));
            Assert.That(stroke, Is.Not.Null);
            Assert.That(stroke.PointCount, Is.EqualTo(3));
            AssertVector(stroke.Points[2], new Vector2(6f, 4f));
            Assert.That(stroke.TotalLengthReferencePixels, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(stroke.CompletionReason, Is.EqualTo(StrokeCompletionReason.MaximumLength));
            Assert.That(stroke.EndedAt, Is.EqualTo(3d));
        }

        [Test]
        public void MaximumPointCountCompletesOnceAtTheLastAcceptedPoint()
        {
            var sampler = new StrokeSampler(new StrokeSamplingSettings(1f, 100f, 3));
            sampler.Begin(2, Vector2.zero, 1d);
            Assert.That(
                sampler.AddPoint(new Vector2(10f, 0f), 2d),
                Is.EqualTo(StrokeSampleResult.Accepted));

            Assert.That(
                sampler.AddPoint(new Vector2(20f, 0f), 3d),
                Is.EqualTo(StrokeSampleResult.CompletedMaximumPointCount));
            StrokeData completed = sampler.CompletedStroke;

            Assert.That(
                sampler.AddPoint(new Vector2(30f, 0f), 4d),
                Is.EqualTo(StrokeSampleResult.IgnoredNotSampling));
            Assert.That(sampler.End(new Vector2(40f, 0f), 5d), Is.SameAs(completed));
            Assert.That(completed.PointCount, Is.EqualTo(3));
            AssertVector(completed.Points[2], new Vector2(20f, 0f));
            Assert.That(completed.TotalLengthReferencePixels, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(completed.CompletionReason, Is.EqualTo(StrokeCompletionReason.MaximumPointCount));
        }

        [Test]
        public void CompletedDataIsImmutableAndUnaffectedBySamplerReuse()
        {
            var sampler = new StrokeSampler(new StrokeSamplingSettings(1f, 100f, 4));
            sampler.Begin(11, Vector2.zero, 1d);
            StrokeData first = sampler.End(new Vector2(10f, 0f), 2d);
            var mutableView = first.Points as IList<Vector2>;

            Assert.That(mutableView, Is.Not.Null);
            Assert.That(mutableView.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutableView[0] = Vector2.one);

            sampler.Begin(12, new Vector2(50f, 50f), 3d);
            sampler.End(new Vector2(60f, 50f), 4d);

            Assert.That(first.StrokeId, Is.EqualTo(11));
            AssertVector(first.Points[0], Vector2.zero);
            AssertVector(first.Points[1], new Vector2(10f, 0f));
        }

        [Test]
        public void RuntimeSettingsAreMappedFromTheSelectedStrokeRule()
        {
            var config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), RuntimeConfigTestFixture.Source);
            StrokeRuleConfig circleRule = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeCircle);

            StrokeSamplingSettings settings = StrokeSamplingSettingsFactory.FromConfig(circleRule);

            Assert.That(
                settings.MinimumPointDistanceReferencePixels,
                Is.EqualTo((float)circleRule.MinPointDistanceRefPx));
            Assert.That(
                settings.MaximumStrokeLengthReferencePixels,
                Is.EqualTo((float)circleRule.MaxStrokeLengthRefPx));
            Assert.That(settings.MaximumPointCount, Is.EqualTo((int)circleRule.MaxPointCount));
            Assert.That(settings.MinimumPointDistanceReferencePixels, Is.EqualTo(8f));
            Assert.That(settings.MaximumStrokeLengthReferencePixels, Is.EqualTo(1600f));
            Assert.That(settings.MaximumPointCount, Is.EqualTo(80));
        }

        [Test]
        public void CollectorConvertsPointerEndAndCancellationIntoDistinctStrokeEvents()
        {
            var pointer = new FakePointerInput();
            using var collector = new StrokeInputCollector(
                pointer,
                new StrokeSamplingSettings(1f, 100f, 10));
            var completed = new List<StrokeData>();
            var canceled = new List<StrokeCanceledEvent>();
            collector.StrokeCompleted += completed.Add;
            collector.StrokeCanceled += canceled.Add;

            pointer.Emit(Event(4, PointerPhase.Began, Vector2.zero, 1d));
            pointer.Emit(Event(4, PointerPhase.Moved, new Vector2(10f, 0f), 2d));
            pointer.Emit(Event(
                4,
                PointerPhase.Canceled,
                new Vector2(10f, 0f),
                3d,
                PointerCancelReason.FocusLost));

            Assert.That(completed, Is.Empty);
            Assert.That(canceled.Count, Is.EqualTo(1));
            Assert.That(canceled[0].StrokeId, Is.EqualTo(1));
            Assert.That(canceled[0].Reason, Is.EqualTo(PointerCancelReason.FocusLost));

            pointer.Emit(Event(4, PointerPhase.Began, new Vector2(20f, 0f), 4d));
            pointer.Emit(Event(4, PointerPhase.Ended, new Vector2(30f, 0f), 5d));

            Assert.That(completed.Count, Is.EqualTo(1));
            Assert.That(completed[0].StrokeId, Is.EqualTo(2));
            Assert.That(completed[0].CompletionReason, Is.EqualTo(StrokeCompletionReason.PointerEnded));
        }

        [Test]
        public void CollectorPublishesAnAutomaticallyClippedStrokeOnlyOnce()
        {
            var pointer = new FakePointerInput();
            using var collector = new StrokeInputCollector(
                pointer,
                new StrokeSamplingSettings(1f, 10f, 10));
            var completed = new List<StrokeData>();
            collector.StrokeCompleted += completed.Add;

            pointer.Emit(Event(8, PointerPhase.Began, Vector2.zero, 1d));
            pointer.Emit(Event(8, PointerPhase.Moved, new Vector2(20f, 0f), 2d));
            pointer.Emit(Event(8, PointerPhase.Moved, new Vector2(30f, 0f), 3d));
            pointer.Emit(Event(8, PointerPhase.Ended, new Vector2(40f, 0f), 4d));

            Assert.That(completed.Count, Is.EqualTo(1));
            Assert.That(completed[0].CompletionReason, Is.EqualTo(StrokeCompletionReason.MaximumLength));
            Assert.That(completed[0].TotalLengthReferencePixels, Is.EqualTo(10f).Within(0.0001f));
            AssertVector(completed[0].Points[1], new Vector2(10f, 0f));
        }

        [Test]
        public void CollectorPublishesAcceptedPreviewPointsBeforeCompletion()
        {
            var pointer = new FakePointerInput();
            using var collector = new StrokeInputCollector(
                pointer,
                new StrokeSamplingSettings(5f, 100f, 10));
            var started = new List<StrokePreviewPointEvent>();
            var added = new List<StrokePreviewPointEvent>();
            var completed = new List<StrokeData>();
            collector.StrokeStarted += started.Add;
            collector.StrokePointAdded += added.Add;
            collector.StrokeCompleted += completed.Add;

            pointer.Emit(Event(9, PointerPhase.Began, Vector2.zero, 1d));
            pointer.Emit(Event(9, PointerPhase.Moved, new Vector2(2f, 0f), 2d));
            pointer.Emit(Event(9, PointerPhase.Moved, new Vector2(10f, 0f), 3d));

            Assert.That(started.Count, Is.EqualTo(1));
            Assert.That(started[0].StrokeId, Is.EqualTo(1));
            AssertVector(started[0].ReferencePosition, Vector2.zero);
            Assert.That(added.Count, Is.EqualTo(1));
            AssertVector(added[0].ReferencePosition, new Vector2(10f, 0f));
            Assert.That(completed, Is.Empty);

            pointer.Emit(Event(9, PointerPhase.Ended, new Vector2(20f, 0f), 4d));

            Assert.That(added.Count, Is.EqualTo(2));
            AssertVector(added[1].ReferencePosition, new Vector2(20f, 0f));
            Assert.That(completed.Count, Is.EqualTo(1));
            Assert.That(completed[0].PointCount, Is.EqualTo(3));
        }

        [Test]
        public void CollectorPublishesStationaryHoldProgressUntilFirstAcceptedMovement()
        {
            var pointer = new FakePointerInput();
            using var collector = new StrokeInputCollector(
                pointer,
                new StrokeSamplingSettings(5f, 100f, 10));
            var progress = new List<StrokeHoldProgressEvent>();
            collector.StrokeHoldProgressed += progress.Add;

            pointer.Emit(Event(12, PointerPhase.Began, new Vector2(30f, 40f), 1d));
            Assert.That(collector.Advance(1.2d), Is.True);
            pointer.Emit(Event(12, PointerPhase.Moved, new Vector2(32f, 40f), 1.3d));
            Assert.That(collector.Advance(1.5d), Is.True);

            Assert.That(progress.Count, Is.EqualTo(2));
            Assert.That(progress[0].StrokeId, Is.EqualTo(1));
            AssertVector(progress[0].ReferencePosition, new Vector2(30f, 40f));
            Assert.That(progress[0].ElapsedSeconds, Is.EqualTo(0.2d).Within(0.000001d));
            Assert.That(progress[1].ElapsedSeconds, Is.EqualTo(0.5d).Within(0.000001d));

            pointer.Emit(Event(12, PointerPhase.Moved, new Vector2(40f, 40f), 1.6d));
            Assert.That(collector.Advance(2d), Is.False);
            Assert.That(progress.Count, Is.EqualTo(2));

            pointer.Emit(Event(12, PointerPhase.Ended, new Vector2(60f, 40f), 2.1d));
            Assert.That(collector.Advance(2.2d), Is.False);
        }

        [Test]
        public void InvalidSamplingLimitsAreRejectedBeforeCapture()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StrokeSamplingSettings(0f, 100f, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StrokeSamplingSettings(1f, float.PositiveInfinity, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StrokeSamplingSettings(1f, 100f, 1));
        }

        [Test]
        public void AcceptedPointHotPathDoesNotAllocateManagedMemory()
        {
            var sampler = new StrokeSampler(new StrokeSamplingSettings(1f, 100000f, 256));
            sampler.Begin(1, Vector2.zero, 0d);
            for (int index = 1; index <= 10; index++)
            {
                sampler.AddPoint(new Vector2(index * 2f, 0f), index);
            }

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 11; index <= 110; index++)
            {
                sampler.AddPoint(new Vector2(index * 2f, 0f), index);
            }

            long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(allocatedAfter - allocatedBefore, Is.Zero);
        }

        private static PointerInputEvent Event(
            int pointerId,
            PointerPhase phase,
            Vector2 referencePosition,
            double timestamp,
            PointerCancelReason reason = PointerCancelReason.None)
        {
            return new PointerInputEvent(
                pointerId,
                PointerSource.Touch,
                phase,
                referencePosition,
                referencePosition,
                timestamp,
                reason);
        }

        private static void AssertVector(Vector2 actual, Vector2 expected)
        {
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(0.0001f));
        }

        private sealed class FakePointerInput : IPointerInput
        {
            public event Action<PointerInputEvent> PointerChanged;

            public bool IsPointerActive { get; private set; }

            public int? ActivePointerId { get; private set; }

            public PointerSource? ActiveSource { get; private set; }

            public void Cancel(PointerCancelReason reason)
            {
                if (!IsPointerActive)
                {
                    return;
                }

                Emit(Event(
                    ActivePointerId.Value,
                    PointerPhase.Canceled,
                    Vector2.zero,
                    0d,
                    reason));
            }

            public void Emit(PointerInputEvent pointerEvent)
            {
                if (pointerEvent.Phase == PointerPhase.Began)
                {
                    IsPointerActive = true;
                    ActivePointerId = pointerEvent.PointerId;
                    ActiveSource = pointerEvent.Source;
                }
                else if (pointerEvent.IsTerminal)
                {
                    IsPointerActive = false;
                    ActivePointerId = null;
                    ActiveSource = null;
                }

                PointerChanged?.Invoke(pointerEvent);
            }
        }
    }
}
