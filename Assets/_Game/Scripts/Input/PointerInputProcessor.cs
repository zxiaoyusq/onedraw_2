using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    internal sealed class PointerInputProcessor
    {
        private readonly ReferencePixelConverter converter;
        private readonly ISafeAreaProvider safeAreaProvider;
        private readonly IPointerUiBlocker uiBlocker;
        private bool isActive;
        private int activePointerId;
        private PointerSource activeSource;
        private Vector2 lastScreenPosition;
        private Vector2 lastReferencePosition;

        public PointerInputProcessor(
            ReferencePixelConverter converter,
            ISafeAreaProvider safeAreaProvider,
            IPointerUiBlocker uiBlocker)
        {
            this.converter = converter ?? throw new ArgumentNullException(nameof(converter));
            this.safeAreaProvider = safeAreaProvider ?? throw new ArgumentNullException(nameof(safeAreaProvider));
            this.uiBlocker = uiBlocker ?? throw new ArgumentNullException(nameof(uiBlocker));
        }

        public event Action<PointerInputEvent> PointerChanged;

        public bool IsPointerActive => isActive;

        public int? ActivePointerId => isActive ? activePointerId : null;

        public PointerSource? ActiveSource => isActive ? activeSource : null;

        public bool TryBegin(
            int pointerId,
            PointerSource source,
            Vector2 screenPosition,
            double timestamp)
        {
            if (isActive ||
                !converter.TryScreenToReference(
                    screenPosition,
                    safeAreaProvider.SafeArea,
                    out Vector2 referencePosition) ||
                uiBlocker.IsBlocked(screenPosition, pointerId))
            {
                return false;
            }

            isActive = true;
            activePointerId = pointerId;
            activeSource = source;
            lastScreenPosition = screenPosition;
            lastReferencePosition = referencePosition;
            Publish(PointerPhase.Began, timestamp, PointerCancelReason.None);
            return true;
        }

        public bool TryMove(
            int pointerId,
            PointerSource source,
            Vector2 screenPosition,
            double timestamp)
        {
            if (!Matches(pointerId, source) || screenPosition == lastScreenPosition ||
                !converter.TryScreenToReferenceClamped(
                    screenPosition,
                    safeAreaProvider.SafeArea,
                    out Vector2 referencePosition))
            {
                return false;
            }

            lastScreenPosition = screenPosition;
            lastReferencePosition = referencePosition;
            Publish(PointerPhase.Moved, timestamp, PointerCancelReason.None);
            return true;
        }

        public bool TryEnd(
            int pointerId,
            PointerSource source,
            Vector2 screenPosition,
            double timestamp)
        {
            if (!Matches(pointerId, source))
            {
                return false;
            }

            if (converter.TryScreenToReferenceClamped(
                screenPosition,
                safeAreaProvider.SafeArea,
                out Vector2 referencePosition))
            {
                lastScreenPosition = screenPosition;
                lastReferencePosition = referencePosition;
            }

            PointerInputEvent pointerEvent = CreateEvent(
                PointerPhase.Ended,
                timestamp,
                PointerCancelReason.None);
            ClearActive();
            PointerChanged?.Invoke(pointerEvent);
            return true;
        }

        public bool Cancel(PointerCancelReason reason, double timestamp)
        {
            if (reason == PointerCancelReason.None)
            {
                throw new ArgumentException("A cancellation event requires a non-None reason.", nameof(reason));
            }

            if (!isActive)
            {
                return false;
            }

            PointerInputEvent pointerEvent = CreateEvent(PointerPhase.Canceled, timestamp, reason);
            ClearActive();
            PointerChanged?.Invoke(pointerEvent);
            return true;
        }

        private bool Matches(int pointerId, PointerSource source)
        {
            return isActive && activePointerId == pointerId && activeSource == source;
        }

        private void Publish(
            PointerPhase phase,
            double timestamp,
            PointerCancelReason cancelReason)
        {
            PointerChanged?.Invoke(CreateEvent(phase, timestamp, cancelReason));
        }

        private PointerInputEvent CreateEvent(
            PointerPhase phase,
            double timestamp,
            PointerCancelReason cancelReason)
        {
            return new PointerInputEvent(
                activePointerId,
                activeSource,
                phase,
                lastScreenPosition,
                lastReferencePosition,
                timestamp,
                cancelReason);
        }

        private void ClearActive()
        {
            isActive = false;
            activePointerId = default;
            activeSource = default;
        }
    }
}
