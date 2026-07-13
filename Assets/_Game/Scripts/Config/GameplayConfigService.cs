using System;
using System.Collections.Generic;

namespace OneStrokeDemon.Config
{
    public enum GameplayConfigServiceState
    {
        Uninitialized,
        Ready,
        Failed,
    }

    public sealed class GameplayConfigService : IConfigProvider
    {
        private GameplayConfigSnapshot snapshot;

        public GameplayConfigServiceState State { get; private set; } = GameplayConfigServiceState.Uninitialized;

        public GameplayConfigLoadSummary Summary { get; private set; }

        public long SchemaVersion => RequireSnapshot().Document.SchemaVersion;

        public string ContentVersion => RequireSnapshot().Document.ContentVersion;

        public string ContentHash => RequireSnapshot().Document.ContentHash;

        public GameplayConfigLoadSummary Load(string json, string source)
        {
            if (State != GameplayConfigServiceState.Uninitialized)
            {
                throw new GameplayConfigException(
                    "CFGRT001",
                    $"A service instance may load exactly once; current state is {State}.",
                    source ?? string.Empty,
                    "lifecycle");
            }

            try
            {
                ParsedGameplayConfig parsed = GameplayConfigParser.Parse(json, source);
                GameplayConfigCompatibility.Validate(parsed.Document, source);
                GameplayConfigSnapshot candidate = GameplayConfigSnapshot.Create(parsed.Document, source);
                ValidateRootGlobalVersions(candidate, source);
                GameplayConfigHash.Verify(parsed.Root, parsed.Document.ContentHash, source);

                var summary = new GameplayConfigLoadSummary(
                    source,
                    parsed.Document.SchemaVersion,
                    parsed.Document.ContentVersion,
                    parsed.Document.ContentHash,
                    parsed.Document.RecordCount,
                    candidate.PrimaryIndexCount,
                    candidate.GroupIndexCount);

                snapshot = candidate;
                Summary = summary;
                State = GameplayConfigServiceState.Ready;
                return summary;
            }
            catch
            {
                State = GameplayConfigServiceState.Failed;
                throw;
            }
        }

        public GlobalConfig GetGlobal(string key) => Get(RequireSnapshot().Globals, "Global", "key", key);
        public PlayerConfig GetPlayer(string playerId) => Get(RequireSnapshot().Players, "Players", "playerId", playerId);
        public StanceConfig GetStance(string stanceId) => Get(RequireSnapshot().Stances, "Stances", "stanceId", stanceId);
        public StrokeRuleConfig GetStrokeRule(string ruleId) => Get(RequireSnapshot().StrokeRules, "StrokeRules", "ruleId", ruleId);
        public IReadOnlyList<StrokeRuleConfig> GetStrokeRules() => RequireSnapshot().StrokeRuleEntries;
        public DamageFormulaConfig GetDamageFormula(string formulaId) => Get(RequireSnapshot().DamageFormulas, "DamageFormulas", "formulaId", formulaId);
        public DefenseRuleConfig GetDefenseRule(string defenseRuleId) => Get(RequireSnapshot().DefenseRules, "DefenseRules", "defenseRuleId", defenseRuleId);
        public WeakpointRuleConfig GetWeakpointRule(string weakpointRuleId) => Get(RequireSnapshot().WeakpointRules, "WeakpointRules", "weakpointRuleId", weakpointRuleId);
        public MovePatternConfig GetMovePattern(string movePatternId) => Get(RequireSnapshot().MovePatterns, "MovePatterns", "movePatternId", movePatternId);
        public EnemyConfig GetEnemy(string enemyId) => Get(RequireSnapshot().Enemies, "Enemies", "enemyId", enemyId);
        public IReadOnlyList<EnemyConfig> GetEnemies() => RequireSnapshot().EnemyEntries;
        public IReadOnlyList<EnemyAttackConfig> GetEnemyAttacks(string attackSetId) => GetGroup(RequireSnapshot().AttacksBySet, "EnemyAttacks", "attackSetId", attackSetId);
        public ProjectileConfig GetProjectile(string projectileId) => Get(RequireSnapshot().Projectiles, "Projectiles", "projectileId", projectileId);
        public BuffConfig GetBuff(string buffId) => Get(RequireSnapshot().Buffs, "Buffs", "buffId", buffId);
        public SkillConfig GetSkill(string skillId) => Get(RequireSnapshot().Skills, "Skills", "skillId", skillId);
        public IReadOnlyList<SkillEffectConfig> GetSkillEffects(string effectGroupId) => GetGroup(RequireSnapshot().EffectsByGroup, "SkillEffects", "effectGroupId", effectGroupId);
        public LevelConfig GetLevel(string levelId) => Get(RequireSnapshot().Levels, "Levels", "levelId", levelId);
        public IReadOnlyList<WaveConfig> GetWaves(string levelId) => GetGroup(RequireSnapshot().WavesByLevel, "Waves", "levelId", levelId);
        public SpawnPointConfig GetSpawnPoint(string spawnPointId) => Get(RequireSnapshot().SpawnPoints, "SpawnPoints", "spawnPointId", spawnPointId);
        public EnemyModifierConfig GetEnemyModifier(string modifierId) => Get(RequireSnapshot().EnemyModifiers, "EnemyModifiers", "modifierId", modifierId);
        public IReadOnlyList<SpawnConfig> GetSpawns(string waveId) => GetGroup(RequireSnapshot().SpawnsByWave, "Spawns", "waveId", waveId);
        public IReadOnlyList<BossPhaseConfig> GetBossPhases(string enemyId) => GetGroup(RequireSnapshot().PhasesByEnemy, "BossPhases", "enemyId", enemyId);
        public IReadOnlyList<RewardConfig> GetRewards(string rewardTableId) => GetGroup(RequireSnapshot().RewardsByTable, "Rewards", "rewardTableId", rewardTableId);
        public IReadOnlyList<TutorialConfig> GetTutorialSteps(string tutorialId) => GetGroup(RequireSnapshot().TutorialsById, "Tutorials", "tutorialId", tutorialId);
        public TextConfig GetText(string textKey) => Get(RequireSnapshot().Texts, "Texts", "textKey", textKey);
        public AudioCueConfig GetAudioCue(string audioKey) => Get(RequireSnapshot().AudioCues, "AudioCues", "audioKey", audioKey);
        public VfxCueConfig GetVfxCue(string vfxKey) => Get(RequireSnapshot().VfxCues, "VfxCues", "vfxKey", vfxKey);
        public AssetManifestConfig GetAsset(string assetKey) => Get(RequireSnapshot().Assets, "AssetManifest", "assetKey", assetKey);
        public IReadOnlyList<AssetManifestConfig> GetAssetManifest() => RequireSnapshot().AssetManifestEntries;

        private static void ValidateRootGlobalVersions(GameplayConfigSnapshot candidate, string source)
        {
            if (!candidate.Globals.TryGetValue("config_schema_version", out GlobalConfig schemaRow) ||
                schemaRow.ValueType != "int" || schemaRow.IntValue != candidate.Document.SchemaVersion)
            {
                throw new GameplayConfigException(
                    "CFGRT003",
                    "Root schemaVersion does not match Global.config_schema_version.",
                    source,
                    "Global.config_schema_version");
            }

            if (!candidate.Globals.TryGetValue("content_version", out GlobalConfig contentRow) ||
                contentRow.ValueType != "string" ||
                !string.Equals(contentRow.StringValue, candidate.Document.ContentVersion, StringComparison.Ordinal))
            {
                throw new GameplayConfigException(
                    "CFGRT004",
                    "Root contentVersion does not match Global.content_version.",
                    source,
                    "Global.content_version");
            }
        }

        private GameplayConfigSnapshot RequireSnapshot()
        {
            if (State != GameplayConfigServiceState.Ready || snapshot == null)
            {
                throw new GameplayConfigException(
                    "CFGRT001",
                    $"Configuration is unavailable while service state is {State}.",
                    Summary?.Source ?? "uninitialized",
                    "lifecycle");
            }

            return snapshot;
        }

        private T Get<T>(IReadOnlyDictionary<string, T> index, string table, string field, string id)
        {
            if (id != null && index.TryGetValue(id, out T value))
            {
                return value;
            }

            throw UnknownId(table, field, id);
        }

        private IReadOnlyList<T> GetGroup<T>(
            IReadOnlyDictionary<string, IReadOnlyList<T>> index,
            string table,
            string field,
            string id)
        {
            if (id != null && index.TryGetValue(id, out IReadOnlyList<T> value))
            {
                return value;
            }

            throw UnknownId(table, field, id);
        }

        private GameplayConfigException UnknownId(string table, string field, string id)
        {
            return new GameplayConfigException(
                "CFGRT007",
                $"Unknown configuration ID '{id ?? "<null>"}'.",
                Summary.Source,
                $"{table}.{field}");
        }
    }
}
