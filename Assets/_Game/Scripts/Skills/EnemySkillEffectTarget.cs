using System;
using System.Globalization;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    // 定义 EnemySkillEffectTarget 的技能领域契约，明确条件、目标或效果执行边界。
    public sealed class EnemySkillEffectTarget : ISkillEffectTarget
    {
        private readonly EnemyController enemy;
        private readonly string targetId;
        private bool isInEffectRadius;
        private bool wasHitByLastStroke;
        private bool isInsideGesture;

        // 初始化 EnemySkillEffectTarget，并建立技能运行时所需的初始状态。
        public EnemySkillEffectTarget(EnemyController enemyController)
        {
            enemy = enemyController ??
                throw new ArgumentNullException(nameof(enemyController));
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (!enemy.IsSpawned)
            {
                throw new ArgumentException(
                    "Enemy controller must be spawned before creating its skill target adapter.",
                    nameof(enemyController));
            }

            targetId = string.Concat(
                enemy.Definition.EnemyId,
                ":",
                enemy.Damage.HitTargetId.ToString(CultureInfo.InvariantCulture));
        }

        public string TargetId => targetId;

        public SkillTargetFaction Faction => SkillTargetFaction.Enemy;

        public SkillEnemyTier EnemyTier
        {
            get
            {
                // 按效果或目标类型选择对应的技能处理分支。
                switch (enemy.Definition.Tier)
                {
                    case OneStrokeDemon.Actors.EnemyTier.Normal: return SkillEnemyTier.Normal;
                    case OneStrokeDemon.Actors.EnemyTier.Elite: return SkillEnemyTier.Elite;
                    case OneStrokeDemon.Actors.EnemyTier.Boss: return SkillEnemyTier.Boss;
                    default: return SkillEnemyTier.None;
                }
            }
        }

        public bool IsAlive => enemy.IsAlive;

        public bool IsInEffectRadius => isInEffectRadius;

        public bool WasHitByLastStroke => wasHitByLastStroke;

        public bool IsInsideGesture => isInsideGesture;

        public EnemyController Enemy => enemy;

        // 设置 SetSelectionFlags 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public void SetSelectionFlags(
            bool inEffectRadius,
            bool hitByLastStroke,
            bool insideGesture)
        {
            isInEffectRadius = inEffectRadius;
            wasHitByLastStroke = hitByLastStroke;
            isInsideGesture = insideGesture;
        }

        // 应用 ApplyDamage 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ApplyDamage(float amount, string sourceId, double timestamp)
        {
            return enemy.ApplyDamage(ToAmount(amount), sourceId, timestamp).Changed;
        }

        // 应用 ApplyHealing 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ApplyHealing(float amount, string sourceId, double timestamp)
        {
            return enemy.Heal(ToAmount(amount), sourceId, timestamp).Changed;
        }

        // 应用 ApplyBuff 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ApplyBuff(
            BuffConfig buff,
            float durationSeconds,
            string sourceId,
            double timestamp)
        {
            return enemy.ApplyBuff(buff, durationSeconds, sourceId, timestamp).Changed;
        }

        // 移除 RemoveArmor 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool RemoveArmor(float amount, string sourceId, double timestamp)
        {
            return enemy.RemoveArmor(ToAmount(amount), sourceId, timestamp).Changed;
        }

        // 应用 ApplyKnockback 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ApplyKnockback(
            float distanceRefPixels,
            float durationSeconds,
            string sourceId,
            double timestamp)
        {
            return enemy.RequestKnockback(
                distanceRefPixels,
                durationSeconds,
                sourceId,
                timestamp);
        }

        // 执行 ExecuteBelowHpRatio 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ExecuteBelowHpRatio(
            float threshold,
            string sourceId,
            double timestamp)
        {
            return enemy.TryExecute(threshold, sourceId, timestamp).DeathTriggered;
        }

        // 处理 IncrementCounter 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool IncrementCounter(
            float amount,
            float limit,
            string sourceId,
            double timestamp)
        {
            return enemy.IncrementCounter(
                sourceId,
                amount,
                limit,
                sourceId,
                timestamp);
        }

        // 处理 ToAmount 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private static long ToAmount(float amount)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Configured enemy effect amount must be finite and non-negative.");
            }

            double rounded = Math.Round(amount, MidpointRounding.AwayFromZero);
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (rounded > long.MaxValue)
            {
                throw new OverflowException(
                    "Configured enemy effect amount exceeds Int64 capacity.");
            }

            return (long)rounded;
        }
    }
}
