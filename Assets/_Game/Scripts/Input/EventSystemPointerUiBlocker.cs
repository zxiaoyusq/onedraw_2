using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OneStrokeDemon.Input
{
    public sealed class EventSystemPointerUiBlocker : IPointerUiBlocker
    {
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(8);

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
            raycastResults.Clear();
            eventSystem.RaycastAll(pointerEvent, raycastResults);
            return raycastResults.Count > 0;
        }
    }
}
