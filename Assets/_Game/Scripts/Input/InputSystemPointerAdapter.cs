using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OneStrokeDemon.Input
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class InputSystemPointerAdapter : MonoBehaviour, IPointerInput
    {
        public const int MousePointerId = -1;

        private PointerInputProcessor processor;
        private InputDevice activeDevice;
        private TouchControl activeTouch;
        private bool subscribedToDeviceChanges;

        public event Action<PointerInputEvent> PointerChanged;

        public bool IsPointerActive => processor != null && processor.IsPointerActive;

        public int? ActivePointerId => processor?.ActivePointerId;

        public PointerSource? ActiveSource => processor?.ActiveSource;

        public Vector2 ReferenceResolution => processor == null
            ? throw new InvalidOperationException("Pointer adapter has not been initialized.")
            : converter.ReferenceResolution;

        private ReferencePixelConverter converter { get; set; }

        public void Initialize(
            ReferencePixelConverter referencePixelConverter,
            ISafeAreaProvider safeAreaProvider,
            IPointerUiBlocker uiBlocker)
        {
            if (processor != null)
            {
                throw new InvalidOperationException("Pointer adapter was already initialized.");
            }

            converter = referencePixelConverter ?? throw new ArgumentNullException(nameof(referencePixelConverter));
            processor = new PointerInputProcessor(converter, safeAreaProvider, uiBlocker);
            processor.PointerChanged += ForwardPointerEvent;
            SubscribeToDeviceChanges();
        }

        public void Cancel(PointerCancelReason reason)
        {
            if (processor != null && processor.Cancel(reason, Time.unscaledTimeAsDouble))
            {
                activeDevice = null;
                activeTouch = null;
            }
        }

        private void OnEnable()
        {
            SubscribeToDeviceChanges();
        }

        private void OnDisable()
        {
            Cancel(PointerCancelReason.AdapterDisabled);
            UnsubscribeFromDeviceChanges();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDeviceChanges();
            if (processor != null)
            {
                processor.PointerChanged -= ForwardPointerEvent;
            }

            PointerInputRuntime.Release(this);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Cancel(PointerCancelReason.FocusLost);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Cancel(PointerCancelReason.ApplicationPaused);
            }
        }

        private void Update()
        {
            if (processor == null)
            {
                return;
            }

            if (!processor.IsPointerActive)
            {
                if (!TryBeginTouch())
                {
                    TryBeginMouse();
                }

                return;
            }

            switch (processor.ActiveSource)
            {
                case PointerSource.Touch:
                    UpdateActiveTouch();
                    break;
                case PointerSource.Mouse:
                    UpdateActiveMouse();
                    break;
            }
        }

        private bool TryBeginTouch()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            TouchControl touch = touchscreen.primaryTouch;
            if (touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Began)
            {
                return false;
            }

            bool began = processor.TryBegin(
                touch.touchId.ReadValue(),
                PointerSource.Touch,
                touch.position.ReadValue(),
                Time.unscaledTimeAsDouble);
            if (began)
            {
                activeDevice = touchscreen;
                activeTouch = FindTouch(touchscreen, touch.touchId.ReadValue());
            }

            return began;
        }

        private bool TryBeginMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return false;
            }

            bool began = processor.TryBegin(
                MousePointerId,
                PointerSource.Mouse,
                mouse.position.ReadValue(),
                Time.unscaledTimeAsDouble);
            if (began)
            {
                activeDevice = mouse;
                activeTouch = null;
            }

            return began;
        }

        private void UpdateActiveTouch()
        {
            var touchscreen = activeDevice as Touchscreen;
            if (touchscreen == null || !touchscreen.added)
            {
                Cancel(PointerCancelReason.DeviceDisconnected);
                return;
            }

            TouchControl touch = activeTouch;
            if (touch == null)
            {
                Cancel(PointerCancelReason.SystemCanceled);
                return;
            }

            int pointerId = processor.ActivePointerId.GetValueOrDefault();
            Vector2 position = touch.position.ReadValue();
            UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
            {
                processor.TryEnd(pointerId, PointerSource.Touch, position, Time.unscaledTimeAsDouble);
                activeDevice = null;
                activeTouch = null;
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                Cancel(PointerCancelReason.SystemCanceled);
            }
            else if (touch.press.isPressed)
            {
                processor.TryMove(pointerId, PointerSource.Touch, position, Time.unscaledTimeAsDouble);
            }
        }

        private void UpdateActiveMouse()
        {
            var mouse = activeDevice as Mouse;
            if (mouse == null || !mouse.added)
            {
                Cancel(PointerCancelReason.DeviceDisconnected);
                return;
            }

            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                processor.TryEnd(MousePointerId, PointerSource.Mouse, position, Time.unscaledTimeAsDouble);
                activeDevice = null;
            }
            else if (mouse.leftButton.isPressed)
            {
                processor.TryMove(MousePointerId, PointerSource.Mouse, position, Time.unscaledTimeAsDouble);
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!ReferenceEquals(device, activeDevice) ||
                (change != InputDeviceChange.Removed && change != InputDeviceChange.Disconnected))
            {
                return;
            }

            Cancel(PointerCancelReason.DeviceDisconnected);
        }

        private static TouchControl FindTouch(Touchscreen touchscreen, int pointerId)
        {
            for (int index = 0; index < touchscreen.touches.Count; index++)
            {
                TouchControl touch = touchscreen.touches[index];
                if (touch.touchId.ReadValue() == pointerId)
                {
                    return touch;
                }
            }

            return null;
        }

        private void SubscribeToDeviceChanges()
        {
            if (subscribedToDeviceChanges || !isActiveAndEnabled)
            {
                return;
            }

            InputSystem.onDeviceChange += OnDeviceChange;
            subscribedToDeviceChanges = true;
        }

        private void UnsubscribeFromDeviceChanges()
        {
            if (!subscribedToDeviceChanges)
            {
                return;
            }

            InputSystem.onDeviceChange -= OnDeviceChange;
            subscribedToDeviceChanges = false;
        }

        private void ForwardPointerEvent(PointerInputEvent pointerEvent)
        {
            PointerChanged?.Invoke(pointerEvent);
        }
    }
}
