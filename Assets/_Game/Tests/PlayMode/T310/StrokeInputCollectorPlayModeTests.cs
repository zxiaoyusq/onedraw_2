using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T310
{
    [Category("StrokeSampling")]
    public sealed class StrokeInputCollectorPlayModeTests : InputTestFixture
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
        public IEnumerator MouseDragFlowsThroughPointerAdapterIntoOneImmutableStroke()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystemPointerAdapter adapter = CreateAdapter();
            using var collector = new StrokeInputCollector(
                adapter,
                new StrokeSamplingSettings(1f, 5000f, 16));
            var completed = new List<StrokeData>();
            collector.StrokeCompleted += completed.Add;
            var begin = new Vector2(Screen.width * 0.1f, Screen.height * 0.2f);
            var move = new Vector2(Screen.width * 0.4f, Screen.height * 0.5f);
            var end = new Vector2(Screen.width * 0.7f, Screen.height * 0.8f);

            Set(mouse.position, begin, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, move, queueEventOnly: true);
            yield return null;
            Set(mouse.position, end, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(completed.Count, Is.EqualTo(1));
            Assert.That(completed[0].StrokeId, Is.EqualTo(1));
            Assert.That(completed[0].PointCount, Is.EqualTo(3));
            Assert.That(completed[0].TotalLengthReferencePixels, Is.GreaterThan(0f));
            Assert.That(completed[0].CompletionReason, Is.EqualTo(StrokeCompletionReason.PointerEnded));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        private InputSystemPointerAdapter CreateAdapter()
        {
            adapterObject = new GameObject("T310 Pointer Adapter");
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
