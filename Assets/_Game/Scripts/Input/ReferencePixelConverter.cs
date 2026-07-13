using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    public sealed class ReferencePixelConverter
    {
        public ReferencePixelConverter(Vector2 referenceResolution)
        {
            if (!IsFinitePositive(referenceResolution.x) || !IsFinitePositive(referenceResolution.y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(referenceResolution),
                    referenceResolution,
                    "Reference resolution components must be finite and greater than zero.");
            }

            ReferenceResolution = referenceResolution;
        }

        public Vector2 ReferenceResolution { get; }

        public bool TryScreenToReference(
            Vector2 screenPosition,
            Rect safeArea,
            out Vector2 referencePosition)
        {
            referencePosition = default;
            if (!IsUsable(safeArea) || !IsFinite(screenPosition.x) || !IsFinite(screenPosition.y) ||
                !ContainsInclusive(safeArea, screenPosition))
            {
                return false;
            }

            referencePosition = Convert(screenPosition, safeArea);
            return true;
        }

        public bool TryScreenToReferenceClamped(
            Vector2 screenPosition,
            Rect safeArea,
            out Vector2 referencePosition)
        {
            referencePosition = default;
            if (!IsUsable(safeArea) || !IsFinite(screenPosition.x) || !IsFinite(screenPosition.y))
            {
                return false;
            }

            var clamped = new Vector2(
                Mathf.Clamp(screenPosition.x, safeArea.xMin, safeArea.xMax),
                Mathf.Clamp(screenPosition.y, safeArea.yMin, safeArea.yMax));
            referencePosition = Convert(clamped, safeArea);
            return true;
        }

        private Vector2 Convert(Vector2 screenPosition, Rect safeArea)
        {
            return new Vector2(
                (screenPosition.x - safeArea.xMin) / safeArea.width * ReferenceResolution.x,
                (screenPosition.y - safeArea.yMin) / safeArea.height * ReferenceResolution.y);
        }

        private static bool ContainsInclusive(Rect area, Vector2 position)
        {
            return position.x >= area.xMin && position.x <= area.xMax &&
                position.y >= area.yMin && position.y <= area.yMax;
        }

        private static bool IsUsable(Rect area)
        {
            return IsFinite(area.x) && IsFinite(area.y) &&
                IsFinitePositive(area.width) && IsFinitePositive(area.height);
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && IsFinite(value);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class ScreenSafeAreaProvider : ISafeAreaProvider
    {
        public Rect SafeArea => Screen.safeArea;
    }
}
