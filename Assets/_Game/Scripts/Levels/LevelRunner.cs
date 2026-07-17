using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Levels
{
    // 定义 ILevelSpawnWorld 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public interface ILevelSpawnWorld
    {
        bool TrySpawn(in LevelSpawnRequest request, out long entityId);
    }

    // 定义 LevelRunnerState 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum LevelRunnerState
    {
        Ready = 0,
        Running = 1,
        Completed = 2,
    }

    // 定义 LevelRuntimeEventKind 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum LevelRuntimeEventKind
    {
        None = 0,
        WaveStarted = 1,
        EnemySpawned = 2,
        WaveCompleted = 3,
        LevelCompleted = 4,
    }

    // 定义 LevelRuntimeEvent 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct LevelRuntimeEvent
    {
        // 初始化 LevelRuntimeEvent，并建立关卡流程所需的初始状态。
        private LevelRuntimeEvent(
            LevelRuntimeEventKind kind,
            string levelId,
            string waveId,
            int waveOrder,
            string musicKey,
            double levelSeconds,
            in LevelSpawnRequest spawnRequest,
            long entityId)
        {
            Kind = kind;
            LevelId = levelId;
            WaveId = waveId;
            WaveOrder = waveOrder;
            MusicKey = musicKey;
            LevelSeconds = levelSeconds;
            SpawnRequest = spawnRequest;
            EntityId = entityId;
        }

        public LevelRuntimeEventKind Kind { get; }

        public string LevelId { get; }

        public string WaveId { get; }

        public int WaveOrder { get; }

        public string MusicKey { get; }

        public double LevelSeconds { get; }

        public LevelSpawnRequest SpawnRequest { get; }

        public long EntityId { get; }

        // 处理 WaveStarted 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal static LevelRuntimeEvent WaveStarted(
            WaveDefinition wave,
            double levelSeconds)
        {
            return new LevelRuntimeEvent(
                LevelRuntimeEventKind.WaveStarted,
                wave.LevelId,
                wave.WaveId,
                wave.Order,
                wave.MusicKey,
                levelSeconds,
                default,
                0L);
        }

        // 处理 EnemySpawned 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal static LevelRuntimeEvent EnemySpawned(
            in LevelSpawnRequest request,
            long entityId,
            double levelSeconds)
        {
            return new LevelRuntimeEvent(
                LevelRuntimeEventKind.EnemySpawned,
                request.LevelId,
                request.WaveId,
                0,
                string.Empty,
                levelSeconds,
                request,
                entityId);
        }

        // 处理 WaveCompleted 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal static LevelRuntimeEvent WaveCompleted(
            WaveDefinition wave,
            double levelSeconds)
        {
            return new LevelRuntimeEvent(
                LevelRuntimeEventKind.WaveCompleted,
                wave.LevelId,
                wave.WaveId,
                wave.Order,
                wave.MusicKey,
                levelSeconds,
                default,
                0L);
        }

        // 处理 LevelCompleted 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal static LevelRuntimeEvent LevelCompleted(
            LevelDefinition level,
            double levelSeconds)
        {
            return new LevelRuntimeEvent(
                LevelRuntimeEventKind.LevelCompleted,
                level.LevelId,
                string.Empty,
                0,
                string.Empty,
                levelSeconds,
                default,
                0L);
        }
    }

    // 定义 LevelAdvanceReport 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class LevelAdvanceReport
    {
        // 初始化 LevelAdvanceReport，并建立关卡流程所需的初始状态。
        internal LevelAdvanceReport(IReadOnlyList<LevelRuntimeEvent> events)
        {
            Events = events;
        }

        public IReadOnlyList<LevelRuntimeEvent> Events { get; }

        public int Count => Events.Count;
    }

    // 定义 LevelRunner 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class LevelRunner
    {
        private readonly LevelDefinition definition;
        private readonly ILevelSpawnWorld world;
        private int currentWaveIndex;
        private WaveRunner currentWave;

        // 初始化 LevelRunner，并建立关卡流程所需的初始状态。
        public LevelRunner(
            IConfigProvider configProvider,
            string levelId,
            ILevelSpawnWorld spawnWorld)
            : this(LevelCatalog.Create(configProvider, levelId), spawnWorld)
        {
        }

        // 初始化 LevelRunner，并建立关卡流程所需的初始状态。
        public LevelRunner(
            LevelDefinition configuredDefinition,
            ILevelSpawnWorld spawnWorld)
        {
            definition = configuredDefinition ??
                throw new ArgumentNullException(nameof(configuredDefinition));
            world = spawnWorld ?? throw new ArgumentNullException(nameof(spawnWorld));
            currentWaveIndex = 0;
            currentWave = new WaveRunner(
                definition.Waves[0],
                definition.BossEnemyId,
                0d);
            State = LevelRunnerState.Ready;
        }

        public LevelDefinition Definition => definition;

        public LevelRunnerState State { get; private set; }

        public WaveRunner CurrentWave => currentWave;

        public int CurrentWaveIndex => currentWaveIndex;

        public double ElapsedSeconds { get; private set; }

        public bool IsPaused { get; private set; }

        public bool IsProgressBlocked { get; private set; }

        public bool DurationLimitReached =>
            ElapsedSeconds >= definition.DurationLimitSeconds;

        // 设置 SetPaused 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public void SetPaused(bool paused)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State != LevelRunnerState.Completed)
            {
                IsPaused = paused;
            }
        }

        // 设置 SetProgressBlocked 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public void SetProgressBlocked(bool blocked)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State != LevelRunnerState.Completed)
            {
                IsProgressBlocked = blocked;
            }
        }

        // 处理 ConfirmPlayerAction 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool ConfirmPlayerAction()
        {
            return !IsPaused &&
                   State != LevelRunnerState.Completed &&
                   currentWave.ConfirmPlayerAction(ElapsedSeconds);
        }

        // 处理 NotifyEnemyDefeated 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool NotifyEnemyDefeated(long entityId)
        {
            return State != LevelRunnerState.Completed &&
                   currentWave.NotifyEnemyDefeated(entityId);
        }

        // 推进 Advance 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public LevelAdvanceReport Advance(double deltaSeconds)
        {
            ValidateDelta(deltaSeconds);
            var events = new List<LevelRuntimeEvent>();
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (IsPaused || State == LevelRunnerState.Completed)
            {
                return new LevelAdvanceReport(Array.AsReadOnly(events.ToArray()));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == LevelRunnerState.Ready)
            {
                State = LevelRunnerState.Running;
            }

            ElapsedSeconds += deltaSeconds;
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            while (State == LevelRunnerState.Running)
            {
                currentWave.Advance(
                    ElapsedSeconds,
                    world,
                    events,
                    IsProgressBlocked);
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!currentWave.IsCompleted)
                {
                    break;
                }

                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (currentWaveIndex + 1 >= definition.Waves.Count)
                {
                    State = LevelRunnerState.Completed;
                    IsPaused = false;
                    IsProgressBlocked = false;
                    events.Add(LevelRuntimeEvent.LevelCompleted(
                        definition,
                        currentWave.CompletedAtLevelSeconds));
                    break;
                }

                double nextActivatedAt = currentWave.CompletedAtLevelSeconds;
                currentWaveIndex++;
                currentWave = new WaveRunner(
                    definition.Waves[currentWaveIndex],
                    definition.BossEnemyId,
                    nextActivatedAt);
            }

            return new LevelAdvanceReport(Array.AsReadOnly(events.ToArray()));
        }

        // 校验 ValidateDelta 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void ValidateDelta(double value)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Level delta time must be finite and non-negative.");
            }
        }
    }
}
