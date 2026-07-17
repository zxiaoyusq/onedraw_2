using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OneStrokeDemon.Input
{
    /// <summary>轮询 Unity Input System 的鼠标与主触摸，并转发为单活动指针事件。</summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class InputSystemPointerAdapter : MonoBehaviour, IPointerInput
    {
        /// <summary>统一鼠标左键使用的固定指针 ID。</summary>
        public const int MousePointerId = -1;

        private PointerInputProcessor processor;
        private InputDevice activeDevice;
        private TouchControl activeTouch;
        private bool subscribedToDeviceChanges;

        /// <summary>统一指针状态变化事件。</summary>
        public event Action<PointerInputEvent> PointerChanged;

        /// <summary>获取处理器当前是否锁定活动指针。</summary>
        public bool IsPointerActive => processor != null && processor.IsPointerActive;

        /// <summary>获取活动物理指针 ID。</summary>
        public int? ActivePointerId => processor?.ActivePointerId;

        /// <summary>获取活动输入来源。</summary>
        public PointerSource? ActiveSource => processor?.ActiveSource;

        /// <summary>获取初始化时绑定的参考分辨率。</summary>
        public Vector2 ReferenceResolution => processor == null
            ? throw new InvalidOperationException("Pointer adapter has not been initialized.")
            : converter.ReferenceResolution;

        private ReferencePixelConverter converter { get; set; }

        /// <summary>注入坐标、Safe Area 与 UI 起笔门，并开始监听设备变化。</summary>
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

        /// <summary>以未缩放时间取消活动指针，并清除物理设备所有权。</summary>
        public void Cancel(PointerCancelReason reason)
        {
            if (processor != null && processor.Cancel(reason, Time.unscaledTimeAsDouble))
            {
                activeDevice = null;
                activeTouch = null;
            }
        }

        /// <summary>组件启用时恢复设备变化订阅。</summary>
        private void OnEnable()
        {
            SubscribeToDeviceChanges();
        }

        /// <summary>组件禁用时先取消活动笔迹，再解除设备变化订阅。</summary>
        private void OnDisable()
        {
            Cancel(PointerCancelReason.AdapterDisabled);
            UnsubscribeFromDeviceChanges();
        }

        /// <summary>销毁时解除所有订阅，并通知全局运行时释放当前适配器。</summary>
        private void OnDestroy()
        {
            UnsubscribeFromDeviceChanges();
            if (processor != null)
            {
                processor.PointerChanged -= ForwardPointerEvent;
            }

            PointerInputRuntime.Release(this);
        }

        /// <summary>应用失焦时取消活动指针，避免返回后续接旧笔迹。</summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Cancel(PointerCancelReason.FocusLost);
            }
        }

        /// <summary>应用暂停时取消活动指针。</summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Cancel(PointerCancelReason.ApplicationPaused);
            }
        }

        /// <summary>每帧优先检测新触摸；存在活动指针时只更新其原始来源。</summary>
        private void Update()
        {
            if (processor == null)
            {
                return;
            }

            // MVP 同时只允许一个活动指针，空闲时触摸优先于鼠标。
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

        /// <summary>尝试从当前触摸屏主触点开始一笔。</summary>
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
                // 记录实际触点而非持续读取 primaryTouch，防止多指切换所有权。
                activeDevice = touchscreen;
                activeTouch = FindTouch(touchscreen, touch.touchId.ReadValue());
            }

            return began;
        }

        /// <summary>尝试从当前鼠标左键开始一笔。</summary>
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

        /// <summary>更新锁定触点的移动、结束或系统取消状态。</summary>
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

        /// <summary>更新锁定鼠标的移动或结束状态。</summary>
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

        /// <summary>活动设备被移除或断开时取消当前指针。</summary>
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!ReferenceEquals(device, activeDevice) ||
                (change != InputDeviceChange.Removed && change != InputDeviceChange.Disconnected))
            {
                return;
            }

            Cancel(PointerCancelReason.DeviceDisconnected);
        }

        /// <summary>按 touchId 在触摸屏所有触点中找到稳定的触点控制对象。</summary>
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

        /// <summary>在组件可用且尚未订阅时监听 Input System 设备变化。</summary>
        private void SubscribeToDeviceChanges()
        {
            if (subscribedToDeviceChanges || !isActiveAndEnabled)
            {
                return;
            }

            InputSystem.onDeviceChange += OnDeviceChange;
            subscribedToDeviceChanges = true;
        }

        /// <summary>幂等解除设备变化订阅。</summary>
        private void UnsubscribeFromDeviceChanges()
        {
            if (!subscribedToDeviceChanges)
            {
                return;
            }

            InputSystem.onDeviceChange -= OnDeviceChange;
            subscribedToDeviceChanges = false;
        }

        /// <summary>把纯处理器事件转发给运行时消费者。</summary>
        private void ForwardPointerEvent(PointerInputEvent pointerEvent)
        {
            PointerChanged?.Invoke(pointerEvent);
        }
    }
}
