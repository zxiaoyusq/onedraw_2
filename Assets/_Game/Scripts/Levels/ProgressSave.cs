using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneStrokeDemon.Levels
{
    // 定义 IProgressSaveStore 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public interface IProgressSaveStore
    {
        bool TryRead(out string payload);

        void Write(string payload);
    }

    // 定义 IProgressSaveMigration 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public interface IProgressSaveMigration
    {
        int SourceVersion { get; }

        int TargetVersion { get; }

        JObject Migrate(JObject source);
    }

    // 定义 ITutorialCompletionProgress 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public interface ITutorialCompletionProgress
    {
        bool IsTutorialCompleted(string tutorialId);

        bool MarkTutorialCompleted(string tutorialId);
    }

    // 定义 ProgressLoadStatus 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum ProgressLoadStatus
    {
        Missing = 0,
        Loaded = 1,
        Migrated = 2,
        RecoveredCorrupt = 3,
        RecoveredIncompatible = 4,
    }

    // 定义 LevelProgress 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct LevelProgress
    {
        // 初始化 LevelProgress，并建立关卡流程所需的初始状态。
        internal LevelProgress(
            string levelId,
            long bestScore,
            int bestStars,
            long clearCount)
        {
            LevelId = levelId;
            BestScore = bestScore;
            BestStars = bestStars;
            ClearCount = clearCount;
        }

        public string LevelId { get; }

        public long BestScore { get; }

        public int BestStars { get; }

        public long ClearCount { get; }
    }

    // 定义 ProgressSnapshot 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class ProgressSnapshot
    {
        private readonly Dictionary<string, LevelProgress> levelsById;
        private readonly HashSet<string> unlockedLevelSet;
        private readonly HashSet<string> unlockedFeatureSet;
        private readonly HashSet<string> appliedSettlementSet;
        private readonly HashSet<string> completedTutorialSet;

        // 初始化 ProgressSnapshot，并建立关卡流程所需的初始状态。
        internal ProgressSnapshot(
            long revision,
            long scoreTokens,
            IEnumerable<LevelProgress> levels,
            IEnumerable<string> unlockedLevelIds,
            IEnumerable<string> unlockedFeatureIds,
            IEnumerable<string> appliedSettlementIds,
            IEnumerable<string> completedTutorialIds)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (revision < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(revision));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (scoreTokens < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(scoreTokens));
            }

            Revision = revision;
            ScoreTokens = scoreTokens;
            LevelProgress[] levelArray = NormalizeLevels(levels);
            string[] unlockedLevels = NormalizeIds(unlockedLevelIds, nameof(unlockedLevelIds));
            string[] unlockedFeatures = NormalizeIds(unlockedFeatureIds, nameof(unlockedFeatureIds));
            string[] appliedSettlements = NormalizeIds(appliedSettlementIds, nameof(appliedSettlementIds));
            string[] completedTutorials = NormalizeIds(completedTutorialIds, nameof(completedTutorialIds));
            Levels = new ReadOnlyCollection<LevelProgress>(levelArray);
            UnlockedLevelIds = new ReadOnlyCollection<string>(unlockedLevels);
            UnlockedFeatureIds = new ReadOnlyCollection<string>(unlockedFeatures);
            AppliedSettlementIds = new ReadOnlyCollection<string>(appliedSettlements);
            CompletedTutorialIds = new ReadOnlyCollection<string>(completedTutorials);
            levelsById = levelArray.ToDictionary(row => row.LevelId, StringComparer.Ordinal);
            unlockedLevelSet = new HashSet<string>(unlockedLevels, StringComparer.Ordinal);
            unlockedFeatureSet = new HashSet<string>(unlockedFeatures, StringComparer.Ordinal);
            appliedSettlementSet = new HashSet<string>(appliedSettlements, StringComparer.Ordinal);
            completedTutorialSet = new HashSet<string>(completedTutorials, StringComparer.Ordinal);
        }

        public long Revision { get; }

        public long ScoreTokens { get; }

        public IReadOnlyList<LevelProgress> Levels { get; }

        public IReadOnlyList<string> UnlockedLevelIds { get; }

        public IReadOnlyList<string> UnlockedFeatureIds { get; }

        public IReadOnlyList<string> AppliedSettlementIds { get; }

        public IReadOnlyList<string> CompletedTutorialIds { get; }

        // 判断是否 IsLevelUnlocked 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool IsLevelUnlocked(string levelId)
        {
            return levelId != null && unlockedLevelSet.Contains(levelId);
        }

        // 判断是否 IsFeatureUnlocked 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool IsFeatureUnlocked(string featureId)
        {
            return featureId != null && unlockedFeatureSet.Contains(featureId);
        }

        // 处理 HasAppliedSettlement 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool HasAppliedSettlement(string settlementId)
        {
            return settlementId != null && appliedSettlementSet.Contains(settlementId);
        }

        // 判断是否 IsTutorialCompleted 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool IsTutorialCompleted(string tutorialId)
        {
            return tutorialId != null && completedTutorialSet.Contains(tutorialId);
        }

        // 尝试执行 TryGetLevel 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool TryGetLevel(string levelId, out LevelProgress progress)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (levelId != null && levelsById.TryGetValue(levelId, out progress))
            {
                return true;
            }

            progress = default;
            return false;
        }

        // 处理 Empty 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal static ProgressSnapshot Empty(IEnumerable<string> rootLevelIds)
        {
            return new ProgressSnapshot(
                0L,
                0L,
                Array.Empty<LevelProgress>(),
                rootLevelIds,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        // 应用 Apply 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal ProgressSnapshot Apply(
            string settlementId,
            string levelId,
            BattleSettlement settlement,
            in ResultScoreBreakdown score,
            IReadOnlyList<RewardGrant> grants)
        {
            var nextLevels = new Dictionary<string, LevelProgress>(levelsById, StringComparer.Ordinal);
            var nextUnlockedLevels = new HashSet<string>(unlockedLevelSet, StringComparer.Ordinal);
            var nextUnlockedFeatures = new HashSet<string>(unlockedFeatureSet, StringComparer.Ordinal);
            var nextAppliedSettlements = new HashSet<string>(appliedSettlementSet, StringComparer.Ordinal)
            {
                settlementId,
            };
            long nextScoreTokens = ScoreTokens;

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (settlement == BattleSettlement.Victory)
            {
                nextLevels.TryGetValue(levelId, out LevelProgress existing);
                long clearCount;
                checked
                {
                    clearCount = existing.ClearCount + 1L;
                }

                nextLevels[levelId] = new LevelProgress(
                    levelId,
                    Math.Max(existing.BestScore, score.FinalScore),
                    Math.Max(existing.BestStars, score.Stars),
                    clearCount);

                // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
                for (int index = 0; index < grants.Count; index += 1)
                {
                    RewardGrant grant = grants[index];
                    // 按当前流程、事件或奖励类型选择对应处理分支。
                    switch (grant.Type)
                    {
                        case RewardGrantType.UnlockLevel:
                            nextUnlockedLevels.Add(grant.RewardId);
                            break;
                        case RewardGrantType.UnlockFeature:
                            nextUnlockedFeatures.Add(grant.RewardId);
                            break;
                        case RewardGrantType.ScoreToken:
                            checked
                            {
                                nextScoreTokens += grant.Amount;
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(grants));
                    }
                }
            }

            long nextRevision;
            checked
            {
                nextRevision = Revision + 1L;
            }

            return new ProgressSnapshot(
                nextRevision,
                nextScoreTokens,
                nextLevels.Values,
                nextUnlockedLevels,
                nextUnlockedFeatures,
                nextAppliedSettlements,
                completedTutorialSet);
        }

        // 完成 CompleteTutorial 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal ProgressSnapshot CompleteTutorial(string tutorialId)
        {
            ValidateId(tutorialId, nameof(tutorialId));
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (completedTutorialSet.Contains(tutorialId))
            {
                return this;
            }

            var nextCompletedTutorials = new HashSet<string>(
                completedTutorialSet,
                StringComparer.Ordinal)
            {
                tutorialId,
            };
            long nextRevision;
            checked
            {
                nextRevision = Revision + 1L;
            }

            return new ProgressSnapshot(
                nextRevision,
                ScoreTokens,
                Levels,
                UnlockedLevelIds,
                UnlockedFeatureIds,
                AppliedSettlementIds,
                nextCompletedTutorials);
        }

        // 处理 NormalizeLevels 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static LevelProgress[] NormalizeLevels(IEnumerable<LevelProgress> levels)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            LevelProgress[] result = levels.OrderBy(row => row.LevelId, StringComparer.Ordinal).ToArray();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < result.Length; index += 1)
            {
                LevelProgress row = result[index];
                ValidateId(row.LevelId, nameof(levels));
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!ids.Add(row.LevelId))
                {
                    throw new ArgumentException($"Duplicate level progress '{row.LevelId}'.", nameof(levels));
                }

                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (row.BestScore < 0L ||
                    row.BestStars < 0 || row.BestStars > 3 ||
                    row.ClearCount < 0L)
                {
                    throw new ArgumentException($"Invalid progress for level '{row.LevelId}'.", nameof(levels));
                }
            }

            return result;
        }

        // 处理 NormalizeIds 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static string[] NormalizeIds(IEnumerable<string> ids, string argumentName)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (ids == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            string[] result = ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < result.Length; index += 1)
            {
                ValidateId(result[index], argumentName);
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (index > 0 && string.Equals(result[index - 1], result[index], StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Duplicate id '{result[index]}'.", argumentName);
                }
            }

            return result;
        }

        // 校验 ValidateId 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void ValidateId(string id, string argumentName)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(id) || !string.Equals(id, id.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("IDs must be non-empty and trimmed.", argumentName);
            }
        }
    }

    // 定义 ProgressLoadResult 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class ProgressLoadResult
    {
        // 初始化 ProgressLoadResult，并建立关卡流程所需的初始状态。
        internal ProgressLoadResult(
            ProgressLoadStatus status,
            ProgressSnapshot progress,
            string diagnostic)
        {
            Status = status;
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Diagnostic = diagnostic ?? string.Empty;
        }

        public ProgressLoadStatus Status { get; }

        public ProgressSnapshot Progress { get; }

        public string Diagnostic { get; }
    }

    // 定义 ProgressSaveCodec 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class ProgressSaveCodec
    {
        public const int CurrentVersion = 2;

        private readonly Dictionary<int, IProgressSaveMigration> migrations;

        // 初始化 ProgressSaveCodec，并建立关卡流程所需的初始状态。
        public ProgressSaveCodec(IEnumerable<IProgressSaveMigration> migrations = null)
        {
            this.migrations = new Dictionary<int, IProgressSaveMigration>();
            AddMigration(new VersionOneTutorialMigration(), nameof(migrations));
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (migrations == null)
            {
                return;
            }

            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            foreach (IProgressSaveMigration migration in migrations)
            {
                AddMigration(migration, nameof(migrations));
            }
        }

        // 处理 Decode 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public ProgressLoadResult Decode(string payload, ProgressSnapshot fallback)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (fallback == null)
            {
                throw new ArgumentNullException(nameof(fallback));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new ProgressLoadResult(ProgressLoadStatus.Missing, fallback, "save_missing");
            }

            try
            {
                JObject root = JObject.Parse(
                    payload,
                    new JsonLoadSettings
                    {
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    });
                int version = ReadVersion(root);
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (version > CurrentVersion)
                {
                    return new ProgressLoadResult(
                        ProgressLoadStatus.RecoveredIncompatible,
                        fallback,
                        $"future_version_{version}");
                }

                bool migrated = false;
                // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
                while (version < CurrentVersion)
                {
                    // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                    if (!migrations.TryGetValue(version, out IProgressSaveMigration migration))
                    {
                        return new ProgressLoadResult(
                            ProgressLoadStatus.RecoveredIncompatible,
                            fallback,
                            $"missing_migration_{version}");
                    }

                    JObject next = migration.Migrate((JObject)root.DeepClone());
                    // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                    if (next == null || ReadVersion(next) != migration.TargetVersion)
                    {
                        throw new JsonSerializationException(
                            $"Migration {migration.SourceVersion}->{migration.TargetVersion} returned an invalid version.");
                    }

                    root = next;
                    version = migration.TargetVersion;
                    migrated = true;
                }

                ProgressSaveDto dto = root.ToObject<ProgressSaveDto>();
                ProgressSnapshot progress = FromDto(dto);
                return new ProgressLoadResult(
                    migrated ? ProgressLoadStatus.Migrated : ProgressLoadStatus.Loaded,
                    progress,
                    migrated ? "migration_complete" : "save_loaded");
            }
            catch (Exception exception) when (
                exception is JsonException ||
                exception is ArgumentException ||
                exception is InvalidOperationException ||
                exception is OverflowException)
            {
                return new ProgressLoadResult(
                    ProgressLoadStatus.RecoveredCorrupt,
                    fallback,
                    exception.GetType().Name);
            }
        }

        // 处理 Encode 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public string Encode(ProgressSnapshot progress)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return JsonConvert.SerializeObject(ToDto(progress), Formatting.None);
        }

        // 处理 RecoverInvalidCatalog 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal static ProgressLoadResult RecoverInvalidCatalog(
            ProgressSnapshot fallback,
            string diagnostic)
        {
            return new ProgressLoadResult(
                ProgressLoadStatus.RecoveredCorrupt,
                fallback,
                diagnostic);
        }

        // 处理 ReadVersion 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static int ReadVersion(JObject root)
        {
            JToken token = root["version"];
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (token == null || token.Type != JTokenType.Integer)
            {
                throw new JsonSerializationException("Progress save version must be an integer.");
            }

            return token.Value<int>();
        }

        // 处理 FromDto 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static ProgressSnapshot FromDto(ProgressSaveDto dto)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (dto == null || dto.Version != CurrentVersion)
            {
                throw new JsonSerializationException("Progress save DTO version is invalid.");
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (dto.Levels == null ||
                dto.UnlockedLevelIds == null ||
                dto.UnlockedFeatureIds == null ||
                dto.AppliedSettlementIds == null ||
                dto.CompletedTutorialIds == null)
            {
                throw new JsonSerializationException("Progress save arrays must not be null.");
            }

            return new ProgressSnapshot(
                dto.Revision,
                dto.ScoreTokens,
                dto.Levels.Select(row => row == null
                    ? throw new JsonSerializationException("Progress level row must not be null.")
                    : new LevelProgress(row.LevelId, row.BestScore, row.BestStars, row.ClearCount)),
                dto.UnlockedLevelIds,
                dto.UnlockedFeatureIds,
                dto.AppliedSettlementIds,
                dto.CompletedTutorialIds);
        }

        // 处理 ToDto 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static ProgressSaveDto ToDto(ProgressSnapshot progress)
        {
            return new ProgressSaveDto
            {
                Version = CurrentVersion,
                Revision = progress.Revision,
                ScoreTokens = progress.ScoreTokens,
                Levels = progress.Levels.Select(row => new LevelProgressDto
                {
                    LevelId = row.LevelId,
                    BestScore = row.BestScore,
                    BestStars = row.BestStars,
                    ClearCount = row.ClearCount,
                }).ToArray(),
                UnlockedLevelIds = progress.UnlockedLevelIds.ToArray(),
                UnlockedFeatureIds = progress.UnlockedFeatureIds.ToArray(),
                AppliedSettlementIds = progress.AppliedSettlementIds.ToArray(),
                CompletedTutorialIds = progress.CompletedTutorialIds.ToArray(),
            };
        }

        // 添加 AddMigration 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void AddMigration(
            IProgressSaveMigration migration,
            string argumentName)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (migration == null ||
                migration.SourceVersion < 0 ||
                migration.TargetVersion <= migration.SourceVersion ||
                migration.TargetVersion > CurrentVersion)
            {
                throw new ArgumentException("Invalid progress migration.", argumentName);
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (migrations.ContainsKey(migration.SourceVersion))
            {
                throw new ArgumentException(
                    $"Duplicate migration source version {migration.SourceVersion}.",
                    argumentName);
            }

            migrations.Add(migration.SourceVersion, migration);
        }

        [JsonObject(MemberSerialization.OptIn)]
        // 定义 ProgressSaveDto 的关卡领域契约，用于描述时间线、流程或持久化边界。
        private sealed class ProgressSaveDto
        {
            [JsonProperty("version", Order = 1, Required = Required.Always)]
            public int Version { get; set; }

            [JsonProperty("revision", Order = 2, Required = Required.Always)]
            public long Revision { get; set; }

            [JsonProperty("scoreTokens", Order = 3, Required = Required.Always)]
            public long ScoreTokens { get; set; }

            [JsonProperty("levels", Order = 4, Required = Required.Always)]
            public LevelProgressDto[] Levels { get; set; }

            [JsonProperty("unlockedLevelIds", Order = 5, Required = Required.Always)]
            public string[] UnlockedLevelIds { get; set; }

            [JsonProperty("unlockedFeatureIds", Order = 6, Required = Required.Always)]
            public string[] UnlockedFeatureIds { get; set; }

            [JsonProperty("appliedSettlementIds", Order = 7, Required = Required.Always)]
            public string[] AppliedSettlementIds { get; set; }

            [JsonProperty("completedTutorialIds", Order = 8, Required = Required.Always)]
            public string[] CompletedTutorialIds { get; set; }
        }

        // 定义 VersionOneTutorialMigration 的关卡领域契约，用于描述时间线、流程或持久化边界。
        private sealed class VersionOneTutorialMigration : IProgressSaveMigration
        {
            public int SourceVersion => 1;

            public int TargetVersion => 2;

            // 迁移 Migrate 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
            public JObject Migrate(JObject source)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                source["version"] = TargetVersion;
                source["completedTutorialIds"] = new JArray();
                return source;
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        // 定义 LevelProgressDto 的关卡领域契约，用于描述时间线、流程或持久化边界。
        private sealed class LevelProgressDto
        {
            [JsonProperty("levelId", Order = 1, Required = Required.Always)]
            public string LevelId { get; set; }

            [JsonProperty("bestScore", Order = 2, Required = Required.Always)]
            public long BestScore { get; set; }

            [JsonProperty("bestStars", Order = 3, Required = Required.Always)]
            public int BestStars { get; set; }

            [JsonProperty("clearCount", Order = 4, Required = Required.Always)]
            public long ClearCount { get; set; }
        }
    }
}
