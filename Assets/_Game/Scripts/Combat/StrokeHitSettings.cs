using System;

namespace OneStrokeDemon.Combat
{
    /// <summary>定义一次笔迹最多去重目标数和底层查询缓冲容量。</summary>
    public readonly struct StrokeHitResolverSettings
    {
        /// <summary>创建并验证必须包含饱和检测余量的解析器容量设置。</summary>
        public StrokeHitResolverSettings(int maximumUniqueTargets, int queryCapacity)
        {
            if (maximumUniqueTargets < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumUniqueTargets),
                    "Maximum unique targets must be positive.");
            }

            if (queryCapacity <= maximumUniqueTargets)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(queryCapacity),
                    "Query capacity must include hitbox and saturation room beyond unique targets.");
            }

            MaximumUniqueTargets = maximumUniqueTargets;
            QueryCapacity = queryCapacity;
        }

        /// <summary>获取一笔允许的最大唯一目标数量。</summary>
        public int MaximumUniqueTargets { get; }

        /// <summary>获取每段底层 Collider 查询容量。</summary>
        public int QueryCapacity { get; }
    }

    /// <summary>把识别规则 ID 与该笔势的命中半径绑定。</summary>
    public readonly struct StrokeHitRule
    {
        /// <summary>创建带正有限参考像素半径的命中规则。</summary>
        public StrokeHitRule(string ruleId, float radiusReferencePixels)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException("Stroke hit rule id is required.", nameof(ruleId));
            }

            if (float.IsNaN(radiusReferencePixels) ||
                float.IsInfinity(radiusReferencePixels) ||
                radiusReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radiusReferencePixels),
                    "Stroke hit radius must be finite and positive.");
            }

            RuleId = ruleId;
            RadiusReferencePixels = radiusReferencePixels;
        }

        /// <summary>获取必须与识别结果一致的规则 ID。</summary>
        public string RuleId { get; }

        /// <summary>获取命中胶囊半径。</summary>
        public float RadiusReferencePixels { get; }
    }
}
