using System;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    // 定义 StrokeTrailPoolSettings 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public readonly struct StrokeTrailPoolSettings
    {
        // 初始化 StrokeTrailPoolSettings，并建立表现层所需的引用与初始显示状态。
        public StrokeTrailPoolSettings(
            int capacity,
            int maximumActiveTrailCount,
            int maximumPointCount)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Pool capacity must be positive.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (maximumActiveTrailCount < 1 || maximumActiveTrailCount > capacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumActiveTrailCount),
                    "Maximum active trail count must fit inside the pool.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (maximumPointCount < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumPointCount),
                    "Trails need room for at least two points.");
            }

            Capacity = capacity;
            MaximumActiveTrailCount = maximumActiveTrailCount;
            MaximumPointCount = maximumPointCount;
        }

        public int Capacity { get; }

        public int MaximumActiveTrailCount { get; }

        public int MaximumPointCount { get; }
    }

    // 定义 StrokeTrailStyle 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public readonly struct StrokeTrailStyle
    {
        /// <summary>仅供旧测试与明确调试兜底使用；生产样式必须走完整配置构造。</summary>
        public StrokeTrailStyle(
            string stanceId,
            float widthReferencePixels,
            float lifetimeSeconds,
            int sortingLayerId,
            int sortingOrder)
            : this(
                stanceId,
                "legacy_white",
                widthReferencePixels,
                lifetimeSeconds,
                sortingLayerId,
                sortingOrder,
                Color.white,
                Color.white,
                Color.white,
                1f,
                1f,
                1f,
                Color.clear,
                float.MaxValue,
                1f,
                0f,
                0.1f,
                2)
        {
        }

        // 初始化完整画笔样式；所有生产数值由StrokeTrailStyles与Stances配置提供。
        public StrokeTrailStyle(
            string stanceId,
            string styleId,
            float widthReferencePixels,
            float lifetimeSeconds,
            int sortingLayerId,
            int sortingOrder,
            Color outerColor,
            Color bodyColor,
            Color coreColor,
            float outerWidthMultiplier,
            float bodyWidthMultiplier,
            float coreWidthMultiplier,
            Color branchColor,
            float branchSpacingReferencePixels,
            float branchLengthReferencePixels,
            float branchJitterReferencePixels,
            float branchWidthMultiplier,
            int branchSegmentCount)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException("Stance id is required.", nameof(stanceId));
            }

            if (string.IsNullOrWhiteSpace(styleId))
            {
                throw new ArgumentException("Stroke trail style id is required.", nameof(styleId));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsFinite(widthReferencePixels) || widthReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(widthReferencePixels),
                    "Trail width must be finite and positive.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsFinite(lifetimeSeconds) || lifetimeSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lifetimeSeconds),
                    "Trail lifetime must be finite and positive.");
            }

            ValidatePositive(outerWidthMultiplier, nameof(outerWidthMultiplier));
            ValidatePositive(bodyWidthMultiplier, nameof(bodyWidthMultiplier));
            ValidatePositive(coreWidthMultiplier, nameof(coreWidthMultiplier));
            ValidatePositive(branchSpacingReferencePixels, nameof(branchSpacingReferencePixels));
            ValidatePositive(branchLengthReferencePixels, nameof(branchLengthReferencePixels));
            ValidateNonNegative(branchJitterReferencePixels, nameof(branchJitterReferencePixels));
            ValidatePositive(branchWidthMultiplier, nameof(branchWidthMultiplier));
            if (branchSegmentCount < 2 || branchSegmentCount > 8)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(branchSegmentCount),
                    "Lightning branches support two to eight segments.");
            }

            StanceId = stanceId;
            StyleId = styleId;
            WidthReferencePixels = widthReferencePixels;
            LifetimeSeconds = lifetimeSeconds;
            SortingLayerId = sortingLayerId;
            SortingOrder = sortingOrder;
            OuterColor = outerColor;
            BodyColor = bodyColor;
            CoreColor = coreColor;
            OuterWidthMultiplier = outerWidthMultiplier;
            BodyWidthMultiplier = bodyWidthMultiplier;
            CoreWidthMultiplier = coreWidthMultiplier;
            BranchColor = branchColor;
            BranchSpacingReferencePixels = branchSpacingReferencePixels;
            BranchLengthReferencePixels = branchLengthReferencePixels;
            BranchJitterReferencePixels = branchJitterReferencePixels;
            BranchWidthMultiplier = branchWidthMultiplier;
            BranchSegmentCount = branchSegmentCount;
        }

        public string StanceId { get; }

        public string StyleId { get; }

        public float WidthReferencePixels { get; }

        public float LifetimeSeconds { get; }

        public int SortingLayerId { get; }

        public int SortingOrder { get; }

        public Color OuterColor { get; }

        public Color BodyColor { get; }

        public Color CoreColor { get; }

        public float OuterWidthMultiplier { get; }

        public float BodyWidthMultiplier { get; }

        public float CoreWidthMultiplier { get; }

        public Color BranchColor { get; }

        public float BranchSpacingReferencePixels { get; }

        public float BranchLengthReferencePixels { get; }

        public float BranchJitterReferencePixels { get; }

        public float BranchWidthMultiplier { get; }

        public int BranchSegmentCount { get; }

        // 判断是否 IsFinite 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        // 统一校验必须为有限正数的画笔配置字段。
        private static void ValidatePositive(float value, string parameterName)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Stroke trail style values must be finite and positive.");
            }
        }

        // 统一校验允许为零但不能为负的画笔配置字段。
        private static void ValidateNonNegative(float value, string parameterName)
        {
            if (!IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Stroke trail style values must be finite and non-negative.");
            }
        }
    }
}
