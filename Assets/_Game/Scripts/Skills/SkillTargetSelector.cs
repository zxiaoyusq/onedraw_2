using System;
using System.Collections.Generic;

namespace OneStrokeDemon.Skills
{
    // 定义 SkillTargetSelector 的技能领域契约，明确条件、目标或效果执行边界。
    internal static class SkillTargetSelector
    {
        // 校验 Validate 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public static void Validate(string targetType, string effectGroupId, long order)
        {
            // 按效果或目标类型选择对应的技能处理分支。
            switch (targetType)
            {
                case SkillTargetTypes.Target:
                case SkillTargetTypes.NextStroke:
                case SkillTargetTypes.EnemiesInRadius:
                case SkillTargetTypes.LastStrokeTargets:
                case SkillTargetTypes.EnemiesInsideGesture:
                case SkillTargetTypes.Battle:
                case SkillTargetTypes.AllEnemies:
                case SkillTargetTypes.NormalEnemies:
                case SkillTargetTypes.Boss:
                    return;
                default:
                    throw new SkillEffectConfigurationException(
                        $"Target type '{targetType}' has no registered selector.",
                        effectGroupId,
                        order);
            }
        }

        // 判断是否 IsWorldScope 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public static bool IsWorldScope(string targetType)
        {
            return string.Equals(targetType, SkillTargetTypes.Battle, StringComparison.Ordinal) ||
                   string.Equals(targetType, SkillTargetTypes.NextStroke, StringComparison.Ordinal);
        }

        // 选择 Select 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public static void Select(
            string targetType,
            ISkillEffectWorld world,
            List<ISkillEffectTarget> selected)
        {
            selected.Clear();
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (string.Equals(targetType, SkillTargetTypes.Target, StringComparison.Ordinal))
            {
                ISkillEffectTarget primary = world.PrimaryTarget;
                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (primary != null && primary.IsAlive)
                {
                    selected.Add(primary);
                }

                return;
            }

            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (IsWorldScope(targetType))
            {
                return;
            }

            IReadOnlyList<ISkillEffectTarget> targets = world.Targets ??
                throw new InvalidOperationException("Skill effect world targets cannot be null.");
            // 逐项处理技能目标或效果，保持配置顺序与执行结果稳定。
            for (int i = 0; i < targets.Count; i++)
            {
                ISkillEffectTarget target = targets[i] ??
                    throw new InvalidOperationException($"Skill effect target at index {i} is null.");
                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (!target.IsAlive || target.Faction != SkillTargetFaction.Enemy)
                {
                    continue;
                }

                bool include;
                // 按效果或目标类型选择对应的技能处理分支。
                switch (targetType)
                {
                    case SkillTargetTypes.EnemiesInRadius:
                        include = target.IsInEffectRadius;
                        break;
                    case SkillTargetTypes.LastStrokeTargets:
                        include = target.WasHitByLastStroke;
                        break;
                    case SkillTargetTypes.EnemiesInsideGesture:
                        include = target.IsInsideGesture;
                        break;
                    case SkillTargetTypes.AllEnemies:
                        include = true;
                        break;
                    case SkillTargetTypes.NormalEnemies:
                        include = target.EnemyTier != SkillEnemyTier.Boss;
                        break;
                    case SkillTargetTypes.Boss:
                        include = target.EnemyTier == SkillEnemyTier.Boss;
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Target type '{targetType}' was not validated before selection.");
                }

                // 检查技能条件或运行时边界，阻止无效状态继续执行。
                if (include)
                {
                    selected.Add(target);
                }
            }
        }
    }
}
