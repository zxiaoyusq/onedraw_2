using System;
using System.Collections.Generic;

namespace OneStrokeDemon.Skills
{
    internal static class SkillTargetSelector
    {
        public static void Validate(string targetType, string effectGroupId, long order)
        {
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

        public static bool IsWorldScope(string targetType)
        {
            return string.Equals(targetType, SkillTargetTypes.Battle, StringComparison.Ordinal) ||
                   string.Equals(targetType, SkillTargetTypes.NextStroke, StringComparison.Ordinal);
        }

        public static void Select(
            string targetType,
            ISkillEffectWorld world,
            List<ISkillEffectTarget> selected)
        {
            selected.Clear();
            if (string.Equals(targetType, SkillTargetTypes.Target, StringComparison.Ordinal))
            {
                ISkillEffectTarget primary = world.PrimaryTarget;
                if (primary != null && primary.IsAlive)
                {
                    selected.Add(primary);
                }

                return;
            }

            if (IsWorldScope(targetType))
            {
                return;
            }

            IReadOnlyList<ISkillEffectTarget> targets = world.Targets ??
                throw new InvalidOperationException("Skill effect world targets cannot be null.");
            for (int i = 0; i < targets.Count; i++)
            {
                ISkillEffectTarget target = targets[i] ??
                    throw new InvalidOperationException($"Skill effect target at index {i} is null.");
                if (!target.IsAlive || target.Faction != SkillTargetFaction.Enemy)
                {
                    continue;
                }

                bool include;
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

                if (include)
                {
                    selected.Add(target);
                }
            }
        }
    }
}
