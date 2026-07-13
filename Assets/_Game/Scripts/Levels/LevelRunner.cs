using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Levels
{
    public interface ILevelSpawnWorld
    {
        bool TrySpawn(in LevelSpawnRequest request, out long entityId);
    }

    public enum LevelRunnerState
    {
        Ready = 0,
        Running = 1,
        Completed = 2,
    }

    public enum LevelRuntimeEventKind
    {
        None = 0,
        WaveStarted = 1,
        EnemySpawned = 2,
        WaveCompleted = 3,
        LevelCompleted = 4,
    }

    public readonly struct LevelRuntimeEvent
    {
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

    public sealed class LevelAdvanceReport
    {
        internal LevelAdvanceReport(IReadOnlyList<LevelRuntimeEvent> events)
        {
            Events = events;
        }

        public IReadOnlyList<LevelRuntimeEvent> Events { get; }

        public int Count => Events.Count;
    }

    public sealed class LevelRunner
    {
        private readonly LevelDefinition definition;
        private readonly ILevelSpawnWorld world;
        private int currentWaveIndex;
        private WaveRunner currentWave;

        public LevelRunner(
            IConfigProvider configProvider,
            string levelId,
            ILevelSpawnWorld spawnWorld)
            : this(LevelCatalog.Create(configProvider, levelId), spawnWorld)
        {
        }

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

        public bool DurationLimitReached =>
            ElapsedSeconds >= definition.DurationLimitSeconds;

        public void SetPaused(bool paused)
        {
            if (State != LevelRunnerState.Completed)
            {
                IsPaused = paused;
            }
        }

        public bool ConfirmPlayerAction()
        {
            return !IsPaused &&
                   State != LevelRunnerState.Completed &&
                   currentWave.ConfirmPlayerAction(ElapsedSeconds);
        }

        public bool NotifyEnemyDefeated(long entityId)
        {
            return State != LevelRunnerState.Completed &&
                   currentWave.NotifyEnemyDefeated(entityId);
        }

        public LevelAdvanceReport Advance(double deltaSeconds)
        {
            ValidateDelta(deltaSeconds);
            var events = new List<LevelRuntimeEvent>();
            if (IsPaused || State == LevelRunnerState.Completed)
            {
                return new LevelAdvanceReport(Array.AsReadOnly(events.ToArray()));
            }

            if (State == LevelRunnerState.Ready)
            {
                State = LevelRunnerState.Running;
            }

            ElapsedSeconds += deltaSeconds;
            while (State == LevelRunnerState.Running)
            {
                currentWave.Advance(ElapsedSeconds, world, events);
                if (!currentWave.IsCompleted)
                {
                    break;
                }

                if (currentWaveIndex + 1 >= definition.Waves.Count)
                {
                    State = LevelRunnerState.Completed;
                    IsPaused = false;
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

        private static void ValidateDelta(double value)
        {
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
