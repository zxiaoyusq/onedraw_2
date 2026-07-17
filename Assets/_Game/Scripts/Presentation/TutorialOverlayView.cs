using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneStrokeDemon.Presentation
{
    // 定义 TutorialHighlightRegistry 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class TutorialHighlightRegistry
    {
        private readonly Dictionary<string, RectTransform> targets =
            new Dictionary<string, RectTransform>(StringComparer.Ordinal);
        private readonly RectTransform fallback;

        // 初始化 TutorialHighlightRegistry，并建立表现层所需的引用与初始显示状态。
        public TutorialHighlightRegistry(RectTransform fallbackTarget)
        {
            fallback = fallbackTarget ??
                throw new ArgumentNullException(nameof(fallbackTarget));
        }

        public RectTransform Fallback => fallback;

        // 处理 Register 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Register(string targetId, RectTransform target)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(targetId) ||
                !string.Equals(targetId, targetId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Tutorial highlight target id must be non-empty and trimmed.",
                    nameof(targetId));
            }

            targets[targetId] = target ?? throw new ArgumentNullException(nameof(target));
        }

        // 处理 Unregister 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool Unregister(string targetId)
        {
            return targetId != null && targets.Remove(targetId);
        }

        // 处理 Resolve 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public RectTransform Resolve(string targetId)
        {
            return targetId != null && targets.TryGetValue(targetId, out RectTransform target) &&
                   target != null
                ? target
                : fallback;
        }

        // 处理 ForBattleHud 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public static TutorialHighlightRegistry ForBattleHud(BattleHudView hud)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (hud == null)
            {
                throw new ArgumentNullException(nameof(hud));
            }

            var registry = new TutorialHighlightRegistry(hud.SafeAreaRoot);
            registry.Register("BattleArea", hud.SafeAreaRoot);
            registry.Register("SwitchButton", hud.StanceTarget);
            registry.Register(
                "UltimateButton",
                hud.UltimateButton.GetComponent<RectTransform>());
            return registry;
        }
    }

    [DisallowMultipleComponent]
    // 定义 TutorialOverlayView 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class TutorialOverlayView : MonoBehaviour, ITutorialOverlayView
    {
        private readonly Vector3[] targetWorldCorners = new Vector3[4];
        private TutorialOverlayViewReferences references;
        private TutorialHighlightRegistry registry;
        private RectTransform currentTarget;

        public event Action SkipRequested;

        public event Action ReviewRequested;

        public TutorialOverlayState LastRendered { get; private set; }

        public int RenderCount { get; private set; }

        public Button SkipButton => RequireReferences().SkipButton;

        public Button ReviewButton => RequireReferences().ReviewButton;

        public TMP_Text PromptText => RequireReferences().PromptText;

        public TutorialGestureGraphic GestureGraphic => RequireReferences().GestureGraphic;

        public RectTransform ResolvedHighlightTarget => currentTarget;

        public bool OverlayVisible => RequireReferences().OverlayLayer.activeSelf;

        // 处理 Initialize 对应的表现逻辑，使视图与只读战斗状态保持同步。
        internal void Initialize(
            TutorialOverlayViewReferences configuredReferences,
            TutorialHighlightRegistry highlightRegistry)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (references != null)
            {
                throw new InvalidOperationException(
                    "Tutorial overlay view is already initialized.");
            }

            references = configuredReferences ??
                throw new ArgumentNullException(nameof(configuredReferences));
            references.Validate();
            registry = highlightRegistry ??
                throw new ArgumentNullException(nameof(highlightRegistry));
            references.SkipButton.onClick.AddListener(HandleSkip);
            references.ReviewButton.onClick.AddListener(HandleReview);
        }

        // 渲染 Render 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Render(TutorialOverlayState state)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            TutorialOverlayViewReferences ui = RequireReferences();
            LastRendered = state;
            RenderCount += 1;
            ui.PromptText.text = state.Prompt;
            ui.SkipText.text = state.SkipText;
            ui.ReviewText.text = state.ReviewText;
            ui.SkipButton.gameObject.SetActive(state.SkipVisible);
            ui.ReviewButton.gameObject.SetActive(state.ReviewVisible);
            ui.OverlayLayer.SetActive(state.OverlayVisible);
            ui.GestureGraphic.SetGesture(state.GestureType);
            currentTarget = state.OverlayVisible
                ? registry.Resolve(state.HighlightTarget)
                : null;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (state.OverlayVisible)
            {
                Canvas.ForceUpdateCanvases();
                UpdateHighlightLayout();
            }
        }

        // 处理 LateUpdate 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void LateUpdate()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (references != null && references.OverlayLayer.activeSelf)
            {
                UpdateHighlightLayout();
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

            references.SkipButton.onClick.RemoveListener(HandleSkip);
            references.ReviewButton.onClick.RemoveListener(HandleReview);
        }

        // 更新 UpdateHighlightLayout 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void UpdateHighlightLayout()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (currentTarget == null)
            {
                return;
            }

            TutorialOverlayViewReferences ui = RequireReferences();
            Rect rootRect = ui.OverlayRect.rect;
            currentTarget.GetWorldCorners(targetWorldCorners);
            Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < targetWorldCorners.Length; index += 1)
            {
                Vector3 local = ui.OverlayRect.InverseTransformPoint(targetWorldCorners[index]);
                minimum = Vector2.Min(minimum, local);
                maximum = Vector2.Max(maximum, local);
            }

            const float padding = 24f;
            minimum -= Vector2.one * padding;
            maximum += Vector2.one * padding;
            minimum.x = Mathf.Clamp(minimum.x, rootRect.xMin, rootRect.xMax);
            minimum.y = Mathf.Clamp(minimum.y, rootRect.yMin, rootRect.yMax);
            maximum.x = Mathf.Clamp(maximum.x, minimum.x, rootRect.xMax);
            maximum.y = Mathf.Clamp(maximum.y, minimum.y, rootRect.yMax);

            SetLocalBounds(ui.MaskTop, rootRect.xMin, maximum.y, rootRect.xMax, rootRect.yMax);
            SetLocalBounds(ui.MaskBottom, rootRect.xMin, rootRect.yMin, rootRect.xMax, minimum.y);
            SetLocalBounds(ui.MaskLeft, rootRect.xMin, minimum.y, minimum.x, maximum.y);
            SetLocalBounds(ui.MaskRight, maximum.x, minimum.y, rootRect.xMax, maximum.y);
            SetLocalBounds(ui.HighlightFrame, minimum.x, minimum.y, maximum.x, maximum.y);
        }

        // 设置 SetLocalBounds 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void SetLocalBounds(
            RectTransform target,
            float xMin,
            float yMin,
            float xMax,
            float yMax)
        {
            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = new Vector2(
                (xMin + xMax) * 0.5f,
                (yMin + yMax) * 0.5f);
            target.sizeDelta = new Vector2(
                Mathf.Max(0f, xMax - xMin),
                Mathf.Max(0f, yMax - yMin));
        }

        // 处理 HandleSkip 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void HandleSkip() => SkipRequested?.Invoke();

        // 处理 HandleReview 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void HandleReview() => ReviewRequested?.Invoke();

        // 处理 RequireReferences 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private TutorialOverlayViewReferences RequireReferences()
        {
            return references ??
                throw new InvalidOperationException(
                    "Tutorial overlay view is not initialized.");
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    // 定义 TutorialGestureGraphic 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class TutorialGestureGraphic : Graphic
    {
        private TutorialGestureType gestureType;

        public TutorialGestureType GestureType => gestureType;

        // 设置 SetGesture 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void SetGesture(TutorialGestureType value)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (gestureType == value)
            {
                return;
            }

            gestureType = value;
            SetVerticesDirty();
        }

        // 响应 OnPopulateMesh 对应的表现逻辑，使视图与只读战斗状态保持同步。
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (gestureType == TutorialGestureType.None)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            float scale = Mathf.Min(rect.width, rect.height) * 0.42f;
            float thickness = Mathf.Clamp(scale * 0.09f, 6f, 18f);
            List<Vector2> points = CreatePoints(gestureType, scale);
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 1; index < points.Count; index += 1)
            {
                AddSegment(vertexHelper, points[index - 1], points[index], thickness);
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (points.Count >= 2)
            {
                Vector2 end = points[points.Count - 1];
                Vector2 direction = (end - points[points.Count - 2]).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                float arrowLength = thickness * 2.2f;
                AddSegment(
                    vertexHelper,
                    end,
                    end - direction * arrowLength + normal * arrowLength * 0.55f,
                    thickness * 0.7f);
                AddSegment(
                    vertexHelper,
                    end,
                    end - direction * arrowLength - normal * arrowLength * 0.55f,
                    thickness * 0.7f);
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (gestureType == TutorialGestureType.Charged)
            {
                AddDisc(vertexHelper, points[0], thickness * 1.35f, 12);
            }
        }

        // 创建 CreatePoints 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static List<Vector2> CreatePoints(
            TutorialGestureType type,
            float scale)
        {
            var points = new List<Vector2>(25);
            // 按当前表现类型或流程状态选择对应的渲染分支。
            switch (type)
            {
                case TutorialGestureType.Vertical:
                    points.Add(new Vector2(0f, -0.82f) * scale);
                    points.Add(new Vector2(0f, 0.82f) * scale);
                    break;
                case TutorialGestureType.Diagonal:
                    points.Add(new Vector2(-0.72f, -0.72f) * scale);
                    points.Add(new Vector2(0.72f, 0.72f) * scale);
                    break;
                case TutorialGestureType.Arc:
                    // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
                    for (int index = 0; index <= 14; index += 1)
                    {
                        float angle = Mathf.Lerp(210f, -30f, index / 14f) * Mathf.Deg2Rad;
                        points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * scale);
                    }
                    break;
                case TutorialGestureType.Circle:
                    // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
                    for (int index = 0; index <= 24; index += 1)
                    {
                        float angle = Mathf.Lerp(90f, 450f, index / 24f) * Mathf.Deg2Rad;
                        points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * scale);
                    }
                    break;
                case TutorialGestureType.Any:
                case TutorialGestureType.Horizontal:
                case TutorialGestureType.Charged:
                default:
                    points.Add(new Vector2(-0.82f, 0f) * scale);
                    points.Add(new Vector2(0.82f, 0f) * scale);
                    break;
            }

            return points;
        }

        // 添加 AddSegment 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void AddSegment(
            VertexHelper vertexHelper,
            Vector2 start,
            Vector2 end,
            float thickness)
        {
            Vector2 direction = end - start;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 normal = new Vector2(-direction.y, direction.x).normalized *
                             (thickness * 0.5f);
            int first = vertexHelper.currentVertCount;
            vertexHelper.AddVert(start - normal, color, Vector2.zero);
            vertexHelper.AddVert(start + normal, color, Vector2.zero);
            vertexHelper.AddVert(end + normal, color, Vector2.zero);
            vertexHelper.AddVert(end - normal, color, Vector2.zero);
            vertexHelper.AddTriangle(first, first + 1, first + 2);
            vertexHelper.AddTriangle(first, first + 2, first + 3);
        }

        // 添加 AddDisc 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void AddDisc(
            VertexHelper vertexHelper,
            Vector2 center,
            float radius,
            int segments)
        {
            int first = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, color, Vector2.zero);
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index <= segments; index += 1)
            {
                float angle = index * Mathf.PI * 2f / segments;
                vertexHelper.AddVert(
                    center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                    color,
                    Vector2.zero);
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (index > 0)
                {
                    vertexHelper.AddTriangle(first, first + index, first + index + 1);
                }
            }
        }
    }

    // 定义 TutorialOverlayViewReferences 的表现层契约，隔离战斗状态与具体Unity视图实现。
    internal sealed class TutorialOverlayViewReferences
    {
        public RectTransform OverlayRect;
        public GameObject OverlayLayer;
        public RectTransform MaskTop;
        public RectTransform MaskBottom;
        public RectTransform MaskLeft;
        public RectTransform MaskRight;
        public RectTransform HighlightFrame;
        public TutorialGestureGraphic GestureGraphic;
        public TMP_Text PromptText;
        public Button SkipButton;
        public TMP_Text SkipText;
        public Button ReviewButton;
        public TMP_Text ReviewText;

        // 校验 Validate 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Validate()
        {
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            foreach (UnityEngine.Object item in new UnityEngine.Object[]
                     {
                         OverlayRect, OverlayLayer, MaskTop, MaskBottom,
                         MaskLeft, MaskRight, HighlightFrame, GestureGraphic,
                         PromptText, SkipButton, SkipText, ReviewButton, ReviewText,
                     })
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (item == null)
                {
                    throw new ArgumentException(
                        "Tutorial overlay view references must be complete.");
                }
            }
        }
    }
}
