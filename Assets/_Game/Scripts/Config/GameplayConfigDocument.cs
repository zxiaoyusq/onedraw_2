using System;
using Newtonsoft.Json;
using JsonRequired = Newtonsoft.Json.Required;

namespace OneStrokeDemon.Config
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class GameplayConfigDocument
    {
        [JsonProperty("schemaVersion", Required = JsonRequired.Always)]
        public long SchemaVersion { get; private set; }

        [JsonProperty("contentVersion", Required = JsonRequired.Always)]
        public string ContentVersion { get; private set; } = string.Empty;

        [JsonProperty("contentHash", Required = JsonRequired.Always)]
        public string ContentHash { get; private set; } = string.Empty;

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

        internal int RecordCount =>
            GlobalRows.Length + PlayerRows.Length + StanceRows.Length + StrokeRuleRows.Length +
            DamageFormulaRows.Length + DefenseRuleRows.Length + WeakpointRuleRows.Length +
            MovePatternRows.Length + EnemyRows.Length + EnemyAttackRows.Length + ProjectileRows.Length +
            BuffRows.Length + SkillRows.Length + SkillEffectRows.Length + LevelRows.Length + WaveRows.Length +
            SpawnPointRows.Length + EnemyModifierRows.Length + SpawnRows.Length + BossPhaseRows.Length +
            RewardRows.Length + TutorialRows.Length + TextRows.Length + AudioCueRows.Length + VfxCueRows.Length +
            AssetManifestRows.Length + EnumRows.Length + FieldDictionaryRows.Length;
    }
}
