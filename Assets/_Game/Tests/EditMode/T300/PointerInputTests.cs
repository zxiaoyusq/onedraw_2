using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Input;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T300
{
    [Category("PointerInput")]
    public sealed class PointerInputTests
    {
        [Test]
        public void ReferencePixelConverterMapsSafeAreaToConfiguredReferencePixels()
        {
            var converter = new ReferencePixelConverter(new Vector2(1920f, 1080f));
            var safeArea = new Rect(120f, 60f, 2160f, 1080f);

            Assert.That(converter.TryScreenToReference(safeArea.min, safeArea, out Vector2 minimum), Is.True);
            AssertVector(minimum, Vector2.zero);
            Assert.That(converter.TryScreenToReference(safeArea.center, safeArea, out Vector2 center), Is.True);
            AssertVector(center, new Vector2(960f, 540f));
            Assert.That(converter.TryScreenToReference(safeArea.max, safeArea, out Vector2 maximum), Is.True);
            AssertVector(maximum, new Vector2(1920f, 1080f));
        }

        [Test]
        public void ReferencePixelConverterRejectsOutsideBeginsAndClampsActiveMotion()
        {
            var converter = new ReferencePixelConverter(new Vector2(1000f, 500f));
            var safeArea = new Rect(100f, 50f, 1000f, 500f);

            Assert.That(
                converter.TryScreenToReference(new Vector2(99f, 300f), safeArea, out _),
                Is.False);
            Assert.That(
                converter.TryScreenToReferenceClamped(
                    new Vector2(-400f, 900f),
                    safeArea,
                    out Vector2 clamped),
                Is.True);
            AssertVector(clamped, new Vector2(0f, 500f));
            Assert.That(
                converter.TryScreenToReference(Vector2.zero, new Rect(0f, 0f, 0f, 500f), out _),
                Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ReferencePixelConverter(new Vector2(0f, 1080f)));
        }

        [Test]
        public void ProcessorBlocksUiOnlyAtBeginAndKeepsAcceptedPointerContinuous()
        {
            var safeArea = new MutableSafeAreaProvider(new Rect(100f, 50f, 1000f, 500f));
            var blocker = new PositionBlocker(position => position.x < 300f);
            var processor = new PointerInputProcessor(
                new ReferencePixelConverter(new Vector2(1000f, 500f)),
                safeArea,
                blocker);
            var events = new List<PointerInputEvent>();
            processor.PointerChanged += events.Add;

            Assert.That(processor.TryBegin(1, PointerSource.Touch, new Vector2(200f, 200f), 1d), Is.False);
            Assert.That(processor.TryBegin(1, PointerSource.Touch, new Vector2(400f, 200f), 2d), Is.True);
            Assert.That(processor.TryMove(1, PointerSource.Touch, new Vector2(150f, 250f), 3d), Is.True);
            Assert.That(processor.TryMove(1, PointerSource.Touch, new Vector2(50f, 900f), 4d), Is.True);
            Assert.That(processor.TryEnd(1, PointerSource.Touch, new Vector2(1200f, 25f), 5d), Is.True);

            Assert.That(events.ConvertAll(pointerEvent => pointerEvent.Phase), Is.EqualTo(new[]
            {
                PointerPhase.Began,
                PointerPhase.Moved,
                PointerPhase.Moved,
                PointerPhase.Ended
            }));
            AssertVector(events[0].ReferencePosition, new Vector2(300f, 150f));
            AssertVector(events[2].ReferencePosition, new Vector2(0f, 500f));
            AssertVector(events[3].ReferencePosition, new Vector2(1000f, 0f));
            Assert.That(processor.IsPointerActive, Is.False);
        }

        [Test]
        public void ProcessorAllowsOnlyOneActivePointer()
        {
            var processor = CreateProcessor();
            var events = new List<PointerInputEvent>();
            processor.PointerChanged += events.Add;

            Assert.That(processor.TryBegin(7, PointerSource.Touch, new Vector2(100f, 100f), 1d), Is.True);
            Assert.That(processor.TryBegin(8, PointerSource.Touch, new Vector2(200f, 200f), 2d), Is.False);
            Assert.That(processor.TryBegin(-1, PointerSource.Mouse, new Vector2(300f, 300f), 3d), Is.False);
            Assert.That(processor.TryMove(8, PointerSource.Touch, new Vector2(400f, 400f), 4d), Is.False);
            Assert.That(processor.ActivePointerId, Is.EqualTo(7));
            Assert.That(events.Count, Is.EqualTo(1));
        }

        [Test]
        public void CancellationIsTerminalExplicitAndIdempotent()
        {
            var processor = CreateProcessor();
            var events = new List<PointerInputEvent>();
            processor.PointerChanged += events.Add;
            processor.TryBegin(InputSystemPointerAdapter.MousePointerId, PointerSource.Mouse, new Vector2(10f, 20f), 1d);

            Assert.That(processor.Cancel(PointerCancelReason.FocusLost, 2d), Is.True);
            Assert.That(processor.Cancel(PointerCancelReason.ApplicationPaused, 3d), Is.False);

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[1].Phase, Is.EqualTo(PointerPhase.Canceled));
            Assert.That(events[1].CancelReason, Is.EqualTo(PointerCancelReason.FocusLost));
            Assert.That(events[1].IsTerminal, Is.True);
            Assert.That(processor.IsPointerActive, Is.False);
            Assert.Throws<ArgumentException>(() => processor.Cancel(PointerCancelReason.None, 4d));
        }

        private static PointerInputProcessor CreateProcessor()
        {
            return new PointerInputProcessor(
                new ReferencePixelConverter(new Vector2(1920f, 1080f)),
                new MutableSafeAreaProvider(new Rect(0f, 0f, 1920f, 1080f)),
                new PositionBlocker(_ => false));
        }

        private static void AssertVector(Vector2 actual, Vector2 expected)
        {
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(0.001f));
        }

        private sealed class MutableSafeAreaProvider : ISafeAreaProvider
        {
            public MutableSafeAreaProvider(Rect safeArea)
            {
                SafeArea = safeArea;
            }

            public Rect SafeArea { get; set; }
        }

        private sealed class PositionBlocker : IPointerUiBlocker
        {
            private readonly Func<Vector2, bool> predicate;

            public PositionBlocker(Func<Vector2, bool> predicate)
            {
                this.predicate = predicate;
            }

            public bool IsBlocked(Vector2 screenPosition, int pointerId)
            {
                return predicate(screenPosition);
            }
        }
    }
}
