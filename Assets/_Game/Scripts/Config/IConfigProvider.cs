using System.Collections.Generic;

namespace OneStrokeDemon.Config
{
    public interface IConfigProvider
    {
        long SchemaVersion { get; }

        string ContentVersion { get; }

        string ContentHash { get; }

        GlobalConfig GetGlobal(string key);
        PlayerConfig GetPlayer(string playerId);
        StanceConfig GetStance(string stanceId);
        StrokeRuleConfig GetStrokeRule(string ruleId);
        IReadOnlyList<StrokeRuleConfig> GetStrokeRules();
        DamageFormulaConfig GetDamageFormula(string formulaId);
        DefenseRuleConfig GetDefenseRule(string defenseRuleId);
        WeakpointRuleConfig GetWeakpointRule(string weakpointRuleId);
        MovePatternConfig GetMovePattern(string movePatternId);
        EnemyConfig GetEnemy(string enemyId);
        IReadOnlyList<EnemyConfig> GetEnemies();
        IReadOnlyList<EnemyAttackConfig> GetEnemyAttacks(string attackSetId);
        ProjectileConfig GetProjectile(string projectileId);
        BuffConfig GetBuff(string buffId);
        SkillConfig GetSkill(string skillId);
        IReadOnlyList<SkillEffectConfig> GetSkillEffects(string effectGroupId);
        LevelConfig GetLevel(string levelId);
        IReadOnlyList<LevelConfig> GetLevels();
        IReadOnlyList<WaveConfig> GetWaves(string levelId);
        SpawnPointConfig GetSpawnPoint(string spawnPointId);
        EnemyModifierConfig GetEnemyModifier(string modifierId);
        IReadOnlyList<SpawnConfig> GetSpawns(string waveId);
        IReadOnlyList<BossPhaseConfig> GetBossPhases(string enemyId);
        IReadOnlyList<RewardConfig> GetRewards(string rewardTableId);
        IReadOnlyList<TutorialConfig> GetTutorialSteps(string tutorialId);
        TextConfig GetText(string textKey);
        AudioCueConfig GetAudioCue(string audioKey);
        VfxCueConfig GetVfxCue(string vfxKey);
        FeedbackCueConfig GetFeedbackCue(string feedbackId);
        AssetManifestConfig GetAsset(string assetKey);
        IReadOnlyList<AssetManifestConfig> GetAssetManifest();
    }
}
