using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneStrokeDemon.Levels
{
    public interface IProgressSaveStore
    {
        bool TryRead(out string payload);

        void Write(string payload);
    }

    public interface IProgressSaveMigration
    {
        int SourceVersion { get; }

        int TargetVersion { get; }

        JObject Migrate(JObject source);
    }

    public interface ITutorialCompletionProgress
    {
        bool IsTutorialCompleted(string tutorialId);

        bool MarkTutorialCompleted(string tutorialId);
    }

    public enum ProgressLoadStatus
    {
        Missing = 0,
        Loaded = 1,
        Migrated = 2,
        RecoveredCorrupt = 3,
        RecoveredIncompatible = 4,
    }

    public readonly struct LevelProgress
    {
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

    public sealed class ProgressSnapshot
    {
        private readonly Dictionary<string, LevelProgress> levelsById;
        private readonly HashSet<string> unlockedLevelSet;
        private readonly HashSet<string> unlockedFeatureSet;
        private readonly HashSet<string> appliedSettlementSet;
        private readonly HashSet<string> completedTutorialSet;

        internal ProgressSnapshot(
            long revision,
            long scoreTokens,
            IEnumerable<LevelProgress> levels,
            IEnumerable<string> unlockedLevelIds,
            IEnumerable<string> unlockedFeatureIds,
            IEnumerable<string> appliedSettlementIds,
            IEnumerable<string> completedTutorialIds)
        {
            if (revision < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(revision));
            }

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

        public bool IsLevelUnlocked(string levelId)
        {
            return levelId != null && unlockedLevelSet.Contains(levelId);
        }

        public bool IsFeatureUnlocked(string featureId)
        {
            return featureId != null && unlockedFeatureSet.Contains(featureId);
        }

        public bool HasAppliedSettlement(string settlementId)
        {
            return settlementId != null && appliedSettlementSet.Contains(settlementId);
        }

        public bool IsTutorialCompleted(string tutorialId)
        {
            return tutorialId != null && completedTutorialSet.Contains(tutorialId);
        }

        public bool TryGetLevel(string levelId, out LevelProgress progress)
        {
            if (levelId != null && levelsById.TryGetValue(levelId, out progress))
            {
                return true;
            }

            progress = default;
            return false;
        }

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

                for (int index = 0; index < grants.Count; index += 1)
                {
                    RewardGrant grant = grants[index];
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

        internal ProgressSnapshot CompleteTutorial(string tutorialId)
        {
            ValidateId(tutorialId, nameof(tutorialId));
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

        private static LevelProgress[] NormalizeLevels(IEnumerable<LevelProgress> levels)
        {
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            LevelProgress[] result = levels.OrderBy(row => row.LevelId, StringComparer.Ordinal).ToArray();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < result.Length; index += 1)
            {
                LevelProgress row = result[index];
                ValidateId(row.LevelId, nameof(levels));
                if (!ids.Add(row.LevelId))
                {
                    throw new ArgumentException($"Duplicate level progress '{row.LevelId}'.", nameof(levels));
                }

                if (row.BestScore < 0L ||
                    row.BestStars < 0 || row.BestStars > 3 ||
                    row.ClearCount < 0L)
                {
                    throw new ArgumentException($"Invalid progress for level '{row.LevelId}'.", nameof(levels));
                }
            }

            return result;
        }

        private static string[] NormalizeIds(IEnumerable<string> ids, string argumentName)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            string[] result = ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            for (int index = 0; index < result.Length; index += 1)
            {
                ValidateId(result[index], argumentName);
                if (index > 0 && string.Equals(result[index - 1], result[index], StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Duplicate id '{result[index]}'.", argumentName);
                }
            }

            return result;
        }

        private static void ValidateId(string id, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(id) || !string.Equals(id, id.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("IDs must be non-empty and trimmed.", argumentName);
            }
        }
    }

    public sealed class ProgressLoadResult
    {
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

    public sealed class ProgressSaveCodec
    {
        public const int CurrentVersion = 2;

        private readonly Dictionary<int, IProgressSaveMigration> migrations;

        public ProgressSaveCodec(IEnumerable<IProgressSaveMigration> migrations = null)
        {
            this.migrations = new Dictionary<int, IProgressSaveMigration>();
            AddMigration(new VersionOneTutorialMigration(), nameof(migrations));
            if (migrations == null)
            {
                return;
            }

            foreach (IProgressSaveMigration migration in migrations)
            {
                AddMigration(migration, nameof(migrations));
            }
        }

        public ProgressLoadResult Decode(string payload, ProgressSnapshot fallback)
        {
            if (fallback == null)
            {
                throw new ArgumentNullException(nameof(fallback));
            }

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
                if (version > CurrentVersion)
                {
                    return new ProgressLoadResult(
                        ProgressLoadStatus.RecoveredIncompatible,
                        fallback,
                        $"future_version_{version}");
                }

                bool migrated = false;
                while (version < CurrentVersion)
                {
                    if (!migrations.TryGetValue(version, out IProgressSaveMigration migration))
                    {
                        return new ProgressLoadResult(
                            ProgressLoadStatus.RecoveredIncompatible,
                            fallback,
                            $"missing_migration_{version}");
                    }

                    JObject next = migration.Migrate((JObject)root.DeepClone());
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

        public string Encode(ProgressSnapshot progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return JsonConvert.SerializeObject(ToDto(progress), Formatting.None);
        }

        internal static ProgressLoadResult RecoverInvalidCatalog(
            ProgressSnapshot fallback,
            string diagnostic)
        {
            return new ProgressLoadResult(
                ProgressLoadStatus.RecoveredCorrupt,
                fallback,
                diagnostic);
        }

        private static int ReadVersion(JObject root)
        {
            JToken token = root["version"];
            if (token == null || token.Type != JTokenType.Integer)
            {
                throw new JsonSerializationException("Progress save version must be an integer.");
            }

            return token.Value<int>();
        }

        private static ProgressSnapshot FromDto(ProgressSaveDto dto)
        {
            if (dto == null || dto.Version != CurrentVersion)
            {
                throw new JsonSerializationException("Progress save DTO version is invalid.");
            }

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

        private void AddMigration(
            IProgressSaveMigration migration,
            string argumentName)
        {
            if (migration == null ||
                migration.SourceVersion < 0 ||
                migration.TargetVersion <= migration.SourceVersion ||
                migration.TargetVersion > CurrentVersion)
            {
                throw new ArgumentException("Invalid progress migration.", argumentName);
            }

            if (migrations.ContainsKey(migration.SourceVersion))
            {
                throw new ArgumentException(
                    $"Duplicate migration source version {migration.SourceVersion}.",
                    argumentName);
            }

            migrations.Add(migration.SourceVersion, migration);
        }

        [JsonObject(MemberSerialization.OptIn)]
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

        private sealed class VersionOneTutorialMigration : IProgressSaveMigration
        {
            public int SourceVersion => 1;

            public int TargetVersion => 2;

            public JObject Migrate(JObject source)
            {
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
