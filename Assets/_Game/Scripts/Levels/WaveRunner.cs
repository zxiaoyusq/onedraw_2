using System;
using System.Collections.Generic;

namespace OneStrokeDemon.Levels
{
    // 定义 WaveRunnerState 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum WaveRunnerState
    {
        Waiting = 0,
        Running = 1,
        Completing = 2,
        Completed = 3,
    }

    // 定义 WaveRunner 的关卡领域契约，用于描述时间线、流程或持久化边界。
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

        // 初始化 WaveRunner，并建立关卡流程所需的初始状态。
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

        // 处理 ConfirmPlayerAction 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool ConfirmPlayerAction(double levelElapsedSeconds)
        {
            ValidateTime(levelElapsedSeconds, nameof(levelElapsedSeconds));
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == WaveRunnerState.Waiting &&
                definition.StartTrigger == WaveStartTrigger.PlayerConfirmed &&
                !startConfirmed)
            {
                startConfirmed = true;
                startConfirmedAt = levelElapsedSeconds;
                return true;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == WaveRunnerState.Running &&
                definition.EndCondition == WaveEndCondition.PlayerConfirmed &&
                double.IsNaN(endConditionAt))
            {
                endConditionAt = levelElapsedSeconds;
                return true;
            }

            return false;
        }

        // 处理 NotifyEnemyDefeated 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool NotifyEnemyDefeated(long entityId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!activeSpawns.TryGetValue(entityId, out LevelSpawnRequest request))
            {
                return false;
            }

            activeSpawns.Remove(entityId);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (request.IsBoss &&
                string.Equals(request.EnemyId, bossEnemyId, StringComparison.Ordinal))
            {
                bossDefeated = true;
            }

            return true;
        }

        // 推进 Advance 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal void Advance(
            double levelElapsedSeconds,
            ILevelSpawnWorld world,
            List<LevelRuntimeEvent> events,
            bool completionBlocked)
        {
            ValidateTime(levelElapsedSeconds, nameof(levelElapsedSeconds));
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == WaveRunnerState.Waiting && TryResolveStart(out double startsAt) &&
                levelElapsedSeconds + TimelineEpsilon >= startsAt)
            {
                StartedAtLevelSeconds = startsAt;
                State = WaveRunnerState.Running;
                events.Add(LevelRuntimeEvent.WaveStarted(definition, startsAt));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == WaveRunnerState.Running)
            {
                double waveElapsedSeconds = levelElapsedSeconds - StartedAtLevelSeconds;
                SpawnDue(levelElapsedSeconds, waveElapsedSeconds, world, events);
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!completionBlocked)
                {
                    EvaluateEndCondition(levelElapsedSeconds, waveElapsedSeconds);
                }
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == WaveRunnerState.Completing &&
                levelElapsedSeconds + TimelineEpsilon >= completionAt)
            {
                State = WaveRunnerState.Completed;
                CompletedAtLevelSeconds = completionAt;
                events.Add(LevelRuntimeEvent.WaveCompleted(definition, completionAt));
            }
        }

        // 尝试执行 TryResolveStart 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private bool TryResolveStart(out double startsAt)
        {
            // 按当前流程、事件或奖励类型选择对应处理分支。
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

        // 处理 SpawnDue 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void SpawnDue(
            double levelElapsedSeconds,
            double waveElapsedSeconds,
            ILevelSpawnWorld world,
            List<LevelRuntimeEvent> events)
        {
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            while (State == WaveRunnerState.Running &&
                   activeSpawns.Count < definition.MaxAlive &&
                   scheduler.TryGetNextDue(waveElapsedSeconds, out LevelSpawnRequest request))
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (!world.TrySpawn(request, out long entityId))
                {
                    return;
                }

                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (entityId <= 0L)
                {
                    throw new InvalidOperationException(
                        $"World accepted spawn '{request.SpawnId}' without a positive entity id.");
                }

                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

        // 评估 EvaluateEndCondition 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void EvaluateEndCondition(
            double levelElapsedSeconds,
            double waveElapsedSeconds)
        {
            // 按当前流程、事件或奖励类型选择对应处理分支。
            switch (definition.EndCondition)
            {
                case WaveEndCondition.AllEnemiesDefeated:
                    // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                    if (scheduler.IsComplete && activeSpawns.Count == 0)
                    {
                        BeginCompleting(
                            levelElapsedSeconds,
                            levelElapsedSeconds + definition.EndDelaySeconds);
                    }
                    break;
                case WaveEndCondition.BossDefeated:
                    // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                    if (bossDefeated)
                    {
                        BeginCompleting(
                            levelElapsedSeconds,
                            levelElapsedSeconds + definition.EndDelaySeconds);
                    }
                    break;
                case WaveEndCondition.PlayerConfirmed:
                    // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                    if (!double.IsNaN(endConditionAt))
                    {
                        BeginCompleting(
                            endConditionAt,
                            endConditionAt + definition.EndDelaySeconds);
                    }
                    break;
                case WaveEndCondition.TimeElapsed:
                    // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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

        // 开始 BeginCompleting 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void BeginCompleting(double conditionAt, double completesAt)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State != WaveRunnerState.Running)
            {
                return;
            }

            endConditionAt = conditionAt;
            completionAt = completesAt;
            State = WaveRunnerState.Completing;
        }

        // 校验 ValidateTime 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void ValidateTime(double value, string parameterName)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
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
