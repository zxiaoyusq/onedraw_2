using System;

namespace OneStrokeDemon.Input
{
    /// <summary>保存一条不可变的笔势识别阈值规则。</summary>
    public sealed class GestureRule
    {
        /// <summary>创建并验证一条配置映射后的笔势规则。</summary>
        public GestureRule(
            string ruleId,
            GestureType gestureType,
            float minimumLengthReferencePixels,
            float directionToleranceDegrees,
            float closeDistanceReferencePixels,
            float minimumAreaReferencePixelsSquared,
            float minimumNormalizedCurvature,
            double chargeHoldSeconds)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException("Gesture rule IDs must not be empty.", nameof(ruleId));
            }

            if (!IsSupported(gestureType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gestureType),
                    $"Unsupported gesture type '{gestureType}'.");
            }

            ValidateFiniteNonNegative(minimumLengthReferencePixels, nameof(minimumLengthReferencePixels));
            ValidateFiniteNonNegative(directionToleranceDegrees, nameof(directionToleranceDegrees));
            if (directionToleranceDegrees > 90f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(directionToleranceDegrees),
                    "Direction tolerance cannot exceed 90 degrees.");
            }

            ValidateFiniteNonNegative(closeDistanceReferencePixels, nameof(closeDistanceReferencePixels));
            ValidateFiniteNonNegative(
                minimumAreaReferencePixelsSquared,
                nameof(minimumAreaReferencePixelsSquared));
            ValidateFiniteNonNegative(minimumNormalizedCurvature, nameof(minimumNormalizedCurvature));
            if (double.IsNaN(chargeHoldSeconds) ||
                double.IsInfinity(chargeHoldSeconds) ||
                chargeHoldSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chargeHoldSeconds),
                    "Charge hold duration must be finite and non-negative.");
            }

            RuleId = ruleId;
            GestureType = gestureType;
            MinimumLengthReferencePixels = minimumLengthReferencePixels;
            DirectionToleranceDegrees = directionToleranceDegrees;
            CloseDistanceReferencePixels = closeDistanceReferencePixels;
            MinimumAreaReferencePixelsSquared = minimumAreaReferencePixelsSquared;
            MinimumNormalizedCurvature = minimumNormalizedCurvature;
            ChargeHoldSeconds = chargeHoldSeconds;
        }

        /// <summary>获取稳定规则 ID。</summary>
        public string RuleId { get; }

        /// <summary>获取该规则要识别的笔势类型。</summary>
        public GestureType GestureType { get; }

        /// <summary>获取最低笔迹长度，单位为参考像素。</summary>
        public float MinimumLengthReferencePixels { get; }

        /// <summary>获取方向允许偏差，单位为度。</summary>
        public float DirectionToleranceDegrees { get; }

        /// <summary>获取圆形允许的最大首尾距离，单位为参考像素。</summary>
        public float CloseDistanceReferencePixels { get; }

        /// <summary>获取圆形要求的最小面积，单位为参考像素平方。</summary>
        public float MinimumAreaReferencePixelsSquared { get; }

        /// <summary>获取弧线或圆形要求的最小归一化曲率。</summary>
        public float MinimumNormalizedCurvature { get; }

        /// <summary>获取蓄力笔势要求的起笔停留秒数。</summary>
        public double ChargeHoldSeconds { get; }

        /// <summary>判断类型是否属于 MVP 显式支持的笔势集合。</summary>
        private static bool IsSupported(GestureType gestureType)
        {
            return gestureType == GestureType.Any ||
                   gestureType == GestureType.Horizontal ||
                   gestureType == GestureType.Vertical ||
                   gestureType == GestureType.Diagonal ||
                   gestureType == GestureType.Arc ||
                   gestureType == GestureType.Circle ||
                   gestureType == GestureType.Charged;
        }

        /// <summary>验证阈值是有限且非负的数值。</summary>
        private static void ValidateFiniteNonNegative(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Gesture thresholds must be finite and non-negative.");
            }
        }
    }
}
