using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;

namespace OneStrokeDemon.Levels
{
    // 定义 BattleFlowState 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum BattleFlowState
    {
        Countdown = 0,
        Playing = 1,
        UltimateDrawing = 2,
        Paused = 3,
        Victory = 4,
        Defeat = 5,
    }

    [Flags]
    // 定义 BattlePauseReason 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum BattlePauseReason
    {
        None = 0,
        PlayerRequested = 1 << 0,
        FocusLost = 1 << 1,
        ApplicationPaused = 1 << 2,
    }

    // 定义 UltimateCancelReason 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum UltimateCancelReason
    {
        None = 0,
        PlayerCanceled = 1,
        InputWindowExpired = 2,
        SkillRejected = 3,
        BattlePaused = 4,
        BattleSettled = 5,
    }

    // 定义 BattleSettlement 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum BattleSettlement
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
    }

    // 定义 BattleFlowEventType 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public enum BattleFlowEventType
    {
        None = 0,
        StateChanged = 1,
        StrokeCancellationRequested = 2,
        UltimateResolved = 3,
        UltimateCanceled = 4,
        Settled = 5,
    }

    // 定义 BattleFlowSettings 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class BattleFlowSettings
    {
        // 初始化 BattleFlowSettings，并建立关卡流程所需的初始状态。
        internal BattleFlowSettings(
            double countdownDurationSeconds,
            bool pauseOnFocusLost,
            string ultimateSkillId,
            string ultimateGestureType,
            double ultimateInputWindowSeconds)
        {
            CountdownDurationSeconds = countdownDurationSeconds;
            PauseOnFocusLost = pauseOnFocusLost;
            UltimateSkillId = ultimateSkillId;
            UltimateGestureType = ultimateGestureType;
            UltimateInputWindowSeconds = ultimateInputWindowSeconds;
        }

        public double CountdownDurationSeconds { get; }

        public bool PauseOnFocusLost { get; }

        public string UltimateSkillId { get; }

        public string UltimateGestureType { get; }

        public double UltimateInputWindowSeconds { get; }
    }

    // 定义 BattleFlowSettingsFactory 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public static class BattleFlowSettingsFactory
    {
        // 创建 Create 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public static BattleFlowSettings Create(
            IConfigProvider configProvider,
            string playerId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException(
                    "Player id must be non-empty.",
                    nameof(playerId));
            }

            double countdown = ReadNonNegativeFloat(
                configProvider,
                ConfigIds.GlobalKeys.BattleCountdownSec);
            bool pauseOnFocusLost = ReadBool(
                configProvider,
                ConfigIds.GlobalKeys.PauseOnFocusLost);
            PlayerConfig player = configProvider.GetPlayer(playerId);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(player.UltimateSkillId))
            {
                throw InvalidConfig(
                    $"Players.{player.PlayerId}.ultimateSkillId must be non-empty.");
            }

            SkillConfig ultimate = configProvider.GetSkill(player.UltimateSkillId);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!string.Equals(
                    ultimate.TriggerType,
                    SkillTriggerTypes.Ultimate,
                    StringComparison.Ordinal))
            {
                throw InvalidConfig(
                    $"Skill '{ultimate.SkillId}' must use the Ultimate trigger.");
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(ultimate.GestureType) ||
                !IsFinite(ultimate.InputWindowSec) ||
                ultimate.InputWindowSec <= 0f)
            {
                throw InvalidConfig(
                    $"Skill '{ultimate.SkillId}' must configure a gesture and positive input window.");
            }

            return new BattleFlowSettings(
                countdown,
                pauseOnFocusLost,
                ultimate.SkillId,
                ultimate.GestureType,
                ultimate.InputWindowSec);
        }

        // 处理 ReadNonNegativeFloat 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static double ReadNonNegativeFloat(
            IConfigProvider configProvider,
            string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!string.Equals(row.ValueType, "float", StringComparison.Ordinal) ||
                !row.FloatValue.HasValue ||
                !IsFinite(row.FloatValue.Value) ||
                row.FloatValue.Value < 0f)
            {
                throw InvalidConfig(
                    $"Global '{key}' must be a finite non-negative float.");
            }

            return row.FloatValue.Value;
        }

        // 处理 ReadBool 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool ReadBool(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!string.Equals(row.ValueType, "bool", StringComparison.Ordinal) ||
                !row.BoolValue.HasValue)
            {
                throw InvalidConfig($"Global '{key}' must be a bool value.");
            }

            return row.BoolValue.Value;
        }

        // 判断是否 IsFinite 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        // 处理 InvalidConfig 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static ArgumentException InvalidConfig(string message)
        {
            return new ArgumentException(message, "configProvider");
        }
    }

    // 定义 BattleTimeSnapshot 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct BattleTimeSnapshot
    {
        // 初始化 BattleTimeSnapshot，并建立关卡流程所需的初始状态。
        internal BattleTimeSnapshot(
            double flowElapsedSeconds,
            double gameplayUnscaledElapsedSeconds,
            double gameplayElapsedSeconds,
            double gameplayScale,
            double gameplayScaleRemainingSeconds)
        {
            FlowElapsedSeconds = flowElapsedSeconds;
            GameplayUnscaledElapsedSeconds = gameplayUnscaledElapsedSeconds;
            GameplayElapsedSeconds = gameplayElapsedSeconds;
            GameplayScale = gameplayScale;
            GameplayScaleRemainingSeconds = gameplayScaleRemainingSeconds;
        }

        public double FlowElapsedSeconds { get; }

        public double GameplayUnscaledElapsedSeconds { get; }

        public double GameplayElapsedSeconds { get; }

        public double GameplayScale { get; }

        public double GameplayScaleRemainingSeconds { get; }
    }

    // 定义 BattleTimeSlice 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct BattleTimeSlice
    {
        // 初始化 BattleTimeSlice，并建立关卡流程所需的初始状态。
        internal BattleTimeSlice(
            double requestedUnscaledDeltaSeconds,
            double flowDeltaSeconds,
            double gameplayUnscaledDeltaSeconds,
            double gameplayDeltaSeconds,
            in BattleTimeSnapshot current)
        {
            RequestedUnscaledDeltaSeconds = requestedUnscaledDeltaSeconds;
            FlowDeltaSeconds = flowDeltaSeconds;
            GameplayUnscaledDeltaSeconds = gameplayUnscaledDeltaSeconds;
            GameplayDeltaSeconds = gameplayDeltaSeconds;
            Current = current;
        }

        public double RequestedUnscaledDeltaSeconds { get; }

        public double FlowDeltaSeconds { get; }

        public double GameplayUnscaledDeltaSeconds { get; }

        public double GameplayDeltaSeconds { get; }

        public BattleTimeSnapshot Current { get; }
    }

    // 定义 BattleTimeSource 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class BattleTimeSource
    {
        private const double TimelineEpsilon = 0.000001d;
        private double gameplayScale = 1d;
        private double gameplayScaleRemainingSeconds;

        public BattleTimeSnapshot Current => new BattleTimeSnapshot(
            FlowElapsedSeconds,
            GameplayUnscaledElapsedSeconds,
            GameplayElapsedSeconds,
            gameplayScale,
            gameplayScaleRemainingSeconds);

        public double FlowElapsedSeconds { get; private set; }

        public double GameplayUnscaledElapsedSeconds { get; private set; }

        public double GameplayElapsedSeconds { get; private set; }

        // 应用 ApplyGameplayScale 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public void ApplyGameplayScale(double scale, double durationSeconds)
        {
            ValidateFiniteNonNegative(scale, nameof(scale));
            ValidateFiniteNonNegative(durationSeconds, nameof(durationSeconds));
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (durationSeconds == 0d)
            {
                gameplayScale = 1d;
                gameplayScaleRemainingSeconds = 0d;
                return;
            }

            gameplayScale = scale;
            gameplayScaleRemainingSeconds = durationSeconds;
        }

        // 推进 AdvanceFlowOnly 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal void AdvanceFlowOnly(double unscaledDeltaSeconds)
        {
            ValidateFiniteNonNegative(unscaledDeltaSeconds, nameof(unscaledDeltaSeconds));
            FlowElapsedSeconds = AddChecked(
                FlowElapsedSeconds,
                unscaledDeltaSeconds,
                "Flow elapsed time");
        }

        // 推进 AdvanceGameplay 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal double AdvanceGameplay(double unscaledDeltaSeconds)
        {
            ValidateFiniteNonNegative(unscaledDeltaSeconds, nameof(unscaledDeltaSeconds));
            FlowElapsedSeconds = AddChecked(
                FlowElapsedSeconds,
                unscaledDeltaSeconds,
                "Flow elapsed time");
            GameplayUnscaledElapsedSeconds = AddChecked(
                GameplayUnscaledElapsedSeconds,
                unscaledDeltaSeconds,
                "Gameplay unscaled elapsed time");

            double scaledDelta = unscaledDeltaSeconds;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (gameplayScaleRemainingSeconds > 0d)
            {
                double affectedSeconds = Math.Min(
                    unscaledDeltaSeconds,
                    gameplayScaleRemainingSeconds);
                double remainder = unscaledDeltaSeconds - affectedSeconds;
                scaledDelta = (affectedSeconds * gameplayScale) + remainder;
                gameplayScaleRemainingSeconds -= affectedSeconds;
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (gameplayScaleRemainingSeconds <= TimelineEpsilon)
                {
                    gameplayScale = 1d;
                    gameplayScaleRemainingSeconds = 0d;
                }
            }

            GameplayElapsedSeconds = AddChecked(
                GameplayElapsedSeconds,
                scaledDelta,
                "Gameplay elapsed time");
            return scaledDelta;
        }

        // 添加 AddChecked 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static double AddChecked(double current, double delta, string field)
        {
            double result = current + delta;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowException($"{field} exceeded finite range.");
            }

            return result;
        }

        // 校验 ValidateFiniteNonNegative 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        internal static void ValidateFiniteNonNegative(double value, string parameterName)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Time values must be finite and non-negative.");
            }
        }
    }

    // 定义 BattleOutcomeFacts 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct BattleOutcomeFacts
    {
        // 初始化 BattleOutcomeFacts，并建立关卡流程所需的初始状态。
        public BattleOutcomeFacts(
            bool playerDied,
            bool levelCompleted,
            bool durationLimitReached)
        {
            PlayerDied = playerDied;
            LevelCompleted = levelCompleted;
            DurationLimitReached = durationLimitReached;
        }

        public bool PlayerDied { get; }

        public bool LevelCompleted { get; }

        public bool DurationLimitReached { get; }
    }

    // 定义 BattleFlowEvent 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public readonly struct BattleFlowEvent
    {
        // 初始化 BattleFlowEvent，并建立关卡流程所需的初始状态。
        internal BattleFlowEvent(
            ulong sequence,
            BattleFlowEventType eventType,
            BattleFlowState previousState,
            BattleFlowState currentState,
            BattlePauseReason pauseReason,
            UltimateCancelReason ultimateCancelReason,
            SkillActivationStatus skillStatus,
            BattleSettlement settlement,
            in BattleTimeSnapshot time)
        {
            Sequence = sequence;
            EventType = eventType;
            PreviousState = previousState;
            CurrentState = currentState;
            PauseReason = pauseReason;
            UltimateCancelReason = ultimateCancelReason;
            SkillStatus = skillStatus;
            Settlement = settlement;
            Time = time;
        }

        public ulong Sequence { get; }

        public BattleFlowEventType EventType { get; }

        public BattleFlowState PreviousState { get; }

        public BattleFlowState CurrentState { get; }

        public BattlePauseReason PauseReason { get; }

        public UltimateCancelReason UltimateCancelReason { get; }

        public SkillActivationStatus SkillStatus { get; }

        public BattleSettlement Settlement { get; }

        public BattleTimeSnapshot Time { get; }
    }

    // 定义 BattleFlowStateMachine 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class BattleFlowStateMachine
    {
        private readonly BattleFlowSettings settings;
        private ulong nextEventSequence = 1UL;
        private double stateElapsedSeconds;
        private BattleFlowState resumeState;
        private double resumeStateElapsedSeconds;
        private BattlePauseReason activePauseReasons;
        private ulong lastUltimateGestureEventId;

        // 初始化 BattleFlowStateMachine，并建立关卡流程所需的初始状态。
        public BattleFlowStateMachine(BattleFlowSettings configuredSettings)
        {
            settings = configuredSettings ??
                throw new ArgumentNullException(nameof(configuredSettings));
            Time = new BattleTimeSource();
            State = BattleFlowState.Countdown;
            resumeState = BattleFlowState.Countdown;
        }

        public event Action<BattleFlowEvent> EventPublished;

        public BattleFlowSettings Settings => settings;

        public BattleTimeSource Time { get; }

        public BattleFlowState State { get; private set; }

        public double StateElapsedSeconds => stateElapsedSeconds;

        public BattlePauseReason ActivePauseReasons => activePauseReasons;

        public ulong LastUltimateGestureEventId => lastUltimateGestureEventId;

        public bool IsTerminal =>
            State == BattleFlowState.Victory || State == BattleFlowState.Defeat;

        // 推进 Advance 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public BattleTimeSlice Advance(double unscaledDeltaSeconds)
        {
            BattleTimeSource.ValidateFiniteNonNegative(
                unscaledDeltaSeconds,
                nameof(unscaledDeltaSeconds));
            double beforeFlow = Time.FlowElapsedSeconds;
            double beforeGameplayUnscaled = Time.GameplayUnscaledElapsedSeconds;
            double beforeGameplay = Time.GameplayElapsedSeconds;

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == BattleFlowState.Paused || IsTerminal)
            {
                return CreateSlice(
                    unscaledDeltaSeconds,
                    beforeFlow,
                    beforeGameplayUnscaled,
                    beforeGameplay);
            }

            double remaining = unscaledDeltaSeconds;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == BattleFlowState.Countdown &&
                stateElapsedSeconds >= settings.CountdownDurationSeconds)
            {
                TransitionTo(BattleFlowState.Playing);
            }

            // 按确定顺序处理时间线或配置集合，保证关卡结果可复现。
            while (remaining > 0d)
            {
                // 按当前流程、事件或奖励类型选择对应处理分支。
                switch (State)
                {
                    case BattleFlowState.Countdown:
                        AdvanceCountdown(ref remaining);
                        break;
                    case BattleFlowState.Playing:
                        stateElapsedSeconds += remaining;
                        Time.AdvanceGameplay(remaining);
                        remaining = 0d;
                        break;
                    case BattleFlowState.UltimateDrawing:
                        AdvanceUltimateDrawing(ref remaining);
                        break;
                    default:
                        remaining = 0d;
                        break;
                }
            }

            return CreateSlice(
                unscaledDeltaSeconds,
                beforeFlow,
                beforeGameplayUnscaled,
                beforeGameplay);
        }

        // 尝试执行 TryBeginUltimateDrawing 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool TryBeginUltimateDrawing()
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State != BattleFlowState.Playing)
            {
                return false;
            }

            TransitionTo(BattleFlowState.UltimateDrawing);
            return true;
        }

        // 判断是否允许 CanAcceptUltimateGestureEvent 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool CanAcceptUltimateGestureEvent(ulong gestureEventId)
        {
            return State == BattleFlowState.UltimateDrawing &&
                   gestureEventId != 0UL &&
                   gestureEventId > lastUltimateGestureEventId;
        }

        // 解析 ResolveUltimate 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool ResolveUltimate(
            ulong gestureEventId,
            in SkillActivationResult result)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!result.IsValid)
            {
                throw new ArgumentException(
                    "Skill activation result must be initialized.",
                    nameof(result));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!string.Equals(
                    result.SkillId,
                    settings.UltimateSkillId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Skill '{result.SkillId}' is not the configured ultimate.",
                    nameof(result));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State != BattleFlowState.UltimateDrawing)
            {
                return false;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (gestureEventId == 0UL)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gestureEventId),
                    gestureEventId,
                    "Ultimate gesture event id must be non-zero.");
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!CanAcceptUltimateGestureEvent(gestureEventId))
            {
                throw new InvalidOperationException(
                    $"Ultimate gesture event '{gestureEventId}' was already consumed or is out of order.");
            }

            lastUltimateGestureEventId = gestureEventId;

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (result.Succeeded)
            {
                Publish(
                    BattleFlowEventType.UltimateResolved,
                    State,
                    State,
                    BattlePauseReason.None,
                    UltimateCancelReason.None,
                    result.Status,
                    BattleSettlement.None);
                TransitionTo(BattleFlowState.Playing);
                return true;
            }

            CancelUltimateDrawing(
                UltimateCancelReason.SkillRejected,
                result.Status,
                transitionToPlaying: true);
            return true;
        }

        // 判断是否允许 CancelUltimateDrawing 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool CancelUltimateDrawing()
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State != BattleFlowState.UltimateDrawing)
            {
                return false;
            }

            CancelUltimateDrawing(
                UltimateCancelReason.PlayerCanceled,
                SkillActivationStatus.None,
                transitionToPlaying: true);
            return true;
        }

        // 设置 SetPlayerPaused 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetPlayerPaused(bool paused)
        {
            return SetPauseReason(BattlePauseReason.PlayerRequested, paused);
        }

        // 设置 SetApplicationFocus 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetApplicationFocus(bool hasFocus)
        {
            return settings.PauseOnFocusLost &&
                   SetPauseReason(BattlePauseReason.FocusLost, !hasFocus);
        }

        // 设置 SetApplicationPaused 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetApplicationPaused(bool paused)
        {
            return settings.PauseOnFocusLost &&
                   SetPauseReason(BattlePauseReason.ApplicationPaused, paused);
        }

        // 设置 SetPauseReason 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool SetPauseReason(BattlePauseReason reason, bool paused)
        {
            ValidateSinglePauseReason(reason);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (IsTerminal)
            {
                return false;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (paused)
            {
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if ((activePauseReasons & reason) != 0)
                {
                    return false;
                }

                activePauseReasons |= reason;
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (State == BattleFlowState.Paused)
                {
                    return true;
                }

                resumeState = State == BattleFlowState.UltimateDrawing
                    ? BattleFlowState.Playing
                    : State;
                resumeStateElapsedSeconds = State == BattleFlowState.UltimateDrawing
                    ? 0d
                    : stateElapsedSeconds;
                // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
                if (State == BattleFlowState.UltimateDrawing)
                {
                    CancelUltimateDrawing(
                        UltimateCancelReason.BattlePaused,
                        SkillActivationStatus.None,
                        transitionToPlaying: false);
                }

                Publish(
                    BattleFlowEventType.StrokeCancellationRequested,
                    State,
                    State,
                    reason,
                    UltimateCancelReason.None,
                    SkillActivationStatus.None,
                    BattleSettlement.None);
                TransitionTo(BattleFlowState.Paused);
                return true;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if ((activePauseReasons & reason) == 0)
            {
                return false;
            }

            activePauseReasons &= ~reason;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == BattleFlowState.Paused &&
                activePauseReasons == BattlePauseReason.None)
            {
                TransitionTo(resumeState);
                stateElapsedSeconds = resumeStateElapsedSeconds;
            }

            return true;
        }

        // 解析 ResolveOutcome 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public bool ResolveOutcome(in BattleOutcomeFacts facts)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (IsTerminal)
            {
                return false;
            }

            BattleSettlement settlement = BattleSettlement.None;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (facts.PlayerDied || facts.DurationLimitReached)
            {
                settlement = BattleSettlement.Defeat;
            }
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            else if (facts.LevelCompleted)
            {
                settlement = BattleSettlement.Victory;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (settlement == BattleSettlement.None)
            {
                return false;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == BattleFlowState.UltimateDrawing)
            {
                CancelUltimateDrawing(
                    UltimateCancelReason.BattleSettled,
                    SkillActivationStatus.None,
                    transitionToPlaying: false);
            }

            BattleFlowState previous = State;
            activePauseReasons = BattlePauseReason.None;
            TransitionTo(settlement == BattleSettlement.Victory
                ? BattleFlowState.Victory
                : BattleFlowState.Defeat);
            Publish(
                BattleFlowEventType.Settled,
                previous,
                State,
                BattlePauseReason.None,
                UltimateCancelReason.None,
                SkillActivationStatus.None,
                settlement);
            return true;
        }

        // 推进 AdvanceCountdown 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void AdvanceCountdown(ref double remaining)
        {
            double untilPlaying =
                settings.CountdownDurationSeconds - stateElapsedSeconds;
            double consumed = Math.Min(remaining, untilPlaying);
            stateElapsedSeconds += consumed;
            Time.AdvanceFlowOnly(consumed);
            remaining -= consumed;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (stateElapsedSeconds >= settings.CountdownDurationSeconds)
            {
                TransitionTo(BattleFlowState.Playing);
            }
        }

        // 推进 AdvanceUltimateDrawing 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void AdvanceUltimateDrawing(ref double remaining)
        {
            double untilBoundary =
                settings.UltimateInputWindowSeconds - stateElapsedSeconds;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (remaining <= untilBoundary)
            {
                stateElapsedSeconds += remaining;
                Time.AdvanceGameplay(remaining);
                remaining = 0d;
                return;
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (untilBoundary > 0d)
            {
                stateElapsedSeconds += untilBoundary;
                Time.AdvanceGameplay(untilBoundary);
                remaining -= untilBoundary;
            }

            CancelUltimateDrawing(
                UltimateCancelReason.InputWindowExpired,
                SkillActivationStatus.InputWindowExpired,
                transitionToPlaying: true);
        }

        // 判断是否允许 CancelUltimateDrawing 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void CancelUltimateDrawing(
            UltimateCancelReason reason,
            SkillActivationStatus skillStatus,
            bool transitionToPlaying)
        {
            Publish(
                BattleFlowEventType.UltimateCanceled,
                State,
                State,
                BattlePauseReason.None,
                reason,
                skillStatus,
                BattleSettlement.None);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (transitionToPlaying)
            {
                TransitionTo(BattleFlowState.Playing);
            }
        }

        // 处理 TransitionTo 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void TransitionTo(BattleFlowState next)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (State == next)
            {
                return;
            }

            BattleFlowState previous = State;
            State = next;
            stateElapsedSeconds = 0d;
            Publish(
                BattleFlowEventType.StateChanged,
                previous,
                next,
                BattlePauseReason.None,
                UltimateCancelReason.None,
                SkillActivationStatus.None,
                BattleSettlement.None);
        }

        // 创建 CreateSlice 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private BattleTimeSlice CreateSlice(
            double requested,
            double beforeFlow,
            double beforeGameplayUnscaled,
            double beforeGameplay)
        {
            BattleTimeSnapshot current = Time.Current;
            return new BattleTimeSlice(
                requested,
                current.FlowElapsedSeconds - beforeFlow,
                current.GameplayUnscaledElapsedSeconds - beforeGameplayUnscaled,
                current.GameplayElapsedSeconds - beforeGameplay,
                current);
        }

        // 处理 Publish 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void Publish(
            BattleFlowEventType eventType,
            BattleFlowState previous,
            BattleFlowState current,
            BattlePauseReason pauseReason,
            UltimateCancelReason cancelReason,
            SkillActivationStatus skillStatus,
            BattleSettlement settlement)
        {
            ulong sequence = nextEventSequence;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (sequence == 0UL || sequence == ulong.MaxValue)
            {
                throw new OverflowException("Battle flow event sequence is exhausted.");
            }

            nextEventSequence = sequence + 1UL;
            BattleTimeSnapshot time = Time.Current;
            EventPublished?.Invoke(new BattleFlowEvent(
                sequence,
                eventType,
                previous,
                current,
                pauseReason,
                cancelReason,
                skillStatus,
                settlement,
                time));
        }

        // 校验 ValidateSinglePauseReason 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private static void ValidateSinglePauseReason(BattlePauseReason reason)
        {
            int value = (int)reason;
            const int supported =
                (int)BattlePauseReason.PlayerRequested |
                (int)BattlePauseReason.FocusLost |
                (int)BattlePauseReason.ApplicationPaused;
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (value == 0 || (value & (value - 1)) != 0 || (value & ~supported) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason),
                    reason,
                    "Exactly one supported pause reason is required.");
            }
        }
    }
}
