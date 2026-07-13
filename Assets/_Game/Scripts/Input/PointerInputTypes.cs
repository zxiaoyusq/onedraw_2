using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    public enum PointerSource
    {
        Mouse,
        Touch
    }

    public enum PointerPhase
    {
        Began,
        Moved,
        Ended,
        Canceled
    }

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

    public readonly struct PointerInputEvent
    {
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

        public int PointerId { get; }

        public PointerSource Source { get; }

        public PointerPhase Phase { get; }

        public Vector2 ScreenPosition { get; }

        public Vector2 ReferencePosition { get; }

        public double Timestamp { get; }

        public PointerCancelReason CancelReason { get; }

        public bool IsTerminal => Phase == PointerPhase.Ended || Phase == PointerPhase.Canceled;
    }

    public interface IPointerInput
    {
        event Action<PointerInputEvent> PointerChanged;

        bool IsPointerActive { get; }

        int? ActivePointerId { get; }

        PointerSource? ActiveSource { get; }

        void Cancel(PointerCancelReason reason);
    }

    public interface ISafeAreaProvider
    {
        Rect SafeArea { get; }
    }

    public interface IPointerUiBlocker
    {
        bool IsBlocked(Vector2 screenPosition, int pointerId);
    }
}
