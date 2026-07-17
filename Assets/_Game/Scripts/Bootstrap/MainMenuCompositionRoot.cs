using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OneStrokeDemon.Bootstrap
{
    [DisallowMultipleComponent]
    // 定义 MainMenuCompositionRoot 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    public sealed class MainMenuCompositionRoot : MonoBehaviour
    {
        private MainMenuView view;
        private ResultService progress;
        private SceneFlowService sceneFlow;

        public MainMenuView View => view;

        // 启动 Start 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void Start()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!GameplayConfigRuntime.IsReady || !AssetRegistryRuntime.IsReady)
            {
                return;
            }

            IConfigProvider config = GameplayConfigRuntime.Current;
            progress = new ResultService(config, new PlayerPrefsProgressSaveStore());
            sceneFlow = new SceneFlowService();
            view = MainMenuViewFactory.Create(
                config,
                AssetRegistryRuntime.Current,
                progress.Current,
                transform);
            view.StartRequested += OnStartRequested;
            view.LevelRequested += OnLevelRequested;
        }

        // 响应 OnDestroy 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnDestroy()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (view != null)
            {
                view.StartRequested -= OnStartRequested;
                view.LevelRequested -= OnLevelRequested;
            }
        }

        // 响应 OnStartRequested 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnStartRequested()
        {
            view.ShowLevelSelection();
        }

        // 响应 OnLevelRequested 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnLevelRequested(string levelId)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!progress.Current.IsLevelUnlocked(levelId))
            {
                return;
            }

            BattleLaunchContext.Select(GameplayConfigRuntime.Current, levelId);
            sceneFlow.LoadBattle();
        }
    }

    // 定义 MainMenuView 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    public sealed class MainMenuView : MonoBehaviour
    {
        private readonly List<MainMenuLevelChoice> choices =
            new List<MainMenuLevelChoice>();
        private GameObject levelSelection;

        public event Action StartRequested;
        public event Action<string> LevelRequested;

        public Button StartButton { get; private set; }

        public IReadOnlyList<MainMenuLevelChoice> LevelChoices => choices;

        // 处理 Initialize 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        internal void Initialize(
            Button configuredStartButton,
            GameObject configuredLevelSelection,
            IEnumerable<MainMenuLevelChoice> configuredChoices)
        {
            StartButton = configuredStartButton ??
                throw new ArgumentNullException(nameof(configuredStartButton));
            levelSelection = configuredLevelSelection ??
                throw new ArgumentNullException(nameof(configuredLevelSelection));
            choices.AddRange(configuredChoices ??
                throw new ArgumentNullException(nameof(configuredChoices)));
            StartButton.onClick.AddListener(() => StartRequested?.Invoke());
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            for (int index = 0; index < choices.Count; index++)
            {
                MainMenuLevelChoice choice = choices[index];
                string captured = choice.LevelId;
                choice.Button.onClick.AddListener(
                    () => LevelRequested?.Invoke(captured));
            }
        }

        // 处理 ShowLevelSelection 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void ShowLevelSelection()
        {
            StartButton.gameObject.SetActive(false);
            levelSelection.SetActive(true);
        }
    }

    // 定义 MainMenuLevelChoice 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    public sealed class MainMenuLevelChoice
    {
        // 初始化 MainMenuLevelChoice，并建立生产入口或战斗会话的依赖关系。
        internal MainMenuLevelChoice(string levelId, Button button, bool unlocked)
        {
            LevelId = levelId;
            Button = button;
            IsUnlocked = unlocked;
        }

        public string LevelId { get; }

        public Button Button { get; }

        public bool IsUnlocked { get; }
    }

    // 定义 MainMenuViewFactory 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    internal static class MainMenuViewFactory
    {
        private static readonly Color PanelColor = new Color32(17, 22, 34, 226);
        private static readonly Color AccentColor = new Color32(174, 54, 48, 255);
        private static readonly Color UnlockedColor = new Color32(170, 61, 48, 255);
        private static readonly Color LockedColor = new Color32(66, 71, 82, 255);

        // 创建 Create 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public static MainMenuView Create(
            IConfigProvider config,
            IAssetRegistry assets,
            ProgressSnapshot progress,
            Transform parent)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var root = new GameObject(
                "Production Main Menu",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(MainMenuView));
            root.transform.SetParent(parent, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReadReferenceResolution(config);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem(root.transform);

            LevelConfig firstLevel = config.GetLevels()[0];
            Sprite background = assets.GetSprite(firstLevel.BackgroundAssetKey);
            RectTransform backdrop = CreateRect("Backdrop", root.transform);
            Stretch(backdrop);
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.sprite = background;
            backdropImage.preserveAspect = false;
            backdropImage.raycastTarget = false;
            Image shade = CreatePanel("Shade", root.transform, new Color32(4, 7, 12, 116));
            Stretch(shade.rectTransform);

            RectTransform card = CreatePanel("MenuCard", root.transform, PanelColor).rectTransform;
            SetAnchored(
                card,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(820f, 790f),
                new Vector2(0.5f, 0.5f));
            TMP_Text title = CreateText("Title", card, 82f, TextAlignmentOptions.Center);
            title.text = Localize(config.GetText(ConfigIds.Texts.TextGameTitle));
            SetAnchored(
                title.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -64f),
                new Vector2(0f, 150f),
                new Vector2(0.5f, 1f));

            Button start = CreateButton(
                "StartButton",
                card,
                new Vector2(0.5f, 0f),
                new Vector2(0f, 98f),
                new Vector2(470f, 104f),
                AccentColor,
                Localize(config.GetText(ConfigIds.Texts.TextUiStartGame)));

            RectTransform selection = CreateRect("LevelSelection", card);
            selection.anchorMin = new Vector2(0f, 0f);
            selection.anchorMax = new Vector2(1f, 1f);
            selection.offsetMin = new Vector2(58f, 48f);
            selection.offsetMax = new Vector2(-58f, -225f);
            TMP_Text selectionTitle = CreateText(
                "SelectionTitle",
                selection,
                34f,
                TextAlignmentOptions.Center);
            selectionTitle.text = Localize(
                config.GetText(ConfigIds.Texts.TextUiSelectLevel));
            SetAnchored(
                selectionTitle.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                new Vector2(0f, 64f),
                new Vector2(0.5f, 1f));

            var choices = new List<MainMenuLevelChoice>();
            IReadOnlyList<LevelConfig> levels = config.GetLevels();
            float buttonHeight = 104f;
            float spacing = 28f;
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            for (int index = 0; index < levels.Count; index++)
            {
                LevelConfig level = levels[index];
                bool unlocked = progress.IsLevelUnlocked(level.LevelId);
                Button button = CreateButton(
                    $"Level {level.LevelId}",
                    selection,
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -92f - (index * (buttonHeight + spacing))),
                    new Vector2(620f, buttonHeight),
                    unlocked ? UnlockedColor : LockedColor,
                    Localize(config.GetText(level.DisplayNameKey)));
                button.interactable = unlocked;
                choices.Add(new MainMenuLevelChoice(level.LevelId, button, unlocked));
            }

            selection.gameObject.SetActive(false);
            MainMenuView view = root.GetComponent<MainMenuView>();
            view.Initialize(start, selection.gameObject, choices);
            return view;
        }

        // 处理 ReadReferenceResolution 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static Vector2 ReadReferenceResolution(IConfigProvider config)
        {
            GlobalConfig width = config.GetGlobal(ConfigIds.GlobalKeys.ReferenceWidth);
            GlobalConfig height = config.GetGlobal(ConfigIds.GlobalKeys.ReferenceHeight);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!width.IntValue.HasValue || !height.IntValue.HasValue ||
                width.IntValue.Value <= 0L || height.IntValue.Value <= 0L)
            {
                throw new InvalidOperationException(
                    "Reference resolution must be positive integer configuration.");
            }

            return new Vector2(width.IntValue.Value, height.IntValue.Value);
        }

        // 处理 Localize 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static string Localize(TextConfig text)
        {
            return text.ZhCN;
        }

        // 创建 CreateButton 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Color color,
            string labelText)
        {
            RectTransform rect = CreateRect(name, parent);
            SetAnchored(rect, anchor, anchor, position, size, new Vector2(0.5f, 0.5f));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            TMP_Text label = CreateText("Label", rect, 34f, TextAlignmentOptions.Center);
            label.text = labelText;
            Stretch(label.rectTransform);
            return button;
        }

        // 创建 CreatePanel 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        // 创建 CreateText 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static TMP_Text CreateText(
            string name,
            Transform parent,
            float size,
            TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(
                BattleHudViewFactory.HudFontResourcePath);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (font == null)
            {
                throw new InvalidOperationException(
                    $"Configured HUD font is missing: {BattleHudViewFactory.HudFontResourcePath}");
            }

            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        // 创建 CreateRect 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        // 设置 SetAnchored 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
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

        // 处理 Stretch 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // 处理 EnsureEventSystem 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static void EnsureEventSystem(Transform parent)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject(
                "MainMenu EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(parent, false);
        }
    }
}
