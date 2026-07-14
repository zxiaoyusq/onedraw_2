using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace OneStrokeDemon.Tests.PlayMode.T300
{
    [Category("PointerInput")]
    public sealed class PointerCancelPlayModeTests : InputTestFixture
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private GameObject disabledSceneUi;

        [SetUp]
        public override void Setup()
        {
            PointerInputRuntime.ResetForTests();
            base.Setup();
        }

        [TearDown]
        public override void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
            if (disabledSceneUi != null)
            {
                disabledSceneUi.SetActive(true);
                disabledSceneUi = null;
            }

            base.TearDown();
        }

        [UnityTest]
        public IEnumerator MouseAndTouchEmitTheSamePointerContract()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Touchscreen touchscreen = InputSystem.AddDevice<Touchscreen>();
            InputSystemPointerAdapter adapter = CreateAdapter(new NeverBlocked());
            var events = new List<PointerInputEvent>();
            adapter.PointerChanged += events.Add;
            var mouseBegin = new Vector2(Screen.width * 0.2f, Screen.height * 0.2f);
            var mouseMove = new Vector2(Screen.width * 0.3f, Screen.height * 0.3f);
            var touchBegin = new Vector2(Screen.width * 0.4f, Screen.height * 0.4f);
            var touchMove = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var touchEnd = new Vector2(Screen.width * 0.6f, Screen.height * 0.6f);

            Set(mouse.position, mouseBegin, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, mouseMove, queueEventOnly: true);
            yield return null;
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            BeginTouch(17, touchBegin, queueEventOnly: true, screen: touchscreen);
            yield return null;
            MoveTouch(17, touchMove, queueEventOnly: true, screen: touchscreen);
            yield return null;
            EndTouch(17, touchEnd, queueEventOnly: true, screen: touchscreen);
            yield return null;

            Assert.That(events.Count, Is.EqualTo(6));
            Assert.That(events[0].Source, Is.EqualTo(PointerSource.Mouse));
            Assert.That(events[0].PointerId, Is.EqualTo(InputSystemPointerAdapter.MousePointerId));
            Assert.That(events[3].Source, Is.EqualTo(PointerSource.Touch));
            Assert.That(events[3].PointerId, Is.EqualTo(17));
            Assert.That(
                events.ConvertAll(pointerEvent => pointerEvent.Phase),
                Is.EqualTo(new[]
                {
                    PointerPhase.Began,
                    PointerPhase.Moved,
                    PointerPhase.Ended,
                    PointerPhase.Began,
                    PointerPhase.Moved,
                    PointerPhase.Ended
                }));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        [UnityTest]
        public IEnumerator UiBeginIsBlockedAndDoesNotBecomeAStrokeAfterLeavingUi()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            CreateBlockingUi();
            InputSystemPointerAdapter adapter = CreateAdapter(new EventSystemPointerUiBlocker());
            var events = new List<PointerInputEvent>();
            adapter.PointerChanged += events.Add;
            Vector2 blockedPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var clearPosition = new Vector2(10f, 10f);

            Set(mouse.position, blockedPosition, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, clearPosition, queueEventOnly: true);
            yield return null;
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(events, Is.Empty, "A pointer that began over UI must never turn into a stroke.");

            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Phase, Is.EqualTo(PointerPhase.Began));
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FocusLossCancelsAnActivePointerExactlyOnce()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystemPointerAdapter adapter = CreateAdapter(new NeverBlocked());
            var events = new List<PointerInputEvent>();
            adapter.PointerChanged += events.Add;

            Set(mouse.position, new Vector2(300f, 400f), queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Assert.That(adapter.IsPointerActive, Is.True);

            adapter.gameObject.SendMessage("OnApplicationFocus", false);
            adapter.gameObject.SendMessage("OnApplicationFocus", false);

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[1].Phase, Is.EqualTo(PointerPhase.Canceled));
            Assert.That(events[1].CancelReason, Is.EqualTo(PointerCancelReason.FocusLost));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        [UnityTest]
        public IEnumerator ApplicationPauseCancelsAnActivePointer()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystemPointerAdapter adapter = CreateAdapter(new NeverBlocked());
            var events = new List<PointerInputEvent>();
            adapter.PointerChanged += events.Add;

            Set(mouse.position, new Vector2(300f, 400f), queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            adapter.gameObject.SendMessage("OnApplicationPause", true);

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[1].Phase, Is.EqualTo(PointerPhase.Canceled));
            Assert.That(events[1].CancelReason, Is.EqualTo(PointerCancelReason.ApplicationPaused));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        [UnityTest]
        public IEnumerator SecondTouchDoesNotReplaceOrExtendTheActiveTouch()
        {
            Touchscreen touchscreen = InputSystem.AddDevice<Touchscreen>();
            InputSystemPointerAdapter adapter = CreateAdapter(new NeverBlocked());
            var events = new List<PointerInputEvent>();
            adapter.PointerChanged += events.Add;
            var firstPosition = new Vector2(Screen.width * 0.3f, Screen.height * 0.3f);
            var secondPosition = new Vector2(Screen.width * 0.7f, Screen.height * 0.7f);

            BeginTouch(21, firstPosition, queueEventOnly: true, screen: touchscreen);
            yield return null;
            BeginTouch(22, secondPosition, queueEventOnly: true, screen: touchscreen);
            yield return null;
            EndTouch(21, firstPosition, queueEventOnly: true, screen: touchscreen);
            yield return null;

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].PointerId, Is.EqualTo(21));
            Assert.That(events[0].Phase, Is.EqualTo(PointerPhase.Began));
            Assert.That(events[1].PointerId, Is.EqualTo(21));
            Assert.That(events[1].Phase, Is.EqualTo(PointerPhase.Ended));
            Assert.That(adapter.IsPointerActive, Is.False);

            EndTouch(22, secondPosition, queueEventOnly: true, screen: touchscreen);
            yield return null;
            Assert.That(events.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RemovingTheActiveDeviceCancelsThePointer()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystemPointerAdapter adapter = CreateAdapter(new NeverBlocked());
            var events = new List<PointerInputEvent>();
            adapter.PointerChanged += events.Add;

            Set(mouse.position, new Vector2(300f, 400f), queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            InputSystem.RemoveDevice(mouse);

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[1].Phase, Is.EqualTo(PointerPhase.Canceled));
            Assert.That(events[1].CancelReason, Is.EqualTo(PointerCancelReason.DeviceDisconnected));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        private InputSystemPointerAdapter CreateAdapter(IPointerUiBlocker blocker)
        {
            var adapterObject = new GameObject("T300 Pointer Adapter");
            createdObjects.Add(adapterObject);
            var adapter = adapterObject.AddComponent<InputSystemPointerAdapter>();
            adapter.Initialize(
                new ReferencePixelConverter(new Vector2(1920f, 1080f)),
                new FixedSafeAreaProvider(new Rect(0f, 0f, Screen.width, Screen.height)),
                blocker);
            return adapter;
        }

        private void CreateBlockingUi()
        {
            if (EventSystem.current != null)
            {
                disabledSceneUi = EventSystem.current.transform.root.gameObject;
                disabledSceneUi.SetActive(false);
            }

            var eventSystemObject = new GameObject("T300 EventSystem");
            createdObjects.Add(eventSystemObject);
            eventSystemObject.AddComponent<EventSystem>();

            var canvasObject = new GameObject("T300 Canvas");
            createdObjects.Add(canvasObject);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();

            var imageObject = new GameObject("Blocking UI", typeof(RectTransform), typeof(Image));
            createdObjects.Add(imageObject);
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rectTransform = (RectTransform)imageObject.transform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(300f, 300f);
            imageObject.GetComponent<Image>().raycastTarget = true;
            Canvas.ForceUpdateCanvases();
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
