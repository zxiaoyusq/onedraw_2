using System;
using OneStrokeDemon.Input;

namespace OneStrokeDemon.Combat
{
    /// <summary>保存一笔对一个去重目标的稳定路径顺序、弱点和笔势事实。</summary>
    public readonly struct HitRecord
    {
        /// <summary>由命中解析器创建并验证完整命中记录。</summary>
        internal HitRecord(
            ulong strokeId,
            IHittable target,
            int targetId,
            bool isWeakpoint,
            float pathParameter,
            float pathDistanceReferencePixels,
            GestureMatchResult gesture,
            double timestamp)
        {
            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId), "Stroke id must be positive.");
            }

            Target = target ?? throw new ArgumentNullException(nameof(target));
            if (targetId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetId), "Hit target id must be non-zero.");
            }

            if (!IsFinite(pathParameter) || pathParameter < 0f || pathParameter > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pathParameter),
                    "Path parameter must be finite and normalized.");
            }

            if (!IsFinite(pathDistanceReferencePixels) || pathDistanceReferencePixels < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pathDistanceReferencePixels),
                    "Path distance must be finite and non-negative.");
            }

            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), "Hit timestamp must be finite.");
            }

            Gesture = gesture ?? throw new ArgumentNullException(nameof(gesture));
            StrokeId = strokeId;
            TargetId = targetId;
            IsWeakpoint = isWeakpoint;
            PathParameter = pathParameter;
            PathDistanceReferencePixels = pathDistanceReferencePixels;
            Timestamp = timestamp;
        }

        // 路径参数、距离和目标 ID 可共同复现一笔多目标的确定顺序。
        public ulong StrokeId { get; }

        public IHittable Target { get; }

        public int TargetId { get; }

        public bool IsWeakpoint { get; }

        public float PathParameter { get; }

        public float PathDistanceReferencePixels { get; }

        public GestureMatchResult Gesture { get; }

        public GestureType GestureType => Gesture.GestureType;

        public string GestureRuleId => Gesture.RuleId;

        public double Timestamp { get; }

        /// <summary>判断浮点数不是 NaN 或无穷。</summary>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
