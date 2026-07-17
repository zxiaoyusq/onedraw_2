using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;

namespace OneStrokeDemon.Presentation
{
    // 定义 TutorialOverlayState 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class TutorialOverlayState
    {
        // 初始化 TutorialOverlayState，并建立表现层所需的引用与初始显示状态。
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

    // 定义 ITutorialOverlayView 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public interface ITutorialOverlayView
    {
        event Action SkipRequested;

        event Action ReviewRequested;

        void Render(TutorialOverlayState state);
    }

    // 定义 TutorialDirector 的表现层契约，隔离战斗状态与具体Unity视图实现。
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

        // 初始化 TutorialDirector，并建立表现层所需的引用与初始显示状态。
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
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (progress.IsTutorialCompleted(tutorialId))
            {
                coordinator.SkipTutorial();
            }
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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

        // 释放 Dispose 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Dispose()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (disposed)
            {
                return;
            }

            disposed = true;
            coordinator.Tutorial.EventPublished -= OnTutorialEvent;
            view.SkipRequested -= OnSkipRequested;
            view.ReviewRequested -= OnReviewRequested;
        }

        // 响应 OnTutorialEvent 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void OnTutorialEvent(TutorialRuntimeEvent runtimeEvent)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (disposed)
            {
                return;
            }

            // 按当前表现类型或流程状态选择对应的渲染分支。
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

        // 响应 OnSkipRequested 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void OnSkipRequested()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!disposed &&
                coordinator.Tutorial.State != TutorialSequenceState.Completed)
            {
                coordinator.SkipTutorial();
            }
        }

        // 响应 OnReviewRequested 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void OnReviewRequested()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (disposed)
            {
                return;
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (showingReview)
            {
                HideOverlay(reviewVisible: true);
                return;
            }

            ShowStep(lastStep, isReview: true);
        }

        // 显示 ShowStep 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void ShowStep(TutorialStepDefinition step, bool isReview)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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

        // 隐藏 HideOverlay 对应的表现逻辑，使视图与只读战斗状态保持同步。
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

        // 处理 Localize 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private string Localize(TextConfig text)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return language == BattleHudLanguage.EnUS ? text.EnUS : text.ZhCN;
        }
    }
}
