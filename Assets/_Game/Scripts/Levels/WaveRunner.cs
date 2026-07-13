using System;
using System.Collections.Generic;

namespace OneStrokeDemon.Levels
{
    public enum WaveRunnerState
    {
        Waiting = 0,
        Running = 1,
        Completing = 2,
        Completed = 3,
    }

    public sealed class WaveRunner
    {
        private const double TimelineEpsilon = 0.000001d;
        private readonly WaveDefinition definition;
        private readonly string bossEnemyId;
        private readonly SpawnScheduler scheduler;
        private readonly Dictionary<long, LevelSpawnRequest> activeSpawns =
            new Dictionary<long, LevelSpawnRequest>();
        private readonly double activatedAt;
        private bool startConfirmed;
        private bool bossDefeated;
        private double startConfirmedAt;
        private double endConditionAt;
        private double completionAt;

        public WaveRunner(
            WaveDefinition configuredDefinition,
            string configuredBossEnemyId,
            double activatedLevelSeconds)
        {
            definition = configuredDefinition ??
                throw new ArgumentNullException(nameof(configuredDefinition));
            ValidateTime(activatedLevelSeconds, nameof(activatedLevelSeconds));
            bossEnemyId = configuredBossEnemyId ?? string.Empty;
            activatedAt = activatedLevelSeconds;
            scheduler = new SpawnScheduler(definition);
            State = WaveRunnerState.Waiting;
            StartedAtLevelSeconds = double.NaN;
            CompletedAtLevelSeconds = double.NaN;
            startConfirmedAt = double.NaN;
            endConditionAt = double.NaN;
            completionAt = double.NaN;
        }

        public WaveDefinition Definition => definition;

        public SpawnScheduler Scheduler => scheduler;

        public WaveRunnerState State { get; private set; }

        public int ActiveCount => activeSpawns.Count;

        public double StartedAtLevelSeconds { get; private set; }

        public double CompletedAtLevelSeconds { get; private set; }

        public bool IsCompleted => State == WaveRunnerState.Completed;

        public bool ConfirmPlayerAction(double levelElapsedSeconds)
        {
            ValidateTime(levelElapsedSeconds, nameof(levelElapsedSeconds));
            if (State == WaveRunnerState.Waiting &&
                definition.StartTrigger == WaveStartTrigger.PlayerConfirmed &&
                !startConfirmed)
            {
                startConfirmed = true;
                startConfirmedAt = levelElapsedSeconds;
                return true;
            }

            if (State == WaveRunnerState.Running &&
                definition.EndCondition == WaveEndCondition.PlayerConfirmed &&
                double.IsNaN(endConditionAt))
            {
                endConditionAt = levelElapsedSeconds;
                return true;
            }

            return false;
        }

        public bool NotifyEnemyDefeated(long entityId)
        {
            if (!activeSpawns.TryGetValue(entityId, out LevelSpawnRequest request))
            {
                return false;
            }

            activeSpawns.Remove(entityId);
            if (request.IsBoss &&
                string.Equals(request.EnemyId, bossEnemyId, StringComparison.Ordinal))
            {
                bossDefeated = true;
            }

            return true;
        }

        internal void Advance(
            double levelElapsedSeconds,
            ILevelSpawnWorld world,
            List<LevelRuntimeEvent> events)
        {
            ValidateTime(levelElapsedSeconds, nameof(levelElapsedSeconds));
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (State == WaveRunnerState.Waiting && TryResolveStart(out double startsAt) &&
                levelElapsedSeconds + TimelineEpsilon >= startsAt)
            {
                StartedAtLevelSeconds = startsAt;
                State = WaveRunnerState.Running;
                events.Add(LevelRuntimeEvent.WaveStarted(definition, startsAt));
            }

            if (State == WaveRunnerState.Running)
            {
                double waveElapsedSeconds = levelElapsedSeconds - StartedAtLevelSeconds;
                SpawnDue(levelElapsedSeconds, waveElapsedSeconds, world, events);
                EvaluateEndCondition(levelElapsedSeconds, waveElapsedSeconds);
            }

            if (State == WaveRunnerState.Completing &&
                levelElapsedSeconds + TimelineEpsilon >= completionAt)
            {
                State = WaveRunnerState.Completed;
                CompletedAtLevelSeconds = completionAt;
                events.Add(LevelRuntimeEvent.WaveCompleted(definition, completionAt));
            }
        }

        private bool TryResolveStart(out double startsAt)
        {
            switch (definition.StartTrigger)
            {
                case WaveStartTrigger.LevelStart:
                case WaveStartTrigger.TimeElapsed:
                    startsAt = definition.StartDelaySeconds;
                    return true;
                case WaveStartTrigger.PreviousWaveEnd:
                    startsAt = activatedAt + definition.StartDelaySeconds;
                    return true;
                case WaveStartTrigger.PlayerConfirmed:
                    startsAt = startConfirmedAt + definition.StartDelaySeconds;
                    return startConfirmed;
                default:
                    throw new InvalidOperationException(
                        $"Wave '{definition.WaveId}' has no supported start trigger.");
            }
        }

        private void SpawnDue(
            double levelElapsedSeconds,
            double waveElapsedSeconds,
            ILevelSpawnWorld world,
            List<LevelRuntimeEvent> events)
        {
            while (State == WaveRunnerState.Running &&
                   activeSpawns.Count < definition.MaxAlive &&
                   scheduler.TryGetNextDue(waveElapsedSeconds, out LevelSpawnRequest request))
            {
                if (!world.TrySpawn(request, out long entityId))
                {
                    return;
                }

                if (entityId <= 0L)
                {
                    throw new InvalidOperationException(
                        $"World accepted spawn '{request.SpawnId}' without a positive entity id.");
                }

                if (activeSpawns.ContainsKey(entityId))
                {
                    throw new InvalidOperationException(
                        $"World reused active entity id '{entityId}'.");
                }

                scheduler.Commit(request);
                activeSpawns.Add(entityId, request);
                events.Add(LevelRuntimeEvent.EnemySpawned(
                    request,
                    entityId,
                    levelElapsedSeconds));
            }
        }

        private void EvaluateEndCondition(
            double levelElapsedSeconds,
            double waveElapsedSeconds)
        {
            switch (definition.EndCondition)
            {
                case WaveEndCondition.AllEnemiesDefeated:
                    if (scheduler.IsComplete && activeSpawns.Count == 0)
                    {
                        BeginCompleting(
                            levelElapsedSeconds,
                            levelElapsedSeconds + definition.EndDelaySeconds);
                    }
                    break;
                case WaveEndCondition.BossDefeated:
                    if (bossDefeated)
                    {
                        BeginCompleting(
                            levelElapsedSeconds,
                            levelElapsedSeconds + definition.EndDelaySeconds);
                    }
                    break;
                case WaveEndCondition.PlayerConfirmed:
                    if (!double.IsNaN(endConditionAt))
                    {
                        BeginCompleting(
                            endConditionAt,
                            endConditionAt + definition.EndDelaySeconds);
                    }
                    break;
                case WaveEndCondition.TimeElapsed:
                    if (waveElapsedSeconds + TimelineEpsilon >= definition.EndDelaySeconds)
                    {
                        double timedCompletion =
                            StartedAtLevelSeconds + definition.EndDelaySeconds;
                        BeginCompleting(timedCompletion, timedCompletion);
                    }
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Wave '{definition.WaveId}' has no supported end condition.");
            }
        }

        private void BeginCompleting(double conditionAt, double completesAt)
        {
            if (State != WaveRunnerState.Running)
            {
                return;
            }

            endConditionAt = conditionAt;
            completionAt = completesAt;
            State = WaveRunnerState.Completing;
        }

        private static void ValidateTime(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Level elapsed time must be finite and non-negative.");
            }
        }
    }
}
