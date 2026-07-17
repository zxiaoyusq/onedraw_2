using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Levels
{
    // 定义 BattleResultMetrics 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct BattleResultMetrics
    {
        // 初始化 BattleResultMetrics，并建立关卡流程所需的初始状态。
        public BattleResultMetrics(
            long combatScore,
            int reflectedProjectileCount,
            long playerDamageTaken,
            double gameplayElapsedSeconds)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (combatScore < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(combatScore));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (reflectedProjectileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reflectedProjectileCount));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (playerDamageTaken < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(playerDamageTaken));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

    // 定义 ResultScoreSettings 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class ResultScoreSettings
    {
        // 初始化 ResultScoreSettings，并建立关卡流程所需的初始状态。
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

    // 定义 ResultScoreSettingsFactory 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public static class ResultScoreSettingsFactory
    {
        // 创建 Create 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public static ResultScoreSettings Create(IConfigProvider configProvider)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            return new ResultScoreSettings(
                ReadNonNegativeInt(configProvider, ConfigIds.GlobalKeys.ResultScorePerReflect),
                ReadNonNegativeInt(configProvider, ConfigIds.GlobalKeys.ResultScoreNoDamageBonus),
                ReadNonNegativeInt(configProvider, ConfigIds.GlobalKeys.ResultScorePerRemainingSecond));
        }

        // 处理 ReadNonNegativeInt 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static long ReadNonNegativeInt(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

    // 定义 ResultScoreBreakdown 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct ResultScoreBreakdown
    {
        // 初始化 ResultScoreBreakdown，并建立关卡流程所需的初始状态。
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

    // 定义 ResultScoring 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public static class ResultScoring
    {
        // 计算 Calculate 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public static ResultScoreBreakdown Calculate(
            ResultScoreSettings settings,
            LevelConfig level,
            BattleSettlement settlement,
            in BattleResultMetrics metrics)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (settlement != BattleSettlement.Victory &&
                settlement != BattleSettlement.Defeat)
            {
                throw new ArgumentOutOfRangeException(nameof(settlement));
            }

            long reflectedScore = 0L;
            long noDamageScore = 0L;
            long remainingTimeScore = 0L;
            long remainingWholeSeconds = 0L;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

        // 计算 CalculateStars 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static int CalculateStars(LevelConfig level, long score)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (score >= level.StarScore3)
            {
                return 3;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (score >= level.StarScore2)
            {
                return 2;
            }

            return score >= level.StarScore1 ? 1 : 0;
        }
    }
}
