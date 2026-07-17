using System;

namespace OneStrokeDemon.Combat
{
    /// <summary>累计已解析伤害结果中的战斗分、能量、伤害和命中分类统计。</summary>
    public sealed class ScoreService
    {
        private long totalScore;
        private long totalEnergyEarned;
        private long totalDamage;
        private int hitCount;
        private int weakpointHitCount;
        private int directionMatchCount;
        private int criticalHitCount;

        /// <summary>累计快照变化时发布。</summary>
        public event Action<CombatScoreSnapshot> Changed;

        /// <summary>获取当前不可变累计快照。</summary>
        public CombatScoreSnapshot Current => new CombatScoreSnapshot(
            totalScore,
            totalEnergyEarned,
            totalDamage,
            hitCount,
            weakpointHitCount,
            directionMatchCount,
            criticalHitCount);

        /// <summary>原子累加一个已解析结果；任何溢出发生时旧状态保持不变。</summary>
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
            // 先在 checked 局部候选中完成全部运算，再统一发布，避免半更新。
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

        /// <summary>清空全部累计值，仅在实际变化时发布零快照。</summary>
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

    /// <summary>保存当前战斗评分与命中统计的不可变快照。</summary>
    public readonly struct CombatScoreSnapshot
    {
        /// <summary>创建完整累计快照。</summary>
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

        // 这些统计是战斗事实，关卡结算只能在其上追加配置定义的额外奖励。
        public long TotalScore { get; }
        public long TotalEnergyEarned { get; }
        public long TotalDamage { get; }
        public int HitCount { get; }
        public int WeakpointHitCount { get; }
        public int DirectionMatchCount { get; }
        public int CriticalHitCount { get; }
    }
}
