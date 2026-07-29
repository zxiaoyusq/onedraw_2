using System;
using System.Collections.Generic;

namespace OneStrokeDemon.Config
{
    /// <summary>表示单个配置服务实例的一次性装载状态。</summary>
    public enum GameplayConfigServiceState
    {
        /// <summary>尚未尝试装载。</summary>
        Uninitialized,

        /// <summary>全部验证通过且快照已发布。</summary>
        Ready,

        /// <summary>装载失败，实例不可重试。</summary>
        Failed,
    }

    /// <summary>
    /// 严格装载玩法配置、构建不可变索引，并提供强类型只读查询。
    /// </summary>
    public sealed class GameplayConfigService : IConfigProvider
    {
        private GameplayConfigSnapshot snapshot;

        /// <summary>获取当前一次性装载状态。</summary>
        public GameplayConfigServiceState State { get; private set; } = GameplayConfigServiceState.Uninitialized;

        /// <summary>获取成功装载摘要；未成功时为 null。</summary>
        public GameplayConfigLoadSummary Summary { get; private set; }

        /// <summary>获取已发布配置的结构版本。</summary>
        public long SchemaVersion => RequireSnapshot().Document.SchemaVersion;

        /// <summary>获取已发布配置的内容版本。</summary>
        public string ContentVersion => RequireSnapshot().Document.ContentVersion;

        /// <summary>获取已发布配置的内容哈希。</summary>
        public string ContentHash => RequireSnapshot().Document.ContentHash;

        /// <summary>严格解析并验证一份 JSON；每个服务实例只允许装载一次。</summary>
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
                // 先在局部变量中完成解析、兼容性、结构、版本镜像和哈希的全部验证。
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

                // 只有所有步骤成功后才原子发布快照、摘要和 Ready 状态。
                snapshot = candidate;
                Summary = summary;
                State = GameplayConfigServiceState.Ready;
                return summary;
            }
            catch
            {
                // 失败实例不可重试，防止调用方误以为部分状态仍可安全使用。
                State = GameplayConfigServiceState.Failed;
                throw;
            }
        }

        /// <summary>按键获取全局配置。</summary>
        public GlobalConfig GetGlobal(string key) => Get(RequireSnapshot().Globals, "Global", "key", key);
        /// <summary>按玩家 ID 获取玩家配置。</summary>
        public PlayerConfig GetPlayer(string playerId) => Get(RequireSnapshot().Players, "Players", "playerId", playerId);
        /// <summary>按架势 ID 获取架势配置。</summary>
        public StanceConfig GetStance(string stanceId) => Get(RequireSnapshot().Stances, "Stances", "stanceId", stanceId);
        /// <summary>按规则 ID 获取笔画规则。</summary>
        public StrokeRuleConfig GetStrokeRule(string ruleId) => Get(RequireSnapshot().StrokeRules, "StrokeRules", "ruleId", ruleId);
        /// <summary>获取全部笔画规则的只读列表。</summary>
        public IReadOnlyList<StrokeRuleConfig> GetStrokeRules() => RequireSnapshot().StrokeRuleEntries;
        /// <summary>按公式 ID 获取伤害公式。</summary>
        public DamageFormulaConfig GetDamageFormula(string formulaId) => Get(RequireSnapshot().DamageFormulas, "DamageFormulas", "formulaId", formulaId);
        /// <summary>按防御规则 ID 获取防御配置。</summary>
        public DefenseRuleConfig GetDefenseRule(string defenseRuleId) => Get(RequireSnapshot().DefenseRules, "DefenseRules", "defenseRuleId", defenseRuleId);
        /// <summary>按弱点规则 ID 获取弱点配置。</summary>
        public WeakpointRuleConfig GetWeakpointRule(string weakpointRuleId) => Get(RequireSnapshot().WeakpointRules, "WeakpointRules", "weakpointRuleId", weakpointRuleId);
        /// <summary>按移动模式 ID 获取移动配置。</summary>
        public MovePatternConfig GetMovePattern(string movePatternId) => Get(RequireSnapshot().MovePatterns, "MovePatterns", "movePatternId", movePatternId);
        /// <summary>按敌人 ID 获取敌人配置。</summary>
        public EnemyConfig GetEnemy(string enemyId) => Get(RequireSnapshot().Enemies, "Enemies", "enemyId", enemyId);
        /// <summary>获取全部敌人配置的只读列表。</summary>
        public IReadOnlyList<EnemyConfig> GetEnemies() => RequireSnapshot().EnemyEntries;
        /// <summary>按攻击集合 ID 获取有序攻击配置。</summary>
        public IReadOnlyList<EnemyAttackConfig> GetEnemyAttacks(string attackSetId) => GetGroup(RequireSnapshot().AttacksBySet, "EnemyAttacks", "attackSetId", attackSetId);
        /// <summary>按投射物 ID 获取投射物配置。</summary>
        public ProjectileConfig GetProjectile(string projectileId) => Get(RequireSnapshot().Projectiles, "Projectiles", "projectileId", projectileId);
        /// <summary>按增益 ID 获取增益配置。</summary>
        public BuffConfig GetBuff(string buffId) => Get(RequireSnapshot().Buffs, "Buffs", "buffId", buffId);
        /// <summary>按技能 ID 获取技能配置。</summary>
        public SkillConfig GetSkill(string skillId) => Get(RequireSnapshot().Skills, "Skills", "skillId", skillId);
        /// <summary>按效果组 ID 获取有序技能效果。</summary>
        public IReadOnlyList<SkillEffectConfig> GetSkillEffects(string effectGroupId) => GetGroup(RequireSnapshot().EffectsByGroup, "SkillEffects", "effectGroupId", effectGroupId);
        /// <summary>按关卡 ID 获取关卡配置。</summary>
        public LevelConfig GetLevel(string levelId) => Get(RequireSnapshot().Levels, "Levels", "levelId", levelId);
        /// <summary>获取全部关卡配置的只读列表。</summary>
        public IReadOnlyList<LevelConfig> GetLevels() => RequireSnapshot().LevelEntries;
        /// <summary>按关卡 ID 获取有序波次配置。</summary>
        public IReadOnlyList<WaveConfig> GetWaves(string levelId) => GetGroup(RequireSnapshot().WavesByLevel, "Waves", "levelId", levelId);
        /// <summary>按出生点 ID 获取出生点配置。</summary>
        public SpawnPointConfig GetSpawnPoint(string spawnPointId) => Get(RequireSnapshot().SpawnPoints, "SpawnPoints", "spawnPointId", spawnPointId);
        /// <summary>按修饰器 ID 获取敌人修饰配置。</summary>
        public EnemyModifierConfig GetEnemyModifier(string modifierId) => Get(RequireSnapshot().EnemyModifiers, "EnemyModifiers", "modifierId", modifierId);
        /// <summary>按波次 ID 获取有序出生配置。</summary>
        public IReadOnlyList<SpawnConfig> GetSpawns(string waveId) => GetGroup(RequireSnapshot().SpawnsByWave, "Spawns", "waveId", waveId);
        /// <summary>按敌人 ID 获取有序 Boss 阶段配置。</summary>
        public IReadOnlyList<BossPhaseConfig> GetBossPhases(string enemyId) => GetGroup(RequireSnapshot().PhasesByEnemy, "BossPhases", "enemyId", enemyId);
        /// <summary>按奖励表 ID 获取有序奖励配置。</summary>
        public IReadOnlyList<RewardConfig> GetRewards(string rewardTableId) => GetGroup(RequireSnapshot().RewardsByTable, "Rewards", "rewardTableId", rewardTableId);
        /// <summary>按教程 ID 获取有序教程步骤。</summary>
        public IReadOnlyList<TutorialConfig> GetTutorialSteps(string tutorialId) => GetGroup(RequireSnapshot().TutorialsById, "Tutorials", "tutorialId", tutorialId);
        /// <summary>按文本键获取本地化文本配置。</summary>
        public TextConfig GetText(string textKey) => Get(RequireSnapshot().Texts, "Texts", "textKey", textKey);
        /// <summary>按音频键获取音频提示配置。</summary>
        public AudioCueConfig GetAudioCue(string audioKey) => Get(RequireSnapshot().AudioCues, "AudioCues", "audioKey", audioKey);
        /// <summary>按特效键获取视觉特效提示配置。</summary>
        public VfxCueConfig GetVfxCue(string vfxKey) => Get(RequireSnapshot().VfxCues, "VfxCues", "vfxKey", vfxKey);
        /// <summary>按反馈 ID 获取组合战斗反馈配置。</summary>
        public FeedbackCueConfig GetFeedbackCue(string feedbackId) => Get(RequireSnapshot().FeedbackCues, "FeedbackCues", "feedbackId", feedbackId);
        /// <summary>按样式 ID 获取画笔表现配置。</summary>
        public StrokeTrailStyleConfig GetStrokeTrailStyle(string styleId) =>
            Get(RequireSnapshot().StrokeTrailStyles, "StrokeTrailStyles", "styleId", styleId);
        /// <summary>按资源键获取资源清单配置。</summary>
        public AssetManifestConfig GetAsset(string assetKey) => Get(RequireSnapshot().Assets, "AssetManifest", "assetKey", assetKey);
        /// <summary>获取完整资源清单的只读列表。</summary>
        public IReadOnlyList<AssetManifestConfig> GetAssetManifest() => RequireSnapshot().AssetManifestEntries;

        /// <summary>验证根版本字段与 Global 表中的镜像值完全一致。</summary>
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

        /// <summary>返回可查询快照；服务未就绪时抛出生命周期异常。</summary>
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

        /// <summary>在主键索引中查询记录，并把未知 ID 转换为统一配置异常。</summary>
        private T Get<T>(IReadOnlyDictionary<string, T> index, string table, string field, string id)
        {
            if (id != null && index.TryGetValue(id, out T value))
            {
                return value;
            }

            throw UnknownId(table, field, id);
        }

        /// <summary>在分组索引中查询只读记录列表，并把未知 ID 转换为统一配置异常。</summary>
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

        /// <summary>创建包含表名、字段名和请求 ID 的未知配置异常。</summary>
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
