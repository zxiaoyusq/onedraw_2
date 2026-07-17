using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
    // 定义 BattleHudView 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class BattleHudView : MonoBehaviour, IBattleHudView
    {
        private BattleHudViewReferences references;
        private Rect lastScreenSafeArea;
        private Vector2 lastScreenSize;

        public event Action PauseToggleRequested;
        public event Action StanceSwitchRequested;
        public event Action UltimateRequested;
        public event Action RestartRequested;
        public event Action NextLevelRequested;
        public event Action MainMenuRequested;

        public bool IsInitialized => references != null;
        public BattleHudViewModel LastRendered { get; private set; }
        public int RenderCount { get; private set; }
        public RectTransform SafeAreaRoot => RequireReferences().SafeAreaRoot;
        public RectTransform StanceTarget => RequireReferences().StanceRoot;
        public Button StanceButton => RequireReferences().StanceButton;
        public Button PauseButton => RequireReferences().PauseButton;
        public Button UltimateButton => RequireReferences().UltimateButton;
        public Button RestartButton => RequireReferences().RestartButton;
        public Button NextLevelButton => RequireReferences().NextLevelButton;
        public Button MainMenuButton => RequireReferences().MainMenuButton;
        public TMP_Text HpValueText => RequireReferences().HpValue;
        public TMP_Text EnergyValueText => RequireReferences().EnergyValue;
        public TMP_Text ComboValueText => RequireReferences().ComboValue;
        public TMP_Text ScoreValueText => RequireReferences().ScoreValue;
        public TMP_Text StanceValueText => RequireReferences().StanceValue;
        public TMP_Text ResultTitleText => RequireReferences().ResultTitle;

        // 处理 Initialize 对应的表现逻辑，使视图与只读战斗状态保持同步。
        internal void Initialize(BattleHudViewReferences configuredReferences)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (references != null)
            {
                throw new InvalidOperationException("Battle HUD view is already initialized.");
            }

            references = configuredReferences ??
                throw new ArgumentNullException(nameof(configuredReferences));
            references.Validate();
            references.PauseButton.onClick.AddListener(HandlePauseToggle);
            references.ResumeButton.onClick.AddListener(HandlePauseToggle);
            references.StanceButton.onClick.AddListener(HandleStanceSwitch);
            references.UltimateButton.onClick.AddListener(HandleUltimate);
            references.RestartButton.onClick.AddListener(HandleRestart);
            references.NextLevelButton.onClick.AddListener(HandleNextLevel);
            references.MainMenuButton.onClick.AddListener(HandleMainMenu);
            ApplyScreenSafeArea();
        }

        // 渲染 Render 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Render(BattleHudViewModel model)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            BattleHudViewReferences ui = RequireReferences();
            LastRendered = model;
            RenderCount += 1;

            ui.LevelName.text = model.LevelName;
            ui.HpLabel.text = model.HpLabel;
            ui.HpValue.text = model.HpValue;
            ui.HpSlider.value = model.HpNormalized;
            ui.EnergyLabel.text = model.EnergyLabel;
            ui.EnergyValue.text = model.EnergyValue;
            ui.EnergySlider.value = model.EnergyNormalized;
            ui.ComboLabel.text = model.ComboLabel;
            ui.ComboValue.text = model.ComboValue;
            ui.ComboRoot.SetActive(model.ComboVisible);
            ui.ScoreLabel.text = model.ScoreLabel;
            ui.ScoreValue.text = model.ScoreValue;
            ui.StanceLabel.text = model.StanceLabel;
            ui.StanceValue.text = model.StanceValue;
            ui.StanceButton.interactable = model.StanceInteractable;
            ui.UltimateLabel.text = model.UltimateLabel;
            ui.UltimateStatus.text = model.UltimateStatus;
            ui.UltimateCooldown.value = model.UltimateCooldownNormalized;
            ui.UltimateButton.gameObject.SetActive(model.UltimateVisible);
            ui.UltimateButton.interactable = model.UltimateInteractable;

            ui.PauseButtonText.text = model.PauseButtonText;
            ui.PauseButton.gameObject.SetActive(model.PauseButtonVisible);
            ui.PauseButton.interactable = model.PauseButtonInteractable;
            ui.PauseOverlay.SetActive(model.PauseOverlayVisible);
            ui.PausedTitle.text = model.PausedTitle;
            ui.ResumeButtonText.text = model.PauseButtonText;

            ui.ResultPanel.SetActive(model.ResultVisible);
            ui.ResultTitle.text = model.ResultTitle;
            ui.ResultScoreLabel.text = model.ResultScoreLabel;
            ui.ResultScoreValue.text = model.ResultScoreValue;
            ui.StarsLabel.text = model.StarsLabel;
            ui.StarsValue.text = model.StarsValue;
            ui.RewardsLabel.text = model.RewardsLabel;
            ui.RewardsBody.text = model.RewardsBody;
            ui.RewardsRoot.SetActive(model.RewardsVisible);
            ui.RestartButtonText.text = model.RestartText;
            ui.NextLevelButtonText.text = model.NextLevelText;
            ui.NextLevelButton.gameObject.SetActive(model.NextLevelVisible);
            ui.NextLevelButton.interactable = model.NextLevelVisible;
            ui.MainMenuButtonText.text = model.MainMenuText;
            ui.MainMenuButton.gameObject.SetActive(model.MainMenuVisible);
            ui.MainMenuButton.interactable = model.MainMenuVisible;
        }

        // 应用 ApplySafeArea 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void ApplySafeArea(Rect safeAreaPixels, Vector2 screenSizePixels)
        {
            BattleHudViewReferences ui = RequireReferences();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (screenSizePixels.x <= 0f || screenSizePixels.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(screenSizePixels),
                    "Screen size must be positive.");
            }

            Rect screen = new Rect(Vector2.zero, screenSizePixels);
            Rect clamped = Intersect(screen, safeAreaPixels);
            ui.SafeAreaRoot.anchorMin = new Vector2(
                clamped.xMin / screenSizePixels.x,
                clamped.yMin / screenSizePixels.y);
            ui.SafeAreaRoot.anchorMax = new Vector2(
                clamped.xMax / screenSizePixels.x,
                clamped.yMax / screenSizePixels.y);
            ui.SafeAreaRoot.offsetMin = Vector2.zero;
            ui.SafeAreaRoot.offsetMax = Vector2.zero;
            lastScreenSafeArea = safeAreaPixels;
            lastScreenSize = screenSizePixels;
        }

        // 更新 Update 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void Update()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (references != null &&
                (Screen.safeArea != lastScreenSafeArea ||
                 Screen.width != lastScreenSize.x ||
                 Screen.height != lastScreenSize.y))
            {
                ApplyScreenSafeArea();
            }
        }

        // 响应 OnDestroy 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void OnDestroy()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (references == null)
            {
                return;
            }

            references.PauseButton.onClick.RemoveListener(HandlePauseToggle);
            references.ResumeButton.onClick.RemoveListener(HandlePauseToggle);
            references.StanceButton.onClick.RemoveListener(HandleStanceSwitch);
            references.UltimateButton.onClick.RemoveListener(HandleUltimate);
            references.RestartButton.onClick.RemoveListener(HandleRestart);
            references.NextLevelButton.onClick.RemoveListener(HandleNextLevel);
            references.MainMenuButton.onClick.RemoveListener(HandleMainMenu);
        }

        // 应用 ApplyScreenSafeArea 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void ApplyScreenSafeArea()
        {
            ApplySafeArea(
                Screen.safeArea,
                new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height)));
        }

        // 处理 HandlePauseToggle 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void HandlePauseToggle() => PauseToggleRequested?.Invoke();
        // 处理 HandleStanceSwitch 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void HandleStanceSwitch() => StanceSwitchRequested?.Invoke();
        // 处理 HandleUltimate 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void HandleUltimate() => UltimateRequested?.Invoke();
        // 处理 HandleRestart 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void HandleRestart() => RestartRequested?.Invoke();
        // 处理 HandleNextLevel 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void HandleNextLevel() => NextLevelRequested?.Invoke();
        // 处理 HandleMainMenu 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void HandleMainMenu() => MainMenuRequested?.Invoke();

        // 处理 RequireReferences 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private BattleHudViewReferences RequireReferences()
        {
            return references ??
                throw new InvalidOperationException("Battle HUD view is not initialized.");
        }

        // 处理 Intersect 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static Rect Intersect(Rect bounds, Rect value)
        {
            float xMin = Mathf.Clamp(value.xMin, bounds.xMin, bounds.xMax);
            float yMin = Mathf.Clamp(value.yMin, bounds.yMin, bounds.yMax);
            float xMax = Mathf.Clamp(value.xMax, xMin, bounds.xMax);
            float yMax = Mathf.Clamp(value.yMax, yMin, bounds.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }

    // 定义 BattleHudViewReferences 的表现层契约，隔离战斗状态与具体Unity视图实现。
    internal sealed class BattleHudViewReferences
    {
        public RectTransform SafeAreaRoot;
        public RectTransform StanceRoot;
        public Button StanceButton;
        public TMP_Text LevelName;
        public TMP_Text HpLabel;
        public TMP_Text HpValue;
        public Slider HpSlider;
        public TMP_Text EnergyLabel;
        public TMP_Text EnergyValue;
        public Slider EnergySlider;
        public GameObject ComboRoot;
        public TMP_Text ComboLabel;
        public TMP_Text ComboValue;
        public TMP_Text ScoreLabel;
        public TMP_Text ScoreValue;
        public TMP_Text StanceLabel;
        public TMP_Text StanceValue;
        public Button UltimateButton;
        public TMP_Text UltimateLabel;
        public TMP_Text UltimateStatus;
        public Slider UltimateCooldown;
        public Button PauseButton;
        public TMP_Text PauseButtonText;
        public GameObject PauseOverlay;
        public TMP_Text PausedTitle;
        public Button ResumeButton;
        public TMP_Text ResumeButtonText;
        public GameObject ResultPanel;
        public TMP_Text ResultTitle;
        public TMP_Text ResultScoreLabel;
        public TMP_Text ResultScoreValue;
        public TMP_Text StarsLabel;
        public TMP_Text StarsValue;
        public GameObject RewardsRoot;
        public TMP_Text RewardsLabel;
        public TMP_Text RewardsBody;
        public Button RestartButton;
        public TMP_Text RestartButtonText;
        public Button NextLevelButton;
        public TMP_Text NextLevelButtonText;
        public Button MainMenuButton;
        public TMP_Text MainMenuButtonText;

        // 校验 Validate 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Validate()
        {
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            foreach (UnityEngine.Object item in new UnityEngine.Object[]
                     {
                         SafeAreaRoot, StanceRoot, StanceButton, LevelName, HpLabel, HpValue, HpSlider,
                         EnergyLabel, EnergyValue, EnergySlider, ComboRoot,
                         ComboLabel, ComboValue, ScoreLabel, ScoreValue,
                         StanceLabel, StanceValue, UltimateButton, UltimateLabel,
                         UltimateStatus, UltimateCooldown, PauseButton,
                         PauseButtonText, PauseOverlay, PausedTitle, ResumeButton,
                         ResumeButtonText, ResultPanel, ResultTitle,
                         ResultScoreLabel, ResultScoreValue, StarsLabel,
                         StarsValue, RewardsRoot, RewardsLabel, RewardsBody,
                         RestartButton, RestartButtonText, NextLevelButton,
                         NextLevelButtonText, MainMenuButton, MainMenuButtonText,
                     })
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (item == null)
                {
                    throw new ArgumentException("Battle HUD view references must be complete.");
                }
            }
        }
    }
}
