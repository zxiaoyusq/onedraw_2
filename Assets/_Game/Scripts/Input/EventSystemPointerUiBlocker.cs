using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OneStrokeDemon.Input
{
    /// <summary>通过当前 uGUI EventSystem 判断起笔位置是否被可射线检测的 UI 阻挡。</summary>
    public sealed class EventSystemPointerUiBlocker : IPointerUiBlocker
    {
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(8);

        /// <summary>对指定屏幕坐标执行一次 UI 射线检测；没有 EventSystem 时视为未阻挡。</summary>
        public bool IsBlocked(Vector2 screenPosition, int pointerId)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            var pointerEvent = new PointerEventData(eventSystem)
            {
                pointerId = pointerId,
                position = screenPosition
            };
            // 复用结果列表，避免每次起笔为 RaycastAll 分配新集合。
            raycastResults.Clear();
            eventSystem.RaycastAll(pointerEvent, raycastResults);
            return raycastResults.Count > 0;
        }
    }
}
