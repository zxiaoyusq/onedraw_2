using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Skills;

namespace OneStrokeDemon.Levels
{
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
    public enum BattlePauseReason
    {
        None = 0,
        PlayerRequested = 1 << 0,
        FocusLost = 1 << 1,
        ApplicationPaused = 1 << 2,
    }

    public enum UltimateCancelReason
    {
        None = 0,
        PlayerCanceled = 1,
        InputWindowExpired = 2,
        SkillRejected = 3,
        BattlePaused = 4,
        BattleSettled = 5,
    }

    public enum BattleSettlement
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
    }

    public enum BattleFlowEventType
    {
        None = 0,
        StateChanged = 1,
        StrokeCancellationRequested = 2,
        UltimateResolved = 3,
        UltimateCanceled = 4,
        Settled = 5,
    }

    public sealed class BattleFlowSettings
    {
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

    public static class BattleFlowSettingsFactory
    {
        public static BattleFlowSettings Create(
            IConfigProvider configProvider,
            string playerId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

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
            if (string.IsNullOrWhiteSpace(player.UltimateSkillId))
            {
                throw InvalidConfig(
                    $"Players.{player.PlayerId}.ultimateSkillId must be non-empty.");
            }

            SkillConfig ultimate = configProvider.GetSkill(player.UltimateSkillId);
            if (!string.Equals(
                    ultimate.TriggerType,
                    SkillTriggerTypes.Ultimate,
                    StringComparison.Ordinal))
            {
                throw InvalidConfig(
                    $"Skill '{ultimate.SkillId}' must use the Ultimate trigger.");
            }

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

        private static double ReadNonNegativeFloat(
            IConfigProvider configProvider,
            string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
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

        private static bool ReadBool(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            if (!string.Equals(row.ValueType, "bool", StringComparison.Ordinal) ||
                !row.BoolValue.HasValue)
            {
                throw InvalidConfig($"Global '{key}' must be a bool value.");
            }

            return row.BoolValue.Value;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static ArgumentException InvalidConfig(string message)
        {
            return new ArgumentException(message, "configProvider");
        }
    }

    public readonly struct BattleTimeSnapshot
    {
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

    public readonly struct BattleTimeSlice
    {
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

        public void ApplyGameplayScale(double scale, double durationSeconds)
        {
            ValidateFiniteNonNegative(scale, nameof(scale));
            ValidateFiniteNonNegative(durationSeconds, nameof(durationSeconds));
            if (durationSeconds == 0d)
            {
                gameplayScale = 1d;
                gameplayScaleRemainingSeconds = 0d;
                return;
            }

            gameplayScale = scale;
            gameplayScaleRemainingSeconds = durationSeconds;
        }

        internal void AdvanceFlowOnly(double unscaledDeltaSeconds)
        {
            ValidateFiniteNonNegative(unscaledDeltaSeconds, nameof(unscaledDeltaSeconds));
            FlowElapsedSeconds = AddChecked(
                FlowElapsedSeconds,
                unscaledDeltaSeconds,
                "Flow elapsed time");
        }

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
            if (gameplayScaleRemainingSeconds > 0d)
            {
                double affectedSeconds = Math.Min(
                    unscaledDeltaSeconds,
                    gameplayScaleRemainingSeconds);
                double remainder = unscaledDeltaSeconds - affectedSeconds;
                scaledDelta = (affectedSeconds * gameplayScale) + remainder;
                gameplayScaleRemainingSeconds -= affectedSeconds;
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

        private static double AddChecked(double current, double delta, string field)
        {
            double result = current + delta;
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowException($"{field} exceeded finite range.");
            }

            return result;
        }

        internal static void ValidateFiniteNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Time values must be finite and non-negative.");
            }
        }
    }

    public readonly struct BattleOutcomeFacts
    {
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

    public readonly struct BattleFlowEvent
    {
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

    public sealed class BattleFlowStateMachine
    {
        private readonly BattleFlowSettings settings;
        private ulong nextEventSequence = 1UL;
        private double stateElapsedSeconds;
        private BattleFlowState resumeState;
        private double resumeStateElapsedSeconds;
        private BattlePauseReason activePauseReasons;
        private ulong lastUltimateGestureEventId;

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

        public BattleTimeSlice Advance(double unscaledDeltaSeconds)
        {
            BattleTimeSource.ValidateFiniteNonNegative(
                unscaledDeltaSeconds,
                nameof(unscaledDeltaSeconds));
            double beforeFlow = Time.FlowElapsedSeconds;
            double beforeGameplayUnscaled = Time.GameplayUnscaledElapsedSeconds;
            double beforeGameplay = Time.GameplayElapsedSeconds;

            if (State == BattleFlowState.Paused || IsTerminal)
            {
                return CreateSlice(
                    unscaledDeltaSeconds,
                    beforeFlow,
                    beforeGameplayUnscaled,
                    beforeGameplay);
            }

            double remaining = unscaledDeltaSeconds;
            if (State == BattleFlowState.Countdown &&
                stateElapsedSeconds >= settings.CountdownDurationSeconds)
            {
                TransitionTo(BattleFlowState.Playing);
            }

            while (remaining > 0d)
            {
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

        public bool TryBeginUltimateDrawing()
        {
            if (State != BattleFlowState.Playing)
            {
                return false;
            }

            TransitionTo(BattleFlowState.UltimateDrawing);
            return true;
        }

        public bool CanAcceptUltimateGestureEvent(ulong gestureEventId)
        {
            return State == BattleFlowState.UltimateDrawing &&
                   gestureEventId != 0UL &&
                   gestureEventId > lastUltimateGestureEventId;
        }

        public bool ResolveUltimate(
            ulong gestureEventId,
            in SkillActivationResult result)
        {
            if (!result.IsValid)
            {
                throw new ArgumentException(
                    "Skill activation result must be initialized.",
                    nameof(result));
            }

            if (!string.Equals(
                    result.SkillId,
                    settings.UltimateSkillId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Skill '{result.SkillId}' is not the configured ultimate.",
                    nameof(result));
            }

            if (State != BattleFlowState.UltimateDrawing)
            {
                return false;
            }

            if (gestureEventId == 0UL)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gestureEventId),
                    gestureEventId,
                    "Ultimate gesture event id must be non-zero.");
            }

            if (!CanAcceptUltimateGestureEvent(gestureEventId))
            {
                throw new InvalidOperationException(
                    $"Ultimate gesture event '{gestureEventId}' was already consumed or is out of order.");
            }

            lastUltimateGestureEventId = gestureEventId;

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

        public bool CancelUltimateDrawing()
        {
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

        public bool SetPlayerPaused(bool paused)
        {
            return SetPauseReason(BattlePauseReason.PlayerRequested, paused);
        }

        public bool SetApplicationFocus(bool hasFocus)
        {
            return settings.PauseOnFocusLost &&
                   SetPauseReason(BattlePauseReason.FocusLost, !hasFocus);
        }

        public bool SetApplicationPaused(bool paused)
        {
            return settings.PauseOnFocusLost &&
                   SetPauseReason(BattlePauseReason.ApplicationPaused, paused);
        }

        public bool SetPauseReason(BattlePauseReason reason, bool paused)
        {
            ValidateSinglePauseReason(reason);
            if (IsTerminal)
            {
                return false;
            }

            if (paused)
            {
                if ((activePauseReasons & reason) != 0)
                {
                    return false;
                }

                activePauseReasons |= reason;
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

            if ((activePauseReasons & reason) == 0)
            {
                return false;
            }

            activePauseReasons &= ~reason;
            if (State == BattleFlowState.Paused &&
                activePauseReasons == BattlePauseReason.None)
            {
                TransitionTo(resumeState);
                stateElapsedSeconds = resumeStateElapsedSeconds;
            }

            return true;
        }

        public bool ResolveOutcome(in BattleOutcomeFacts facts)
        {
            if (IsTerminal)
            {
                return false;
            }

            BattleSettlement settlement = BattleSettlement.None;
            if (facts.PlayerDied || facts.DurationLimitReached)
            {
                settlement = BattleSettlement.Defeat;
            }
            else if (facts.LevelCompleted)
            {
                settlement = BattleSettlement.Victory;
            }

            if (settlement == BattleSettlement.None)
            {
                return false;
            }

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

        private void AdvanceCountdown(ref double remaining)
        {
            double untilPlaying =
                settings.CountdownDurationSeconds - stateElapsedSeconds;
            double consumed = Math.Min(remaining, untilPlaying);
            stateElapsedSeconds += consumed;
            Time.AdvanceFlowOnly(consumed);
            remaining -= consumed;
            if (stateElapsedSeconds >= settings.CountdownDurationSeconds)
            {
                TransitionTo(BattleFlowState.Playing);
            }
        }

        private void AdvanceUltimateDrawing(ref double remaining)
        {
            double untilBoundary =
                settings.UltimateInputWindowSeconds - stateElapsedSeconds;
            if (remaining <= untilBoundary)
            {
                stateElapsedSeconds += remaining;
                Time.AdvanceGameplay(remaining);
                remaining = 0d;
                return;
            }

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
            if (transitionToPlaying)
            {
                TransitionTo(BattleFlowState.Playing);
            }
        }

        private void TransitionTo(BattleFlowState next)
        {
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

        private static void ValidateSinglePauseReason(BattlePauseReason reason)
        {
            int value = (int)reason;
            const int supported =
                (int)BattlePauseReason.PlayerRequested |
                (int)BattlePauseReason.FocusLost |
                (int)BattlePauseReason.ApplicationPaused;
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
