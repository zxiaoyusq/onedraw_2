using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>维护单活动指针所有权，并执行 Safe Area 转换与 UI 起笔门。</summary>
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

        /// <summary>创建使用指定坐标转换、Safe Area 与 UI 阻挡服务的处理器。</summary>
        public PointerInputProcessor(
            ReferencePixelConverter converter,
            ISafeAreaProvider safeAreaProvider,
            IPointerUiBlocker uiBlocker)
        {
            this.converter = converter ?? throw new ArgumentNullException(nameof(converter));
            this.safeAreaProvider = safeAreaProvider ?? throw new ArgumentNullException(nameof(safeAreaProvider));
            this.uiBlocker = uiBlocker ?? throw new ArgumentNullException(nameof(uiBlocker));
        }

        /// <summary>指针生命周期发生变化时发布统一事件。</summary>
        public event Action<PointerInputEvent> PointerChanged;

        /// <summary>获取当前是否存在活动指针。</summary>
        public bool IsPointerActive => isActive;

        /// <summary>获取活动指针 ID。</summary>
        public int? ActivePointerId => isActive ? activePointerId : null;

        /// <summary>获取活动输入来源。</summary>
        public PointerSource? ActiveSource => isActive ? activeSource : null;

        /// <summary>尝试取得指针所有权；Safe Area 外、UI 上或已有活动指针时拒绝。</summary>
        public bool TryBegin(
            int pointerId,
            PointerSource source,
            Vector2 screenPosition,
            double timestamp)
        {
            // UI 只在起笔时检查；合法起笔后的移动可跨过 UI 而保持连续。
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

        /// <summary>更新属于当前所有者的指针，并把越界坐标夹紧到参考空间边缘。</summary>
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

        /// <summary>结束当前所有者；终点无效时仍使用最后一个合法坐标发布终止事件。</summary>
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

            // 先复制终止事件再清状态，确保事件保留原指针 ID、来源与末坐标。
            PointerInputEvent pointerEvent = CreateEvent(
                PointerPhase.Ended,
                timestamp,
                PointerCancelReason.None);
            ClearActive();
            PointerChanged?.Invoke(pointerEvent);
            return true;
        }

        /// <summary>以明确原因取消活动指针；没有活动指针时不重复发布。</summary>
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

        /// <summary>判断事件是否来自当前锁定的同一物理指针。</summary>
        private bool Matches(int pointerId, PointerSource source)
        {
            return isActive && activePointerId == pointerId && activeSource == source;
        }

        /// <summary>使用当前所有权和末坐标发布非终止事件。</summary>
        private void Publish(
            PointerPhase phase,
            double timestamp,
            PointerCancelReason cancelReason)
        {
            PointerChanged?.Invoke(CreateEvent(phase, timestamp, cancelReason));
        }

        /// <summary>根据当前活动状态创建不可变统一指针事件。</summary>
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

        /// <summary>清除活动所有权；末坐标保留但在下一次起笔时会完整覆盖。</summary>
        private void ClearActive()
        {
            isActive = false;
            activePointerId = default;
            activeSource = default;
        }
    }
}
