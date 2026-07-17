using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>指针事件的物理输入来源。</summary>
    public enum PointerSource
    {
        Mouse,
        Touch
    }

    /// <summary>统一指针生命周期阶段。</summary>
    public enum PointerPhase
    {
        Began,
        Moved,
        Ended,
        Canceled
    }

    /// <summary>活动指针被取消的明确原因。</summary>
    public enum PointerCancelReason
    {
        None,
        FocusLost,
        ApplicationPaused,
        AdapterDisabled,
        DeviceDisconnected,
        SystemCanceled,
        RuntimeReset
    }

    /// <summary>同时携带屏幕坐标和参考像素坐标的不可变统一指针事件。</summary>
    public readonly struct PointerInputEvent
    {
        /// <summary>创建一个完整的指针生命周期事件。</summary>
        public PointerInputEvent(
            int pointerId,
            PointerSource source,
            PointerPhase phase,
            Vector2 screenPosition,
            Vector2 referencePosition,
            double timestamp,
            PointerCancelReason cancelReason)
        {
            PointerId = pointerId;
            Source = source;
            Phase = phase;
            ScreenPosition = screenPosition;
            ReferencePosition = referencePosition;
            Timestamp = timestamp;
            CancelReason = cancelReason;
        }

        /// <summary>获取物理指针 ID。</summary>
        public int PointerId { get; }

        /// <summary>获取鼠标或触摸来源。</summary>
        public PointerSource Source { get; }

        /// <summary>获取生命周期阶段。</summary>
        public PointerPhase Phase { get; }

        /// <summary>获取设备屏幕坐标。</summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>获取 Safe Area 内的参考像素坐标。</summary>
        public Vector2 ReferencePosition { get; }

        /// <summary>获取未缩放时间戳。</summary>
        public double Timestamp { get; }

        /// <summary>获取取消原因；非取消事件为 None。</summary>
        public PointerCancelReason CancelReason { get; }

        /// <summary>获取该事件是否结束当前指针生命周期。</summary>
        public bool IsTerminal => Phase == PointerPhase.Ended || Phase == PointerPhase.Canceled;
    }

    /// <summary>统一鼠标与触摸的单活动指针输入端口。</summary>
    public interface IPointerInput
    {
        /// <summary>指针状态发生变化时发布统一事件。</summary>
        event Action<PointerInputEvent> PointerChanged;

        /// <summary>获取当前是否存在活动指针。</summary>
        bool IsPointerActive { get; }

        /// <summary>获取活动指针 ID；没有活动指针时为空。</summary>
        int? ActivePointerId { get; }

        /// <summary>获取活动输入来源；没有活动指针时为空。</summary>
        PointerSource? ActiveSource { get; }

        /// <summary>以指定原因取消当前活动指针。</summary>
        void Cancel(PointerCancelReason reason);
    }

    /// <summary>提供每次转换时动态读取的屏幕安全区域。</summary>
    public interface ISafeAreaProvider
    {
        /// <summary>获取当前屏幕像素坐标中的安全区域。</summary>
        Rect SafeArea { get; }
    }

    /// <summary>判断屏幕起笔位置是否被 UI 阻挡。</summary>
    public interface IPointerUiBlocker
    {
        /// <summary>检查指定物理指针在该屏幕坐标上是否命中 UI。</summary>
        bool IsBlocked(Vector2 screenPosition, int pointerId);
    }
}
