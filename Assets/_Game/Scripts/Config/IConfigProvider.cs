using System.Collections.Generic;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 向玩法系统提供只读、强类型的配置查询入口，屏蔽 JSON 与索引实现细节。
    /// </summary>
    public interface IConfigProvider
    {
        /// <summary>获取配置结构版本。</summary>
        long SchemaVersion { get; }

        /// <summary>获取配置内容版本。</summary>
        string ContentVersion { get; }

        /// <summary>获取规范化配置内容哈希。</summary>
        string ContentHash { get; }

        /// <summary>按键获取全局配置。</summary>
        GlobalConfig GetGlobal(string key);

        /// <summary>按玩家 ID 获取玩家配置。</summary>
        PlayerConfig GetPlayer(string playerId);

        /// <summary>按架势 ID 获取架势配置。</summary>
        StanceConfig GetStance(string stanceId);

        /// <summary>按规则 ID 获取笔画规则。</summary>
        StrokeRuleConfig GetStrokeRule(string ruleId);

        /// <summary>获取全部笔画规则的只读列表。</summary>
        IReadOnlyList<StrokeRuleConfig> GetStrokeRules();

        /// <summary>按公式 ID 获取伤害公式。</summary>
        DamageFormulaConfig GetDamageFormula(string formulaId);

        /// <summary>按防御规则 ID 获取防御配置。</summary>
        DefenseRuleConfig GetDefenseRule(string defenseRuleId);

        /// <summary>按弱点规则 ID 获取弱点配置。</summary>
        WeakpointRuleConfig GetWeakpointRule(string weakpointRuleId);

        /// <summary>按移动模式 ID 获取移动配置。</summary>
        MovePatternConfig GetMovePattern(string movePatternId);

        /// <summary>按敌人 ID 获取敌人配置。</summary>
        EnemyConfig GetEnemy(string enemyId);

        /// <summary>获取全部敌人配置的只读列表。</summary>
        IReadOnlyList<EnemyConfig> GetEnemies();

        /// <summary>按攻击集合 ID 获取有序攻击配置。</summary>
        IReadOnlyList<EnemyAttackConfig> GetEnemyAttacks(string attackSetId);

        /// <summary>按投射物 ID 获取投射物配置。</summary>
        ProjectileConfig GetProjectile(string projectileId);

        /// <summary>按增益 ID 获取增益配置。</summary>
        BuffConfig GetBuff(string buffId);

        /// <summary>按技能 ID 获取技能配置。</summary>
        SkillConfig GetSkill(string skillId);

        /// <summary>按效果组 ID 获取有序技能效果。</summary>
        IReadOnlyList<SkillEffectConfig> GetSkillEffects(string effectGroupId);

        /// <summary>按关卡 ID 获取关卡配置。</summary>
        LevelConfig GetLevel(string levelId);

        /// <summary>获取全部关卡配置的只读列表。</summary>
        IReadOnlyList<LevelConfig> GetLevels();

        /// <summary>按关卡 ID 获取有序波次配置。</summary>
        IReadOnlyList<WaveConfig> GetWaves(string levelId);

        /// <summary>按出生点 ID 获取出生点配置。</summary>
        SpawnPointConfig GetSpawnPoint(string spawnPointId);

        /// <summary>按修饰器 ID 获取敌人修饰配置。</summary>
        EnemyModifierConfig GetEnemyModifier(string modifierId);

        /// <summary>按波次 ID 获取有序出生配置。</summary>
        IReadOnlyList<SpawnConfig> GetSpawns(string waveId);

        /// <summary>按敌人 ID 获取有序 Boss 阶段配置。</summary>
        IReadOnlyList<BossPhaseConfig> GetBossPhases(string enemyId);

        /// <summary>按奖励表 ID 获取有序奖励配置。</summary>
        IReadOnlyList<RewardConfig> GetRewards(string rewardTableId);

        /// <summary>按教程 ID 获取有序教程步骤。</summary>
        IReadOnlyList<TutorialConfig> GetTutorialSteps(string tutorialId);

        /// <summary>按文本键获取本地化文本配置。</summary>
        TextConfig GetText(string textKey);

        /// <summary>按音频键获取音频提示配置。</summary>
        AudioCueConfig GetAudioCue(string audioKey);

        /// <summary>按特效键获取视觉特效提示配置。</summary>
        VfxCueConfig GetVfxCue(string vfxKey);

        /// <summary>按反馈 ID 获取组合战斗反馈配置。</summary>
        FeedbackCueConfig GetFeedbackCue(string feedbackId);

        /// <summary>按资源键获取资源清单配置。</summary>
        AssetManifestConfig GetAsset(string assetKey);

        /// <summary>获取完整资源清单的只读列表。</summary>
        IReadOnlyList<AssetManifestConfig> GetAssetManifest();
    }
}
