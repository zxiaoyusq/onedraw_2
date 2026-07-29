using Newtonsoft.Json;
using JsonRequired = Newtonsoft.Json.Required;

namespace OneStrokeDemon.Config
{
    /// <summary>描述一个全局键值及其类型、单位和用途。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class GlobalConfig
    {
        // 属性与导出 JSON 字段一一对应；字段约束和单位以 CONFIG_SCHEMA 为准。
        [JsonProperty("key", Required = JsonRequired.Always)] public string Key { get; private set; } = string.Empty;
        [JsonProperty("valueType", Required = JsonRequired.Always)] public string ValueType { get; private set; } = string.Empty;
        [JsonProperty("intValue", Required = JsonRequired.AllowNull)] public long? IntValue { get; private set; }
        [JsonProperty("floatValue", Required = JsonRequired.AllowNull)] public float? FloatValue { get; private set; }
        [JsonProperty("stringValue", Required = JsonRequired.Always)] public string StringValue { get; private set; } = string.Empty;
        [JsonProperty("boolValue", Required = JsonRequired.AllowNull)] public bool? BoolValue { get; private set; }
        [JsonProperty("unit", Required = JsonRequired.Always)] public string Unit { get; private set; } = string.Empty;
        [JsonProperty("description", Required = JsonRequired.Always)] public string Description { get; private set; } = string.Empty;
    }

    /// <summary>描述玩家的基础生命、能量、默认架势和资源引用。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class PlayerConfig
    {
        // 私有写入仅供严格 JSON 反序列化，运行时通过只读属性消费配置。
        [JsonProperty("playerId", Required = JsonRequired.Always)] public string PlayerId { get; private set; } = string.Empty;
        [JsonProperty("displayNameKey", Required = JsonRequired.Always)] public string DisplayNameKey { get; private set; } = string.Empty;
        [JsonProperty("maxHp", Required = JsonRequired.Always)] public long MaxHp { get; private set; }
        [JsonProperty("maxEnergy", Required = JsonRequired.Always)] public long MaxEnergy { get; private set; }
        [JsonProperty("defaultStanceId", Required = JsonRequired.Always)] public string DefaultStanceId { get; private set; } = string.Empty;
        [JsonProperty("ultimateSkillId", Required = JsonRequired.Always)] public string UltimateSkillId { get; private set; } = string.Empty;
        [JsonProperty("hitInvulnSec", Required = JsonRequired.Always)] public float HitInvulnSec { get; private set; }
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
    }

    /// <summary>描述一种战斗架势及其伤害、笔宽和切换参数。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class StanceConfig
    {
        // 所有数值均来自配置导出物，代码和 Inspector 不保存重复兜底值。
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
        [JsonProperty("strokeTrailStyleId", Required = JsonRequired.Always)] public string StrokeTrailStyleId { get; private set; } = string.Empty;
    }

    /// <summary>描述手势采样、简化、识别和命中的统一笔画规则。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class StrokeRuleConfig
    {
        // RefPx/RefPx2 字段使用参考分辨率像素或面积，Sec/Deg 字段按名称标注单位。
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

    /// <summary>描述一次玩家攻击的基础伤害、暴击、连击、能量和得分公式。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DamageFormulaConfig
    {
        // 属性保持与冻结 JSON Schema 完全一致，避免运行时产生第二套公式定义。
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

    /// <summary>描述护甲破除所需手势、架势和错误输入反馈。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DefenseRuleConfig
    {
        // 空字符串表示配置合同允许的“无对应项”，具体约束由导出器校验。
        [JsonProperty("defenseRuleId", Required = JsonRequired.Always)] public string DefenseRuleId { get; private set; } = string.Empty;
        [JsonProperty("armorHp", Required = JsonRequired.Always)] public long ArmorHp { get; private set; }
        [JsonProperty("requiredGestureType", Required = JsonRequired.Always)] public string RequiredGestureType { get; private set; } = string.Empty;
        [JsonProperty("requiredStanceId", Required = JsonRequired.Always)] public string RequiredStanceId { get; private set; } = string.Empty;
        [JsonProperty("breakDamageMultiplier", Required = JsonRequired.Always)] public float BreakDamageMultiplier { get; private set; }
        [JsonProperty("wrongGestureDamageMultiplier", Required = JsonRequired.Always)] public float WrongGestureDamageMultiplier { get; private set; }
        [JsonProperty("reflectDamage", Required = JsonRequired.Always)] public long ReflectDamage { get; private set; }
        [JsonProperty("breakEffectGroupId", Required = JsonRequired.Always)] public string BreakEffectGroupId { get; private set; } = string.Empty;
    }

    /// <summary>描述敌人弱点窗口、命中半径、倍率和额外奖励。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class WeakpointRuleConfig
    {
        // 时间窗口和参考像素半径由玩法系统直接读取，不在此 DTO 中计算。
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

    /// <summary>描述敌人或投射物的移动轨迹及归一化起止位置。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class MovePatternConfig
    {
        // DTO 只承载数据，模式解释和坐标换算由对应移动规则完成。
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

    /// <summary>描述敌人的生命、移动、攻击、防御、弱点、得分和池化资源。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnemyConfig
    {
        // 各 ID 是配置外键；运行时通过 IConfigProvider 解析关联记录。
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

    /// <summary>描述攻击集合中的一项攻击时序、伤害、中断窗口和权重。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnemyAttackConfig
    {
        // Order 决定稳定顺序，Weight 只由攻击选择规则解释。
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

    /// <summary>描述投射物移动、生命周期、伤害、可切割性和资源引用。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class ProjectileConfig
    {
        // 资源和特效字段保存稳定配置键，不直接保存 Unity 对象。
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

    /// <summary>描述可叠加状态的持续时间、强度、刷新策略和表现键。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BuffConfig
    {
        // 状态行为由 Type 与 RefreshPolicy 驱动，本 DTO 不包含执行逻辑。
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

    /// <summary>描述技能触发条件、架势要求、消耗、冷却和效果组。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SkillConfig
    {
        // EffectGroupId 关联一组按顺序执行的 SkillEffectConfig。
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

    /// <summary>描述技能效果组中的单个有序效果及其目标和条件。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SkillEffectConfig
    {
        // Value1/Value2 的含义由 EffectType 决定，并由配置字段字典约束。
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

    /// <summary>描述关卡场景、时限、后继关卡、奖励、星级和 Boss。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class LevelConfig
    {
        // SceneKey 和 BackgroundAssetKey 均通过资源注册表解析为 Unity 资源。
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

    /// <summary>描述关卡中的一个有序波次及其开始、结束和并发限制。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class WaveConfig
    {
        // 波次按 LevelId 分组并按 Order 保持配置顺序。
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

    /// <summary>描述关卡中的归一化出生位置、抖动、通道和朝向。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SpawnPointConfig
    {
        // 归一化坐标由关卡布局转换为当前参考空间位置。
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

    /// <summary>描述对敌人生命、伤害、速度、得分和外观的组合修饰。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnemyModifierConfig
    {
        // 修饰器只声明数据，叠加和应用时机由敌人生成流程负责。
        [JsonProperty("modifierId", Required = JsonRequired.Always)] public string ModifierId { get; private set; } = string.Empty;
        [JsonProperty("displayNameKey", Required = JsonRequired.Always)] public string DisplayNameKey { get; private set; } = string.Empty;
        [JsonProperty("hpMultiplier", Required = JsonRequired.Always)] public float HpMultiplier { get; private set; }
        [JsonProperty("damageMultiplier", Required = JsonRequired.Always)] public float DamageMultiplier { get; private set; }
        [JsonProperty("speedMultiplier", Required = JsonRequired.Always)] public float SpeedMultiplier { get; private set; }
        [JsonProperty("scoreMultiplier", Required = JsonRequired.Always)] public float ScoreMultiplier { get; private set; }
        [JsonProperty("tintHex", Required = JsonRequired.Always)] public string TintHex { get; private set; } = string.Empty;
        [JsonProperty("extraBuffId", Required = JsonRequired.Always)] public string ExtraBuffId { get; private set; } = string.Empty;
    }

    /// <summary>描述波次中的敌人种类、数量、时间、出生点和修饰器。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SpawnConfig
    {
        // SpawnTimeSec 与 IntervalSec 共同决定同一条记录的生成时序。
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

    /// <summary>描述 Boss 在指定生命比例范围内的阶段行为和入场效果。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BossPhaseConfig
    {
        // 阶段按 EnemyId 分组并按 Order 执行，比例边界在配置导出时校验。
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

    /// <summary>描述奖励表中的条件、奖励类型、目标 ID 和数量。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class RewardConfig
    {
        // 同一奖励表内的记录按 Order 保持确定顺序。
        [JsonProperty("rewardTableId", Required = JsonRequired.Always)] public string RewardTableId { get; private set; } = string.Empty;
        [JsonProperty("order", Required = JsonRequired.Always)] public long Order { get; private set; }
        [JsonProperty("conditionType", Required = JsonRequired.Always)] public string ConditionType { get; private set; } = string.Empty;
        [JsonProperty("conditionValue", Required = JsonRequired.Always)] public string ConditionValue { get; private set; } = string.Empty;
        [JsonProperty("rewardType", Required = JsonRequired.Always)] public string RewardType { get; private set; } = string.Empty;
        [JsonProperty("rewardId", Required = JsonRequired.Always)] public string RewardId { get; private set; } = string.Empty;
        [JsonProperty("amount", Required = JsonRequired.Always)] public long Amount { get; private set; }
    }

    /// <summary>描述教程步骤的触发、阻塞、完成条件、文案和高亮目标。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class TutorialConfig
    {
        // 教程导演只读取这些声明，不把步骤文案或手势规则硬编码在场景中。
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

    /// <summary>描述一个本地化文本键及其中英文内容和使用上下文。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class TextConfig
    {
        // 文案以 TextKey 查询，运行时不直接依赖 Excel 行号。
        [JsonProperty("textKey", Required = JsonRequired.Always)] public string TextKey { get; private set; } = string.Empty;
        [JsonProperty("zhCN", Required = JsonRequired.Always)] public string ZhCN { get; private set; } = string.Empty;
        [JsonProperty("enUS", Required = JsonRequired.Always)] public string EnUS { get; private set; } = string.Empty;
        [JsonProperty("context", Required = JsonRequired.Always)] public string Context { get; private set; } = string.Empty;
    }

    /// <summary>描述音频资源键、分类、音量、音高和并发限制。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AudioCueConfig
    {
        // AssetKey 通过资源注册表解析，音频播放规则读取其余参数。
        [JsonProperty("audioKey", Required = JsonRequired.Always)] public string AudioKey { get; private set; } = string.Empty;
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
        [JsonProperty("category", Required = JsonRequired.Always)] public string Category { get; private set; } = string.Empty;
        [JsonProperty("volume", Required = JsonRequired.Always)] public float Volume { get; private set; }
        [JsonProperty("pitchMin", Required = JsonRequired.Always)] public float PitchMin { get; private set; }
        [JsonProperty("pitchMax", Required = JsonRequired.Always)] public float PitchMax { get; private set; }
        [JsonProperty("maxConcurrent", Required = JsonRequired.Always)] public long MaxConcurrent { get; private set; }
        [JsonProperty("cooldownSec", Required = JsonRequired.Always)] public float CooldownSec { get; private set; }
    }

    /// <summary>描述视觉特效资源、生命周期、预热和渲染顺序。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class VfxCueConfig
    {
        // VfxKey 是玩法引用键，AssetKey 是资源注册表引用键。
        [JsonProperty("vfxKey", Required = JsonRequired.Always)] public string VfxKey { get; private set; } = string.Empty;
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
        [JsonProperty("lifeSec", Required = JsonRequired.Always)] public float LifeSec { get; private set; }
        [JsonProperty("poolPrewarm", Required = JsonRequired.Always)] public long PoolPrewarm { get; private set; }
        [JsonProperty("followTarget", Required = JsonRequired.Always)] public bool FollowTarget { get; private set; }
        [JsonProperty("sortingLayer", Required = JsonRequired.Always)] public string SortingLayer { get; private set; } = string.Empty;
        [JsonProperty("sortingOrder", Required = JsonRequired.Always)] public long SortingOrder { get; private set; }
    }

    /// <summary>描述命中等战斗事件的特效、音频、时间、震屏、震动和伤害数字表现。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class FeedbackCueConfig
    {
        // 此表只定义表现参数，不改变伤害、得分或战斗结算真相。
        [JsonProperty("feedbackId", Required = JsonRequired.Always)] public string FeedbackId { get; private set; } = string.Empty;
        [JsonProperty("vfxKey", Required = JsonRequired.Always)] public string VfxKey { get; private set; } = string.Empty;
        [JsonProperty("audioKey", Required = JsonRequired.Always)] public string AudioKey { get; private set; } = string.Empty;
        [JsonProperty("timeScale", Required = JsonRequired.Always)] public float TimeScale { get; private set; }
        [JsonProperty("timeScaleSec", Required = JsonRequired.Always)] public float TimeScaleSec { get; private set; }
        [JsonProperty("flashSec", Required = JsonRequired.Always)] public float FlashSec { get; private set; }
        [JsonProperty("shakeStrengthRefPx", Required = JsonRequired.Always)] public float ShakeStrengthRefPx { get; private set; }
        [JsonProperty("shakeSec", Required = JsonRequired.Always)] public float ShakeSec { get; private set; }
        [JsonProperty("vibrationPattern", Required = JsonRequired.Always)] public string VibrationPattern { get; private set; } = string.Empty;
        [JsonProperty("damageNumberColorHex", Required = JsonRequired.Always)] public string DamageNumberColorHex { get; private set; } = string.Empty;
        [JsonProperty("damageNumberFontSizeRefPx", Required = JsonRequired.Always)] public long DamageNumberFontSizeRefPx { get; private set; }
        [JsonProperty("damageNumberLifeSec", Required = JsonRequired.Always)] public float DamageNumberLifeSec { get; private set; }
        [JsonProperty("damageNumberRiseRefPx", Required = JsonRequired.Always)] public float DamageNumberRiseRefPx { get; private set; }
        [JsonProperty("vfxTintColorHex", Required = JsonRequired.Always)] public string VfxTintColorHex { get; private set; } = string.Empty;
        [JsonProperty("vfxScaleRefPx", Required = JsonRequired.Always)] public float VfxScaleRefPx { get; private set; }
        [JsonProperty("description", Required = JsonRequired.Always)] public string Description { get; private set; } = string.Empty;
    }

    /// <summary>描述画笔分层颜色、宽度与确定性电弧参数。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class StrokeTrailStyleConfig
    {
        // 架势只选择样式ID并提供基础笔宽；本表保存全部可调视觉数值。
        [JsonProperty("styleId", Required = JsonRequired.Always)] public string StyleId { get; private set; } = string.Empty;
        [JsonProperty("outerColorHex", Required = JsonRequired.Always)] public string OuterColorHex { get; private set; } = string.Empty;
        [JsonProperty("bodyColorHex", Required = JsonRequired.Always)] public string BodyColorHex { get; private set; } = string.Empty;
        [JsonProperty("coreColorHex", Required = JsonRequired.Always)] public string CoreColorHex { get; private set; } = string.Empty;
        [JsonProperty("outerWidthMultiplier", Required = JsonRequired.Always)] public float OuterWidthMultiplier { get; private set; }
        [JsonProperty("bodyWidthMultiplier", Required = JsonRequired.Always)] public float BodyWidthMultiplier { get; private set; }
        [JsonProperty("coreWidthMultiplier", Required = JsonRequired.Always)] public float CoreWidthMultiplier { get; private set; }
        [JsonProperty("branchColorHex", Required = JsonRequired.Always)] public string BranchColorHex { get; private set; } = string.Empty;
        [JsonProperty("branchSpacingRefPx", Required = JsonRequired.Always)] public float BranchSpacingRefPx { get; private set; }
        [JsonProperty("branchLengthRefPx", Required = JsonRequired.Always)] public float BranchLengthRefPx { get; private set; }
        [JsonProperty("branchJitterRefPx", Required = JsonRequired.Always)] public float BranchJitterRefPx { get; private set; }
        [JsonProperty("branchWidthMultiplier", Required = JsonRequired.Always)] public float BranchWidthMultiplier { get; private set; }
        [JsonProperty("branchSegmentCount", Required = JsonRequired.Always)] public long BranchSegmentCount { get; private set; }
    }

    /// <summary>描述配置资源键所要求的 Unity 资源类型、路径和 MVP 必需性。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AssetManifestConfig
    {
        // 运行时注册表必须覆盖清单且类型一致，不能额外注册未声明资源。
        [JsonProperty("assetKey", Required = JsonRequired.Always)] public string AssetKey { get; private set; } = string.Empty;
        [JsonProperty("assetType", Required = JsonRequired.Always)] public string AssetType { get; private set; } = string.Empty;
        [JsonProperty("addressOrPath", Required = JsonRequired.Always)] public string AddressOrPath { get; private set; } = string.Empty;
        [JsonProperty("requiredInMvp", Required = JsonRequired.Always)] public bool RequiredInMvp { get; private set; }
        [JsonProperty("notes", Required = JsonRequired.Always)] public string Notes { get; private set; } = string.Empty;
    }

    /// <summary>描述配置字段允许使用的枚举值及其展示名称。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnumConfig
    {
        // EnumType 与 Value 组成复合唯一键，供导出器执行字段约束。
        [JsonProperty("enumType", Required = JsonRequired.Always)] public string EnumType { get; private set; } = string.Empty;
        [JsonProperty("value", Required = JsonRequired.Always)] public string Value { get; private set; } = string.Empty;
        [JsonProperty("displayName", Required = JsonRequired.Always)] public string DisplayName { get; private set; } = string.Empty;
        [JsonProperty("description", Required = JsonRequired.Always)] public string Description { get; private set; } = string.Empty;
    }

    /// <summary>描述每张配置表字段的类型、必填、范围、枚举和外键约束。</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class FieldDictionaryConfig
    {
        // 字段字典随配置一同导出，便于追踪数据合同；运行时不重新解释 Excel。
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
