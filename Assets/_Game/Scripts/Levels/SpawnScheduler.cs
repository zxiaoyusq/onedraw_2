using System;
using System.Collections.Generic;

namespace OneStrokeDemon.Levels
{
    // 定义 LevelSpawnRequest 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct LevelSpawnRequest
    {
        // 初始化 LevelSpawnRequest，并建立关卡流程所需的初始状态。
        internal LevelSpawnRequest(
            long scheduleSequence,
            string levelId,
            string waveId,
            string spawnId,
            int occurrenceIndex,
            double scheduledWaveSeconds,
            string enemyId,
            bool isBoss,
            in NormalizedSpawnPosition position,
            SpawnLane lane,
            SpawnFacing facing,
            SpawnPattern pattern,
            in EnemyModifierDefinition modifier)
        {
            ScheduleSequence = scheduleSequence;
            LevelId = levelId;
            WaveId = waveId;
            SpawnId = spawnId;
            OccurrenceIndex = occurrenceIndex;
            ScheduledWaveSeconds = scheduledWaveSeconds;
            EnemyId = enemyId;
            IsBoss = isBoss;
            Position = position;
            Lane = lane;
            Facing = facing;
            Pattern = pattern;
            Modifier = modifier;
        }

        public long ScheduleSequence { get; }

        public string LevelId { get; }

        public string WaveId { get; }

        public string SpawnId { get; }

        public int OccurrenceIndex { get; }

        public double ScheduledWaveSeconds { get; }

        public string EnemyId { get; }

        public bool IsBoss { get; }

        public NormalizedSpawnPosition Position { get; }

        public SpawnLane Lane { get; }

        public SpawnFacing Facing { get; }

        public SpawnPattern Pattern { get; }

        public EnemyModifierDefinition Modifier { get; }

        public bool IsValid =>
            ScheduleSequence > 0L &&
            !string.IsNullOrEmpty(LevelId) &&
            !string.IsNullOrEmpty(WaveId) &&
            !string.IsNullOrEmpty(SpawnId) &&
            OccurrenceIndex >= 0 &&
            IsFinite(ScheduledWaveSeconds) &&
            ScheduledWaveSeconds >= 0d &&
            !string.IsNullOrEmpty(EnemyId) &&
            Position.IsNormalized &&
            Lane != SpawnLane.None &&
            Facing != SpawnFacing.None &&
            Pattern != SpawnPattern.None &&
            Modifier.IsConfigured;

        // 判断是否 IsFinite 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    // 定义 SpawnScheduler 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class SpawnScheduler
    {
        private const double TimelineEpsilon = 0.000001d;
        private readonly LevelSpawnRequest[] schedule;
        private int nextIndex;

        // 初始化 SpawnScheduler，并建立关卡流程所需的初始状态。
        public SpawnScheduler(WaveDefinition wave)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (wave == null)
            {
                throw new ArgumentNullException(nameof(wave));
            }

            var entries = new List<PendingSpawn>();
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int spawnIndex = 0; spawnIndex < wave.Spawns.Count; spawnIndex++)
            {
                SpawnDefinition spawn = wave.Spawns[spawnIndex];
                // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
                for (int occurrence = 0; occurrence < spawn.Count; occurrence++)
                {
                    entries.Add(new PendingSpawn(
                        spawn,
                        occurrence,
                        spawn.SpawnTimeSeconds + (occurrence * spawn.IntervalSeconds)));
                }
            }

            entries.Sort(PendingSpawnComparer.Instance);
            schedule = new LevelSpawnRequest[entries.Count];
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < entries.Count; index++)
            {
                PendingSpawn pending = entries[index];
                SpawnDefinition spawn = pending.Spawn;
                NormalizedSpawnPosition position = SpawnRegionSampler.Sample(
                    spawn.SpawnPoint,
                    spawn.Pattern,
                    spawn.SpawnId,
                    pending.OccurrenceIndex,
                    spawn.Count);
                schedule[index] = new LevelSpawnRequest(
                    index + 1L,
                    wave.LevelId,
                    wave.WaveId,
                    spawn.SpawnId,
                    pending.OccurrenceIndex,
                    pending.DueSeconds,
                    spawn.EnemyId,
                    spawn.IsBoss,
                    position,
                    spawn.SpawnPoint.Lane,
                    spawn.SpawnPoint.Facing,
                    spawn.Pattern,
                    spawn.Modifier);
            }
        }

        public int ScheduledCount => schedule.Length;

        public int EmittedCount => nextIndex;

        public int RemainingCount => schedule.Length - nextIndex;

        public bool IsComplete => nextIndex >= schedule.Length;

        // 尝试执行 TryGetNextDue 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool TryGetNextDue(
            double waveElapsedSeconds,
            out LevelSpawnRequest request)
        {
            ValidateElapsed(waveElapsedSeconds);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (nextIndex >= schedule.Length ||
                schedule[nextIndex].ScheduledWaveSeconds - waveElapsedSeconds >
                TimelineEpsilon)
            {
                request = default;
                return false;
            }

            request = schedule[nextIndex];
            return true;
        }

        // 处理 Commit 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public void Commit(in LevelSpawnRequest request)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (nextIndex >= schedule.Length)
            {
                throw new InvalidOperationException("Spawn schedule is already complete.");
            }

            LevelSpawnRequest expected = schedule[nextIndex];
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!request.IsValid ||
                request.ScheduleSequence != expected.ScheduleSequence ||
                !string.Equals(request.SpawnId, expected.SpawnId, StringComparison.Ordinal) ||
                request.OccurrenceIndex != expected.OccurrenceIndex)
            {
                throw new InvalidOperationException(
                    "Only the currently pending spawn request may be committed.");
            }

            nextIndex++;
        }

        // 校验 ValidateElapsed 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void ValidateElapsed(double value)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Wave elapsed time must be finite and non-negative.");
            }
        }

        // 定义 PendingSpawn 的关卡领域契约，用于描述时间线、流程或持久化边界。
        private readonly struct PendingSpawn
        {
            // 初始化 PendingSpawn，并建立关卡流程所需的初始状态。
            public PendingSpawn(
                SpawnDefinition spawn,
                int occurrenceIndex,
                double dueSeconds)
            {
                Spawn = spawn;
                OccurrenceIndex = occurrenceIndex;
                DueSeconds = dueSeconds;
            }

            public SpawnDefinition Spawn { get; }

            public int OccurrenceIndex { get; }

            public double DueSeconds { get; }
        }

        // 定义 PendingSpawnComparer 的关卡领域契约，用于描述时间线、流程或持久化边界。
        private sealed class PendingSpawnComparer : IComparer<PendingSpawn>
        {
            public static readonly PendingSpawnComparer Instance =
                new PendingSpawnComparer();

            // 处理 Compare 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
            public int Compare(PendingSpawn left, PendingSpawn right)
            {
                int time = left.DueSeconds.CompareTo(right.DueSeconds);
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (time != 0)
                {
                    return time;
                }

                int spawnId = string.CompareOrdinal(
                    left.Spawn.SpawnId,
                    right.Spawn.SpawnId);
                return spawnId != 0
                    ? spawnId
                    : left.OccurrenceIndex.CompareTo(right.OccurrenceIndex);
            }
        }
    }

    // 定义 SpawnRegionSampler 的关卡领域契约，用于描述时间线、流程或持久化边界。
    internal static class SpawnRegionSampler
    {
        // 处理 Sample 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public static NormalizedSpawnPosition Sample(
            in SpawnPointDefinition point,
            SpawnPattern pattern,
            string spawnId,
            int occurrenceIndex,
            int count)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!point.IsConfigured)
            {
                throw new ArgumentException("Spawn point must be configured.", nameof(point));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrEmpty(spawnId))
            {
                throw new ArgumentException("Spawn id must be non-empty.", nameof(spawnId));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (occurrenceIndex < 0 || count <= 0 || occurrenceIndex >= count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(occurrenceIndex),
                    occurrenceIndex,
                    "Occurrence index must be within the configured spawn count.");
            }

            double xOffset = 0d;
            double yOffset = 0d;
            // 按当前流程、事件或奖励类型选择对应处理分支。
            switch (pattern)
            {
                case SpawnPattern.Single:
                    break;
                case SpawnPattern.Line:
                    yOffset = count == 1
                        ? 0d
                        : Lerp(-1d, 1d, (double)occurrenceIndex / (count - 1));
                    break;
                case SpawnPattern.Scatter:
                    xOffset = (StableUnit(spawnId, occurrenceIndex, 0xA341316Cu) * 2d) - 1d;
                    yOffset = (StableUnit(spawnId, occurrenceIndex, 0xC8013EA4u) * 2d) - 1d;
                    break;
                case SpawnPattern.Stagger:
                    xOffset = occurrenceIndex % 2 == 0 ? -1d : 1d;
                    yOffset = occurrenceIndex % 2 == 0 ? 1d : -1d;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(pattern),
                        pattern,
                        "Spawn pattern must be configured.");
            }

            return new NormalizedSpawnPosition(
                point.NormalizedX + (xOffset * point.JitterX),
                point.NormalizedY + (yOffset * point.JitterY));
        }

        // 处理 StableUnit 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static double StableUnit(string spawnId, int occurrenceIndex, uint salt)
        {
            const uint offsetBasis = 2166136261u;
            const uint prime = 16777619u;
            uint hash = offsetBasis ^ salt;
            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            for (int index = 0; index < spawnId.Length; index++)
            {
                hash ^= spawnId[index];
                hash *= prime;
            }

            hash ^= (uint)occurrenceIndex;
            hash *= prime;
            return (hash & 0x00FFFFFFu) / 16777215d;
        }

        // 处理 Lerp 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static double Lerp(double from, double to, double t)
        {
            return from + ((to - from) * t);
        }
    }
}
