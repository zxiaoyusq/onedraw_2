using System;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    // 定义 PlayerSkillEffectTarget 的技能领域契约，明确条件、目标或效果执行边界。
    public sealed class PlayerSkillEffectTarget : ISkillEffectTarget
    {
        private readonly PlayerCombatController player;

        // 初始化 PlayerSkillEffectTarget，并建立技能运行时所需的初始状态。
        public PlayerSkillEffectTarget(PlayerCombatController playerController)
        {
            player = playerController ??
                throw new ArgumentNullException(nameof(playerController));
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (!player.IsInitialized)
            {
                throw new ArgumentException(
                    "Player combat controller must be initialized.",
                    nameof(playerController));
            }
        }

        public string TargetId => player.Current.PlayerId;

        public SkillTargetFaction Faction => SkillTargetFaction.Player;

        public SkillEnemyTier EnemyTier => SkillEnemyTier.None;

        public bool IsAlive => !player.Current.IsDead;

        public bool IsInEffectRadius => false;

        public bool WasHitByLastStroke => false;

        public bool IsInsideGesture => false;

        // 应用 ApplyDamage 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ApplyDamage(float amount, string sourceId, double timestamp)
        {
            return player.ApplyDamage(ToAmount(amount), timestamp, sourceId).ChangedHp;
        }

        // 应用 ApplyHealing 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ApplyHealing(float amount, string sourceId, double timestamp)
        {
            return player.Heal(ToAmount(amount), timestamp, sourceId).ChangedHp;
        }

        // 应用 ApplyBuff 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ApplyBuff(
            BuffConfig buff,
            float durationSeconds,
            string sourceId,
            double timestamp)
        {
            return false;
        }

        // 移除 RemoveArmor 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool RemoveArmor(float amount, string sourceId, double timestamp)
        {
            return false;
        }

        // 应用 ApplyKnockback 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ApplyKnockback(
            float distanceRefPixels,
            float durationSeconds,
            string sourceId,
            double timestamp)
        {
            return false;
        }

        // 执行 ExecuteBelowHpRatio 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool ExecuteBelowHpRatio(float threshold, string sourceId, double timestamp)
        {
            return false;
        }

        // 处理 IncrementCounter 对应的技能逻辑，并保持条件、目标与效果结果一致。
        public bool IncrementCounter(
            float amount,
            float limit,
            string sourceId,
            double timestamp)
        {
            return false;
        }

        // 处理 ToAmount 对应的技能逻辑，并保持条件、目标与效果结果一致。
        private static long ToAmount(float amount)
        {
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Configured player HP effect amount must be finite and non-negative.");
            }

            double rounded = Math.Round(amount, MidpointRounding.AwayFromZero);
            // 检查技能条件或运行时边界，阻止无效状态继续执行。
            if (rounded > long.MaxValue)
            {
                throw new OverflowException("Configured player HP effect amount exceeds Int64.");
            }

            return (long)rounded;
        }
    }
}
