using System;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    public sealed class PlayerSkillEffectTarget : ISkillEffectTarget
    {
        private readonly PlayerCombatController player;

        public PlayerSkillEffectTarget(PlayerCombatController playerController)
        {
            player = playerController ??
                throw new ArgumentNullException(nameof(playerController));
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

        public bool ApplyDamage(float amount, string sourceId, double timestamp)
        {
            return player.ApplyDamage(ToAmount(amount), timestamp, sourceId).ChangedHp;
        }

        public bool ApplyHealing(float amount, string sourceId, double timestamp)
        {
            return player.Heal(ToAmount(amount), timestamp, sourceId).ChangedHp;
        }

        public bool ApplyBuff(
            BuffConfig buff,
            float durationSeconds,
            string sourceId,
            double timestamp)
        {
            return false;
        }

        public bool RemoveArmor(float amount, string sourceId, double timestamp)
        {
            return false;
        }

        public bool ApplyKnockback(
            float distanceRefPixels,
            float durationSeconds,
            string sourceId,
            double timestamp)
        {
            return false;
        }

        public bool ExecuteBelowHpRatio(float threshold, string sourceId, double timestamp)
        {
            return false;
        }

        public bool IncrementCounter(
            float amount,
            float limit,
            string sourceId,
            double timestamp)
        {
            return false;
        }

        private static long ToAmount(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Configured player HP effect amount must be finite and non-negative.");
            }

            double rounded = Math.Round(amount, MidpointRounding.AwayFromZero);
            if (rounded > long.MaxValue)
            {
                throw new OverflowException("Configured player HP effect amount exceeds Int64.");
            }

            return (long)rounded;
        }
    }
}
