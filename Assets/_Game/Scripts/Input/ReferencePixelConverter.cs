using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>把动态 Safe Area 中的屏幕坐标映射到固定参考像素空间。</summary>
    public sealed class ReferencePixelConverter
    {
        /// <summary>创建使用指定正有限参考分辨率的坐标转换器。</summary>
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

        /// <summary>获取目标参考分辨率。</summary>
        public Vector2 ReferenceResolution { get; }

        /// <summary>仅当点位于有效 Safe Area 内时转换坐标，供起笔门使用。</summary>
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

        /// <summary>把有效屏幕点夹紧到 Safe Area 后转换，供合法笔迹越界移动和结束使用。</summary>
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

        /// <summary>按 Safe Area 内归一化位置缩放到参考像素坐标。</summary>
        private Vector2 Convert(Vector2 screenPosition, Rect safeArea)
        {
            return new Vector2(
                (screenPosition.x - safeArea.xMin) / safeArea.width * ReferenceResolution.x,
                (screenPosition.y - safeArea.yMin) / safeArea.height * ReferenceResolution.y);
        }

        /// <summary>使用含边界语义判断点是否位于矩形内。</summary>
        private static bool ContainsInclusive(Rect area, Vector2 position)
        {
            return position.x >= area.xMin && position.x <= area.xMax &&
                position.y >= area.yMin && position.y <= area.yMax;
        }

        /// <summary>判断 Safe Area 的位置有限且宽高为正。</summary>
        private static bool IsUsable(Rect area)
        {
            return IsFinite(area.x) && IsFinite(area.y) &&
                IsFinitePositive(area.width) && IsFinitePositive(area.height);
        }

        /// <summary>判断值有限且大于零。</summary>
        private static bool IsFinitePositive(float value)
        {
            return value > 0f && IsFinite(value);
        }

        /// <summary>判断浮点数不是 NaN 或无穷。</summary>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>从 Unity Screen API 实时提供当前设备 Safe Area。</summary>
    public sealed class ScreenSafeAreaProvider : ISafeAreaProvider
    {
        /// <summary>获取当前帧的屏幕安全区域，不缓存设备绝对边距。</summary>
        public Rect SafeArea => Screen.safeArea;
    }
}
