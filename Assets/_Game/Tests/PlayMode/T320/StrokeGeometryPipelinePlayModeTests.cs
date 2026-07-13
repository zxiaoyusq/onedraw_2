using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T320
{
    [Category("StrokeGeometry")]
    public sealed class StrokeGeometryPipelinePlayModeTests : InputTestFixture
    {
        private GameObject adapterObject;

        [TearDown]
        public override void TearDown()
        {
            if (adapterObject != null)
            {
                Object.DestroyImmediate(adapterObject);
            }

            base.TearDown();
        }

        [UnityTest]
        public IEnumerator MouseGestureProducesOneSharedImmutableGeometryResult()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystemPointerAdapter adapter = CreateAdapter();
            using var collector = new StrokeInputCollector(
                adapter,
                new StrokeSamplingSettings(1f, 5000f, 32));
            var processedStrokes = new List<StrokeGeometryData>();
            collector.StrokeCompleted += stroke => processedStrokes.Add(
                StrokeGeometry.Process(stroke, new StrokeGeometrySettings(0f, 16)));
            var begin = new Vector2(Screen.width * 0.1f, Screen.height * 0.2f);
            var firstMove = new Vector2(Screen.width * 0.35f, Screen.height * 0.75f);
            var secondMove = new Vector2(Screen.width * 0.65f, Screen.height * 0.25f);
            var end = new Vector2(Screen.width * 0.9f, Screen.height * 0.8f);

            Set(mouse.position, begin, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, firstMove, queueEventOnly: true);
            yield return null;
            Set(mouse.position, secondMove, queueEventOnly: true);
            yield return null;
            Set(mouse.position, end, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(processedStrokes.Count, Is.EqualTo(1));
            StrokeGeometryData processed = processedStrokes[0];
            Assert.That(processed.StrokeId, Is.EqualTo(1));
            Assert.That(processed.SourcePointCount, Is.EqualTo(4));
            Assert.That(processed.PointCount, Is.InRange(2, 4));
            Assert.That(processed.LengthReferencePixels, Is.GreaterThan(0f));
            Assert.That(
                processed.LengthReferencePixels,
                Is.EqualTo(StrokeGeometry.CalculateLength(processed.Points)).Within(0.0001f));
            Assert.That(processed.BoundsReferencePixels.width, Is.GreaterThan(0f));
            Assert.That(processed.TotalCurvatureRadians, Is.GreaterThan(0f));
            Assert.That(processed.CompletionReason, Is.EqualTo(StrokeCompletionReason.PointerEnded));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        private InputSystemPointerAdapter CreateAdapter()
        {
            adapterObject = new GameObject("T320 Pointer Adapter");
            var adapter = adapterObject.AddComponent<InputSystemPointerAdapter>();
            adapter.Initialize(
                new ReferencePixelConverter(new Vector2(1920f, 1080f)),
                new FixedSafeAreaProvider(new Rect(0f, 0f, Screen.width, Screen.height)),
                new NeverBlocked());
            return adapter;
        }

        private sealed class FixedSafeAreaProvider : ISafeAreaProvider
        {
            public FixedSafeAreaProvider(Rect safeArea)
            {
                SafeArea = safeArea;
            }

            public Rect SafeArea { get; }
        }

        private sealed class NeverBlocked : IPointerUiBlocker
        {
            public bool IsBlocked(Vector2 screenPosition, int pointerId)
            {
                return false;
            }
        }
    }
}
