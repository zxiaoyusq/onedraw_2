using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    // 定义 StrokeTrailSettingsFactory 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public static class StrokeTrailSettingsFactory
    {
        private const int TechnicalMaximumActiveTrails = 3;
        private const int TechnicalMaximumPointsPerTrail = 96;

        // 创建 CreatePoolSettings 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public static StrokeTrailPoolSettings CreatePoolSettings(
            IConfigProvider configProvider,
            string vfxKey)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            VfxCueConfig vfxCue = configProvider.GetVfxCue(vfxKey);
            int capacity = CheckedPositiveInt(vfxCue.PoolPrewarm, nameof(vfxCue.PoolPrewarm));
            IReadOnlyList<StrokeRuleConfig> strokeRules = configProvider.GetStrokeRules();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (strokeRules == null || strokeRules.Count == 0)
            {
                throw new ArgumentException(
                    "StrokeRules must contain at least one row.",
                    nameof(configProvider));
            }

            int maximumPointCount = 0;
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < strokeRules.Count; index++)
            {
                StrokeRuleConfig row = strokeRules[index] ?? throw new ArgumentException(
                    $"StrokeRules row at index {index} is null.",
                    nameof(configProvider));
                int rowMaximum = CheckedPositiveInt(row.MaxPointCount, nameof(row.MaxPointCount));
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (rowMaximum > maximumPointCount)
                {
                    maximumPointCount = rowMaximum;
                }
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (maximumPointCount > TechnicalMaximumPointsPerTrail)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configProvider),
                    $"Configured stroke point count {maximumPointCount} exceeds the technical limit {TechnicalMaximumPointsPerTrail}.");
            }

            return new StrokeTrailPoolSettings(
                capacity,
                Math.Min(capacity, TechnicalMaximumActiveTrails),
                maximumPointCount);
        }

        // 创建 CreateStyle 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public static StrokeTrailStyle CreateStyle(
            IConfigProvider configProvider,
            string stanceId,
            string vfxKey)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            StanceConfig stance = configProvider.GetStance(stanceId);
            VfxCueConfig vfxCue = configProvider.GetVfxCue(vfxKey);
            StrokeTrailStyleConfig style = configProvider.GetStrokeTrailStyle(
                stance.StrokeTrailStyleId);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (vfxCue.LifeSec <= 0f || float.IsNaN(vfxCue.LifeSec) || float.IsInfinity(vfxCue.LifeSec))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vfxKey),
                    $"VFX cue '{vfxKey}' must have a finite positive lifetime.");
            }

            int width = CheckedPositiveInt(stance.StrokeWidthRefPx, nameof(stance.StrokeWidthRefPx));
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (vfxCue.SortingOrder < int.MinValue || vfxCue.SortingOrder > int.MaxValue - 3L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vfxKey),
                    $"VFX cue '{vfxKey}' sorting order exceeds the runtime range.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(vfxCue.SortingLayer))
            {
                throw new ArgumentException(
                    $"VFX cue '{vfxKey}' must name a sorting layer.",
                    nameof(vfxKey));
            }

            return new StrokeTrailStyle(
                stance.StanceId,
                style.StyleId,
                width,
                vfxCue.LifeSec,
                SortingLayer.NameToID(vfxCue.SortingLayer),
                (int)vfxCue.SortingOrder,
                ParseColor(style.OuterColorHex, nameof(style.OuterColorHex)),
                ParseColor(style.BodyColorHex, nameof(style.BodyColorHex)),
                ParseColor(style.CoreColorHex, nameof(style.CoreColorHex)),
                CheckedPositive(style.OuterWidthMultiplier, nameof(style.OuterWidthMultiplier)),
                CheckedPositive(style.BodyWidthMultiplier, nameof(style.BodyWidthMultiplier)),
                CheckedPositive(style.CoreWidthMultiplier, nameof(style.CoreWidthMultiplier)),
                ParseColor(style.BranchColorHex, nameof(style.BranchColorHex)),
                CheckedPositive(style.BranchSpacingRefPx, nameof(style.BranchSpacingRefPx)),
                CheckedPositive(style.BranchLengthRefPx, nameof(style.BranchLengthRefPx)),
                CheckedNonNegative(style.BranchJitterRefPx, nameof(style.BranchJitterRefPx)),
                CheckedPositive(style.BranchWidthMultiplier, nameof(style.BranchWidthMultiplier)),
                CheckedPositiveInt(style.BranchSegmentCount, nameof(style.BranchSegmentCount)));
        }

        // 处理 CheckedPositiveInt 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static int CheckedPositiveInt(long value, string fieldName)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (value < 1 || value > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    fieldName,
                    $"Configured value {value} must fit in a positive runtime integer.");
            }

            return (int)value;
        }

        // 将已通过导出器校验的#RRGGBBAA颜色转换为Unity颜色，并保留运行时防御。
        private static Color ParseColor(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !ColorUtility.TryParseHtmlString(value, out Color color))
            {
                throw new ArgumentException(
                    $"Configured color '{value}' is invalid.",
                    fieldName);
            }

            return color;
        }

        // 把配置浮点值收紧为有限正数。
        private static float CheckedPositive(float value, string fieldName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    fieldName,
                    $"Configured value {value} must be finite and positive.");
            }

            return value;
        }

        // 把配置浮点值收紧为有限非负数。
        private static float CheckedNonNegative(float value, string fieldName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    fieldName,
                    $"Configured value {value} must be finite and non-negative.");
            }

            return value;
        }
    }
}
