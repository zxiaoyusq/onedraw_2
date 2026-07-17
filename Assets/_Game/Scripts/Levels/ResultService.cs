using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Levels
{
    // 定义 RewardGrantType 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum RewardGrantType
    {
        UnlockLevel = 0,
        UnlockFeature = 1,
        ScoreToken = 2,
    }

    // 定义 RewardGrant 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct RewardGrant
    {
        // 初始化 RewardGrant，并建立关卡流程所需的初始状态。
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

    // 定义 SettlementApplyStatus 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum SettlementApplyStatus
    {
        Applied = 0,
        Duplicate = 1,
    }

    // 定义 ResultRequest 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct ResultRequest
    {
        // 初始化 ResultRequest，并建立关卡流程所需的初始状态。
        public ResultRequest(
            string settlementId,
            string levelId,
            BattleSettlement settlement,
            in BattleResultMetrics metrics)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(settlementId) ||
                !string.Equals(settlementId, settlementId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Settlement id must be non-empty and trimmed.",
                    nameof(settlementId));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(levelId) ||
                !string.Equals(levelId, levelId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Level id must be non-empty and trimmed.",
                    nameof(levelId));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

    // 定义 ResultReceipt 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class ResultReceipt
    {
        // 初始化 ResultReceipt，并建立关卡流程所需的初始状态。
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

    // 定义 ResultService 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class ResultService : ITutorialCompletionProgress
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
        private readonly HashSet<string> configuredTutorialIds;
        private readonly string[] rootLevelIds;

        public event Action<ResultReceipt> ReceiptPublished;

        // 初始化 ResultService，并建立关卡流程所需的初始状态。
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
            configuredTutorialIds = BuildConfiguredTutorialIds(levels.Values);
            ProgressSnapshot initial = ProgressSnapshot.Empty(rootLevelIds);

            ProgressLoadResult loaded = store.TryRead(out string payload)
                ? codec.Decode(payload, initial)
                : codec.Decode(null, initial);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if ((loaded.Status == ProgressLoadStatus.Loaded ||
                 loaded.Status == ProgressLoadStatus.Migrated) &&
                !IsCatalogCompatible(loaded.Progress))
            {
                loaded = ProgressSaveCodec.RecoverInvalidCatalog(initial, "save_catalog_mismatch");
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (loaded.Status == ProgressLoadStatus.Migrated)
            {
                store.Write(codec.Encode(loaded.Progress));
            }

            LoadResult = loaded;
            Current = loaded.Progress;
        }

        public ProgressLoadResult LoadResult { get; }

        public ProgressSnapshot Current { get; private set; }

        // 判断是否 IsTutorialCompleted 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool IsTutorialCompleted(string tutorialId)
        {
            RequireConfiguredTutorial(tutorialId);
            return Current.IsTutorialCompleted(tutorialId);
        }

        // 处理 MarkTutorialCompleted 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool MarkTutorialCompleted(string tutorialId)
        {
            RequireConfiguredTutorial(tutorialId);
            ProgressSnapshot next = Current.CompleteTutorial(tutorialId);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (ReferenceEquals(next, Current))
            {
                return false;
            }

            store.Write(codec.Encode(next));
            Current = next;
            return true;
        }

        // 设置 Settle 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public ResultReceipt Settle(in ResultRequest request)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!levels.TryGetValue(request.LevelId, out LevelConfig level))
            {
                throw new ArgumentException(
                    $"Unknown level '{request.LevelId}'.",
                    nameof(request));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (Current.HasAppliedSettlement(request.SettlementId))
            {
                ResultReceipt duplicate = CreateReceipt(
                    SettlementApplyStatus.Duplicate,
                    request,
                    level,
                    score,
                    Array.Empty<RewardGrant>(),
                    Current);
                ReceiptPublished?.Invoke(duplicate);
                return duplicate;
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
            ResultReceipt receipt = CreateReceipt(
                SettlementApplyStatus.Applied,
                request,
                level,
                score,
                grants,
                next);
            ReceiptPublished?.Invoke(receipt);
            return receipt;
        }

        // 创建 CreateReceipt 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
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

        // 评估 EvaluateRewards 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private IReadOnlyList<RewardGrant> EvaluateRewards(
            LevelConfig level,
            in ResultScoreBreakdown score)
        {
            IReadOnlyList<RewardConfig> rows = configProvider.GetRewards(level.RewardTableId);
            var grants = new List<RewardGrant>(rows.Count);
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < rows.Count; index += 1)
            {
                RewardConfig row = rows[index];
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!ConditionMet(row, score))
                {
                    continue;
                }

                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (row.Amount <= 0L || string.IsNullOrWhiteSpace(row.RewardId))
                {
                    throw InvalidReward(row, "Reward id and amount must be positive.");
                }

                RewardGrantType type;
                // 按当前流程、事件或奖励类型选择对应处理分支。
                switch (row.RewardType)
                {
                    case UnlockLevelReward:
                        // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                        if (row.Amount != 1L)
                        {
                            throw InvalidReward(row, "UnlockLevel amount must be 1.");
                        }

                        // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                        if (!levels.ContainsKey(row.RewardId))
                        {
                            throw InvalidReward(row, $"Unknown level reward '{row.RewardId}'.");
                        }

                        type = RewardGrantType.UnlockLevel;
                        break;
                    case UnlockFeatureReward:
                        // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

        // 处理 ConditionMet 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool ConditionMet(RewardConfig row, in ResultScoreBreakdown score)
        {
            // 按当前流程、事件或奖励类型选择对应处理分支。
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

        // 处理 ParseThreshold 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static long ParseThreshold(RewardConfig row)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

        // 处理 InvalidReward 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static InvalidOperationException InvalidReward(
            RewardConfig row,
            string message)
        {
            return new InvalidOperationException(
                $"Reward table '{row.RewardTableId}' order {row.Order}: {message}");
        }

        // 构建 BuildLevelCatalog 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static Dictionary<string, LevelConfig> BuildLevelCatalog(
            IReadOnlyList<LevelConfig> configuredLevels)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (configuredLevels == null || configuredLevels.Count == 0)
            {
                throw new ArgumentException("At least one configured level is required.");
            }

            var result = new Dictionary<string, LevelConfig>(StringComparer.Ordinal);
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < configuredLevels.Count; index += 1)
            {
                LevelConfig level = configuredLevels[index];
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (level == null || string.IsNullOrWhiteSpace(level.LevelId) ||
                    !result.TryAdd(level.LevelId, level))
                {
                    throw new ArgumentException("Level catalog contains an invalid or duplicate id.");
                }
            }

            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            foreach (LevelConfig level in result.Values)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!string.IsNullOrEmpty(level.NextLevelId) &&
                    !result.ContainsKey(level.NextLevelId))
                {
                    throw new ArgumentException(
                        $"Level '{level.LevelId}' references unknown next level '{level.NextLevelId}'.");
                }
            }

            return result;
        }

        // 处理 FindRootLevelIds 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
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
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (roots.Length == 0)
            {
                throw new ArgumentException("Level graph must have at least one root.");
            }

            var byId = levels.ToDictionary(level => level.LevelId, StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex += 1)
            {
                string current = roots[rootIndex];
                var currentPath = new HashSet<string>(StringComparer.Ordinal);
                // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
                while (!string.IsNullOrEmpty(current))
                {
                    // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                    if (!currentPath.Add(current))
                    {
                        throw new ArgumentException($"Level graph cycles at '{current}'.");
                    }

                    // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                    if (!visited.Add(current))
                    {
                        break;
                    }

                    current = byId[current].NextLevelId;
                }
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (visited.Count != levels.Length)
            {
                throw new ArgumentException("Level graph contains an unreachable cycle.");
            }

            return roots;
        }

        // 构建 BuildConfiguredFeatureIds 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static HashSet<string> BuildConfiguredFeatureIds(
            IConfigProvider configProvider,
            IEnumerable<LevelConfig> configuredLevels)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            foreach (LevelConfig level in configuredLevels)
            {
                IReadOnlyList<RewardConfig> rewards =
                    configProvider.GetRewards(level.RewardTableId);
                // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
                for (int index = 0; index < rewards.Count; index += 1)
                {
                    RewardConfig reward = rewards[index];
                    // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

        // 构建 BuildConfiguredTutorialIds 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static HashSet<string> BuildConfiguredTutorialIds(
            IEnumerable<LevelConfig> configuredLevels)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            foreach (LevelConfig level in configuredLevels)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!string.IsNullOrWhiteSpace(level.TutorialId))
                {
                    result.Add(level.TutorialId);
                }
            }

            return result;
        }

        // 处理 RequireConfiguredTutorial 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void RequireConfiguredTutorial(string tutorialId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(tutorialId) ||
                !string.Equals(tutorialId, tutorialId.Trim(), StringComparison.Ordinal) ||
                !configuredTutorialIds.Contains(tutorialId))
            {
                throw new ArgumentException(
                    $"Unknown tutorial '{tutorialId}'.",
                    nameof(tutorialId));
            }
        }

        // 判断是否 IsCatalogCompatible 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private bool IsCatalogCompatible(ProgressSnapshot progress)
        {
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < rootLevelIds.Length; index += 1)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!progress.IsLevelUnlocked(rootLevelIds[index]))
                {
                    return false;
                }
            }

            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < progress.UnlockedLevelIds.Count; index += 1)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!levels.ContainsKey(progress.UnlockedLevelIds[index]))
                {
                    return false;
                }
            }

            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < progress.Levels.Count; index += 1)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!levels.ContainsKey(progress.Levels[index].LevelId))
                {
                    return false;
                }
            }

            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < progress.UnlockedFeatureIds.Count; index += 1)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!configuredFeatureIds.Contains(progress.UnlockedFeatureIds[index]))
                {
                    return false;
                }
            }

            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < progress.CompletedTutorialIds.Count; index += 1)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!configuredTutorialIds.Contains(progress.CompletedTutorialIds[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
