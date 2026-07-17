using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneStrokeDemon.Presentation
{
    // 定义 TutorialOverlayViewFactory 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public static class TutorialOverlayViewFactory
    {
        private static readonly Color MaskColor = new Color32(4, 9, 18, 205);
        private static readonly Color CardColor = new Color32(15, 28, 48, 242);
        private static readonly Color AccentColor = new Color32(74, 210, 226, 255);
        private static readonly Color ReviewColor = new Color32(76, 91, 111, 245);
        private static readonly Color TextColor = new Color32(245, 246, 248, 255);

        // 创建 Create 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public static TutorialOverlayView Create(
            IConfigProvider configProvider,
            BattleHudView hud,
            TutorialHighlightRegistry registry)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (hud == null)
            {
                throw new ArgumentNullException(nameof(hud));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            RectTransform root = CreateRect(
                "TutorialOverlay",
                hud.SafeAreaRoot,
                typeof(TutorialOverlayView));
            Stretch(root);
            root.SetAsLastSibling();

            RectTransform overlay = CreateRect("OverlayLayer", root);
            Stretch(overlay);
            var references = new TutorialOverlayViewReferences
            {
                OverlayRect = overlay,
                OverlayLayer = overlay.gameObject,
                MaskTop = CreateImage("MaskTop", overlay, MaskColor),
                MaskBottom = CreateImage("MaskBottom", overlay, MaskColor),
                MaskLeft = CreateImage("MaskLeft", overlay, MaskColor),
                MaskRight = CreateImage("MaskRight", overlay, MaskColor),
            };

            references.HighlightFrame = CreateRect("HighlightFrame", overlay);
            CreateFrameBorder(
                "TopBorder",
                references.HighlightFrame,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 6f),
                new Vector2(0f, -3f));
            CreateFrameBorder(
                "BottomBorder",
                references.HighlightFrame,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 6f),
                new Vector2(0f, 3f));
            CreateFrameBorder(
                "LeftBorder",
                references.HighlightFrame,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(6f, 0f),
                new Vector2(3f, 0f));
            CreateFrameBorder(
                "RightBorder",
                references.HighlightFrame,
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(6f, 0f),
                new Vector2(-3f, 0f));

            RectTransform gestureRect = CreateRect(
                "Gesture",
                references.HighlightFrame,
                typeof(TutorialGestureGraphic));
            StretchWithInset(gestureRect, 16f);
            references.GestureGraphic = gestureRect.GetComponent<TutorialGestureGraphic>();
            references.GestureGraphic.color = AccentColor;
            references.GestureGraphic.raycastTarget = false;

            RectTransform card = CreateImage("PromptCard", overlay, CardColor);
            SetAnchored(
                card,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 150f),
                new Vector2(980f, 150f),
                new Vector2(0.5f, 0f));
            references.PromptText = CreateText(
                "Prompt",
                card,
                34f,
                TextAlignmentOptions.Center);
            SetAnchored(
                references.PromptText.rectTransform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(-48f, -28f),
                new Vector2(0.5f, 0.5f));

            references.SkipButton = CreateButton(
                "SkipButton",
                overlay,
                new Vector2(1f, 1f),
                new Vector2(-32f, -126f),
                new Vector2(250f, 72f),
                new Vector2(1f, 1f),
                AccentColor,
                out references.SkipText);
            references.ReviewButton = CreateButton(
                "ReviewButton",
                root,
                new Vector2(0f, 1f),
                new Vector2(32f, -238f),
                new Vector2(220f, 68f),
                new Vector2(0f, 1f),
                ReviewColor,
                out references.ReviewText);

            TutorialOverlayView view = root.GetComponent<TutorialOverlayView>();
            view.Initialize(references, registry);
            references.OverlayLayer.SetActive(false);
            references.SkipButton.gameObject.SetActive(false);
            references.ReviewButton.gameObject.SetActive(false);
            return view;
        }

        // 创建 CreateImage 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static RectTransform CreateImage(
            string name,
            Transform parent,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        // 创建 CreateFrameBorder 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void CreateFrameBorder(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2 position)
        {
            RectTransform border = CreateImage(name, parent, AccentColor);
            border.anchorMin = anchorMin;
            border.anchorMax = anchorMax;
            border.pivot = new Vector2(0.5f, 0.5f);
            border.anchoredPosition = position;
            border.sizeDelta = size;
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

        // 创建 CreateText 对应的表现逻辑，使视图与只读战斗状态保持同步。
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
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (font == null)
            {
                throw new InvalidOperationException(
                    "Configured tutorial font is missing from Resources: " +
                    BattleHudViewFactory.HudFontResourcePath);
            }

            text.font = font;
            text.text = string.Empty;
            text.fontSize = size;
            text.color = TextColor;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        // 创建 CreateRect 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static RectTransform CreateRect(
            string name,
            Transform parent,
            params Type[] additionalComponents)
        {
            var components = new Type[additionalComponents.Length + 1];
            components[0] = typeof(RectTransform);
            Array.Copy(
                additionalComponents,
                0,
                components,
                1,
                additionalComponents.Length);
            var gameObject = new GameObject(name, components);
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

        // 处理 StretchWithInset 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void StretchWithInset(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = -Vector2.one * inset;
        }
    }

    // 定义 TutorialOverlayRuntime 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class TutorialOverlayRuntime : IDisposable
    {
        private bool disposed;

        // 初始化 TutorialOverlayRuntime，并建立表现层所需的引用与初始显示状态。
        private TutorialOverlayRuntime(
            TutorialOverlayView view,
            TutorialDirector director,
            TutorialHighlightRegistry registry)
        {
            View = view;
            Director = director;
            HighlightRegistry = registry;
        }

        public TutorialOverlayView View { get; private set; }

        public TutorialDirector Director { get; private set; }

        public TutorialHighlightRegistry HighlightRegistry { get; }

        // 创建 Create 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public static TutorialOverlayRuntime Create(
            IConfigProvider configProvider,
            TutorialLevelCoordinator coordinator,
            ITutorialCompletionProgress progress,
            BattleHudView hud,
            BattleHudLanguage language = BattleHudLanguage.ZhCN,
            TutorialHighlightRegistry highlightRegistry = null)
        {
            TutorialHighlightRegistry registry = highlightRegistry ??
                TutorialHighlightRegistry.ForBattleHud(hud);
            TutorialOverlayView view = TutorialOverlayViewFactory.Create(
                configProvider,
                hud,
                registry);
            try
            {
                var director = new TutorialDirector(
                    configProvider,
                    coordinator,
                    progress,
                    view,
                    language);
                return new TutorialOverlayRuntime(view, director, registry);
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
            Director?.Dispose();
            Director = null;
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
