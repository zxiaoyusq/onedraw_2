using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneStrokeDemon.Config
{
    internal sealed class GameplayConfigSnapshot
    {
        private GameplayConfigSnapshot(GameplayConfigDocument document, string source)
        {
            Document = document;
            Globals = BuildIndex(document.GlobalRows, row => row.Key, "Global", "key", source);
            Players = BuildIndex(document.PlayerRows, row => row.PlayerId, "Players", "playerId", source);
            Stances = BuildIndex(document.StanceRows, row => row.StanceId, "Stances", "stanceId", source);
            StrokeRules = BuildIndex(document.StrokeRuleRows, row => row.RuleId, "StrokeRules", "ruleId", source);
            StrokeRuleEntries = new ReadOnlyCollection<StrokeRuleConfig>(
                (StrokeRuleConfig[])document.StrokeRuleRows.Clone());
            DamageFormulas = BuildIndex(document.DamageFormulaRows, row => row.FormulaId, "DamageFormulas", "formulaId", source);
            DefenseRules = BuildIndex(document.DefenseRuleRows, row => row.DefenseRuleId, "DefenseRules", "defenseRuleId", source);
            WeakpointRules = BuildIndex(document.WeakpointRuleRows, row => row.WeakpointRuleId, "WeakpointRules", "weakpointRuleId", source);
            MovePatterns = BuildIndex(document.MovePatternRows, row => row.MovePatternId, "MovePatterns", "movePatternId", source);
            Enemies = BuildIndex(document.EnemyRows, row => row.EnemyId, "Enemies", "enemyId", source);
            EnemyEntries = new ReadOnlyCollection<EnemyConfig>(
                (EnemyConfig[])document.EnemyRows.Clone());
            EnemyAttacks = BuildIndex(document.EnemyAttackRows, row => row.AttackId, "EnemyAttacks", "attackId", source);
            Projectiles = BuildIndex(document.ProjectileRows, row => row.ProjectileId, "Projectiles", "projectileId", source);
            Buffs = BuildIndex(document.BuffRows, row => row.BuffId, "Buffs", "buffId", source);
            Skills = BuildIndex(document.SkillRows, row => row.SkillId, "Skills", "skillId", source);
            Levels = BuildIndex(document.LevelRows, row => row.LevelId, "Levels", "levelId", source);
            LevelEntries = new ReadOnlyCollection<LevelConfig>(
                (LevelConfig[])document.LevelRows.Clone());
            Waves = BuildIndex(document.WaveRows, row => row.WaveId, "Waves", "waveId", source);
            SpawnPoints = BuildIndex(document.SpawnPointRows, row => row.SpawnPointId, "SpawnPoints", "spawnPointId", source);
            EnemyModifiers = BuildIndex(document.EnemyModifierRows, row => row.ModifierId, "EnemyModifiers", "modifierId", source);
            Spawns = BuildIndex(document.SpawnRows, row => row.SpawnId, "Spawns", "spawnId", source);
            BossPhases = BuildIndex(document.BossPhaseRows, row => row.BossPhaseId, "BossPhases", "bossPhaseId", source);
            Texts = BuildIndex(document.TextRows, row => row.TextKey, "Texts", "textKey", source);
            AudioCues = BuildIndex(document.AudioCueRows, row => row.AudioKey, "AudioCues", "audioKey", source);
            VfxCues = BuildIndex(document.VfxCueRows, row => row.VfxKey, "VfxCues", "vfxKey", source);
            Assets = BuildIndex(document.AssetManifestRows, row => row.AssetKey, "AssetManifest", "assetKey", source);
            FeedbackCues = BuildIndex(document.FeedbackCueRows, row => row.FeedbackId, "FeedbackCues", "feedbackId", source);
            AssetManifestEntries = new ReadOnlyCollection<AssetManifestConfig>(
                (AssetManifestConfig[])document.AssetManifestRows.Clone());

            AttacksBySet = BuildGroups(document.EnemyAttackRows, row => row.AttackSetId, "EnemyAttacks", "attackSetId", source);
            EffectsByGroup = BuildGroups(document.SkillEffectRows, row => row.EffectGroupId, "SkillEffects", "effectGroupId", source);
            WavesByLevel = BuildGroups(document.WaveRows, row => row.LevelId, "Waves", "levelId", source);
            SpawnsByWave = BuildGroups(document.SpawnRows, row => row.WaveId, "Spawns", "waveId", source);
            PhasesByEnemy = BuildGroups(document.BossPhaseRows, row => row.EnemyId, "BossPhases", "enemyId", source);
            RewardsByTable = BuildGroups(document.RewardRows, row => row.RewardTableId, "Rewards", "rewardTableId", source);
            TutorialsById = BuildGroups(document.TutorialRows, row => row.TutorialId, "Tutorials", "tutorialId", source);

            ValidateCompositeKeys(
                document.SkillEffectRows,
                row => $"{row.EffectGroupId}\u001f{row.Order}",
                "SkillEffects",
                "effectGroupId+order",
                source);
            ValidateCompositeKeys(
                document.RewardRows,
                row => $"{row.RewardTableId}\u001f{row.Order}",
                "Rewards",
                "rewardTableId+order",
                source);
            ValidateCompositeKeys(
                document.TutorialRows,
                row => $"{row.TutorialId}\u001f{row.Order}",
                "Tutorials",
                "tutorialId+order",
                source);
            ValidateCompositeKeys(
                document.EnumRows,
                row => $"{row.EnumType}\u001f{row.Value}",
                "Enums",
                "enumType+value",
                source);
            ValidateCompositeKeys(
                document.FieldDictionaryRows,
                row => $"{row.Sheet}\u001f{row.Field}",
                "FieldDictionary",
                "sheet+field",
                source);

            PrimaryIndexCount = Globals.Count + Players.Count + Stances.Count + StrokeRules.Count +
                DamageFormulas.Count + DefenseRules.Count + WeakpointRules.Count + MovePatterns.Count +
                Enemies.Count + EnemyAttacks.Count + Projectiles.Count + Buffs.Count + Skills.Count + Levels.Count +
                Waves.Count + SpawnPoints.Count + EnemyModifiers.Count + Spawns.Count + BossPhases.Count + Texts.Count +
                AudioCues.Count + VfxCues.Count + Assets.Count + FeedbackCues.Count;
            GroupIndexCount = AttacksBySet.Count + EffectsByGroup.Count + WavesByLevel.Count + SpawnsByWave.Count +
                PhasesByEnemy.Count + RewardsByTable.Count + TutorialsById.Count;
        }

        public GameplayConfigDocument Document { get; }
        public IReadOnlyDictionary<string, GlobalConfig> Globals { get; }
        public IReadOnlyDictionary<string, PlayerConfig> Players { get; }
        public IReadOnlyDictionary<string, StanceConfig> Stances { get; }
        public IReadOnlyDictionary<string, StrokeRuleConfig> StrokeRules { get; }
        public IReadOnlyList<StrokeRuleConfig> StrokeRuleEntries { get; }
        public IReadOnlyDictionary<string, DamageFormulaConfig> DamageFormulas { get; }
        public IReadOnlyDictionary<string, DefenseRuleConfig> DefenseRules { get; }
        public IReadOnlyDictionary<string, WeakpointRuleConfig> WeakpointRules { get; }
        public IReadOnlyDictionary<string, MovePatternConfig> MovePatterns { get; }
        public IReadOnlyDictionary<string, EnemyConfig> Enemies { get; }
        public IReadOnlyList<EnemyConfig> EnemyEntries { get; }
        public IReadOnlyDictionary<string, EnemyAttackConfig> EnemyAttacks { get; }
        public IReadOnlyDictionary<string, ProjectileConfig> Projectiles { get; }
        public IReadOnlyDictionary<string, BuffConfig> Buffs { get; }
        public IReadOnlyDictionary<string, SkillConfig> Skills { get; }
        public IReadOnlyDictionary<string, LevelConfig> Levels { get; }
        public IReadOnlyList<LevelConfig> LevelEntries { get; }
        public IReadOnlyDictionary<string, WaveConfig> Waves { get; }
        public IReadOnlyDictionary<string, SpawnPointConfig> SpawnPoints { get; }
        public IReadOnlyDictionary<string, EnemyModifierConfig> EnemyModifiers { get; }
        public IReadOnlyDictionary<string, SpawnConfig> Spawns { get; }
        public IReadOnlyDictionary<string, BossPhaseConfig> BossPhases { get; }
        public IReadOnlyDictionary<string, TextConfig> Texts { get; }
        public IReadOnlyDictionary<string, AudioCueConfig> AudioCues { get; }
        public IReadOnlyDictionary<string, VfxCueConfig> VfxCues { get; }
        public IReadOnlyDictionary<string, AssetManifestConfig> Assets { get; }
        public IReadOnlyDictionary<string, FeedbackCueConfig> FeedbackCues { get; }
        public IReadOnlyList<AssetManifestConfig> AssetManifestEntries { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<EnemyAttackConfig>> AttacksBySet { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<SkillEffectConfig>> EffectsByGroup { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<WaveConfig>> WavesByLevel { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<SpawnConfig>> SpawnsByWave { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<BossPhaseConfig>> PhasesByEnemy { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<RewardConfig>> RewardsByTable { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<TutorialConfig>> TutorialsById { get; }
        public int PrimaryIndexCount { get; }
        public int GroupIndexCount { get; }

        public static GameplayConfigSnapshot Create(GameplayConfigDocument document, string source)
        {
            return new GameplayConfigSnapshot(document, source);
        }

        private static IReadOnlyDictionary<string, T> BuildIndex<T>(
            IReadOnlyList<T> rows,
            Func<T, string> keySelector,
            string table,
            string field,
            string source)
            where T : class
        {
            if (rows == null)
            {
                throw StructureFailure(source, table, "Table array is null.");
            }

            var result = new Dictionary<string, T>(rows.Count, StringComparer.Ordinal);
            for (int index = 0; index < rows.Count; index += 1)
            {
                T row = rows[index];
                if (row == null)
                {
                    throw StructureFailure(source, $"{table}[{index}]", "Row is null.");
                }

                string key = keySelector(row);
                if (string.IsNullOrEmpty(key))
                {
                    throw StructureFailure(source, $"{table}[{index}].{field}", "Index key is empty.");
                }

                if (result.ContainsKey(key))
                {
                    throw StructureFailure(source, $"{table}.{field}", $"Duplicate index key '{key}'.");
                }

                result.Add(key, row);
            }

            return new ReadOnlyDictionary<string, T>(result);
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<T>> BuildGroups<T>(
            IReadOnlyList<T> rows,
            Func<T, string> keySelector,
            string table,
            string field,
            string source)
            where T : class
        {
            var mutable = new Dictionary<string, List<T>>(StringComparer.Ordinal);
            for (int index = 0; index < rows.Count; index += 1)
            {
                T row = rows[index];
                if (row == null)
                {
                    throw StructureFailure(source, $"{table}[{index}]", "Row is null.");
                }

                string key = keySelector(row);
                if (string.IsNullOrEmpty(key))
                {
                    throw StructureFailure(source, $"{table}[{index}].{field}", "Group key is empty.");
                }

                if (!mutable.TryGetValue(key, out List<T> group))
                {
                    group = new List<T>();
                    mutable.Add(key, group);
                }

                group.Add(row);
            }

            var result = new Dictionary<string, IReadOnlyList<T>>(mutable.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<T>> pair in mutable)
            {
                result.Add(pair.Key, new ReadOnlyCollection<T>(pair.Value));
            }

            return new ReadOnlyDictionary<string, IReadOnlyList<T>>(result);
        }

        private static void ValidateCompositeKeys<T>(
            IReadOnlyList<T> rows,
            Func<T, string> keySelector,
            string table,
            string fields,
            string source)
            where T : class
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < rows.Count; index += 1)
            {
                T row = rows[index];
                if (row == null)
                {
                    throw StructureFailure(source, $"{table}[{index}]", "Row is null.");
                }

                string key = keySelector(row);
                if (!keys.Add(key))
                {
                    throw StructureFailure(source, $"{table}.{fields}", $"Duplicate composite key '{key}'.");
                }
            }
        }

        private static GameplayConfigException StructureFailure(string source, string context, string message)
        {
            return new GameplayConfigException("CFGRT006", message, source, context);
        }
    }
}
