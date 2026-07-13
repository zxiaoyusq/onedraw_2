using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Levels
{
    public readonly struct BattleResultMetrics
    {
        public BattleResultMetrics(
            long combatScore,
            int reflectedProjectileCount,
            long playerDamageTaken,
            double gameplayElapsedSeconds)
        {
            if (combatScore < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(combatScore));
            }

            if (reflectedProjectileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reflectedProjectileCount));
            }

            if (playerDamageTaken < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(playerDamageTaken));
            }

            if (double.IsNaN(gameplayElapsedSeconds) ||
                double.IsInfinity(gameplayElapsedSeconds) ||
                gameplayElapsedSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(gameplayElapsedSeconds));
            }

            CombatScore = combatScore;
            ReflectedProjectileCount = reflectedProjectileCount;
            PlayerDamageTaken = playerDamageTaken;
            GameplayElapsedSeconds = gameplayElapsedSeconds;
        }

        public long CombatScore { get; }

        public int ReflectedProjectileCount { get; }

        public long PlayerDamageTaken { get; }

        public double GameplayElapsedSeconds { get; }
    }

    public sealed class ResultScoreSettings
    {
        internal ResultScoreSettings(
            long scorePerReflect,
            long noDamageBonus,
            long scorePerRemainingSecond)
        {
            ScorePerReflect = scorePerReflect;
            NoDamageBonus = noDamageBonus;
            ScorePerRemainingSecond = scorePerRemainingSecond;
        }

        public long ScorePerReflect { get; }

        public long NoDamageBonus { get; }

        public long ScorePerRemainingSecond { get; }
    }

    public static class ResultScoreSettingsFactory
    {
        public static ResultScoreSettings Create(IConfigProvider configProvider)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            return new ResultScoreSettings(
                ReadNonNegativeInt(configProvider, ConfigIds.GlobalKeys.ResultScorePerReflect),
                ReadNonNegativeInt(configProvider, ConfigIds.GlobalKeys.ResultScoreNoDamageBonus),
                ReadNonNegativeInt(configProvider, ConfigIds.GlobalKeys.ResultScorePerRemainingSecond));
        }

        private static long ReadNonNegativeInt(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            if (!string.Equals(row.ValueType, "int", StringComparison.Ordinal) ||
                !row.IntValue.HasValue ||
                row.IntValue.Value < 0L)
            {
                throw new ArgumentException(
                    $"Global '{key}' must be a non-negative int value.",
                    nameof(configProvider));
            }

            return row.IntValue.Value;
        }
    }

    public readonly struct ResultScoreBreakdown
    {
        internal ResultScoreBreakdown(
            long combatScore,
            long reflectedProjectileScore,
            long noDamageScore,
            long remainingTimeScore,
            long remainingWholeSeconds,
            long finalScore,
            int stars)
        {
            CombatScore = combatScore;
            ReflectedProjectileScore = reflectedProjectileScore;
            NoDamageScore = noDamageScore;
            RemainingTimeScore = remainingTimeScore;
            RemainingWholeSeconds = remainingWholeSeconds;
            FinalScore = finalScore;
            Stars = stars;
        }

        public long CombatScore { get; }

        public long ReflectedProjectileScore { get; }

        public long NoDamageScore { get; }

        public long RemainingTimeScore { get; }

        public long RemainingWholeSeconds { get; }

        public long FinalScore { get; }

        public int Stars { get; }
    }

    public static class ResultScoring
    {
        public static ResultScoreBreakdown Calculate(
            ResultScoreSettings settings,
            LevelConfig level,
            BattleSettlement settlement,
            in BattleResultMetrics metrics)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (settlement != BattleSettlement.Victory &&
                settlement != BattleSettlement.Defeat)
            {
                throw new ArgumentOutOfRangeException(nameof(settlement));
            }

            long reflectedScore = 0L;
            long noDamageScore = 0L;
            long remainingTimeScore = 0L;
            long remainingWholeSeconds = 0L;
            if (settlement == BattleSettlement.Victory)
            {
                double remainingSeconds = Math.Max(
                    0d,
                    level.DurationLimitSec - metrics.GameplayElapsedSeconds);
                remainingWholeSeconds = checked((long)Math.Floor(remainingSeconds));
                checked
                {
                    reflectedScore = settings.ScorePerReflect * metrics.ReflectedProjectileCount;
                    noDamageScore = metrics.PlayerDamageTaken == 0L
                        ? settings.NoDamageBonus
                        : 0L;
                    remainingTimeScore =
                        settings.ScorePerRemainingSecond * remainingWholeSeconds;
                }
            }

            long finalScore;
            checked
            {
                finalScore = metrics.CombatScore + reflectedScore + noDamageScore + remainingTimeScore;
            }

            int stars = settlement == BattleSettlement.Victory
                ? CalculateStars(level, finalScore)
                : 0;
            return new ResultScoreBreakdown(
                metrics.CombatScore,
                reflectedScore,
                noDamageScore,
                remainingTimeScore,
                remainingWholeSeconds,
                finalScore,
                stars);
        }

        private static int CalculateStars(LevelConfig level, long score)
        {
            if (score >= level.StarScore3)
            {
                return 3;
            }

            if (score >= level.StarScore2)
            {
                return 2;
            }

            return score >= level.StarScore1 ? 1 : 0;
        }
    }
}
