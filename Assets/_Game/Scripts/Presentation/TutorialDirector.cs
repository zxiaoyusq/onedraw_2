using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;

namespace OneStrokeDemon.Presentation
{
    public sealed class TutorialOverlayState
    {
        internal TutorialOverlayState(
            bool overlayVisible,
            string prompt,
            string highlightTarget,
            TutorialGestureType gestureType,
            string skipText,
            string reviewText,
            bool skipVisible,
            bool reviewVisible,
            bool isReview)
        {
            OverlayVisible = overlayVisible;
            Prompt = prompt ?? string.Empty;
            HighlightTarget = highlightTarget ?? string.Empty;
            GestureType = gestureType;
            SkipText = skipText ?? throw new ArgumentNullException(nameof(skipText));
            ReviewText = reviewText ?? throw new ArgumentNullException(nameof(reviewText));
            SkipVisible = skipVisible;
            ReviewVisible = reviewVisible;
            IsReview = isReview;
        }

        public bool OverlayVisible { get; }

        public string Prompt { get; }

        public string HighlightTarget { get; }

        public TutorialGestureType GestureType { get; }

        public string SkipText { get; }

        public string ReviewText { get; }

        public bool SkipVisible { get; }

        public bool ReviewVisible { get; }

        public bool IsReview { get; }
    }

    public interface ITutorialOverlayView
    {
        event Action SkipRequested;

        event Action ReviewRequested;

        void Render(TutorialOverlayState state);
    }

    public sealed class TutorialDirector : IDisposable
    {
        private readonly IConfigProvider configProvider;
        private readonly TutorialLevelCoordinator coordinator;
        private readonly ITutorialCompletionProgress progress;
        private readonly ITutorialOverlayView view;
        private readonly BattleHudLanguage language;
        private readonly string skipText;
        private readonly string reviewText;
        private TutorialStepDefinition lastStep;
        private bool showingReview;
        private bool disposed;

        public TutorialDirector(
            IConfigProvider configuredProvider,
            TutorialLevelCoordinator tutorialCoordinator,
            ITutorialCompletionProgress completionProgress,
            ITutorialOverlayView overlayView,
            BattleHudLanguage configuredLanguage = BattleHudLanguage.ZhCN)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            coordinator = tutorialCoordinator ??
                throw new ArgumentNullException(nameof(tutorialCoordinator));
            progress = completionProgress ??
                throw new ArgumentNullException(nameof(completionProgress));
            view = overlayView ?? throw new ArgumentNullException(nameof(overlayView));
            language = configuredLanguage;
            skipText = Localize(configProvider.GetText(ConfigIds.Texts.TextUiTutorialSkip));
            reviewText = Localize(configProvider.GetText(ConfigIds.Texts.TextUiTutorialReview));
            lastStep = coordinator.Tutorial.CurrentStep ??
                coordinator.Tutorial.Definition.Steps[0];

            coordinator.Tutorial.EventPublished += OnTutorialEvent;
            view.SkipRequested += OnSkipRequested;
            view.ReviewRequested += OnReviewRequested;

            string tutorialId = coordinator.Tutorial.Definition.TutorialId;
            if (progress.IsTutorialCompleted(tutorialId))
            {
                coordinator.SkipTutorial();
            }
            else if (coordinator.Tutorial.State == TutorialSequenceState.Active)
            {
                ShowStep(coordinator.Tutorial.CurrentStep, isReview: false);
            }
            else
            {
                HideOverlay(reviewVisible: true);
            }
        }

        public TutorialOverlayState Current { get; private set; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            coordinator.Tutorial.EventPublished -= OnTutorialEvent;
            view.SkipRequested -= OnSkipRequested;
            view.ReviewRequested -= OnReviewRequested;
        }

        private void OnTutorialEvent(TutorialRuntimeEvent runtimeEvent)
        {
            if (disposed)
            {
                return;
            }

            switch (runtimeEvent.EventType)
            {
                case TutorialRuntimeEventType.StepStarted:
                    lastStep = runtimeEvent.Step;
                    ShowStep(lastStep, isReview: false);
                    break;
                case TutorialRuntimeEventType.StepCompleted:
                    lastStep = runtimeEvent.Step;
                    HideOverlay(reviewVisible: true);
                    break;
                case TutorialRuntimeEventType.TutorialSkipped:
                    lastStep = runtimeEvent.Step ?? lastStep;
                    HideOverlay(reviewVisible: true);
                    break;
                case TutorialRuntimeEventType.TutorialCompleted:
                    progress.MarkTutorialCompleted(
                        coordinator.Tutorial.Definition.TutorialId);
                    HideOverlay(reviewVisible: true);
                    break;
            }
        }

        private void OnSkipRequested()
        {
            if (!disposed &&
                coordinator.Tutorial.State != TutorialSequenceState.Completed)
            {
                coordinator.SkipTutorial();
            }
        }

        private void OnReviewRequested()
        {
            if (disposed)
            {
                return;
            }

            if (showingReview)
            {
                HideOverlay(reviewVisible: true);
                return;
            }

            ShowStep(lastStep, isReview: true);
        }

        private void ShowStep(TutorialStepDefinition step, bool isReview)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            showingReview = isReview;
            Current = new TutorialOverlayState(
                overlayVisible: true,
                prompt: Localize(configProvider.GetText(step.TextKey)),
                highlightTarget: step.HighlightTarget,
                gestureType: step.GestureType,
                skipText,
                reviewText,
                skipVisible: !isReview &&
                    coordinator.Tutorial.State != TutorialSequenceState.Completed,
                reviewVisible: isReview,
                isReview: isReview);
            view.Render(Current);
        }

        private void HideOverlay(bool reviewVisible)
        {
            showingReview = false;
            Current = new TutorialOverlayState(
                overlayVisible: false,
                prompt: string.Empty,
                highlightTarget: string.Empty,
                gestureType: TutorialGestureType.None,
                skipText,
                reviewText,
                skipVisible: false,
                reviewVisible,
                isReview: false);
            view.Render(Current);
        }

        private string Localize(TextConfig text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return language == BattleHudLanguage.EnUS ? text.EnUS : text.ZhCN;
        }
    }
}
