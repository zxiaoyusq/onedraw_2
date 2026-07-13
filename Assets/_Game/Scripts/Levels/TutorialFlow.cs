using System;
using System.Collections.Generic;
using System.Globalization;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Levels
{
    public enum TutorialEventType
    {
        None = 0,
        BattleReady = 1,
        EnemyWeakpointShown = 2,
        WaveMultiTarget = 3,
        ProjectileSpawned = 4,
        ArmoredEnemySpawned = 5,
        GhostSpawned = 6,
        UltimateReady = 7,
        ValidStroke = 8,
        WeakpointHit = 9,
        StrokeHitCount = 10,
        ProjectileCut = 11,
        ArmorBroken = 12,
        StanceChanged = 13,
        UltimateSucceeded = 14,
    }

    public enum TutorialGestureType
    {
        None = 0,
        Any = 1,
        Horizontal = 2,
        Vertical = 3,
        Diagonal = 4,
        Arc = 5,
        Circle = 6,
        Charged = 7,
    }

    public enum TutorialSequenceState
    {
        WaitingForTrigger = 0,
        Active = 1,
        Completed = 2,
    }

    public enum TutorialRuntimeEventType
    {
        None = 0,
        StepStarted = 1,
        StepCompleted = 2,
        TutorialCompleted = 3,
    }

    public readonly struct TutorialGameplayEvent
    {
        public TutorialGameplayEvent(
            TutorialEventType eventType,
            long value = 1L,
            TutorialGestureType gestureType = TutorialGestureType.Any)
        {
            if (eventType == TutorialEventType.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eventType),
                    eventType,
                    "Tutorial gameplay events require a concrete event type.");
            }

            if (value < 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Tutorial event values must be non-negative.");
            }

            if (gestureType == TutorialGestureType.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gestureType),
                    gestureType,
                    "Tutorial gameplay events require a concrete gesture type.");
            }

            EventType = eventType;
            Value = value;
            GestureType = gestureType;
            IsValid = true;
        }

        public TutorialEventType EventType { get; }

        public long Value { get; }

        public TutorialGestureType GestureType { get; }

        public bool IsValid { get; }
    }

    public readonly struct TutorialEventRequirement
    {
        internal TutorialEventRequirement(
            TutorialEventType eventType,
            long minimumValue)
        {
            EventType = eventType;
            MinimumValue = minimumValue;
            IsConfigured = true;
        }

        public TutorialEventType EventType { get; }

        public long MinimumValue { get; }

        public bool IsConfigured { get; }

        public bool Matches(in TutorialGameplayEvent gameplayEvent)
        {
            return gameplayEvent.IsValid &&
                   gameplayEvent.EventType == EventType &&
                   gameplayEvent.Value >= MinimumValue;
        }
    }

    public sealed class TutorialStepDefinition
    {
        internal TutorialStepDefinition(
            string tutorialId,
            int order,
            in TutorialEventRequirement trigger,
            bool blockProgress,
            double minimumDisplaySeconds,
            in TutorialEventRequirement completion,
            string textKey,
            string highlightTarget,
            TutorialGestureType gestureType)
        {
            TutorialId = tutorialId;
            Order = order;
            Trigger = trigger;
            BlockProgress = blockProgress;
            MinimumDisplaySeconds = minimumDisplaySeconds;
            Completion = completion;
            TextKey = textKey;
            HighlightTarget = highlightTarget;
            GestureType = gestureType;
        }

        public string TutorialId { get; }

        public int Order { get; }

        public TutorialEventRequirement Trigger { get; }

        public bool BlockProgress { get; }

        public double MinimumDisplaySeconds { get; }

        public TutorialEventRequirement Completion { get; }

        public string TextKey { get; }

        public string HighlightTarget { get; }

        public TutorialGestureType GestureType { get; }

        internal bool MatchesCompletion(in TutorialGameplayEvent gameplayEvent)
        {
            if (!Completion.Matches(gameplayEvent))
            {
                return false;
            }

            return GestureType == TutorialGestureType.Any ||
                   gameplayEvent.GestureType == GestureType;
        }
    }

    public sealed class TutorialDefinition
    {
        internal TutorialDefinition(
            string levelId,
            string tutorialId,
            IReadOnlyList<TutorialStepDefinition> steps)
        {
            LevelId = levelId;
            TutorialId = tutorialId;
            Steps = steps;
        }

        public string LevelId { get; }

        public string TutorialId { get; }

        public IReadOnlyList<TutorialStepDefinition> Steps { get; }
    }

    public static class TutorialDefinitionFactory
    {
        private const string ComparisonOperator = ">=";

        public static TutorialDefinition Create(
            IConfigProvider configProvider,
            string levelId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException(
                    "Tutorial level id must be non-empty.",
                    nameof(levelId));
            }

            LevelConfig level = configProvider.GetLevel(levelId);
            if (string.IsNullOrWhiteSpace(level.TutorialId))
            {
                throw Invalid(level.LevelId, "tutorialId must be non-empty");
            }

            IReadOnlyList<TutorialConfig> configured =
                configProvider.GetTutorialSteps(level.TutorialId);
            if (configured.Count == 0)
            {
                throw Invalid(level.TutorialId, "tutorial must contain at least one step");
            }

            var rows = new TutorialConfig[configured.Count];
            for (int index = 0; index < rows.Length; index++)
            {
                rows[index] = configured[index] ??
                    throw Invalid(level.TutorialId, "tutorial step cannot be null");
            }

            Array.Sort(rows, TutorialOrderComparer.Instance);
            var steps = new TutorialStepDefinition[rows.Length];
            for (int index = 0; index < rows.Length; index++)
            {
                TutorialConfig row = rows[index];
                int expectedOrder = index + 1;
                if (!string.Equals(
                        row.TutorialId,
                        level.TutorialId,
                        StringComparison.Ordinal) ||
                    row.Order != expectedOrder)
                {
                    throw Invalid(
                        level.TutorialId,
                        "steps must have matching ownership and contiguous order starting at 1");
                }

                if (row.Order > int.MaxValue)
                {
                    throw Invalid(level.TutorialId, "step order exceeds Int32 range");
                }

                RequireFiniteNonNegative(
                    row.MinDisplaySec,
                    row.TutorialId,
                    expectedOrder,
                    "minDisplaySec");
                RequireNonEmpty(row.TextKey, row.TutorialId, expectedOrder, "textKey");
                RequireNonEmpty(
                    row.HighlightTarget,
                    row.TutorialId,
                    expectedOrder,
                    "highlightTarget");
                configProvider.GetText(row.TextKey);

                TutorialEventRequirement trigger = ParseTrigger(
                    row.TriggerEvent,
                    row.TutorialId,
                    expectedOrder);
                TutorialEventRequirement completion = ParseCompletion(
                    row.CompleteEvent,
                    row.TutorialId,
                    expectedOrder);
                TutorialGestureType gesture = TutorialProtocol.ParseGesture(
                    row.GestureType,
                    row.TutorialId,
                    expectedOrder);

                steps[index] = new TutorialStepDefinition(
                    row.TutorialId,
                    expectedOrder,
                    trigger,
                    row.BlockProgress,
                    row.MinDisplaySec,
                    completion,
                    row.TextKey,
                    row.HighlightTarget,
                    gesture);
            }

            return new TutorialDefinition(
                level.LevelId,
                level.TutorialId,
                Array.AsReadOnly(steps));
        }

        private static TutorialEventRequirement ParseTrigger(
            string configured,
            string tutorialId,
            int order)
        {
            RequireNonEmpty(configured, tutorialId, order, "triggerEvent");
            if (configured.IndexOf(ComparisonOperator, StringComparison.Ordinal) >= 0)
            {
                throw Invalid(
                    tutorialId,
                    order,
                    "triggerEvent cannot contain a value comparison");
            }

            return new TutorialEventRequirement(
                TutorialProtocol.ParseEvent(configured, tutorialId, order, "triggerEvent"),
                1L);
        }

        private static TutorialEventRequirement ParseCompletion(
            string configured,
            string tutorialId,
            int order)
        {
            RequireNonEmpty(configured, tutorialId, order, "completeEvent");
            int operatorIndex = configured.IndexOf(
                ComparisonOperator,
                StringComparison.Ordinal);
            if (operatorIndex < 0)
            {
                return new TutorialEventRequirement(
                    TutorialProtocol.ParseEvent(
                        configured,
                        tutorialId,
                        order,
                        "completeEvent"),
                    1L);
            }

            if (operatorIndex == 0 ||
                operatorIndex + ComparisonOperator.Length >= configured.Length ||
                configured.IndexOf(
                    ComparisonOperator,
                    operatorIndex + ComparisonOperator.Length,
                    StringComparison.Ordinal) >= 0)
            {
                throw Invalid(
                    tutorialId,
                    order,
                    "completeEvent comparison must use one 'event>=positiveInteger' expression");
            }

            string eventName = configured.Substring(0, operatorIndex);
            string thresholdText = configured.Substring(
                operatorIndex + ComparisonOperator.Length);
            if (!long.TryParse(
                    thresholdText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long minimum) ||
                minimum <= 0L)
            {
                throw Invalid(
                    tutorialId,
                    order,
                    "completeEvent threshold must be a positive integer");
            }

            return new TutorialEventRequirement(
                TutorialProtocol.ParseEvent(
                    eventName,
                    tutorialId,
                    order,
                    "completeEvent"),
                minimum);
        }

        private static void RequireNonEmpty(
            string value,
            string tutorialId,
            int order,
            string field)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw Invalid(tutorialId, order, $"{field} must be non-empty");
            }
        }

        private static void RequireFiniteNonNegative(
            float value,
            string tutorialId,
            int order,
            string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw Invalid(
                    tutorialId,
                    order,
                    $"{field} must be finite and non-negative");
            }
        }

        private static ArgumentException Invalid(string owner, string message)
        {
            return new ArgumentException(
                $"Tutorial '{owner}' {message}.",
                "configProvider");
        }

        private static ArgumentException Invalid(
            string tutorialId,
            int order,
            string message)
        {
            return Invalid($"{tutorialId}' step {order}", message);
        }

        private sealed class TutorialOrderComparer : IComparer<TutorialConfig>
        {
            public static readonly TutorialOrderComparer Instance =
                new TutorialOrderComparer();

            public int Compare(TutorialConfig x, TutorialConfig y)
            {
                return x.Order.CompareTo(y.Order);
            }
        }
    }

    public readonly struct TutorialUpdateReport
    {
        internal TutorialUpdateReport(
            bool eventAccepted,
            bool stepStarted,
            bool stepCompleted,
            bool tutorialCompleted,
            int completedStepOrder)
        {
            EventAccepted = eventAccepted;
            StepStarted = stepStarted;
            StepCompleted = stepCompleted;
            TutorialCompleted = tutorialCompleted;
            CompletedStepOrder = completedStepOrder;
        }

        public bool EventAccepted { get; }

        public bool StepStarted { get; }

        public bool StepCompleted { get; }

        public bool TutorialCompleted { get; }

        public int CompletedStepOrder { get; }

        public bool Changed => StepStarted || StepCompleted || TutorialCompleted;
    }

    public readonly struct TutorialRuntimeEvent
    {
        internal TutorialRuntimeEvent(
            ulong sequence,
            TutorialRuntimeEventType eventType,
            TutorialStepDefinition step,
            TutorialEventType sourceEventType,
            long sourceValue,
            double displayElapsedSeconds)
        {
            Sequence = sequence;
            EventType = eventType;
            Step = step;
            SourceEventType = sourceEventType;
            SourceValue = sourceValue;
            DisplayElapsedSeconds = displayElapsedSeconds;
        }

        public ulong Sequence { get; }

        public TutorialRuntimeEventType EventType { get; }

        public TutorialStepDefinition Step { get; }

        public TutorialEventType SourceEventType { get; }

        public long SourceValue { get; }

        public double DisplayElapsedSeconds { get; }
    }

    public sealed class TutorialSequence
    {
        private readonly TutorialDefinition definition;
        private int currentStepIndex;
        private bool completionObserved;
        private TutorialGameplayEvent observedCompletion;
        private ulong nextEventSequence = 1UL;

        public TutorialSequence(TutorialDefinition configuredDefinition)
        {
            definition = configuredDefinition ??
                throw new ArgumentNullException(nameof(configuredDefinition));
            if (definition.Steps == null || definition.Steps.Count == 0)
            {
                throw new ArgumentException(
                    "Tutorial sequence requires at least one configured step.",
                    nameof(configuredDefinition));
            }

            State = TutorialSequenceState.WaitingForTrigger;
        }

        public event Action<TutorialRuntimeEvent> EventPublished;

        public TutorialDefinition Definition => definition;

        public TutorialSequenceState State { get; private set; }

        public TutorialStepDefinition CurrentStep =>
            State == TutorialSequenceState.Completed
                ? null
                : definition.Steps[currentStepIndex];

        public int CurrentStepIndex => currentStepIndex;

        public double DisplayElapsedSeconds { get; private set; }

        public bool CompletionObserved => completionObserved;

        public bool IsProgressBlocked =>
            State == TutorialSequenceState.Active &&
            CurrentStep.BlockProgress;

        public TutorialUpdateReport Notify(in TutorialGameplayEvent gameplayEvent)
        {
            if (!gameplayEvent.IsValid)
            {
                throw new ArgumentException(
                    "Tutorial gameplay event must be initialized.",
                    nameof(gameplayEvent));
            }

            if (State == TutorialSequenceState.Completed)
            {
                return default;
            }

            TutorialStepDefinition step = CurrentStep;
            if (State == TutorialSequenceState.WaitingForTrigger)
            {
                if (!step.Trigger.Matches(gameplayEvent))
                {
                    return default;
                }

                State = TutorialSequenceState.Active;
                DisplayElapsedSeconds = 0d;
                completionObserved = false;
                observedCompletion = default;
                Publish(
                    TutorialRuntimeEventType.StepStarted,
                    step,
                    gameplayEvent.EventType,
                    gameplayEvent.Value);
                return new TutorialUpdateReport(
                    eventAccepted: true,
                    stepStarted: true,
                    stepCompleted: false,
                    tutorialCompleted: false,
                    completedStepOrder: 0);
            }

            if (completionObserved || !step.MatchesCompletion(gameplayEvent))
            {
                return default;
            }

            completionObserved = true;
            observedCompletion = gameplayEvent;
            if (DisplayElapsedSeconds >= step.MinimumDisplaySeconds)
            {
                return CompleteCurrentStep(eventAccepted: true);
            }

            return new TutorialUpdateReport(
                eventAccepted: true,
                stepStarted: false,
                stepCompleted: false,
                tutorialCompleted: false,
                completedStepOrder: 0);
        }

        public TutorialUpdateReport Advance(double unscaledGameplayDeltaSeconds)
        {
            ValidateDelta(unscaledGameplayDeltaSeconds);
            if (State != TutorialSequenceState.Active)
            {
                return default;
            }

            double next = DisplayElapsedSeconds + unscaledGameplayDeltaSeconds;
            if (double.IsNaN(next) || double.IsInfinity(next))
            {
                throw new OverflowException(
                    "Tutorial display elapsed time exceeded finite range.");
            }

            DisplayElapsedSeconds = next;
            if (completionObserved &&
                DisplayElapsedSeconds >= CurrentStep.MinimumDisplaySeconds)
            {
                return CompleteCurrentStep(eventAccepted: false);
            }

            return default;
        }

        private TutorialUpdateReport CompleteCurrentStep(bool eventAccepted)
        {
            TutorialStepDefinition completed = CurrentStep;
            TutorialGameplayEvent source = observedCompletion;
            Publish(
                TutorialRuntimeEventType.StepCompleted,
                completed,
                source.EventType,
                source.Value);

            currentStepIndex++;
            completionObserved = false;
            observedCompletion = default;
            DisplayElapsedSeconds = 0d;
            bool allCompleted = currentStepIndex >= definition.Steps.Count;
            if (allCompleted)
            {
                State = TutorialSequenceState.Completed;
                Publish(
                    TutorialRuntimeEventType.TutorialCompleted,
                    completed,
                    source.EventType,
                    source.Value);
            }
            else
            {
                State = TutorialSequenceState.WaitingForTrigger;
            }

            return new TutorialUpdateReport(
                eventAccepted,
                stepStarted: false,
                stepCompleted: true,
                tutorialCompleted: allCompleted,
                completedStepOrder: completed.Order);
        }

        private void Publish(
            TutorialRuntimeEventType eventType,
            TutorialStepDefinition step,
            TutorialEventType sourceEventType,
            long sourceValue)
        {
            ulong sequence = nextEventSequence;
            if (sequence == 0UL || sequence == ulong.MaxValue)
            {
                throw new OverflowException(
                    "Tutorial runtime event sequence is exhausted.");
            }

            nextEventSequence = sequence + 1UL;
            EventPublished?.Invoke(new TutorialRuntimeEvent(
                sequence,
                eventType,
                step,
                sourceEventType,
                sourceValue,
                DisplayElapsedSeconds));
        }

        private static void ValidateDelta(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Tutorial delta time must be finite and non-negative.");
            }
        }
    }

    internal static class TutorialProtocol
    {
        public static TutorialEventType ParseEvent(
            string value,
            string tutorialId,
            int order,
            string field)
        {
            switch (value)
            {
                case "BattleReady": return TutorialEventType.BattleReady;
                case "EnemyWeakpointShown": return TutorialEventType.EnemyWeakpointShown;
                case "WaveMultiTarget": return TutorialEventType.WaveMultiTarget;
                case "ProjectileSpawned": return TutorialEventType.ProjectileSpawned;
                case "ArmoredEnemySpawned": return TutorialEventType.ArmoredEnemySpawned;
                case "GhostSpawned": return TutorialEventType.GhostSpawned;
                case "UltimateReady": return TutorialEventType.UltimateReady;
                case "ValidStroke": return TutorialEventType.ValidStroke;
                case "WeakpointHit": return TutorialEventType.WeakpointHit;
                case "StrokeHitCount": return TutorialEventType.StrokeHitCount;
                case "ProjectileCut": return TutorialEventType.ProjectileCut;
                case "ArmorBroken": return TutorialEventType.ArmorBroken;
                case "StanceChanged": return TutorialEventType.StanceChanged;
                case "UltimateSucceeded": return TutorialEventType.UltimateSucceeded;
                default:
                    throw new ArgumentException(
                        $"Tutorial '{tutorialId}' step {order} {field} " +
                        $"contains unsupported event '{value}'.",
                        "configProvider");
            }
        }

        public static TutorialGestureType ParseGesture(
            string value,
            string tutorialId,
            int order)
        {
            switch (value)
            {
                case "Any": return TutorialGestureType.Any;
                case "Horizontal": return TutorialGestureType.Horizontal;
                case "Vertical": return TutorialGestureType.Vertical;
                case "Diagonal": return TutorialGestureType.Diagonal;
                case "Arc": return TutorialGestureType.Arc;
                case "Circle": return TutorialGestureType.Circle;
                case "Charged": return TutorialGestureType.Charged;
                default:
                    throw new ArgumentException(
                        $"Tutorial '{tutorialId}' step {order} gestureType " +
                        $"contains unsupported gesture '{value}'.",
                        "configProvider");
            }
        }
    }
}
