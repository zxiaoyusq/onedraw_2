using System;
using OneStrokeDemon.Config;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OneStrokeDemon.Presentation
{
    // 定义 BattleHudViewFactory 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public static class BattleHudViewFactory
    {
        public const string HudFontResourcePath = "Fonts/OneStrokeDemon UI Latin SDF";

        private static readonly Color PanelColor = new Color32(15, 28, 48, 220);
        private static readonly Color OverlayColor = new Color32(4, 9, 18, 225);
        private static readonly Color AccentColor = new Color32(74, 210, 226, 255);
        private static readonly Color EnergyColor = new Color32(236, 183, 65, 255);
        private static readonly Color DangerColor = new Color32(224, 76, 82, 255);
        private static readonly Color TextColor = new Color32(245, 246, 248, 255);

        // 创建 Create 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public static BattleHudView Create(
            IConfigProvider configProvider,
            Transform parent = null)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            Vector2 referenceResolution = new Vector2(
                ReadPositiveInt(configProvider, ConfigIds.GlobalKeys.ReferenceWidth),
                ReadPositiveInt(configProvider, ConfigIds.GlobalKeys.ReferenceHeight));
            var root = new GameObject(
                "BattleHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(BattleHudView));
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem(root.transform);
            RectTransform safeRoot = CreateRect("SafeArea", root.transform);
            Stretch(safeRoot);

            var ui = new BattleHudViewReferences
            {
                SafeAreaRoot = safeRoot,
            };

            BuildTopHud(safeRoot, ui);
            BuildActionHud(safeRoot, ui);
            BuildPauseOverlay(safeRoot, ui);
            BuildResultOverlay(safeRoot, ui);
            BuildMainMenuButton(safeRoot, ui);

            BattleHudView view = root.GetComponent<BattleHudView>();
            view.Initialize(ui);
            ui.PauseOverlay.SetActive(false);
            ui.ResultPanel.SetActive(false);
            ui.MainMenuButton.gameObject.SetActive(false);
            ui.NextLevelButton.gameObject.SetActive(false);
            ui.ComboRoot.SetActive(false);
            return view;
        }

        // 构建 BuildTopHud 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void BuildTopHud(RectTransform parent, BattleHudViewReferences ui)
        {
            RectTransform panel = CreatePanel("TopHUD", parent, PanelColor);
            SetAnchored(panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(32f, -32f), new Vector2(740f, 190f), new Vector2(0f, 1f));
            ui.LevelName = CreateText("LevelName", panel, 28f, TextAlignmentOptions.MidlineLeft);
            SetAnchored(ui.LevelName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -12f), new Vector2(-40f, 46f), new Vector2(0f, 1f));

            HudBar hp = CreateBar("HP", panel, new Vector2(20f, -62f), DangerColor);
            ui.HpLabel = hp.Label;
            ui.HpValue = hp.Value;
            ui.HpSlider = hp.Slider;
            HudBar energy = CreateBar("Energy", panel, new Vector2(20f, -124f), EnergyColor);
            ui.EnergyLabel = energy.Label;
            ui.EnergyValue = energy.Value;
            ui.EnergySlider = energy.Slider;

            RectTransform livePanel = CreatePanel("LiveScore", parent, PanelColor);
            SetAnchored(livePanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -32f), new Vector2(360f, 122f), new Vector2(0.5f, 1f));
            ui.ScoreLabel = CreateText("ScoreLabel", livePanel, 21f, TextAlignmentOptions.Center);
            SetAnchored(ui.ScoreLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -10f), new Vector2(0f, 32f), new Vector2(0.5f, 1f));
            ui.ScoreValue = CreateText("ScoreValue", livePanel, 34f, TextAlignmentOptions.Center);
            SetAnchored(ui.ScoreValue.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 4f), new Vector2(0f, -38f), new Vector2(0.5f, 0.5f));

            RectTransform combo = CreateRect("Combo", parent);
            SetAnchored(combo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 180f), new Vector2(320f, 92f), new Vector2(0.5f, 0.5f));
            ui.ComboRoot = combo.gameObject;
            ui.ComboLabel = CreateText("ComboLabel", combo, 24f, TextAlignmentOptions.Center);
            SetAnchored(ui.ComboLabel.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(0f, 12f), Vector2.zero, new Vector2(0.5f, 0.5f));
            ui.ComboValue = CreateText("ComboValue", combo, 48f, TextAlignmentOptions.Center);
            SetAnchored(ui.ComboValue.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(0f, -20f), Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        // 构建 BuildActionHud 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void BuildActionHud(RectTransform parent, BattleHudViewReferences ui)
        {
            RectTransform stance = CreatePanel("Stance", parent, PanelColor);
            ui.StanceRoot = stance;
            ui.StanceButton = stance.gameObject.AddComponent<Button>();
            ui.StanceButton.targetGraphic = stance.GetComponent<Image>();
            ui.StanceButton.navigation = new Navigation { mode = Navigation.Mode.None };
            SetAnchored(stance, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(32f, 32f), new Vector2(310f, 96f), Vector2.zero);
            ui.StanceLabel = CreateText("StanceLabel", stance, 18f, TextAlignmentOptions.MidlineLeft);
            SetAnchored(ui.StanceLabel.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(18f, 24f), new Vector2(-36f, 0f), new Vector2(0f, 0.5f));
            ui.StanceValue = CreateText("StanceValue", stance, 28f, TextAlignmentOptions.MidlineRight);
            SetAnchored(ui.StanceValue.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(80f, 0f), new Vector2(-18f, 0f), new Vector2(1f, 0.5f));

            ui.UltimateButton = CreateButton(
                "UltimateButton",
                parent,
                new Vector2(1f, 0f),
                new Vector2(-32f, 32f),
                new Vector2(360f, 126f),
                new Vector2(1f, 0f),
                AccentColor,
                out ui.UltimateLabel);
            ui.UltimateStatus = CreateText(
                "UltimateStatus",
                ui.UltimateButton.transform,
                17f,
                TextAlignmentOptions.Bottom);
            SetAnchored(ui.UltimateStatus.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(0f, 9f), new Vector2(0f, -58f), new Vector2(0.5f, 0f));
            ui.UltimateCooldown = CreateSlider(
                "UltimateCooldown",
                ui.UltimateButton.transform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(12f, 8f),
                new Vector2(-24f, 8f),
                AccentColor);

            ui.PauseButton = CreateButton(
                "PauseButton",
                parent,
                new Vector2(1f, 1f),
                new Vector2(-32f, -32f),
                new Vector2(190f, 72f),
                new Vector2(1f, 1f),
                AccentColor,
                out ui.PauseButtonText);
        }

        // 构建 BuildPauseOverlay 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void BuildPauseOverlay(RectTransform parent, BattleHudViewReferences ui)
        {
            RectTransform overlay = CreatePanel("PauseOverlay", parent, OverlayColor);
            Stretch(overlay);
            ui.PauseOverlay = overlay.gameObject;
            ui.PausedTitle = CreateText("PausedTitle", overlay, 54f, TextAlignmentOptions.Center);
            SetAnchored(ui.PausedTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 110f), new Vector2(720f, 100f), new Vector2(0.5f, 0.5f));
            ui.ResumeButton = CreateButton(
                "ResumeButton",
                overlay,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -16f),
                new Vector2(360f, 84f),
                new Vector2(0.5f, 0.5f),
                AccentColor,
                out ui.ResumeButtonText);
        }

        // 构建 BuildResultOverlay 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void BuildResultOverlay(RectTransform parent, BattleHudViewReferences ui)
        {
            RectTransform overlay = CreatePanel("ResultPanel", parent, OverlayColor);
            Stretch(overlay);
            ui.ResultPanel = overlay.gameObject;
            RectTransform card = CreatePanel("ResultCard", overlay, PanelColor);
            SetAnchored(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(920f, 720f), new Vector2(0.5f, 0.5f));
            ui.ResultTitle = CreateText("ResultTitle", card, 58f, TextAlignmentOptions.Center);
            SetAnchored(ui.ResultTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -32f), new Vector2(0f, 96f), new Vector2(0.5f, 1f));

            ui.ResultScoreLabel = CreateText("ResultScoreLabel", card, 24f, TextAlignmentOptions.MidlineLeft);
            SetAnchored(ui.ResultScoreLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(72f, -150f), new Vector2(-20f, 46f), new Vector2(0f, 1f));
            ui.ResultScoreValue = CreateText("ResultScoreValue", card, 44f, TextAlignmentOptions.MidlineRight);
            SetAnchored(ui.ResultScoreValue.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -150f), new Vector2(-72f, 68f), new Vector2(1f, 1f));
            ui.StarsLabel = CreateText("StarsLabel", card, 24f, TextAlignmentOptions.MidlineLeft);
            SetAnchored(ui.StarsLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(72f, -220f), new Vector2(-20f, 46f), new Vector2(0f, 1f));
            ui.StarsValue = CreateText("StarsValue", card, 36f, TextAlignmentOptions.MidlineRight);
            SetAnchored(ui.StarsValue.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -220f), new Vector2(-72f, 58f), new Vector2(1f, 1f));

            RectTransform rewards = CreateRect("Rewards", card);
            SetAnchored(rewards, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(72f, 188f), new Vector2(-144f, -300f), new Vector2(0.5f, 0.5f));
            ui.RewardsRoot = rewards.gameObject;
            ui.RewardsLabel = CreateText("RewardsLabel", rewards, 24f, TextAlignmentOptions.TopLeft);
            SetAnchored(ui.RewardsLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(0f, 42f), new Vector2(0f, 1f));
            ui.RewardsBody = CreateText("RewardsBody", rewards, 22f, TextAlignmentOptions.TopLeft);
            SetAnchored(ui.RewardsBody.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(0f, 0f), new Vector2(0f, -48f), new Vector2(0f, 1f));

            ui.RestartButton = CreateButton(
                "RestartButton",
                card,
                new Vector2(0.5f, 0f),
                new Vector2(-200f, 48f),
                new Vector2(340f, 84f),
                new Vector2(0.5f, 0f),
                AccentColor,
                out ui.RestartButtonText);
            ui.NextLevelButton = CreateButton(
                "NextLevelButton",
                card,
                new Vector2(0.5f, 0f),
                new Vector2(200f, 48f),
                new Vector2(340f, 84f),
                new Vector2(0.5f, 0f),
                EnergyColor,
                out ui.NextLevelButtonText);
        }

        // 构建 BuildMainMenuButton 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void BuildMainMenuButton(RectTransform parent, BattleHudViewReferences ui)
        {
            ui.MainMenuButton = CreateButton(
                "MainMenuButton",
                parent,
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(300f, 64f),
                new Vector2(0.5f, 0f),
                new Color32(76, 91, 111, 255),
                out ui.MainMenuButtonText);
        }

        // 创建 CreateBar 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static HudBar CreateBar(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Color fillColor)
        {
            RectTransform root = CreateRect(name, parent);
            SetAnchored(root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                anchoredPosition, new Vector2(700f, 54f), new Vector2(0f, 1f));
            TMP_Text label = CreateText($"{name}Label", root, 18f, TextAlignmentOptions.MidlineLeft);
            SetAnchored(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(150f, 28f), new Vector2(0f, 1f));
            TMP_Text value = CreateText($"{name}Value", root, 18f, TextAlignmentOptions.MidlineRight);
            SetAnchored(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(210f, 28f), new Vector2(1f, 1f));
            Slider slider = CreateSlider(
                $"{name}Slider",
                root,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 4f),
                new Vector2(0f, 16f),
                fillColor);
            return new HudBar(label, value, slider);
        }

        // 创建 CreateSlider 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static Slider CreateSlider(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color fillColor)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchorMin = anchorMin;
            root.anchorMax = anchorMax;
            root.offsetMin = offsetMin;
            root.offsetMax = offsetMax;
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color32(255, 255, 255, 44);
            background.raycastTarget = false;
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            RectTransform fill = CreateRect("Fill", root);
            Stretch(fill);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            slider.fillRect = fill;
            slider.targetGraphic = fillImage;
            return slider;
        }

        // 创建 CreateButton 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Vector2 pivot,
            Color color,
            out TMP_Text label)
        {
            RectTransform root = CreateRect(name, parent);
            SetAnchored(root, anchor, anchor, position, size, pivot);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            label = CreateText("Label", root, 24f, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            return button;
        }

        // 创建 CreatePanel 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = color.a > 0.75f;
            return rect;
        }

        // 创建 CreateText 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static TMP_Text CreateText(
            string name,
            Transform parent,
            float size,
            TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset hudFont = Resources.Load<TMP_FontAsset>(HudFontResourcePath);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (hudFont == null)
            {
                throw new InvalidOperationException(
                    $"Configured HUD font is missing from Resources: {HudFontResourcePath}");
            }

            text.font = hudFont;
            text.text = string.Empty;
            text.fontSize = size;
            text.color = TextColor;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        // 创建 CreateRect 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        // 设置 SetAnchored 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void SetAnchored(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // 处理 Stretch 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // 处理 EnsureEventSystem 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void EnsureEventSystem(Transform owner)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject(
                "BattleHUD EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(owner, false);
        }

        // 处理 ReadPositiveInt 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static float ReadPositiveInt(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!string.Equals(row.ValueType, "int", StringComparison.Ordinal) ||
                !row.IntValue.HasValue ||
                row.IntValue.Value <= 0L)
            {
                throw new ArgumentException(
                    $"Global '{key}' must define a positive int reference dimension.",
                    nameof(configProvider));
            }

            return row.IntValue.Value;
        }

        // 定义 HudBar 的表现层契约，隔离战斗状态与具体Unity视图实现。
        private readonly struct HudBar
        {
            // 初始化 HudBar，并建立表现层所需的引用与初始显示状态。
            public HudBar(TMP_Text label, TMP_Text value, Slider slider)
            {
                Label = label;
                Value = value;
                Slider = slider;
            }

            public TMP_Text Label { get; }
            public TMP_Text Value { get; }
            public Slider Slider { get; }
        }
    }

    // 定义 BattleHudRuntime 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class BattleHudRuntime : IDisposable
    {
        private bool disposed;

        // 初始化 BattleHudRuntime，并建立表现层所需的引用与初始显示状态。
        private BattleHudRuntime(BattleHudView view, BattleHudPresenter presenter)
        {
            View = view;
            Presenter = presenter;
        }

        public BattleHudView View { get; private set; }
        public BattleHudPresenter Presenter { get; private set; }

        // 创建 Create 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public static BattleHudRuntime Create(
            IConfigProvider configProvider,
            IBattleHudStateSource stateSource,
            IBattleHudCommandSink commands,
            string playerId,
            BattleHudLanguage language = BattleHudLanguage.ZhCN,
            Transform parent = null)
        {
            BattleHudView view = BattleHudViewFactory.Create(configProvider, parent);
            try
            {
                var presenter = new BattleHudPresenter(
                    configProvider,
                    stateSource,
                    view,
                    commands,
                    playerId,
                    language);
                return new BattleHudRuntime(view, presenter);
            }
            catch
            {
                Destroy(view.gameObject);
                throw;
            }
        }

        // 释放 Dispose 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Dispose()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (disposed)
            {
                return;
            }

            disposed = true;
            Presenter?.Dispose();
            Presenter = null;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (View != null)
            {
                Destroy(View.gameObject);
                View = null;
            }
        }

        // 处理 Destroy 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void Destroy(GameObject gameObject)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
