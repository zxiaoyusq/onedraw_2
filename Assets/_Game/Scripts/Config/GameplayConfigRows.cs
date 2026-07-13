using Newtonsoft.Json;
using JsonRequired = Newtonsoft.Json.Required;

namespace OneStrokeDemon.Config
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class GlobalConfig
    {
        [JsonProperty("key", Required = JsonRequired.Always)] public string Key { get; private set; } = string.Empty;
        [JsonProperty("valueType", Required = JsonRequired.Always)] public string ValueType { get; private set; } = string.Empty;
        [JsonProperty("intValue", Required = JsonRequired.AllowNull)] public long? IntValue { get; private set; }
        [JsonProperty("floatValue", Required = JsonRequired.AllowNull)] public float? FloatValue { get; private set; }
        [JsonProperty("stringValue", Required = JsonRequired.Always)] public string StringValue { get; private set; } = string.Empty;
        [JsonProperty("boolValue", Required = JsonRequired.AllowNull)] public bool? BoolValue { get; private set; }
        [JsonProperty("unit", Required = JsonRequired.Always)] public string Unit { get; private set; } = string.Empty;
        [JsonProperty("description", Required = JsonRequired.Always)] public string Description { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class PlayerConfig
    {
        [JsonProperty("playerId", Required = JsonRequired.Always)] public string PlayerId { get; private set; } = string.Empty;
        [JsonProperty("displayNameKey", Required = JsonRequired.Always)] public string DisplayNameKey { get; private set; } = string.Empty;
        [JsonProperty("maxHp", Required = JsonRequired.Always)] public long MaxHp { get; private set; }
        [JsonProperty("maxEnergy", Required = JsonRequired.Always)] public long MaxEnergy { get; private set; }
        [JsonProperty("defaultStanceId", Required = JsonRequired.Always)] public string DefaultStanceId { get; private set; } = string.Empty;
        [JsonProperty("ultimateSkillId", Required = JsonRequired.Always)] public string UltimateSkillId { get; private set; } = string.Empty;
        [JsonProperty("hitInvulnSec", Required = JsonRequired.Always)] public float HitInvulnSec { get; private set; }
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class StanceConfig
    {
        [JsonProperty("stanceId", Required = JsonRequired.Always)] public string StanceId { get; private set; } = string.Empty;
        [JsonProperty("displayNameKey", Required = JsonRequired.Always)] public string DisplayNameKey { get; private set; } = string.Empty;
        [JsonProperty("damageFormulaId", Required = JsonRequired.Always)] public string DamageFormulaId { get; private set; } = string.Empty;
        [JsonProperty("damageMultiplier", Required = JsonRequired.Always)] public float DamageMultiplier { get; private set; }
        [JsonProperty("ghostDamageMultiplier", Required = JsonRequired.Always)] public float GhostDamageMultiplier { get; private set; }
        [JsonProperty("projectileCutMultiplier", Required = JsonRequired.Always)] public float ProjectileCutMultiplier { get; private set; }
        [JsonProperty("strokeWidthRefPx", Required = JsonRequired.Always)] public long StrokeWidthRefPx { get; private set; }
        [JsonProperty("switchCooldownSec", Required = JsonRequired.Always)] public float SwitchCooldownSec { get; private set; }
        [JsonProperty("onSwitchEffectGroupId", Required = JsonRequired.Always)] public string OnSwitchEffectGroupId { get; private set; } = string.Empty;
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class StrokeRuleConfig
    {
        [JsonProperty("ruleId", Required = JsonRequired.Always)] public string RuleId { get; private set; } = string.Empty;
        [JsonProperty("gestureType", Required = JsonRequired.Always)] public string GestureType { get; private set; } = string.Empty;
        [JsonProperty("minPointDistanceRefPx", Required = JsonRequired.Always)] public long MinPointDistanceRefPx { get; private set; }
        [JsonProperty("maxStrokeLengthRefPx", Required = JsonRequired.Always)] public long MaxStrokeLengthRefPx { get; private set; }
        [JsonProperty("rdpEpsilonRefPx", Required = JsonRequired.Always)] public long RdpEpsilonRefPx { get; private set; }
        [JsonProperty("maxPointCount", Required = JsonRequired.Always)] public long MaxPointCount { get; private set; }
        [JsonProperty("minLengthRefPx", Required = JsonRequired.Always)] public long MinLengthRefPx { get; private set; }
        [JsonProperty("directionToleranceDeg", Required = JsonRequired.Always)] public long DirectionToleranceDeg { get; private set; }
        [JsonProperty("closeDistanceRefPx", Required = JsonRequired.Always)] public long CloseDistanceRefPx { get; private set; }
        [JsonProperty("minAreaRefPx2", Required = JsonRequired.Always)] public long MinAreaRefPx2 { get; private set; }
        [JsonProperty("minArcCurvature", Required = JsonRequired.Always)] public float MinArcCurvature { get; private set; }
        [JsonProperty("chargeHoldSec", Required = JsonRequired.Always)] public float ChargeHoldSec { get; private set; }
        [JsonProperty("hitRadiusRefPx", Required = JsonRequired.Always)] public long HitRadiusRefPx { get; private set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DamageFormulaConfig
    {
        [JsonProperty("formulaId", Required = JsonRequired.Always)] public string FormulaId { get; private set; } = string.Empty;
        [JsonProperty("baseDamage", Required = JsonRequired.Always)] public long BaseDamage { get; private set; }
        [JsonProperty("criticalChance", Required = JsonRequired.Always)] public float CriticalChance { get; private set; }
        [JsonProperty("criticalMultiplier", Required = JsonRequired.Always)] public float CriticalMultiplier { get; private set; }
        [JsonProperty("weakpointMultiplier", Required = JsonRequired.Always)] public float WeakpointMultiplier { get; private set; }
        [JsonProperty("wrongDirectionMultiplier", Required = JsonRequired.Always)] public float WrongDirectionMultiplier { get; private set; }
        [JsonProperty("comboStep", Required = JsonRequired.Always)] public float ComboStep { get; private set; }
        [JsonProperty("comboMaxMultiplier", Required = JsonRequired.Always)] public float ComboMaxMultiplier { get; private set; }
        [JsonProperty("energyPerHit", Required = JsonRequired.Always)] public long EnergyPerHit { get; private set; }
        [JsonProperty("scorePerHit", Required = JsonRequired.Always)] public long ScorePerHit { get; private set; }
        [JsonProperty("scorePerDamage", Required = JsonRequired.Always)] public float ScorePerDamage { get; private set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DefenseRuleConfig
    {
        [JsonProperty("defenseRuleId", Required = JsonRequired.Always)] public string DefenseRuleId { get; private set; } = string.Empty;
        [JsonProperty("armorHp", Required = JsonRequired.Always)] public long ArmorHp { get; private set; }
        [JsonProperty("requiredGestureType", Required = JsonRequired.Always)] public string RequiredGestureType { get; private set; } = string.Empty;
        [JsonProperty("requiredStanceId", Required = JsonRequired.Always)] public string RequiredStanceId { get; private set; } = string.Empty;
        [JsonProperty("breakDamageMultiplier", Required = JsonRequired.Always)] public float BreakDamageMultiplier { get; private set; }
        [JsonProperty("wrongGestureDamageMultiplier", Required = JsonRequired.Always)] public float WrongGestureDamageMultiplier { get; private set; }
        [JsonProperty("reflectDamage", Required = JsonRequired.Always)] public long ReflectDamage { get; private set; }
        [JsonProperty("breakEffectGroupId", Required = JsonRequired.Always)] public string BreakEffectGroupId { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class WeakpointRuleConfig
    {
        [JsonProperty("weakpointRuleId", Required = JsonRequired.Always)] public string WeakpointRuleId { get; private set; } = string.Empty;
        [JsonProperty("windowStartSec", Required = JsonRequired.Always)] public float WindowStartSec { get; private set; }
        [JsonProperty("windowEndSec", Required = JsonRequired.Always)] public float WindowEndSec { get; private set; }
        [JsonProperty("radiusRefPx", Required = JsonRequired.Always)] public long RadiusRefPx { get; private set; }
        [JsonProperty("damageMultiplier", Required = JsonRequired.Always)] public float DamageMultiplier { get; private set; }
        [JsonProperty("interruptAttack", Required = JsonRequired.Always)] public bool InterruptAttack { get; private set; }
        [JsonProperty("energyBonus", Required = JsonRequired.Always)] public long EnergyBonus { get; private set; }
        [JsonProperty("scoreBonus", Required = JsonRequired.Always)] public long ScoreBonus { get; private set; }
        [JsonProperty("vfxKey", Required = JsonRequired.Always)] public string VfxKey { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class MovePatternConfig
    {
        [JsonProperty("movePatternId", Required = JsonRequired.Always)] public string MovePatternId { get; private set; } = string.Empty;
        [JsonProperty("patternType", Required = JsonRequired.Always)] public string PatternType { get; private set; } = string.Empty;
        [JsonProperty("speedMultiplier", Required = JsonRequired.Always)] public float SpeedMultiplier { get; private set; }
        [JsonProperty("amplitudeRefPx", Required = JsonRequired.Always)] public long AmplitudeRefPx { get; private set; }
        [JsonProperty("frequency", Required = JsonRequired.Always)] public float Frequency { get; private set; }
        [JsonProperty("startXNorm", Required = JsonRequired.Always)] public float StartXNorm { get; private set; }
        [JsonProperty("endXNorm", Required = JsonRequired.Always)] public float EndXNorm { get; private set; }
        [JsonProperty("startYNorm", Required = JsonRequired.Always)] public float StartYNorm { get; private set; }
        [JsonProperty("endYNorm", Required = JsonRequired.Always)] public float EndYNorm { get; private set; }
        [JsonProperty("loop", Required = JsonRequired.Always)] public bool Loop { get; private set; }
        [JsonProperty("description", Required = JsonRequired.Always)] public string Description { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnemyConfig
    {
        [JsonProperty("enemyId", Required = JsonRequired.Always)] public string EnemyId { get; private set; } = string.Empty;
        [JsonProperty("displayNameKey", Required = JsonRequired.Always)] public string DisplayNameKey { get; private set; } = string.Empty;
        [JsonProperty("tier", Required = JsonRequired.Always)] public string Tier { get; private set; } = string.Empty;
        [JsonProperty("maxHp", Required = JsonRequired.Always)] public long MaxHp { get; private set; }
        [JsonProperty("movePatternId", Required = JsonRequired.Always)] public string MovePatternId { get; private set; } = string.Empty;
        [JsonProperty("moveSpeedRefPxSec", Required = JsonRequired.Always)] public long MoveSpeedRefPxSec { get; private set; }
        [JsonProperty("attackSetId", Required = JsonRequired.Always)] public string AttackSetId { get; private set; } = string.Empty;
        [JsonProperty("defenseRuleId", Required = JsonRequired.Always)] public string DefenseRuleId { get; private set; } = string.Empty;
        [JsonProperty("weakpointRuleId", Required = JsonRequired.Always)] public string WeakpointRuleId { get; private set; } = string.Empty;
        [JsonProperty("stanceVulnerability", Required = JsonRequired.Always)] public string StanceVulnerability { get; private set; } = string.Empty;
        [JsonProperty("contactDamage", Required = JsonRequired.Always)] public long ContactDamage { get; private set; }
        [JsonProperty("scoreValue", Required = JsonRequired.Always)] public long ScoreValue { get; private set; }
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
        [JsonProperty("poolPrewarm", Required = JsonRequired.Always)] public long PoolPrewarm { get; private set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnemyAttackConfig
    {
        [JsonProperty("attackId", Required = JsonRequired.Always)] public string AttackId { get; private set; } = string.Empty;
        [JsonProperty("attackSetId", Required = JsonRequired.Always)] public string AttackSetId { get; private set; } = string.Empty;
        [JsonProperty("order", Required = JsonRequired.Always)] public long Order { get; private set; }
        [JsonProperty("triggerType", Required = JsonRequired.Always)] public string TriggerType { get; private set; } = string.Empty;
        [JsonProperty("cooldownSec", Required = JsonRequired.Always)] public float CooldownSec { get; private set; }
        [JsonProperty("windupSec", Required = JsonRequired.Always)] public float WindupSec { get; private set; }
        [JsonProperty("activeSec", Required = JsonRequired.Always)] public float ActiveSec { get; private set; }
        [JsonProperty("damage", Required = JsonRequired.Always)] public long Damage { get; private set; }
        [JsonProperty("projectileId", Required = JsonRequired.Always)] public string ProjectileId { get; private set; } = string.Empty;
        [JsonProperty("gestureInterruptType", Required = JsonRequired.Always)] public string GestureInterruptType { get; private set; } = string.Empty;
        [JsonProperty("interruptStartSec", Required = JsonRequired.Always)] public float InterruptStartSec { get; private set; }
        [JsonProperty("interruptEndSec", Required = JsonRequired.Always)] public float InterruptEndSec { get; private set; }
        [JsonProperty("effectGroupId", Required = JsonRequired.Always)] public string EffectGroupId { get; private set; } = string.Empty;
        [JsonProperty("weight", Required = JsonRequired.Always)] public float Weight { get; private set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class ProjectileConfig
    {
        [JsonProperty("projectileId", Required = JsonRequired.Always)] public string ProjectileId { get; private set; } = string.Empty;
        [JsonProperty("movePatternId", Required = JsonRequired.Always)] public string MovePatternId { get; private set; } = string.Empty;
        [JsonProperty("speedRefPxSec", Required = JsonRequired.Always)] public long SpeedRefPxSec { get; private set; }
        [JsonProperty("lifeSec", Required = JsonRequired.Always)] public float LifeSec { get; private set; }
        [JsonProperty("damage", Required = JsonRequired.Always)] public long Damage { get; private set; }
        [JsonProperty("cuttable", Required = JsonRequired.Always)] public bool Cuttable { get; private set; }
        [JsonProperty("reflectable", Required = JsonRequired.Always)] public bool Reflectable { get; private set; }
        [JsonProperty("requiredStanceId", Required = JsonRequired.Always)] public string RequiredStanceId { get; private set; } = string.Empty;
        [JsonProperty("hitRadiusRefPx", Required = JsonRequired.Always)] public long HitRadiusRefPx { get; private set; }
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
        [JsonProperty("vfxKey", Required = JsonRequired.Always)] public string VfxKey { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BuffConfig
    {
        [JsonProperty("buffId", Required = JsonRequired.Always)] public string BuffId { get; private set; } = string.Empty;
        [JsonProperty("type", Required = JsonRequired.Always)] public string Type { get; private set; } = string.Empty;
        [JsonProperty("durationSec", Required = JsonRequired.Always)] public float DurationSec { get; private set; }
        [JsonProperty("maxStacks", Required = JsonRequired.Always)] public long MaxStacks { get; private set; }
        [JsonProperty("magnitude", Required = JsonRequired.Always)] public float Magnitude { get; private set; }
        [JsonProperty("tickSec", Required = JsonRequired.Always)] public float TickSec { get; private set; }
        [JsonProperty("refreshPolicy", Required = JsonRequired.Always)] public string RefreshPolicy { get; private set; } = string.Empty;
        [JsonProperty("vfxKey", Required = JsonRequired.Always)] public string VfxKey { get; private set; } = string.Empty;
        [JsonProperty("textKey", Required = JsonRequired.Always)] public string TextKey { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SkillConfig
    {
        [JsonProperty("skillId", Required = JsonRequired.Always)] public string SkillId { get; private set; } = string.Empty;
        [JsonProperty("displayNameKey", Required = JsonRequired.Always)] public string DisplayNameKey { get; private set; } = string.Empty;
        [JsonProperty("triggerType", Required = JsonRequired.Always)] public string TriggerType { get; private set; } = string.Empty;
        [JsonProperty("requiredStanceId", Required = JsonRequired.Always)] public string RequiredStanceId { get; private set; } = string.Empty;
        [JsonProperty("energyCost", Required = JsonRequired.Always)] public long EnergyCost { get; private set; }
        [JsonProperty("cooldownSec", Required = JsonRequired.Always)] public float CooldownSec { get; private set; }
        [JsonProperty("gestureType", Required = JsonRequired.Always)] public string GestureType { get; private set; } = string.Empty;
        [JsonProperty("inputWindowSec", Required = JsonRequired.Always)] public float InputWindowSec { get; private set; }
        [JsonProperty("effectGroupId", Required = JsonRequired.Always)] public string EffectGroupId { get; private set; } = string.Empty;
        [JsonProperty("iconAssetKey", Required = JsonRequired.Always)] public string IconAssetKey { get; private set; } = string.Empty;
        [JsonProperty("textKey", Required = JsonRequired.Always)] public string TextKey { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SkillEffectConfig
    {
        [JsonProperty("effectGroupId", Required = JsonRequired.Always)] public string EffectGroupId { get; private set; } = string.Empty;
        [JsonProperty("order", Required = JsonRequired.Always)] public long Order { get; private set; }
        [JsonProperty("effectType", Required = JsonRequired.Always)] public string EffectType { get; private set; } = string.Empty;
        [JsonProperty("targetType", Required = JsonRequired.Always)] public string TargetType { get; private set; } = string.Empty;
        [JsonProperty("value1", Required = JsonRequired.Always)] public float Value1 { get; private set; }
        [JsonProperty("value2", Required = JsonRequired.Always)] public float Value2 { get; private set; }
        [JsonProperty("durationSec", Required = JsonRequired.Always)] public float DurationSec { get; private set; }
        [JsonProperty("buffId", Required = JsonRequired.Always)] public string BuffId { get; private set; } = string.Empty;
        [JsonProperty("vfxKey", Required = JsonRequired.Always)] public string VfxKey { get; private set; } = string.Empty;
        [JsonProperty("audioKey", Required = JsonRequired.Always)] public string AudioKey { get; private set; } = string.Empty;
        [JsonProperty("condition", Required = JsonRequired.Always)] public string Condition { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class LevelConfig
    {
        [JsonProperty("levelId", Required = JsonRequired.Always)] public string LevelId { get; private set; } = string.Empty;
        [JsonProperty("displayNameKey", Required = JsonRequired.Always)] public string DisplayNameKey { get; private set; } = string.Empty;
        [JsonProperty("sceneKey", Required = JsonRequired.Always)] public string SceneKey { get; private set; } = string.Empty;
        [JsonProperty("backgroundAssetKey", Required = JsonRequired.Always)] public string BackgroundAssetKey { get; private set; } = string.Empty;
        [JsonProperty("durationLimitSec", Required = JsonRequired.Always)] public long DurationLimitSec { get; private set; }
        [JsonProperty("nextLevelId", Required = JsonRequired.Always)] public string NextLevelId { get; private set; } = string.Empty;
        [JsonProperty("rewardTableId", Required = JsonRequired.Always)] public string RewardTableId { get; private set; } = string.Empty;
        [JsonProperty("starScore1", Required = JsonRequired.Always)] public long StarScore1 { get; private set; }
        [JsonProperty("starScore2", Required = JsonRequired.Always)] public long StarScore2 { get; private set; }
        [JsonProperty("starScore3", Required = JsonRequired.Always)] public long StarScore3 { get; private set; }
        [JsonProperty("tutorialId", Required = JsonRequired.Always)] public string TutorialId { get; private set; } = string.Empty;
        [JsonProperty("bossEnemyId", Required = JsonRequired.Always)] public string BossEnemyId { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class WaveConfig
    {
        [JsonProperty("waveId", Required = JsonRequired.Always)] public string WaveId { get; private set; } = string.Empty;
        [JsonProperty("levelId", Required = JsonRequired.Always)] public string LevelId { get; private set; } = string.Empty;
        [JsonProperty("order", Required = JsonRequired.Always)] public long Order { get; private set; }
        [JsonProperty("startTrigger", Required = JsonRequired.Always)] public string StartTrigger { get; private set; } = string.Empty;
        [JsonProperty("startDelaySec", Required = JsonRequired.Always)] public float StartDelaySec { get; private set; }
        [JsonProperty("endCondition", Required = JsonRequired.Always)] public string EndCondition { get; private set; } = string.Empty;
        [JsonProperty("endDelaySec", Required = JsonRequired.Always)] public float EndDelaySec { get; private set; }
        [JsonProperty("musicKey", Required = JsonRequired.Always)] public string MusicKey { get; private set; } = string.Empty;
        [JsonProperty("maxAlive", Required = JsonRequired.Always)] public long MaxAlive { get; private set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SpawnPointConfig
    {
        [JsonProperty("spawnPointId", Required = JsonRequired.Always)] public string SpawnPointId { get; private set; } = string.Empty;
        [JsonProperty("levelId", Required = JsonRequired.Always)] public string LevelId { get; private set; } = string.Empty;
        [JsonProperty("normalizedX", Required = JsonRequired.Always)] public float NormalizedX { get; private set; }
        [JsonProperty("normalizedY", Required = JsonRequired.Always)] public float NormalizedY { get; private set; }
        [JsonProperty("lane", Required = JsonRequired.Always)] public string Lane { get; private set; } = string.Empty;
        [JsonProperty("jitterX", Required = JsonRequired.Always)] public float JitterX { get; private set; }
        [JsonProperty("jitterY", Required = JsonRequired.Always)] public float JitterY { get; private set; }
        [JsonProperty("facing", Required = JsonRequired.Always)] public string Facing { get; private set; } = string.Empty;
        [JsonProperty("notes", Required = JsonRequired.Always)] public string Notes { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnemyModifierConfig
    {
        [JsonProperty("modifierId", Required = JsonRequired.Always)] public string ModifierId { get; private set; } = string.Empty;
        [JsonProperty("displayNameKey", Required = JsonRequired.Always)] public string DisplayNameKey { get; private set; } = string.Empty;
        [JsonProperty("hpMultiplier", Required = JsonRequired.Always)] public float HpMultiplier { get; private set; }
        [JsonProperty("damageMultiplier", Required = JsonRequired.Always)] public float DamageMultiplier { get; private set; }
        [JsonProperty("speedMultiplier", Required = JsonRequired.Always)] public float SpeedMultiplier { get; private set; }
        [JsonProperty("scoreMultiplier", Required = JsonRequired.Always)] public float ScoreMultiplier { get; private set; }
        [JsonProperty("tintHex", Required = JsonRequired.Always)] public string TintHex { get; private set; } = string.Empty;
        [JsonProperty("extraBuffId", Required = JsonRequired.Always)] public string ExtraBuffId { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SpawnConfig
    {
        [JsonProperty("spawnId", Required = JsonRequired.Always)] public string SpawnId { get; private set; } = string.Empty;
        [JsonProperty("waveId", Required = JsonRequired.Always)] public string WaveId { get; private set; } = string.Empty;
        [JsonProperty("spawnTimeSec", Required = JsonRequired.Always)] public float SpawnTimeSec { get; private set; }
        [JsonProperty("enemyId", Required = JsonRequired.Always)] public string EnemyId { get; private set; } = string.Empty;
        [JsonProperty("count", Required = JsonRequired.Always)] public long Count { get; private set; }
        [JsonProperty("intervalSec", Required = JsonRequired.Always)] public float IntervalSec { get; private set; }
        [JsonProperty("spawnPointId", Required = JsonRequired.Always)] public string SpawnPointId { get; private set; } = string.Empty;
        [JsonProperty("spawnPattern", Required = JsonRequired.Always)] public string SpawnPattern { get; private set; } = string.Empty;
        [JsonProperty("modifierId", Required = JsonRequired.Always)] public string ModifierId { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BossPhaseConfig
    {
        [JsonProperty("bossPhaseId", Required = JsonRequired.Always)] public string BossPhaseId { get; private set; } = string.Empty;
        [JsonProperty("enemyId", Required = JsonRequired.Always)] public string EnemyId { get; private set; } = string.Empty;
        [JsonProperty("order", Required = JsonRequired.Always)] public long Order { get; private set; }
        [JsonProperty("enterHpRatio", Required = JsonRequired.Always)] public float EnterHpRatio { get; private set; }
        [JsonProperty("exitHpRatio", Required = JsonRequired.Always)] public float ExitHpRatio { get; private set; }
        [JsonProperty("movementPatternId", Required = JsonRequired.Always)] public string MovementPatternId { get; private set; } = string.Empty;
        [JsonProperty("attackSetId", Required = JsonRequired.Always)] public string AttackSetId { get; private set; } = string.Empty;
        [JsonProperty("defenseRuleId", Required = JsonRequired.Always)] public string DefenseRuleId { get; private set; } = string.Empty;
        [JsonProperty("weakpointRuleId", Required = JsonRequired.Always)] public string WeakpointRuleId { get; private set; } = string.Empty;
        [JsonProperty("onEnterEffectGroupId", Required = JsonRequired.Always)] public string OnEnterEffectGroupId { get; private set; } = string.Empty;
        [JsonProperty("descriptionKey", Required = JsonRequired.Always)] public string DescriptionKey { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class RewardConfig
    {
        [JsonProperty("rewardTableId", Required = JsonRequired.Always)] public string RewardTableId { get; private set; } = string.Empty;
        [JsonProperty("order", Required = JsonRequired.Always)] public long Order { get; private set; }
        [JsonProperty("conditionType", Required = JsonRequired.Always)] public string ConditionType { get; private set; } = string.Empty;
        [JsonProperty("conditionValue", Required = JsonRequired.Always)] public string ConditionValue { get; private set; } = string.Empty;
        [JsonProperty("rewardType", Required = JsonRequired.Always)] public string RewardType { get; private set; } = string.Empty;
        [JsonProperty("rewardId", Required = JsonRequired.Always)] public string RewardId { get; private set; } = string.Empty;
        [JsonProperty("amount", Required = JsonRequired.Always)] public long Amount { get; private set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class TutorialConfig
    {
        [JsonProperty("tutorialId", Required = JsonRequired.Always)] public string TutorialId { get; private set; } = string.Empty;
        [JsonProperty("order", Required = JsonRequired.Always)] public long Order { get; private set; }
        [JsonProperty("triggerEvent", Required = JsonRequired.Always)] public string TriggerEvent { get; private set; } = string.Empty;
        [JsonProperty("blockProgress", Required = JsonRequired.Always)] public bool BlockProgress { get; private set; }
        [JsonProperty("minDisplaySec", Required = JsonRequired.Always)] public float MinDisplaySec { get; private set; }
        [JsonProperty("completeEvent", Required = JsonRequired.Always)] public string CompleteEvent { get; private set; } = string.Empty;
        [JsonProperty("textKey", Required = JsonRequired.Always)] public string TextKey { get; private set; } = string.Empty;
        [JsonProperty("highlightTarget", Required = JsonRequired.Always)] public string HighlightTarget { get; private set; } = string.Empty;
        [JsonProperty("gestureType", Required = JsonRequired.Always)] public string GestureType { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class TextConfig
    {
        [JsonProperty("textKey", Required = JsonRequired.Always)] public string TextKey { get; private set; } = string.Empty;
        [JsonProperty("zhCN", Required = JsonRequired.Always)] public string ZhCN { get; private set; } = string.Empty;
        [JsonProperty("enUS", Required = JsonRequired.Always)] public string EnUS { get; private set; } = string.Empty;
        [JsonProperty("context", Required = JsonRequired.Always)] public string Context { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AudioCueConfig
    {
        [JsonProperty("audioKey", Required = JsonRequired.Always)] public string AudioKey { get; private set; } = string.Empty;
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
        [JsonProperty("category", Required = JsonRequired.Always)] public string Category { get; private set; } = string.Empty;
        [JsonProperty("volume", Required = JsonRequired.Always)] public float Volume { get; private set; }
        [JsonProperty("pitchMin", Required = JsonRequired.Always)] public float PitchMin { get; private set; }
        [JsonProperty("pitchMax", Required = JsonRequired.Always)] public float PitchMax { get; private set; }
        [JsonProperty("maxConcurrent", Required = JsonRequired.Always)] public long MaxConcurrent { get; private set; }
        [JsonProperty("cooldownSec", Required = JsonRequired.Always)] public float CooldownSec { get; private set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class VfxCueConfig
    {
        [JsonProperty("vfxKey", Required = JsonRequired.Always)] public string VfxKey { get; private set; } = string.Empty;
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
        [JsonProperty("lifeSec", Required = JsonRequired.Always)] public float LifeSec { get; private set; }
        [JsonProperty("poolPrewarm", Required = JsonRequired.Always)] public long PoolPrewarm { get; private set; }
        [JsonProperty("followTarget", Required = JsonRequired.Always)] public bool FollowTarget { get; private set; }
        [JsonProperty("sortingLayer", Required = JsonRequired.Always)] public string SortingLayer { get; private set; } = string.Empty;
        [JsonProperty("sortingOrder", Required = JsonRequired.Always)] public long SortingOrder { get; private set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AssetManifestConfig
    {
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
        [JsonProperty("assetType", Required = JsonRequired.Always)] public string AssetType { get; private set; } = string.Empty;
        [JsonProperty("addressOrPath", Required = JsonRequired.Always)] public string AddressOrPath { get; private set; } = string.Empty;
        [JsonProperty("requiredInMvp", Required = JsonRequired.Always)] public bool RequiredInMvp { get; private set; }
        [JsonProperty("notes", Required = JsonRequired.Always)] public string Notes { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnumConfig
    {
        [JsonProperty("enumType", Required = JsonRequired.Always)] public string EnumType { get; private set; } = string.Empty;
        [JsonProperty("value", Required = JsonRequired.Always)] public string Value { get; private set; } = string.Empty;
        [JsonProperty("displayName", Required = JsonRequired.Always)] public string DisplayName { get; private set; } = string.Empty;
        [JsonProperty("description", Required = JsonRequired.Always)] public string Description { get; private set; } = string.Empty;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class FieldDictionaryConfig
    {
        [JsonProperty("sheet", Required = JsonRequired.Always)] public string Sheet { get; private set; } = string.Empty;
        [JsonProperty("field", Required = JsonRequired.Always)] public string Field { get; private set; } = string.Empty;
        [JsonProperty("type", Required = JsonRequired.Always)] public string Type { get; private set; } = string.Empty;
        [JsonProperty("required", Required = JsonRequired.Always)] public string Required { get; private set; } = string.Empty;
        [JsonProperty("default", Required = JsonRequired.Always)] public string Default { get; private set; } = string.Empty;
        [JsonProperty("min", Required = JsonRequired.AllowNull)] public float? Min { get; private set; }
        [JsonProperty("max", Required = JsonRequired.AllowNull)] public float? Max { get; private set; }
        [JsonProperty("enumType", Required = JsonRequired.Always)] public string EnumType { get; private set; } = string.Empty;
        [JsonProperty("foreignKey", Required = JsonRequired.Always)] public string ForeignKey { get; private set; } = string.Empty;
        [JsonProperty("description", Required = JsonRequired.Always)] public string Description { get; private set; } = string.Empty;
    }
}
