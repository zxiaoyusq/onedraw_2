using System;

namespace OneStrokeDemon.Actors
{
    public enum EnemyState
    {
        None = 0,
        Spawn = 1,
        Move = 2,
        Windup = 3,
        Attack = 4,
        Recovery = 5,
        Stun = 6,
        Dead = 7
    }

    public enum EnemyTransitionReason
    {
        None = 0,
        Spawned = 1,
        SpawnCompleted = 2,
        AttackStarted = 3,
        WindupCompleted = 4,
        AttackCompleted = 5,
        RecoveryCompleted = 6,
        Interrupted = 7,
        TimedStunApplied = 8,
        StunCompleted = 9,
        Killed = 10,
        Released = 11
    }

    public enum EnemyInterruptStatus
    {
        None = 0,
        Interrupted = 1,
        Inactive = 2,
        Dead = 3,
        AlreadyStunned = 4,
        NotAttacking = 5,
        GestureMismatch = 6,
        OutsideWindow = 7
    }

    public enum EnemyKillStatus
    {
        None = 0,
        Killed = 1,
        Inactive = 2,
        AlreadyDead = 3
    }

    public enum EnemyReleaseStatus
    {
        None = 0,
        Released = 1,
        AlreadyReleased = 2
    }

    public readonly struct EnemyStateSnapshot
    {
        internal EnemyStateSnapshot(
            EnemyState state,
            double enteredAt,
            double lastTimestamp,
            ulong transitionSequence,
            string attackId,
            double attackStartedAt,
            double stunUntil,
            bool hasClock)
        {
            State = state;
            EnteredAt = enteredAt;
            LastTimestamp = lastTimestamp;
            TransitionSequence = transitionSequence;
            AttackId = attackId ?? string.Empty;
            AttackStartedAt = attackStartedAt;
            StunUntil = stunUntil;
            HasClock = hasClock;
            IsValid = true;
        }

        public EnemyState State { get; }

        public double EnteredAt { get; }

        public double LastTimestamp { get; }

        public ulong TransitionSequence { get; }

        public string AttackId { get; }

        public double AttackStartedAt { get; }

        public double StunUntil { get; }

        public bool HasClock { get; }

        public bool IsValid { get; }

        public bool IsSpawned => State != EnemyState.None;

        public bool IsAlive => IsSpawned && State != EnemyState.Dead;

        public bool CanReceiveCombatHits =>
            State == EnemyState.Move ||
            State == EnemyState.Windup ||
            State == EnemyState.Attack ||
            State == EnemyState.Recovery ||
            State == EnemyState.Stun;
    }

    public readonly struct EnemyStateTransition
    {
        internal EnemyStateTransition(
            ulong sequence,
            EnemyState previousState,
            EnemyState currentState,
            EnemyTransitionReason reason,
            double timestamp,
            string attackId)
        {
            Sequence = sequence;
            PreviousState = previousState;
            CurrentState = currentState;
            Reason = reason;
            Timestamp = timestamp;
            AttackId = attackId ?? string.Empty;
            IsValid = true;
        }

        public ulong Sequence { get; }

        public EnemyState PreviousState { get; }

        public EnemyState CurrentState { get; }

        public EnemyTransitionReason Reason { get; }

        public double Timestamp { get; }

        public string AttackId { get; }

        public bool IsValid { get; }
    }

    public readonly struct EnemyInterruptResult
    {
        internal EnemyInterruptResult(
            EnemyInterruptStatus status,
            string attackId,
            double attackElapsedSeconds,
            EnemyStateSnapshot state)
        {
            Status = status;
            AttackId = attackId ?? string.Empty;
            AttackElapsedSeconds = attackElapsedSeconds;
            State = state;
            IsValid = true;
        }

        public EnemyInterruptStatus Status { get; }

        public string AttackId { get; }

        public double AttackElapsedSeconds { get; }

        public EnemyStateSnapshot State { get; }

        public bool IsValid { get; }

        public bool DidInterrupt => Status == EnemyInterruptStatus.Interrupted;
    }

    public sealed class EnemyStateMachine
    {
        private EnemyState state;
        private double enteredAt;
        private double lastTimestamp;
        private double attackStartedAt;
        private double stunUntil;
        private ulong transitionSequence;
        private bool hasClock;
        private EnemyAttackTimeline activeAttack;

        public event Action<EnemyStateTransition> Transitioned;

        public EnemyStateSnapshot Current => new EnemyStateSnapshot(
            state,
            enteredAt,
            lastTimestamp,
            transitionSequence,
            activeAttack.IsConfigured ? activeAttack.AttackId : string.Empty,
            attackStartedAt,
            stunUntil,
            hasClock);

        public void Spawn(double timestamp)
        {
            ValidateTimestamp(timestamp, nameof(timestamp));
            if (state != EnemyState.None)
            {
                throw new InvalidOperationException(
                    $"Enemy in state '{state}' must be released before it can spawn again.");
            }

            hasClock = true;
            lastTimestamp = timestamp;
            enteredAt = timestamp;
            attackStartedAt = 0d;
            stunUntil = double.PositiveInfinity;
            activeAttack = default;
            TransitionTo(
                EnemyState.Spawn,
                EnemyTransitionReason.Spawned,
                timestamp,
                string.Empty);
        }

        public bool CompleteSpawn(double timestamp)
        {
            ObserveTimestamp(timestamp, nameof(timestamp));
            if (state != EnemyState.Spawn)
            {
                return false;
            }

            TransitionTo(
                EnemyState.Move,
                EnemyTransitionReason.SpawnCompleted,
                timestamp,
                string.Empty);
            return true;
        }

        public bool BeginAttack(in EnemyAttackTimeline attack, double timestamp)
        {
            if (!attack.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy attack timeline must be configured.",
                    nameof(attack));
            }

            ObserveTimestamp(timestamp, nameof(timestamp));
            if (state != EnemyState.Move)
            {
                return false;
            }

            activeAttack = attack;
            attackStartedAt = timestamp;
            TransitionTo(
                EnemyState.Windup,
                EnemyTransitionReason.AttackStarted,
                timestamp,
                attack.AttackId);
            AdvanceTimedTransitions(timestamp);
            return true;
        }

        public int Tick(double timestamp)
        {
            ObserveTimestamp(timestamp, nameof(timestamp));
            return AdvanceTimedTransitions(timestamp);
        }

        public EnemyInterruptResult TryInterrupt(string gestureType, double timestamp)
        {
            if (string.IsNullOrWhiteSpace(gestureType))
            {
                throw new ArgumentException(
                    "Interrupt gesture type must be non-empty.",
                    nameof(gestureType));
            }

            ObserveTimestamp(timestamp, nameof(timestamp));
            if (state == EnemyState.None)
            {
                return InterruptResult(EnemyInterruptStatus.Inactive, string.Empty, 0d);
            }

            if (state == EnemyState.Dead)
            {
                return InterruptResult(EnemyInterruptStatus.Dead, string.Empty, 0d);
            }

            if (state == EnemyState.Stun)
            {
                return InterruptResult(EnemyInterruptStatus.AlreadyStunned, string.Empty, 0d);
            }

            if ((state != EnemyState.Windup && state != EnemyState.Attack) ||
                !activeAttack.IsConfigured)
            {
                return InterruptResult(EnemyInterruptStatus.NotAttacking, string.Empty, 0d);
            }

            string attackId = activeAttack.AttackId;
            double elapsed = timestamp - attackStartedAt;
            if (!activeAttack.GestureMatches(gestureType))
            {
                return InterruptResult(
                    EnemyInterruptStatus.GestureMismatch,
                    attackId,
                    elapsed);
            }

            if (!activeAttack.IsInsideInterruptWindow(elapsed))
            {
                return InterruptResult(
                    EnemyInterruptStatus.OutsideWindow,
                    attackId,
                    elapsed);
            }

            stunUntil = double.PositiveInfinity;
            TransitionTo(
                EnemyState.Stun,
                EnemyTransitionReason.Interrupted,
                timestamp,
                attackId);
            activeAttack = default;
            attackStartedAt = 0d;
            return InterruptResult(EnemyInterruptStatus.Interrupted, attackId, elapsed);
        }

        public bool ApplyTimedStun(double durationSeconds, double timestamp)
        {
            ValidateFiniteNonNegative(durationSeconds, nameof(durationSeconds));
            ObserveTimestamp(timestamp, nameof(timestamp));
            if (durationSeconds <= 0d || state == EnemyState.None || state == EnemyState.Dead)
            {
                return false;
            }

            double requestedUntil = timestamp + durationSeconds;
            if (double.IsInfinity(requestedUntil) || double.IsNaN(requestedUntil))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationSeconds),
                    "Timed stun end must remain finite.");
            }

            if (state == EnemyState.Stun)
            {
                if (!double.IsPositiveInfinity(stunUntil))
                {
                    stunUntil = Math.Max(stunUntil, requestedUntil);
                }

                return true;
            }

            string interruptedAttack = activeAttack.IsConfigured
                ? activeAttack.AttackId
                : string.Empty;
            stunUntil = requestedUntil;
            TransitionTo(
                EnemyState.Stun,
                EnemyTransitionReason.TimedStunApplied,
                timestamp,
                interruptedAttack);
            activeAttack = default;
            attackStartedAt = 0d;
            return true;
        }

        public bool RecoverFromStun(double timestamp)
        {
            ObserveTimestamp(timestamp, nameof(timestamp));
            if (state != EnemyState.Stun)
            {
                return false;
            }

            stunUntil = double.PositiveInfinity;
            TransitionTo(
                EnemyState.Move,
                EnemyTransitionReason.StunCompleted,
                timestamp,
                string.Empty);
            return true;
        }

        public EnemyKillStatus TryKill(double timestamp)
        {
            ObserveTimestamp(timestamp, nameof(timestamp));
            if (state == EnemyState.None)
            {
                return EnemyKillStatus.Inactive;
            }

            if (state == EnemyState.Dead)
            {
                return EnemyKillStatus.AlreadyDead;
            }

            string interruptedAttack = activeAttack.IsConfigured
                ? activeAttack.AttackId
                : string.Empty;
            TransitionTo(
                EnemyState.Dead,
                EnemyTransitionReason.Killed,
                timestamp,
                interruptedAttack);
            activeAttack = default;
            attackStartedAt = 0d;
            stunUntil = double.PositiveInfinity;
            return EnemyKillStatus.Killed;
        }

        public EnemyReleaseStatus Release(double timestamp)
        {
            ValidateTimestamp(timestamp, nameof(timestamp));
            if (state == EnemyState.None)
            {
                return EnemyReleaseStatus.AlreadyReleased;
            }

            ObserveTimestamp(timestamp, nameof(timestamp));
            string attackId = activeAttack.IsConfigured
                ? activeAttack.AttackId
                : string.Empty;
            TransitionTo(
                EnemyState.None,
                EnemyTransitionReason.Released,
                timestamp,
                attackId);
            activeAttack = default;
            attackStartedAt = 0d;
            stunUntil = double.PositiveInfinity;
            enteredAt = 0d;
            lastTimestamp = 0d;
            hasClock = false;
            return EnemyReleaseStatus.Released;
        }

        private int AdvanceTimedTransitions(double timestamp)
        {
            int transitionCount = 0;
            bool advanced;
            do
            {
                advanced = false;
                if (state == EnemyState.Windup && activeAttack.IsConfigured)
                {
                    double boundary = attackStartedAt + activeAttack.WindupSeconds;
                    if (timestamp >= boundary)
                    {
                        TransitionTo(
                            EnemyState.Attack,
                            EnemyTransitionReason.WindupCompleted,
                            boundary,
                            activeAttack.AttackId);
                        transitionCount++;
                        advanced = true;
                    }
                }
                else if (state == EnemyState.Attack && activeAttack.IsConfigured)
                {
                    double boundary = attackStartedAt +
                                      activeAttack.WindupSeconds +
                                      activeAttack.ActiveSeconds;
                    if (timestamp >= boundary)
                    {
                        TransitionTo(
                            EnemyState.Recovery,
                            EnemyTransitionReason.AttackCompleted,
                            boundary,
                            activeAttack.AttackId);
                        transitionCount++;
                        advanced = true;
                    }
                }
                else if (state == EnemyState.Recovery && activeAttack.IsConfigured)
                {
                    double boundary = attackStartedAt + activeAttack.CooldownSeconds;
                    if (timestamp >= boundary)
                    {
                        string completedAttack = activeAttack.AttackId;
                        TransitionTo(
                            EnemyState.Move,
                            EnemyTransitionReason.RecoveryCompleted,
                            boundary,
                            completedAttack);
                        activeAttack = default;
                        attackStartedAt = 0d;
                        transitionCount++;
                        advanced = true;
                    }
                }
                else if (state == EnemyState.Stun &&
                         !double.IsPositiveInfinity(stunUntil) &&
                         timestamp >= stunUntil)
                {
                    double boundary = stunUntil;
                    stunUntil = double.PositiveInfinity;
                    TransitionTo(
                        EnemyState.Move,
                        EnemyTransitionReason.StunCompleted,
                        boundary,
                        string.Empty);
                    transitionCount++;
                    advanced = true;
                }
            }
            while (advanced);

            return transitionCount;
        }

        private EnemyInterruptResult InterruptResult(
            EnemyInterruptStatus status,
            string attackId,
            double elapsed)
        {
            return new EnemyInterruptResult(status, attackId, elapsed, Current);
        }

        private void TransitionTo(
            EnemyState nextState,
            EnemyTransitionReason reason,
            double timestamp,
            string attackId)
        {
            EnemyState previousState = state;
            state = nextState;
            enteredAt = timestamp;
            ulong sequence = transitionSequence + 1UL;
            if (sequence == 0UL)
            {
                throw new OverflowException("Enemy transition sequence is exhausted.");
            }

            transitionSequence = sequence;
            Transitioned?.Invoke(new EnemyStateTransition(
                sequence,
                previousState,
                nextState,
                reason,
                timestamp,
                attackId));
        }

        private void ObserveTimestamp(double timestamp, string parameterName)
        {
            ValidateTimestamp(timestamp, parameterName);
            if (!hasClock)
            {
                if (state == EnemyState.None)
                {
                    return;
                }

                throw new InvalidOperationException("Spawned enemy state has no active clock.");
            }

            if (timestamp < lastTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    timestamp,
                    $"Enemy timestamp cannot move backwards from {lastTimestamp}.");
            }

            lastTimestamp = timestamp;
        }

        private static void ValidateTimestamp(double timestamp, string parameterName)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    timestamp,
                    "Enemy timestamp must be finite and non-negative.");
            }
        }

        private static void ValidateFiniteNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Enemy duration must be finite and non-negative.");
            }
        }
    }
}
