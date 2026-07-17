using System;

namespace OneStrokeDemon.Combat
{
    /// <summary>玩家笔迹与投射物交互的确定结果。</summary>
    public enum ProjectileStrokeOutcome
    {
        None = 0,
        FriendlyOwned = 1,
        RequiredStanceMismatch = 2,
        Uncuttable = 3,
        Cut = 4,
        Reflected = 5
    }

    /// <summary>保存一次投射物笔迹规则判断及其生命周期影响。</summary>
    public readonly struct ProjectileStrokeResolution
    {
        /// <summary>创建标记为已解析的交互结果。</summary>
        internal ProjectileStrokeResolution(ProjectileStrokeOutcome outcome)
        {
            Outcome = outcome;
            IsResolved = true;
        }

        /// <summary>获取交互结果类型。</summary>
        public ProjectileStrokeOutcome Outcome { get; }

        /// <summary>获取是否由解析器生成有效结果。</summary>
        public bool IsResolved { get; }

        /// <summary>获取该结果是否要求回收投射物。</summary>
        public bool ReleasesProjectile => Outcome == ProjectileStrokeOutcome.Cut;

        /// <summary>获取该结果是否要求切换投射物归属。</summary>
        public bool ChangesOwnership => Outcome == ProjectileStrokeOutcome.Reflected;
    }

    /// <summary>按阵营、架势门和反弹优先级解析笔迹对投射物的作用。</summary>
    public static class ProjectileCutResolver
    {
        /// <summary>验证输入后按友方、架势、反弹、切断和不可切断顺序返回唯一结果。</summary>
        public static ProjectileStrokeResolution Resolve(
            in ProjectileRuleSet rules,
            in ProjectileOwnership ownership,
            string stanceId,
            ProjectileOwner reflector)
        {
            if (!rules.IsConfigured)
            {
                throw new ArgumentException("Projectile rules must be configured.", nameof(rules));
            }

            if (!ownership.IsValid)
            {
                throw new ArgumentException("Projectile ownership must be initialized.", nameof(ownership));
            }

            if (string.IsNullOrWhiteSpace(stanceId))
            {
                throw new ArgumentException("Current stance id must be non-empty.", nameof(stanceId));
            }

            if (!reflector.IsValid)
            {
                throw new ArgumentException("Reflector owner must be initialized.", nameof(reflector));
            }

            if (reflector.Faction == ownership.CurrentOwner.Faction)
            {
                return new ProjectileStrokeResolution(ProjectileStrokeOutcome.FriendlyOwned);
            }

            if (!string.IsNullOrEmpty(rules.RequiredStanceId) &&
                !string.Equals(rules.RequiredStanceId, stanceId, StringComparison.Ordinal))
            {
                return new ProjectileStrokeResolution(
                    ProjectileStrokeOutcome.RequiredStanceMismatch);
            }

            // 两个配置开关同时为 true 时反弹优先，避免同一笔既换归属又回收。
            if (rules.Reflectable)
            {
                return new ProjectileStrokeResolution(ProjectileStrokeOutcome.Reflected);
            }

            return new ProjectileStrokeResolution(
                rules.Cuttable
                    ? ProjectileStrokeOutcome.Cut
                    : ProjectileStrokeOutcome.Uncuttable);
        }
    }
}
