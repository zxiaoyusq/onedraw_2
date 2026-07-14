using System;

namespace OneStrokeDemon.Combat
{
    public sealed class ScoreService
    {
        private long totalScore;
        private long totalEnergyEarned;
        private long totalDamage;
        private int hitCount;
        private int weakpointHitCount;
        private int directionMatchCount;
        private int criticalHitCount;

        public event Action<CombatScoreSnapshot> Changed;

        public CombatScoreSnapshot Current => new CombatScoreSnapshot(
            totalScore,
            totalEnergyEarned,
            totalDamage,
            hitCount,
            weakpointHitCount,
            directionMatchCount,
            criticalHitCount);

        public CombatScoreSnapshot Record(in DamageResult result)
        {
            if (!result.IsResolved)
            {
                throw new ArgumentException("Only a resolved damage result can be recorded.", nameof(result));
            }

            long nextScore;
            long nextEnergy;
            long nextDamage;
            int nextHitCount;
            int nextWeakpointCount;
            int nextDirectionCount;
            int nextCriticalCount;
            checked
            {
                nextScore = totalScore + result.ScoreAward;
                nextEnergy = totalEnergyEarned + result.EnergyAward;
                nextDamage = totalDamage + result.Damage;
                nextHitCount = hitCount + 1;
                nextWeakpointCount = weakpointHitCount + (result.IsWeakpoint ? 1 : 0);
                nextDirectionCount = directionMatchCount + (result.DirectionMatched ? 1 : 0);
                nextCriticalCount = criticalHitCount + (result.IsCritical ? 1 : 0);
            }

            totalScore = nextScore;
            totalEnergyEarned = nextEnergy;
            totalDamage = nextDamage;
            hitCount = nextHitCount;
            weakpointHitCount = nextWeakpointCount;
            directionMatchCount = nextDirectionCount;
            criticalHitCount = nextCriticalCount;
            CombatScoreSnapshot snapshot = Current;
            Changed?.Invoke(snapshot);
            return snapshot;
        }

        public void Reset()
        {
            bool changed = totalScore != 0L ||
                           totalEnergyEarned != 0L ||
                           totalDamage != 0L ||
                           hitCount != 0 ||
                           weakpointHitCount != 0 ||
                           directionMatchCount != 0 ||
                           criticalHitCount != 0;
            totalScore = 0L;
            totalEnergyEarned = 0L;
            totalDamage = 0L;
            hitCount = 0;
            weakpointHitCount = 0;
            directionMatchCount = 0;
            criticalHitCount = 0;
            if (changed)
            {
                Changed?.Invoke(Current);
            }
        }
    }

    public readonly struct CombatScoreSnapshot
    {
        internal CombatScoreSnapshot(
            long totalScore,
            long totalEnergyEarned,
            long totalDamage,
            int hitCount,
            int weakpointHitCount,
            int directionMatchCount,
            int criticalHitCount)
        {
            TotalScore = totalScore;
            TotalEnergyEarned = totalEnergyEarned;
            TotalDamage = totalDamage;
            HitCount = hitCount;
            WeakpointHitCount = weakpointHitCount;
            DirectionMatchCount = directionMatchCount;
            CriticalHitCount = criticalHitCount;
        }

        public long TotalScore { get; }
        public long TotalEnergyEarned { get; }
        public long TotalDamage { get; }
        public int HitCount { get; }
        public int WeakpointHitCount { get; }
        public int DirectionMatchCount { get; }
        public int CriticalHitCount { get; }
    }
}
