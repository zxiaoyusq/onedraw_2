namespace OneStrokeDemon.Input
{
    /// <summary>保存一次笔势分类的不可变结果及用于解释结果的几何指标。</summary>
    public sealed class GestureMatchResult
    {
        /// <summary>由分类器创建一份完整识别结果。</summary>
        internal GestureMatchResult(
            ulong strokeId,
            string ruleId,
            GestureType gestureType,
            float confidence,
            float lengthReferencePixels,
            double averageSpeedReferencePixelsPerSecond,
            float directionAngleDegrees,
            float normalizedCurvature,
            float closureRatio,
            float closureDistanceReferencePixels,
            float areaReferencePixelsSquared,
            double initialHoldSeconds)
        {
            StrokeId = strokeId;
            RuleId = ruleId ?? string.Empty;
            GestureType = gestureType;
            Confidence = confidence;
            LengthReferencePixels = lengthReferencePixels;
            AverageSpeedReferencePixelsPerSecond = averageSpeedReferencePixelsPerSecond;
            DirectionAngleDegrees = directionAngleDegrees;
            NormalizedCurvature = normalizedCurvature;
            ClosureRatio = closureRatio;
            ClosureDistanceReferencePixels = closureDistanceReferencePixels;
            AreaReferencePixelsSquared = areaReferencePixelsSquared;
            InitialHoldSeconds = initialHoldSeconds;
        }

        /// <summary>获取来源笔迹 ID。</summary>
        public ulong StrokeId { get; }

        /// <summary>获取是否有规则成功匹配。</summary>
        public bool IsMatch => GestureType != GestureType.None;

        /// <summary>获取命中的规则 ID；未匹配时为空字符串。</summary>
        public string RuleId { get; }

        /// <summary>获取识别出的笔势类型。</summary>
        public GestureType GestureType { get; }

        /// <summary>获取零到一之间的匹配置信度。</summary>
        public float Confidence { get; }

        /// <summary>获取处理后笔迹长度。</summary>
        public float LengthReferencePixels { get; }

        /// <summary>获取平均绘制速度。</summary>
        public double AverageSpeedReferencePixelsPerSecond { get; }

        /// <summary>获取首尾位移在半圆范围内的方向角。</summary>
        public float DirectionAngleDegrees { get; }

        /// <summary>获取总绝对转角除以 π 的归一化曲率。</summary>
        public float NormalizedCurvature { get; }

        /// <summary>获取首尾距离与路径长度之比。</summary>
        public float ClosureRatio { get; }

        /// <summary>获取首尾闭合距离。</summary>
        public float ClosureDistanceReferencePixels { get; }

        /// <summary>获取笔迹隐式闭合面积。</summary>
        public float AreaReferencePixelsSquared { get; }

        /// <summary>获取起笔到首次有效移动之间的停留秒数。</summary>
        public double InitialHoldSeconds { get; }
    }
}
