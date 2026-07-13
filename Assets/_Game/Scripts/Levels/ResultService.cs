using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Levels
{
    public enum RewardGrantType
    {
        UnlockLevel = 0,
        UnlockFeature = 1,
        ScoreToken = 2,
    }

    public readonly struct RewardGrant
    {
        internal RewardGrant(
            long order,
            RewardGrantType type,
            string rewardId,
            long amount)
        {
            Order = order;
            Type = type;
            RewardId = rewardId;
            Amount = amount;
        }

        public long Order { get; }

        public RewardGrantType Type { get; }

        public string RewardId { get; }

        public long Amount { get; }
    }

    public enum SettlementApplyStatus
    {
        Applied = 0,
        Duplicate = 1,
    }

    public readonly struct ResultRequest
    {
        public ResultRequest(
            string settlementId,
            string levelId,
            BattleSettlement settlement,
            in BattleResultMetrics metrics)
        {
            if (string.IsNullOrWhiteSpace(settlementId) ||
                !string.Equals(settlementId, settlementId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Settlement id must be non-empty and trimmed.",
                    nameof(settlementId));
            }

            if (string.IsNullOrWhiteSpace(levelId) ||
                !string.Equals(levelId, levelId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Level id must be non-empty and trimmed.",
                    nameof(levelId));
            }

            if (settlement != BattleSettlement.Victory &&
                settlement != BattleSettlement.Defeat)
            {
                throw new ArgumentOutOfRangeException(nameof(settlement));
            }

            SettlementId = settlementId;
            LevelId = levelId;
            Settlement = settlement;
            Metrics = metrics;
        }

        public string SettlementId { get; }

        public string LevelId { get; }

        public BattleSettlement Settlement { get; }

        public BattleResultMetrics Metrics { get; }
    }

    public sealed class ResultReceipt
    {
        internal ResultReceipt(
            SettlementApplyStatus status,
            string settlementId,
            string levelId,
            BattleSettlement settlement,
            in ResultScoreBreakdown score,
            IReadOnlyList<RewardGrant> appliedRewards,
            string nextLevelId,
            bool canGoNext,
            ProgressSnapshot progress)
        {
            Status = status;
            SettlementId = settlementId;
            LevelId = levelId;
            Settlement = settlement;
            Score = score;
            AppliedRewards = appliedRewards;
            NextLevelId = nextLevelId;
            CanGoNext = canGoNext;
            Progress = progress;
        }

        public SettlementApplyStatus Status { get; }

        public string SettlementId { get; }

        public string LevelId { get; }

        public BattleSettlement Settlement { get; }

        public ResultScoreBreakdown Score { get; }

        public IReadOnlyList<RewardGrant> AppliedRewards { get; }

        public string NextLevelId { get; }

        public bool CanGoNext { get; }

        public ProgressSnapshot Progress { get; }
    }

    public sealed class ResultService
    {
        private const string ClearCondition = "Clear";
        private const string ScoreAtLeastCondition = "ScoreAtLeast";
        private const string StarAtLeastCondition = "StarAtLeast";
        private const string UnlockLevelReward = "UnlockLevel";
        private const string UnlockFeatureReward = "UnlockFeature";
        private const string ScoreTokenReward = "ScoreToken";

        private readonly IConfigProvider configProvider;
        private readonly IProgressSaveStore store;
        private readonly ProgressSaveCodec codec;
        private readonly ResultScoreSettings scoreSettings;
        private readonly Dictionary<string, LevelConfig> levels;
        private readonly HashSet<string> configuredFeatureIds;
        private readonly string[] rootLevelIds;

        public ResultService(
            IConfigProvider configProvider,
            IProgressSaveStore store,
            IEnumerable<IProgressSaveMigration> migrations = null)
        {
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            codec = new ProgressSaveCodec(migrations);
            scoreSettings = ResultScoreSettingsFactory.Create(configProvider);
            levels = BuildLevelCatalog(configProvider.GetLevels());
            rootLevelIds = FindRootLevelIds(levels.Values);
            configuredFeatureIds = BuildConfiguredFeatureIds(configProvider, levels.Values);
            ProgressSnapshot initial = ProgressSnapshot.Empty(rootLevelIds);

            ProgressLoadResult loaded = store.TryRead(out string payload)
                ? codec.Decode(payload, initial)
                : codec.Decode(null, initial);
            if ((loaded.Status == ProgressLoadStatus.Loaded ||
                 loaded.Status == ProgressLoadStatus.Migrated) &&
                !IsCatalogCompatible(loaded.Progress))
            {
                loaded = ProgressSaveCodec.RecoverInvalidCatalog(initial, "save_catalog_mismatch");
            }

            if (loaded.Status == ProgressLoadStatus.Migrated)
            {
                store.Write(codec.Encode(loaded.Progress));
            }

            LoadResult = loaded;
            Current = loaded.Progress;
        }

        public ProgressLoadResult LoadResult { get; }

        public ProgressSnapshot Current { get; private set; }

        public ResultReceipt Settle(in ResultRequest request)
        {
            if (!levels.TryGetValue(request.LevelId, out LevelConfig level))
            {
                throw new ArgumentException(
                    $"Unknown level '{request.LevelId}'.",
                    nameof(request));
            }

            if (!Current.IsLevelUnlocked(request.LevelId))
            {
                throw new InvalidOperationException(
                    $"Level '{request.LevelId}' is not unlocked.");
            }

            ResultScoreBreakdown score = ResultScoring.Calculate(
                scoreSettings,
                level,
                request.Settlement,
                request.Metrics);
            if (Current.HasAppliedSettlement(request.SettlementId))
            {
                return CreateReceipt(
                    SettlementApplyStatus.Duplicate,
                    request,
                    level,
                    score,
                    Array.Empty<RewardGrant>(),
                    Current);
            }

            IReadOnlyList<RewardGrant> grants = request.Settlement == BattleSettlement.Victory
                ? EvaluateRewards(level, score)
                : Array.Empty<RewardGrant>();
            ProgressSnapshot next = Current.Apply(
                request.SettlementId,
                request.LevelId,
                request.Settlement,
                score,
                grants);
            string payload = codec.Encode(next);
            store.Write(payload);
            Current = next;
            return CreateReceipt(
                SettlementApplyStatus.Applied,
                request,
                level,
                score,
                grants,
                next);
        }

        private ResultReceipt CreateReceipt(
            SettlementApplyStatus status,
            in ResultRequest request,
            LevelConfig level,
            in ResultScoreBreakdown score,
            IReadOnlyList<RewardGrant> grants,
            ProgressSnapshot progress)
        {
            string nextLevelId = level.NextLevelId ?? string.Empty;
            bool canGoNext =
                request.Settlement == BattleSettlement.Victory &&
                nextLevelId.Length > 0 &&
                progress.IsLevelUnlocked(nextLevelId);
            return new ResultReceipt(
                status,
                request.SettlementId,
                request.LevelId,
                request.Settlement,
                score,
                grants,
                nextLevelId,
                canGoNext,
                progress);
        }

        private IReadOnlyList<RewardGrant> EvaluateRewards(
            LevelConfig level,
            in ResultScoreBreakdown score)
        {
            IReadOnlyList<RewardConfig> rows = configProvider.GetRewards(level.RewardTableId);
            var grants = new List<RewardGrant>(rows.Count);
            for (int index = 0; index < rows.Count; index += 1)
            {
                RewardConfig row = rows[index];
                if (!ConditionMet(row, score))
                {
                    continue;
                }

                if (row.Amount <= 0L || string.IsNullOrWhiteSpace(row.RewardId))
                {
                    throw InvalidReward(row, "Reward id and amount must be positive.");
                }

                RewardGrantType type;
                switch (row.RewardType)
                {
                    case UnlockLevelReward:
                        if (row.Amount != 1L)
                        {
                            throw InvalidReward(row, "UnlockLevel amount must be 1.");
                        }

                        if (!levels.ContainsKey(row.RewardId))
                        {
                            throw InvalidReward(row, $"Unknown level reward '{row.RewardId}'.");
                        }

                        type = RewardGrantType.UnlockLevel;
                        break;
                    case UnlockFeatureReward:
                        if (row.Amount != 1L)
                        {
                            throw InvalidReward(row, "UnlockFeature amount must be 1.");
                        }

                        type = RewardGrantType.UnlockFeature;
                        break;
                    case ScoreTokenReward:
                        type = RewardGrantType.ScoreToken;
                        break;
                    default:
                        throw InvalidReward(row, $"Unknown reward type '{row.RewardType}'.");
                }

                grants.Add(new RewardGrant(row.Order, type, row.RewardId, row.Amount));
            }

            return new ReadOnlyCollection<RewardGrant>(grants);
        }

        private static bool ConditionMet(RewardConfig row, in ResultScoreBreakdown score)
        {
            switch (row.ConditionType)
            {
                case ClearCondition:
                    return true;
                case ScoreAtLeastCondition:
                    return score.FinalScore >= ParseThreshold(row);
                case StarAtLeastCondition:
                    return score.Stars >= ParseThreshold(row);
                default:
                    throw InvalidReward(row, $"Unknown condition type '{row.ConditionType}'.");
            }
        }

        private static long ParseThreshold(RewardConfig row)
        {
            if (!long.TryParse(
                    row.ConditionValue,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long value) ||
                value < 0L)
            {
                throw InvalidReward(
                    row,
                    $"Invalid condition value '{row.ConditionValue}'.");
            }

            return value;
        }

        private static InvalidOperationException InvalidReward(
            RewardConfig row,
            string message)
        {
            return new InvalidOperationException(
                $"Reward table '{row.RewardTableId}' order {row.Order}: {message}");
        }

        private static Dictionary<string, LevelConfig> BuildLevelCatalog(
            IReadOnlyList<LevelConfig> configuredLevels)
        {
            if (configuredLevels == null || configuredLevels.Count == 0)
            {
                throw new ArgumentException("At least one configured level is required.");
            }

            var result = new Dictionary<string, LevelConfig>(StringComparer.Ordinal);
            for (int index = 0; index < configuredLevels.Count; index += 1)
            {
                LevelConfig level = configuredLevels[index];
                if (level == null || string.IsNullOrWhiteSpace(level.LevelId) ||
                    !result.TryAdd(level.LevelId, level))
                {
                    throw new ArgumentException("Level catalog contains an invalid or duplicate id.");
                }
            }

            foreach (LevelConfig level in result.Values)
            {
                if (!string.IsNullOrEmpty(level.NextLevelId) &&
                    !result.ContainsKey(level.NextLevelId))
                {
                    throw new ArgumentException(
                        $"Level '{level.LevelId}' references unknown next level '{level.NextLevelId}'.");
                }
            }

            return result;
        }

        private static string[] FindRootLevelIds(IEnumerable<LevelConfig> configuredLevels)
        {
            LevelConfig[] levels = configuredLevels.ToArray();
            var referenced = new HashSet<string>(
                levels.Where(level => !string.IsNullOrEmpty(level.NextLevelId))
                    .Select(level => level.NextLevelId),
                StringComparer.Ordinal);
            string[] roots = levels.Select(level => level.LevelId)
                .Where(levelId => !referenced.Contains(levelId))
                .OrderBy(levelId => levelId, StringComparer.Ordinal)
                .ToArray();
            if (roots.Length == 0)
            {
                throw new ArgumentException("Level graph must have at least one root.");
            }

            var byId = levels.ToDictionary(level => level.LevelId, StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex += 1)
            {
                string current = roots[rootIndex];
                var currentPath = new HashSet<string>(StringComparer.Ordinal);
                while (!string.IsNullOrEmpty(current))
                {
                    if (!currentPath.Add(current))
                    {
                        throw new ArgumentException($"Level graph cycles at '{current}'.");
                    }

                    if (!visited.Add(current))
                    {
                        break;
                    }

                    current = byId[current].NextLevelId;
                }
            }

            if (visited.Count != levels.Length)
            {
                throw new ArgumentException("Level graph contains an unreachable cycle.");
            }

            return roots;
        }

        private static HashSet<string> BuildConfiguredFeatureIds(
            IConfigProvider configProvider,
            IEnumerable<LevelConfig> configuredLevels)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (LevelConfig level in configuredLevels)
            {
                IReadOnlyList<RewardConfig> rewards =
                    configProvider.GetRewards(level.RewardTableId);
                for (int index = 0; index < rewards.Count; index += 1)
                {
                    RewardConfig reward = rewards[index];
                    if (string.Equals(
                            reward.RewardType,
                            UnlockFeatureReward,
                            StringComparison.Ordinal))
                    {
                        result.Add(reward.RewardId);
                    }
                }
            }

            return result;
        }

        private bool IsCatalogCompatible(ProgressSnapshot progress)
        {
            for (int index = 0; index < rootLevelIds.Length; index += 1)
            {
                if (!progress.IsLevelUnlocked(rootLevelIds[index]))
                {
                    return false;
                }
            }

            for (int index = 0; index < progress.UnlockedLevelIds.Count; index += 1)
            {
                if (!levels.ContainsKey(progress.UnlockedLevelIds[index]))
                {
                    return false;
                }
            }

            for (int index = 0; index < progress.Levels.Count; index += 1)
            {
                if (!levels.ContainsKey(progress.Levels[index].LevelId))
                {
                    return false;
                }
            }

            for (int index = 0; index < progress.UnlockedFeatureIds.Count; index += 1)
            {
                if (!configuredFeatureIds.Contains(progress.UnlockedFeatureIds[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
