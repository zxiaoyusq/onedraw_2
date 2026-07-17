using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Levels
{
    // 定义 WaveStartTrigger 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum WaveStartTrigger
    {
        None = 0,
        LevelStart = 1,
        PlayerConfirmed = 2,
        PreviousWaveEnd = 3,
        TimeElapsed = 4,
    }

    // 定义 WaveEndCondition 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum WaveEndCondition
    {
        None = 0,
        AllEnemiesDefeated = 1,
        BossDefeated = 2,
        PlayerConfirmed = 3,
        TimeElapsed = 4,
    }

    // 定义 SpawnPattern 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum SpawnPattern
    {
        None = 0,
        Line = 1,
        Scatter = 2,
        Single = 3,
        Stagger = 4,
    }

    // 定义 SpawnLane 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum SpawnLane
    {
        None = 0,
        Air = 1,
        Boss = 2,
        Ground = 3,
    }

    // 定义 SpawnFacing 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum SpawnFacing
    {
        None = 0,
        Left = 1,
        Right = 2,
    }

    // 定义 NormalizedSpawnPosition 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct NormalizedSpawnPosition
    {
        // 初始化 NormalizedSpawnPosition，并建立关卡流程所需的初始状态。
        internal NormalizedSpawnPosition(double x, double y)
        {
            X = Clamp01(x);
            Y = Clamp01(y);
        }

        public double X { get; }

        public double Y { get; }

        public bool IsNormalized =>
            IsFinite(X) &&
            IsFinite(Y) &&
            X >= 0d && X <= 1d &&
            Y >= 0d && Y <= 1d;

        // 处理 Clamp01 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static double Clamp01(double value)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (value < 0d)
            {
                return 0d;
            }

            return value > 1d ? 1d : value;
        }

        // 判断是否 IsFinite 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    // 定义 SpawnPointDefinition 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct SpawnPointDefinition
    {
        // 初始化 SpawnPointDefinition，并建立关卡流程所需的初始状态。
        internal SpawnPointDefinition(
            string spawnPointId,
            string levelId,
            double normalizedX,
            double normalizedY,
            SpawnLane lane,
            double jitterX,
            double jitterY,
            SpawnFacing facing)
        {
            SpawnPointId = spawnPointId;
            LevelId = levelId;
            NormalizedX = normalizedX;
            NormalizedY = normalizedY;
            Lane = lane;
            JitterX = jitterX;
            JitterY = jitterY;
            Facing = facing;
            IsConfigured = true;
        }

        public string SpawnPointId { get; }

        public string LevelId { get; }

        public double NormalizedX { get; }

        public double NormalizedY { get; }

        public SpawnLane Lane { get; }

        public double JitterX { get; }

        public double JitterY { get; }

        public SpawnFacing Facing { get; }

        public bool IsConfigured { get; }
    }

    // 定义 EnemyModifierDefinition 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct EnemyModifierDefinition
    {
        // 初始化 EnemyModifierDefinition，并建立关卡流程所需的初始状态。
        internal EnemyModifierDefinition(
            string modifierId,
            string displayNameKey,
            double hpMultiplier,
            double damageMultiplier,
            double speedMultiplier,
            double scoreMultiplier,
            string tintHex,
            string extraBuffId)
        {
            ModifierId = modifierId;
            DisplayNameKey = displayNameKey;
            HpMultiplier = hpMultiplier;
            DamageMultiplier = damageMultiplier;
            SpeedMultiplier = speedMultiplier;
            ScoreMultiplier = scoreMultiplier;
            TintHex = tintHex;
            ExtraBuffId = extraBuffId;
            IsConfigured = true;
        }

        public string ModifierId { get; }

        public string DisplayNameKey { get; }

        public double HpMultiplier { get; }

        public double DamageMultiplier { get; }

        public double SpeedMultiplier { get; }

        public double ScoreMultiplier { get; }

        public string TintHex { get; }

        public string ExtraBuffId { get; }

        public bool IsConfigured { get; }
    }

    // 定义 SpawnDefinition 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class SpawnDefinition
    {
        // 初始化 SpawnDefinition，并建立关卡流程所需的初始状态。
        internal SpawnDefinition(
            string spawnId,
            string waveId,
            double spawnTimeSeconds,
            string enemyId,
            bool isBoss,
            int count,
            double intervalSeconds,
            in SpawnPointDefinition spawnPoint,
            SpawnPattern pattern,
            in EnemyModifierDefinition modifier)
        {
            SpawnId = spawnId;
            WaveId = waveId;
            SpawnTimeSeconds = spawnTimeSeconds;
            EnemyId = enemyId;
            IsBoss = isBoss;
            Count = count;
            IntervalSeconds = intervalSeconds;
            SpawnPoint = spawnPoint;
            Pattern = pattern;
            Modifier = modifier;
        }

        public string SpawnId { get; }

        public string WaveId { get; }

        public double SpawnTimeSeconds { get; }

        public string EnemyId { get; }

        public bool IsBoss { get; }

        public int Count { get; }

        public double IntervalSeconds { get; }

        public SpawnPointDefinition SpawnPoint { get; }

        public SpawnPattern Pattern { get; }

        public EnemyModifierDefinition Modifier { get; }
    }

    // 定义 WaveDefinition 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class WaveDefinition
    {
        // 初始化 WaveDefinition，并建立关卡流程所需的初始状态。
        internal WaveDefinition(
            string waveId,
            string levelId,
            int order,
            WaveStartTrigger startTrigger,
            double startDelaySeconds,
            WaveEndCondition endCondition,
            double endDelaySeconds,
            string musicKey,
            int maxAlive,
            IReadOnlyList<SpawnDefinition> spawns)
        {
            WaveId = waveId;
            LevelId = levelId;
            Order = order;
            StartTrigger = startTrigger;
            StartDelaySeconds = startDelaySeconds;
            EndCondition = endCondition;
            EndDelaySeconds = endDelaySeconds;
            MusicKey = musicKey;
            MaxAlive = maxAlive;
            Spawns = spawns;
        }

        public string WaveId { get; }

        public string LevelId { get; }

        public int Order { get; }

        public WaveStartTrigger StartTrigger { get; }

        public double StartDelaySeconds { get; }

        public WaveEndCondition EndCondition { get; }

        public double EndDelaySeconds { get; }

        public string MusicKey { get; }

        public int MaxAlive { get; }

        public IReadOnlyList<SpawnDefinition> Spawns { get; }
    }

    // 定义 LevelDefinition 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class LevelDefinition
    {
        // 初始化 LevelDefinition，并建立关卡流程所需的初始状态。
        internal LevelDefinition(
            string levelId,
            string displayNameKey,
            string sceneKey,
            string backgroundAssetKey,
            double durationLimitSeconds,
            string bossEnemyId,
            IReadOnlyList<WaveDefinition> waves)
        {
            LevelId = levelId;
            DisplayNameKey = displayNameKey;
            SceneKey = sceneKey;
            BackgroundAssetKey = backgroundAssetKey;
            DurationLimitSeconds = durationLimitSeconds;
            BossEnemyId = bossEnemyId;
            Waves = waves;
        }

        public string LevelId { get; }

        public string DisplayNameKey { get; }

        public string SceneKey { get; }

        public string BackgroundAssetKey { get; }

        public double DurationLimitSeconds { get; }

        public string BossEnemyId { get; }

        public IReadOnlyList<WaveDefinition> Waves { get; }
    }

    // 定义 LevelCatalog 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public static class LevelCatalog
    {
        // 创建 Create 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public static LevelDefinition Create(IConfigProvider configProvider, string levelId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            LevelConfig level = configProvider.GetLevel(levelId);
            RequireNonEmpty(level.LevelId, "levelId");
            RequirePositive(level.DurationLimitSec, level.LevelId, "durationLimitSec");

            IReadOnlyList<WaveConfig> configuredWaves = configProvider.GetWaves(level.LevelId);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (configuredWaves.Count == 0)
            {
                throw Invalid(level.LevelId, "level must contain at least one wave");
            }

            var rows = new WaveConfig[configuredWaves.Count];
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < rows.Length; index++)
            {
                rows[index] = configuredWaves[index] ??
                    throw Invalid(level.LevelId, "wave row cannot be null");
            }

            Array.Sort(rows, WaveOrderComparer.Instance);
            var waves = new WaveDefinition[rows.Length];
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < rows.Length; index++)
            {
                WaveConfig row = rows[index];
                int expectedOrder = index + 1;
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!string.Equals(row.LevelId, level.LevelId, StringComparison.Ordinal) ||
                    row.Order != expectedOrder)
                {
                    throw Invalid(
                        level.LevelId,
                        "waves must have matching ownership and contiguous order starting at 1");
                }

                WaveStartTrigger startTrigger = ParseStartTrigger(row.StartTrigger, row.WaveId);
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (index == 0 && startTrigger == WaveStartTrigger.PreviousWaveEnd)
                {
                    throw Invalid(row.WaveId, "first wave cannot use PreviousWaveEnd");
                }

                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (index > 0 && startTrigger == WaveStartTrigger.LevelStart)
                {
                    throw Invalid(row.WaveId, "only the first wave may use LevelStart");
                }

                WaveEndCondition endCondition = ParseEndCondition(row.EndCondition, row.WaveId);
                RequireFiniteNonNegative(row.StartDelaySec, row.WaveId, "startDelaySec");
                RequireFiniteNonNegative(row.EndDelaySec, row.WaveId, "endDelaySec");
                RequirePositive(row.MaxAlive, row.WaveId, "maxAlive");
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (row.MaxAlive > int.MaxValue)
                {
                    throw Invalid(row.WaveId, "maxAlive exceeds Int32 range");
                }

                IReadOnlyList<SpawnDefinition> spawns = CreateSpawns(
                    configProvider,
                    level,
                    row);
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (endCondition == WaveEndCondition.BossDefeated)
                {
                    ValidateBossWave(level, row, spawns);
                }

                waves[index] = new WaveDefinition(
                    row.WaveId,
                    row.LevelId,
                    expectedOrder,
                    startTrigger,
                    row.StartDelaySec,
                    endCondition,
                    row.EndDelaySec,
                    row.MusicKey,
                    (int)row.MaxAlive,
                    spawns);
            }

            return new LevelDefinition(
                level.LevelId,
                level.DisplayNameKey,
                level.SceneKey,
                level.BackgroundAssetKey,
                level.DurationLimitSec,
                level.BossEnemyId,
                Array.AsReadOnly(waves));
        }

        // 创建 CreateSpawns 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static IReadOnlyList<SpawnDefinition> CreateSpawns(
            IConfigProvider configProvider,
            LevelConfig level,
            WaveConfig wave)
        {
            IReadOnlyList<SpawnConfig> configured = configProvider.GetSpawns(wave.WaveId);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (configured.Count == 0)
            {
                throw Invalid(wave.WaveId, "wave must contain at least one spawn row");
            }

            var definitions = new SpawnDefinition[configured.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < configured.Count; index++)
            {
                SpawnConfig row = configured[index] ??
                    throw Invalid(wave.WaveId, "spawn row cannot be null");
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!string.Equals(row.WaveId, wave.WaveId, StringComparison.Ordinal))
                {
                    throw Invalid(row.SpawnId, "spawn wave ownership does not match");
                }

                RequireNonEmpty(row.SpawnId, "spawnId");
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!ids.Add(row.SpawnId))
                {
                    throw Invalid(row.SpawnId, "duplicate spawn id");
                }

                RequireFiniteNonNegative(row.SpawnTimeSec, row.SpawnId, "spawnTimeSec");
                RequireFiniteNonNegative(row.IntervalSec, row.SpawnId, "intervalSec");
                RequirePositive(row.Count, row.SpawnId, "count");
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (row.Count > int.MaxValue)
                {
                    throw Invalid(row.SpawnId, "count exceeds Int32 range");
                }

                EnemyConfig enemy = configProvider.GetEnemy(row.EnemyId);
                SpawnPointDefinition point = CreateSpawnPoint(
                    configProvider.GetSpawnPoint(row.SpawnPointId),
                    level.LevelId,
                    row.SpawnId);
                EnemyModifierDefinition modifier = CreateModifier(
                    configProvider,
                    configProvider.GetEnemyModifier(row.ModifierId),
                    row.SpawnId);
                definitions[index] = new SpawnDefinition(
                    row.SpawnId,
                    row.WaveId,
                    row.SpawnTimeSec,
                    enemy.EnemyId,
                    string.Equals(enemy.Tier, "Boss", StringComparison.Ordinal),
                    (int)row.Count,
                    row.IntervalSec,
                    point,
                    ParseSpawnPattern(row.SpawnPattern, row.SpawnId),
                    modifier);
            }

            Array.Sort(definitions, SpawnDefinitionComparer.Instance);
            return Array.AsReadOnly(definitions);
        }

        // 创建 CreateSpawnPoint 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static SpawnPointDefinition CreateSpawnPoint(
            SpawnPointConfig row,
            string levelId,
            string spawnId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!string.Equals(row.LevelId, "*", StringComparison.Ordinal) &&
                !string.Equals(row.LevelId, levelId, StringComparison.Ordinal))
            {
                throw Invalid(
                    spawnId,
                    $"spawn point '{row.SpawnPointId}' is not scoped to level '{levelId}'");
            }

            RequireUnit(row.NormalizedX, row.SpawnPointId, "normalizedX");
            RequireUnit(row.NormalizedY, row.SpawnPointId, "normalizedY");
            RequireUnit(row.JitterX, row.SpawnPointId, "jitterX");
            RequireUnit(row.JitterY, row.SpawnPointId, "jitterY");
            return new SpawnPointDefinition(
                row.SpawnPointId,
                row.LevelId,
                row.NormalizedX,
                row.NormalizedY,
                ParseLane(row.Lane, row.SpawnPointId),
                row.JitterX,
                row.JitterY,
                ParseFacing(row.Facing, row.SpawnPointId));
        }

        // 创建 CreateModifier 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static EnemyModifierDefinition CreateModifier(
            IConfigProvider configProvider,
            EnemyModifierConfig row,
            string spawnId)
        {
            RequirePositiveFinite(row.HpMultiplier, row.ModifierId, "hpMultiplier");
            RequirePositiveFinite(row.DamageMultiplier, row.ModifierId, "damageMultiplier");
            RequirePositiveFinite(row.SpeedMultiplier, row.ModifierId, "speedMultiplier");
            RequirePositiveFinite(row.ScoreMultiplier, row.ModifierId, "scoreMultiplier");
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!IsHexColor(row.TintHex))
            {
                throw Invalid(row.ModifierId, "tintHex must be #RRGGBB");
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!string.IsNullOrEmpty(row.ExtraBuffId))
            {
                configProvider.GetBuff(row.ExtraBuffId);
            }

            return new EnemyModifierDefinition(
                row.ModifierId,
                row.DisplayNameKey,
                row.HpMultiplier,
                row.DamageMultiplier,
                row.SpeedMultiplier,
                row.ScoreMultiplier,
                row.TintHex,
                row.ExtraBuffId);
        }

        // 校验 ValidateBossWave 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void ValidateBossWave(
            LevelConfig level,
            WaveConfig wave,
            IReadOnlyList<SpawnDefinition> spawns)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(level.BossEnemyId))
            {
                throw Invalid(wave.WaveId, "BossDefeated requires level.bossEnemyId");
            }

            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < spawns.Count; index++)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (spawns[index].IsBoss &&
                    string.Equals(
                        spawns[index].EnemyId,
                        level.BossEnemyId,
                        StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw Invalid(
                wave.WaveId,
                $"BossDefeated wave must spawn configured boss '{level.BossEnemyId}'");
        }

        // 处理 ParseStartTrigger 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static WaveStartTrigger ParseStartTrigger(string value, string ownerId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (Enum.TryParse(value, false, out WaveStartTrigger parsed) &&
                parsed != WaveStartTrigger.None &&
                string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
            {
                return parsed;
            }

            throw Invalid(ownerId, $"unsupported startTrigger '{value}'");
        }

        // 处理 ParseEndCondition 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static WaveEndCondition ParseEndCondition(string value, string ownerId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (Enum.TryParse(value, false, out WaveEndCondition parsed) &&
                parsed != WaveEndCondition.None &&
                string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
            {
                return parsed;
            }

            throw Invalid(ownerId, $"unsupported endCondition '{value}'");
        }

        // 处理 ParseSpawnPattern 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static SpawnPattern ParseSpawnPattern(string value, string ownerId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (Enum.TryParse(value, false, out SpawnPattern parsed) &&
                parsed != SpawnPattern.None &&
                string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
            {
                return parsed;
            }

            throw Invalid(ownerId, $"unsupported spawnPattern '{value}'");
        }

        // 处理 ParseLane 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static SpawnLane ParseLane(string value, string ownerId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (Enum.TryParse(value, false, out SpawnLane parsed) &&
                parsed != SpawnLane.None &&
                string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
            {
                return parsed;
            }

            throw Invalid(ownerId, $"unsupported lane '{value}'");
        }

        // 处理 ParseFacing 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static SpawnFacing ParseFacing(string value, string ownerId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (Enum.TryParse(value, false, out SpawnFacing parsed) &&
                parsed != SpawnFacing.None &&
                string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
            {
                return parsed;
            }

            throw Invalid(ownerId, $"unsupported facing '{value}'");
        }

        // 判断是否 IsHexColor 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool IsHexColor(string value)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrEmpty(value) || value.Length != 7 || value[0] != '#')
            {
                return false;
            }

            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 1; index < value.Length; index++)
            {
                char character = value[index];
                bool digit = character >= '0' && character <= '9';
                bool upper = character >= 'A' && character <= 'F';
                bool lower = character >= 'a' && character <= 'f';
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!digit && !upper && !lower)
                {
                    return false;
                }
            }

            return true;
        }

        // 处理 RequireNonEmpty 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void RequireNonEmpty(string value, string field)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(value))
            {
                throw Invalid(field, "value must be non-empty");
            }
        }

        // 处理 RequirePositive 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void RequirePositive(long value, string ownerId, string field)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (value <= 0L)
            {
                throw Invalid(ownerId, $"{field} must be positive");
            }
        }

        // 处理 RequireFiniteNonNegative 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void RequireFiniteNonNegative(double value, string ownerId, string field)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!IsFinite(value) || value < 0d)
            {
                throw Invalid(ownerId, $"{field} must be finite and non-negative");
            }
        }

        // 处理 RequirePositiveFinite 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void RequirePositiveFinite(double value, string ownerId, string field)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!IsFinite(value) || value <= 0d)
            {
                throw Invalid(ownerId, $"{field} must be finite and positive");
            }
        }

        // 处理 RequireUnit 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void RequireUnit(double value, string ownerId, string field)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!IsFinite(value) || value < 0d || value > 1d)
            {
                throw Invalid(ownerId, $"{field} must be within [0,1]");
            }
        }

        // 判断是否 IsFinite 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        // 处理 Invalid 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static ArgumentException Invalid(string ownerId, string message)
        {
            return new ArgumentException($"Level config '{ownerId}': {message}.");
        }

        // 定义 WaveOrderComparer 的关卡领域契约，用于描述时间线、流程或持久化边界。
        private sealed class WaveOrderComparer : IComparer<WaveConfig>
        {
            public static readonly WaveOrderComparer Instance = new WaveOrderComparer();

            // 处理 Compare 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
            public int Compare(WaveConfig left, WaveConfig right)
            {
                int order = left.Order.CompareTo(right.Order);
                return order != 0 ? order : string.CompareOrdinal(left.WaveId, right.WaveId);
            }
        }

        // 定义 SpawnDefinitionComparer 的关卡领域契约，用于描述时间线、流程或持久化边界。
        private sealed class SpawnDefinitionComparer : IComparer<SpawnDefinition>
        {
            public static readonly SpawnDefinitionComparer Instance =
                new SpawnDefinitionComparer();

            // 处理 Compare 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
            public int Compare(SpawnDefinition left, SpawnDefinition right)
            {
                int time = left.SpawnTimeSeconds.CompareTo(right.SpawnTimeSeconds);
                return time != 0 ? time : string.CompareOrdinal(left.SpawnId, right.SpawnId);
            }
        }
    }
}
