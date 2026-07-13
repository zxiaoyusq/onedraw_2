using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    public static class StrokeTrailSettingsFactory
    {
        private const int TechnicalMaximumActiveTrails = 3;
        private const int TechnicalMaximumPointsPerTrail = 96;

        public static StrokeTrailPoolSettings CreatePoolSettings(
            IConfigProvider configProvider,
            string vfxKey)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            VfxCueConfig vfxCue = configProvider.GetVfxCue(vfxKey);
            int capacity = CheckedPositiveInt(vfxCue.PoolPrewarm, nameof(vfxCue.PoolPrewarm));
            IReadOnlyList<StrokeRuleConfig> strokeRules = configProvider.GetStrokeRules();
            if (strokeRules == null || strokeRules.Count == 0)
            {
                throw new ArgumentException(
                    "StrokeRules must contain at least one row.",
                    nameof(configProvider));
            }

            int maximumPointCount = 0;
            for (int index = 0; index < strokeRules.Count; index++)
            {
                StrokeRuleConfig row = strokeRules[index] ?? throw new ArgumentException(
                    $"StrokeRules row at index {index} is null.",
                    nameof(configProvider));
                int rowMaximum = CheckedPositiveInt(row.MaxPointCount, nameof(row.MaxPointCount));
                if (rowMaximum > maximumPointCount)
                {
                    maximumPointCount = rowMaximum;
                }
            }

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

        public static StrokeTrailStyle CreateStyle(
            IConfigProvider configProvider,
            string stanceId,
            string vfxKey)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            StanceConfig stance = configProvider.GetStance(stanceId);
            VfxCueConfig vfxCue = configProvider.GetVfxCue(vfxKey);
            if (vfxCue.LifeSec <= 0f || float.IsNaN(vfxCue.LifeSec) || float.IsInfinity(vfxCue.LifeSec))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vfxKey),
                    $"VFX cue '{vfxKey}' must have a finite positive lifetime.");
            }

            int width = CheckedPositiveInt(stance.StrokeWidthRefPx, nameof(stance.StrokeWidthRefPx));
            if (vfxCue.SortingOrder < int.MinValue || vfxCue.SortingOrder > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vfxKey),
                    $"VFX cue '{vfxKey}' sorting order exceeds the runtime range.");
            }

            if (string.IsNullOrWhiteSpace(vfxCue.SortingLayer))
            {
                throw new ArgumentException(
                    $"VFX cue '{vfxKey}' must name a sorting layer.",
                    nameof(vfxKey));
            }

            return new StrokeTrailStyle(
                stance.StanceId,
                width,
                vfxCue.LifeSec,
                SortingLayer.NameToID(vfxCue.SortingLayer),
                (int)vfxCue.SortingOrder);
        }

        private static int CheckedPositiveInt(long value, string fieldName)
        {
            if (value < 1 || value > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    fieldName,
                    $"Configured value {value} must fit in a positive runtime integer.");
            }

            return (int)value;
        }
    }
}
