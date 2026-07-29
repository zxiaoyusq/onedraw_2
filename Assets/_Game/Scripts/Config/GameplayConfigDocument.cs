using System;
using Newtonsoft.Json;
using JsonRequired = Newtonsoft.Json.Required;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 严格映射完整玩法配置 JSON 根对象，保留版本、哈希和全部表记录。
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class GameplayConfigDocument
    {
        /// <summary>获取配置结构版本。</summary>
        [JsonProperty("schemaVersion", Required = JsonRequired.Always)]
        public long SchemaVersion { get; private set; }

        /// <summary>获取配置内容版本。</summary>
        [JsonProperty("contentVersion", Required = JsonRequired.Always)]
        public string ContentVersion { get; private set; } = string.Empty;

        /// <summary>获取导出器声明的规范化内容哈希。</summary>
        [JsonProperty("contentHash", Required = JsonRequired.Always)]
        public string ContentHash { get; private set; } = string.Empty;

        // 以下数组与冻结 JSON Schema 的 30 张表一一对应，仅供快照构建器建立只读索引。
        [JsonProperty("global", Required = JsonRequired.Always)] internal GlobalConfig[] GlobalRows { get; private set; } = Array.Empty<GlobalConfig>();
        [JsonProperty("players", Required = JsonRequired.Always)] internal PlayerConfig[] PlayerRows { get; private set; } = Array.Empty<PlayerConfig>();
        [JsonProperty("stances", Required = JsonRequired.Always)] internal StanceConfig[] StanceRows { get; private set; } = Array.Empty<StanceConfig>();
        [JsonProperty("strokeRules", Required = JsonRequired.Always)] internal StrokeRuleConfig[] StrokeRuleRows { get; private set; } = Array.Empty<StrokeRuleConfig>();
        [JsonProperty("damageFormulas", Required = JsonRequired.Always)] internal DamageFormulaConfig[] DamageFormulaRows { get; private set; } = Array.Empty<DamageFormulaConfig>();
        [JsonProperty("defenseRules", Required = JsonRequired.Always)] internal DefenseRuleConfig[] DefenseRuleRows { get; private set; } = Array.Empty<DefenseRuleConfig>();
        [JsonProperty("weakpointRules", Required = JsonRequired.Always)] internal WeakpointRuleConfig[] WeakpointRuleRows { get; private set; } = Array.Empty<WeakpointRuleConfig>();
        [JsonProperty("movePatterns", Required = JsonRequired.Always)] internal MovePatternConfig[] MovePatternRows { get; private set; } = Array.Empty<MovePatternConfig>();
        [JsonProperty("enemies", Required = JsonRequired.Always)] internal EnemyConfig[] EnemyRows { get; private set; } = Array.Empty<EnemyConfig>();
        [JsonProperty("enemyAttacks", Required = JsonRequired.Always)] internal EnemyAttackConfig[] EnemyAttackRows { get; private set; } = Array.Empty<EnemyAttackConfig>();
        [JsonProperty("projectiles", Required = JsonRequired.Always)] internal ProjectileConfig[] ProjectileRows { get; private set; } = Array.Empty<ProjectileConfig>();
        [JsonProperty("buffs", Required = JsonRequired.Always)] internal BuffConfig[] BuffRows { get; private set; } = Array.Empty<BuffConfig>();
        [JsonProperty("skills", Required = JsonRequired.Always)] internal SkillConfig[] SkillRows { get; private set; } = Array.Empty<SkillConfig>();
        [JsonProperty("skillEffects", Required = JsonRequired.Always)] internal SkillEffectConfig[] SkillEffectRows { get; private set; } = Array.Empty<SkillEffectConfig>();
        [JsonProperty("levels", Required = JsonRequired.Always)] internal LevelConfig[] LevelRows { get; private set; } = Array.Empty<LevelConfig>();
        [JsonProperty("waves", Required = JsonRequired.Always)] internal WaveConfig[] WaveRows { get; private set; } = Array.Empty<WaveConfig>();
        [JsonProperty("spawnPoints", Required = JsonRequired.Always)] internal SpawnPointConfig[] SpawnPointRows { get; private set; } = Array.Empty<SpawnPointConfig>();
        [JsonProperty("enemyModifiers", Required = JsonRequired.Always)] internal EnemyModifierConfig[] EnemyModifierRows { get; private set; } = Array.Empty<EnemyModifierConfig>();
        [JsonProperty("spawns", Required = JsonRequired.Always)] internal SpawnConfig[] SpawnRows { get; private set; } = Array.Empty<SpawnConfig>();
        [JsonProperty("bossPhases", Required = JsonRequired.Always)] internal BossPhaseConfig[] BossPhaseRows { get; private set; } = Array.Empty<BossPhaseConfig>();
        [JsonProperty("rewards", Required = JsonRequired.Always)] internal RewardConfig[] RewardRows { get; private set; } = Array.Empty<RewardConfig>();
        [JsonProperty("tutorials", Required = JsonRequired.Always)] internal TutorialConfig[] TutorialRows { get; private set; } = Array.Empty<TutorialConfig>();
        [JsonProperty("texts", Required = JsonRequired.Always)] internal TextConfig[] TextRows { get; private set; } = Array.Empty<TextConfig>();
        [JsonProperty("audioCues", Required = JsonRequired.Always)] internal AudioCueConfig[] AudioCueRows { get; private set; } = Array.Empty<AudioCueConfig>();
        [JsonProperty("vfxCues", Required = JsonRequired.Always)] internal VfxCueConfig[] VfxCueRows { get; private set; } = Array.Empty<VfxCueConfig>();
        [JsonProperty("assetManifest", Required = JsonRequired.Always)] internal AssetManifestConfig[] AssetManifestRows { get; private set; } = Array.Empty<AssetManifestConfig>();
        [JsonProperty("enums", Required = JsonRequired.Always)] internal EnumConfig[] EnumRows { get; private set; } = Array.Empty<EnumConfig>();
        [JsonProperty("fieldDictionary", Required = JsonRequired.Always)] internal FieldDictionaryConfig[] FieldDictionaryRows { get; private set; } = Array.Empty<FieldDictionaryConfig>();
        [JsonProperty("feedbackCues", Required = JsonRequired.Always)] internal FeedbackCueConfig[] FeedbackCueRows { get; private set; } = Array.Empty<FeedbackCueConfig>();
        [JsonProperty("strokeTrailStyles", Required = JsonRequired.Always)] internal StrokeTrailStyleConfig[] StrokeTrailStyleRows { get; private set; } = Array.Empty<StrokeTrailStyleConfig>();

        /// <summary>获取所有配置表记录数量之和，用于装载摘要和完整性检查。</summary>
        internal int RecordCount =>
            GlobalRows.Length + PlayerRows.Length + StanceRows.Length + StrokeRuleRows.Length +
            DamageFormulaRows.Length + DefenseRuleRows.Length + WeakpointRuleRows.Length +
            MovePatternRows.Length + EnemyRows.Length + EnemyAttackRows.Length + ProjectileRows.Length +
            BuffRows.Length + SkillRows.Length + SkillEffectRows.Length + LevelRows.Length + WaveRows.Length +
            SpawnPointRows.Length + EnemyModifierRows.Length + SpawnRows.Length + BossPhaseRows.Length +
            RewardRows.Length + TutorialRows.Length + TextRows.Length + AudioCueRows.Length + VfxCueRows.Length +
            AssetManifestRows.Length + EnumRows.Length + FieldDictionaryRows.Length + FeedbackCueRows.Length +
            StrokeTrailStyleRows.Length;
    }
}
