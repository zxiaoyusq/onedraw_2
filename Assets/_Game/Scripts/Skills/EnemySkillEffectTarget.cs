using System;
using System.Globalization;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Skills
{
    public sealed class EnemySkillEffectTarget : ISkillEffectTarget
    {
        private readonly EnemyController enemy;
        private readonly string targetId;
        private bool isInEffectRadius;
        private bool wasHitByLastStroke;
        private bool isInsideGesture;

        public EnemySkillEffectTarget(EnemyController enemyController)
        {
            enemy = enemyController ??
                throw new ArgumentNullException(nameof(enemyController));
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

        public void SetSelectionFlags(
            bool inEffectRadius,
            bool hitByLastStroke,
            bool insideGesture)
        {
            isInEffectRadius = inEffectRadius;
            wasHitByLastStroke = hitByLastStroke;
            isInsideGesture = insideGesture;
        }

        public bool ApplyDamage(float amount, string sourceId, double timestamp)
        {
            return enemy.ApplyDamage(ToAmount(amount), sourceId, timestamp).Changed;
        }

        public bool ApplyHealing(float amount, string sourceId, double timestamp)
        {
            return enemy.Heal(ToAmount(amount), sourceId, timestamp).Changed;
        }

        public bool ApplyBuff(
            BuffConfig buff,
            float durationSeconds,
            string sourceId,
            double timestamp)
        {
            return enemy.ApplyBuff(buff, durationSeconds, sourceId, timestamp).Changed;
        }

        public bool RemoveArmor(float amount, string sourceId, double timestamp)
        {
            return enemy.RemoveArmor(ToAmount(amount), sourceId, timestamp).Changed;
        }

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

        public bool ExecuteBelowHpRatio(
            float threshold,
            string sourceId,
            double timestamp)
        {
            return enemy.TryExecute(threshold, sourceId, timestamp).DeathTriggered;
        }

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

        private static long ToAmount(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Configured enemy effect amount must be finite and non-negative.");
            }

            double rounded = Math.Round(amount, MidpointRounding.AwayFromZero);
            if (rounded > long.MaxValue)
            {
                throw new OverflowException(
                    "Configured enemy effect amount exceeds Int64 capacity.");
            }

            return (long)rounded;
        }
    }
}
