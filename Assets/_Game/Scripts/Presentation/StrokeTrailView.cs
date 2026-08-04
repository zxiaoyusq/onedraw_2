using System;
using System.Collections.Generic;
using OneStrokeDemon.Combat;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
    /// <summary>显示共享命中路径的外辉光、主体、核心与确定性闪电分支。</summary>
    public sealed class StrokeTrailView : MonoBehaviour
    {
        /// <summary>单个预热视图内固定的技术性分支渲染器容量。</summary>
        public const int BranchRendererCapacity = 12;

        private const int MaximumBranchPointCount = 9;

        private static readonly Color TransparentWhite = new Color(1f, 1f, 1f, 0f);

        [SerializeField] private LineRenderer outerLineRenderer;
        [SerializeField] private LineRenderer bodyLineRenderer;
        [SerializeField] private LineRenderer coreLineRenderer;
        [SerializeField] private LineRenderer[] branchLineRenderers = Array.Empty<LineRenderer>();

        private Material sharedTrailMaterial;
        private Transform referenceSpace;
        private IReadOnlyList<Vector2> sourcePoints;
        private Vector2[][] branchPointBuffers;
        private float elapsedSeconds;
        private float lifetimeSeconds;
        private Color outerColor;
        private Color bodyColor;
        private Color coreColor;
        private Color branchColor;
        private StrokeTrailStyle currentStyle;
        private string[] chargeVfxAssetKeys = Array.Empty<string>();
        private StrokeChargeVfxView[] chargeVfxViews = Array.Empty<StrokeChargeVfxView>();
        private StrokeChargeVfxView activeChargeVfx;

        public bool IsInitialized { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsPreviewing { get; private set; }

        /// <summary>获取当前是否正在触点位置显示蓄力进度环。</summary>
        public bool IsChargePreviewVisible { get; private set; }

        /// <summary>获取当前蓄力环的0到1归一化进度。</summary>
        public float ChargePreviewProgress { get; private set; }

        /// <summary>获取蓄力环使用的配置参考像素半径。</summary>
        public float ChargePreviewRadiusReferencePixels { get; private set; }

        public ulong StrokeId { get; private set; }

        public ulong ActivationSequence { get; private set; }

        public string StanceId { get; private set; }

        public string StyleId { get; private set; }

        public IReadOnlyList<Vector2> SourcePoints => sourcePoints;

        /// <summary>保留原有外层LineRenderer访问口，既有调用方无需改写。</summary>
        public LineRenderer LineRenderer => outerLineRenderer;

        public LineRenderer BodyLineRenderer => bodyLineRenderer;

        public LineRenderer CoreLineRenderer => coreLineRenderer;

        public IReadOnlyList<LineRenderer> BranchLineRenderers => branchLineRenderers;

        public StrokeChargeVfxView ActiveChargeVfx => activeChargeVfx;

        public int ActiveBranchCount { get; private set; }

        public float ReferencePixelWorldScale { get; private set; }

        public float NormalizedLifetime =>
            IsActive && lifetimeSeconds > 0f ? Mathf.Clamp01(elapsedSeconds / lifetimeSeconds) : 0f;

        /// <summary>
        /// 供Unity作者工具或明确测试兜底绑定拓扑引用；此方法不保存任何视觉数值。
        /// </summary>
        public void ConfigureRenderersForAuthoring(
            LineRenderer outer,
            LineRenderer body,
            LineRenderer core,
            LineRenderer[] branches)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException(
                    "Renderer references cannot change after initialization.");
            }

            outerLineRenderer = outer != null
                ? outer
                : throw new ArgumentNullException(nameof(outer));
            bodyLineRenderer = body != null
                ? body
                : throw new ArgumentNullException(nameof(body));
            coreLineRenderer = core != null
                ? core
                : throw new ArgumentNullException(nameof(core));
            if (branches == null || branches.Length != BranchRendererCapacity)
            {
                throw new ArgumentException(
                    $"Exactly {BranchRendererCapacity} branch renderers are required.",
                    nameof(branches));
            }

            branchLineRenderers = (LineRenderer[])branches.Clone();
            ValidateRendererTopology();
        }

        /// <summary>用共享材质和参考空间初始化已由Prefab绑定的分层渲染拓扑。</summary>
        public void Initialize(
            Material sharedMaterial,
            Transform configuredReferenceSpace = null,
            IReadOnlyList<StrokeChargeVfxPrefab> chargeVfxPrefabs = null)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Stroke trail view is already initialized.");
            }

            sharedTrailMaterial = sharedMaterial != null
                ? sharedMaterial
                : throw new ArgumentNullException(nameof(sharedMaterial));
            referenceSpace = configuredReferenceSpace != null
                ? configuredReferenceSpace
                : transform;
            ValidateRendererTopology();
            branchPointBuffers = new Vector2[BranchRendererCapacity][];
            for (int index = 0; index < branchPointBuffers.Length; index++)
            {
                branchPointBuffers[index] = new Vector2[MaximumBranchPointCount];
            }

            ConfigureTechnicalRenderer(outerLineRenderer, capVertices: 4, cornerVertices: 2);
            ConfigureTechnicalRenderer(bodyLineRenderer, capVertices: 4, cornerVertices: 2);
            ConfigureTechnicalRenderer(coreLineRenderer, capVertices: 4, cornerVertices: 2);
            for (int index = 0; index < branchLineRenderers.Length; index++)
            {
                ConfigureTechnicalRenderer(
                    branchLineRenderers[index],
                    capVertices: 2,
                    cornerVertices: 1);
            }

            InitializeChargeVfx(chargeVfxPrefabs);

            IsInitialized = true;
            ResetForPool();
        }

        /// <summary>一次性显示完成笔迹，并立即生成方案C的稀疏电弧。</summary>
        public void Show(
            StrokeTrailPath path,
            StrokeTrailStyle style,
            ulong activationSequence)
        {
            EnsureInitialized();
            if (path.PointCount < 2)
            {
                throw new ArgumentException("A visible trail needs at least two points.", nameof(path));
            }

            if (activationSequence == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activationSequence),
                    "Activation sequence must be positive.");
            }

            ResetForPool();
            StrokeId = path.StrokeId;
            ActivationSequence = activationSequence;
            StanceId = style.StanceId;
            StyleId = style.StyleId;
            sourcePoints = path.Points;
            lifetimeSeconds = style.LifetimeSeconds;

            ConfigureStyle(style);
            SetMainPath(path.Points);
            RenderBranches(path.Points, style);
            IsActive = true;
            IsPreviewing = false;
            SetMainEnabled(true);
        }

        /// <summary>开始实时主轨迹预览；分支在完成并取得最终几何后统一生成。</summary>
        public void BeginPreview(
            ulong strokeId,
            Vector2 firstPoint,
            StrokeTrailStyle style,
            ulong activationSequence)
        {
            EnsureInitialized();
            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId));
            }

            if (activationSequence == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(activationSequence));
            }

            ResetForPool();
            StrokeId = strokeId;
            ActivationSequence = activationSequence;
            StanceId = style.StanceId;
            StyleId = style.StyleId;
            lifetimeSeconds = style.LifetimeSeconds;
            ConfigureStyle(style);
            activeChargeVfx = FindChargeVfx(style.ChargeVfxAssetKey);
            SetMainPositionCount(1);
            SetMainPosition(0, ReferenceToWorld(firstPoint));
            IsActive = true;
            IsPreviewing = true;
            SetMainEnabled(false);
        }

        /// <summary>把新增采样点同步写入三个主轨迹层，不产生临时集合。</summary>
        public void AppendPreviewPoint(Vector2 point)
        {
            EnsureInitialized();
            if (!IsActive || !IsPreviewing)
            {
                throw new InvalidOperationException(
                    "Only an active preview can append a stroke point.");
            }

            HideChargePreview();
            int index = outerLineRenderer.positionCount;
            SetMainPositionCount(index + 1);
            SetMainPosition(index, ReferenceToWorld(point));
            SetMainEnabled(true);
        }

        /// <summary>在首个有效移动点出现前，把进度转发给配置选择的独立蓄力Prefab。</summary>
        public void UpdateChargePreview(
            Vector2 referencePosition,
            float normalizedProgress,
            float radiusReferencePixels)
        {
            EnsureInitialized();
            if (!IsActive || !IsPreviewing)
            {
                throw new InvalidOperationException(
                    "Only an active stroke preview can display charge progress.");
            }

            if (float.IsNaN(normalizedProgress) || float.IsInfinity(normalizedProgress))
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedProgress));
            }

            if (float.IsNaN(radiusReferencePixels) ||
                float.IsInfinity(radiusReferencePixels) ||
                radiusReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radiusReferencePixels));
            }

            if (activeChargeVfx == null)
            {
                throw new InvalidOperationException(
                    "The configured stroke style has no preloaded charge VFX prefab.");
            }

            ChargePreviewProgress = Mathf.Clamp01(normalizedProgress);
            ChargePreviewRadiusReferencePixels = radiusReferencePixels;
            activeChargeVfx.Show(
                referencePosition,
                ChargePreviewProgress,
                radiusReferencePixels,
                currentStyle,
                ReferencePixelWorldScale);
            IsChargePreviewVisible = true;
        }

        /// <summary>隐藏蓄力环并保留活动笔迹的主轨迹预览。</summary>
        public void HideChargePreview()
        {
            if (!IsInitialized || !IsChargePreviewVisible)
            {
                return;
            }

            activeChargeVfx?.Hide();

            IsChargePreviewVisible = false;
            ChargePreviewProgress = 0f;
            ChargePreviewRadiusReferencePixels = 0f;
        }

        /// <summary>使用非缩放时间同步淡出全部主层和分支，并在到期时回池。</summary>
        public bool Advance(float unscaledDeltaSeconds)
        {
            EnsureInitialized();
            if (!IsActive)
            {
                return false;
            }

            if (float.IsNaN(unscaledDeltaSeconds) ||
                float.IsInfinity(unscaledDeltaSeconds) ||
                unscaledDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unscaledDeltaSeconds),
                    "Delta time must be finite and non-negative.");
            }

            if (IsPreviewing)
            {
                return false;
            }

            elapsedSeconds += unscaledDeltaSeconds;
            if (elapsedSeconds >= lifetimeSeconds)
            {
                ResetForPool();
                return true;
            }

            ApplyAlpha(1f - (elapsedSeconds / lifetimeSeconds));
            return false;
        }

        /// <summary>清空所有LineRenderer状态，保证下一次池化复用不残留颜色或分支。</summary>
        public void ResetForPool()
        {
            EnsureInitialized();
            activeChargeVfx?.Hide();
            activeChargeVfx = null;
            IsActive = false;
            IsPreviewing = false;
            IsChargePreviewVisible = false;
            ChargePreviewProgress = 0f;
            ChargePreviewRadiusReferencePixels = 0f;
            StrokeId = 0;
            ActivationSequence = 0;
            StanceId = null;
            StyleId = null;
            sourcePoints = null;
            elapsedSeconds = 0f;
            lifetimeSeconds = 0f;
            ReferencePixelWorldScale = 0f;
            ActiveBranchCount = 0;
            outerColor = TransparentWhite;
            bodyColor = TransparentWhite;
            coreColor = TransparentWhite;
            branchColor = TransparentWhite;
            currentStyle = default;

            ResetRenderer(outerLineRenderer);
            ResetRenderer(bodyLineRenderer);
            ResetRenderer(coreLineRenderer);
            for (int index = 0; index < branchLineRenderers.Length; index++)
            {
                ResetRenderer(branchLineRenderers[index]);
            }
        }

        // 将配置样式投射到世界空间，并建立稳定的层级排序。
        private void ConfigureStyle(StrokeTrailStyle style)
        {
            currentStyle = style;
            Transform ownTransform = transform;
            ownTransform.localPosition = Vector3.zero;
            ownTransform.localRotation = Quaternion.identity;
            ownTransform.localScale = Vector3.one;
            ReferencePixelWorldScale = Mathf.Max(
                Mathf.Abs(referenceSpace.lossyScale.x),
                Mathf.Abs(referenceSpace.lossyScale.y));
            float worldBaseWidth = style.WidthReferencePixels * ReferencePixelWorldScale;

            outerColor = style.OuterColor;
            bodyColor = style.BodyColor;
            coreColor = style.CoreColor;
            branchColor = style.BranchColor;
            float worldBranchBaseWidth =
                worldBaseWidth * style.BranchWidthMultiplier;
            ApplyRendererStyle(
                outerLineRenderer,
                worldBaseWidth * style.OuterWidthMultiplier,
                outerColor,
                style.SortingLayerId,
                style.SortingOrder);
            ApplyRendererStyle(
                bodyLineRenderer,
                worldBaseWidth * style.BodyWidthMultiplier,
                bodyColor,
                style.SortingLayerId,
                style.SortingOrder + 1);
            ApplyRendererStyle(
                coreLineRenderer,
                worldBaseWidth * style.CoreWidthMultiplier,
                coreColor,
                style.SortingLayerId,
                style.SortingOrder + 2);
            for (int index = 0; index < branchLineRenderers.Length; index++)
            {
                ApplyRendererStyle(
                    branchLineRenderers[index],
                    worldBranchBaseWidth,
                    branchColor,
                    style.SortingLayerId,
                    style.SortingOrder + 3);
            }
        }

        // 三个主层严格共享同一组完成路径点。
        private void SetMainPath(IReadOnlyList<Vector2> path)
        {
            SetMainPositionCount(path.Count);
            for (int index = 0; index < path.Count; index++)
            {
                SetMainPosition(index, ReferenceToWorld(path[index]));
            }
        }

        // 分支仅从共享完成路径派生，不回写Combat几何或命中真相。
        private void RenderBranches(
            IReadOnlyList<Vector2> path,
            StrokeTrailStyle style)
        {
            int branchCount = LightningBranchLayout.CountBranches(
                path,
                style.BranchSpacingReferencePixels,
                branchLineRenderers.Length);
            ActiveBranchCount = branchCount;
            for (int branchIndex = 0; branchIndex < branchLineRenderers.Length; branchIndex++)
            {
                LineRenderer renderer = branchLineRenderers[branchIndex];
                if (branchIndex >= branchCount ||
                    !LightningBranchLayout.TryWriteBranch(
                        StrokeId,
                        branchIndex,
                        path,
                        style.BranchSpacingReferencePixels,
                        style.BranchLengthReferencePixels,
                        style.BranchJitterReferencePixels,
                        style.BranchSegmentCount,
                        branchPointBuffers[branchIndex]))
                {
                    renderer.enabled = false;
                    renderer.positionCount = 0;
                    continue;
                }

                renderer.positionCount = style.BranchSegmentCount + 1;
                for (int pointIndex = 0; pointIndex <= style.BranchSegmentCount; pointIndex++)
                {
                    renderer.SetPosition(
                        pointIndex,
                        ReferenceToWorld(branchPointBuffers[branchIndex][pointIndex]));
                }

                renderer.enabled = true;
            }
        }

        private void SetMainPositionCount(int count)
        {
            outerLineRenderer.positionCount = count;
            bodyLineRenderer.positionCount = count;
            coreLineRenderer.positionCount = count;
        }

        private void SetMainPosition(int index, Vector3 point)
        {
            outerLineRenderer.SetPosition(index, point);
            bodyLineRenderer.SetPosition(index, point);
            coreLineRenderer.SetPosition(index, point);
        }

        private void SetMainEnabled(bool enabled)
        {
            outerLineRenderer.enabled = enabled;
            bodyLineRenderer.enabled = enabled;
            coreLineRenderer.enabled = enabled;
        }

        private void ApplyAlpha(float normalizedAlpha)
        {
            SetRendererColor(outerLineRenderer, outerColor, normalizedAlpha);
            SetRendererColor(bodyLineRenderer, bodyColor, normalizedAlpha);
            SetRendererColor(coreLineRenderer, coreColor, normalizedAlpha);
            for (int index = 0; index < ActiveBranchCount; index++)
            {
                SetRendererColor(branchLineRenderers[index], branchColor, normalizedAlpha);
            }
        }

        private static void SetRendererColor(
            LineRenderer renderer,
            Color baseColor,
            float normalizedAlpha)
        {
            baseColor.a *= normalizedAlpha;
            renderer.startColor = baseColor;
            renderer.endColor = baseColor;
        }

        private void ApplyRendererStyle(
            LineRenderer renderer,
            float width,
            Color color,
            int sortingLayerId,
            int sortingOrder)
        {
            renderer.sharedMaterial = sharedTrailMaterial;
            renderer.startWidth = width;
            renderer.endWidth = width;
            renderer.startColor = color;
            renderer.endColor = color;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }

        private void ConfigureTechnicalRenderer(
            LineRenderer renderer,
            int capVertices,
            int cornerVertices)
        {
            renderer.sharedMaterial = sharedTrailMaterial;
            renderer.useWorldSpace = true;
            renderer.loop = false;
            renderer.alignment = LineAlignment.View;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.numCapVertices = capVertices;
            renderer.numCornerVertices = cornerVertices;
            renderer.generateLightingData = false;
        }

        private static void ResetRenderer(LineRenderer renderer)
        {
            renderer.enabled = false;
            renderer.positionCount = 0;
            renderer.startWidth = 0f;
            renderer.endWidth = 0f;
            renderer.startColor = TransparentWhite;
            renderer.endColor = TransparentWhite;
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = 0;
            renderer.useWorldSpace = true;
        }

        private Vector3 ReferenceToWorld(Vector2 point)
        {
            return referenceSpace.TransformPoint(new Vector3(point.x, point.y, 0f));
        }

        // 为每个轨迹池实例预建全部已配置蓄力Prefab，避免输入热点路径中临时实例化。
        private void InitializeChargeVfx(IReadOnlyList<StrokeChargeVfxPrefab> prefabs)
        {
            if (prefabs == null || prefabs.Count == 0)
            {
                chargeVfxAssetKeys = Array.Empty<string>();
                chargeVfxViews = Array.Empty<StrokeChargeVfxView>();
                return;
            }

            chargeVfxAssetKeys = new string[prefabs.Count];
            chargeVfxViews = new StrokeChargeVfxView[prefabs.Count];
            for (int index = 0; index < prefabs.Count; index++)
            {
                StrokeChargeVfxPrefab definition = prefabs[index];
                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(
                        chargeVfxAssetKeys[previous],
                        definition.AssetKey,
                        StringComparison.Ordinal))
                    {
                        throw new ArgumentException(
                            $"Duplicate charge VFX asset key '{definition.AssetKey}'.",
                            nameof(prefabs));
                    }
                }

                GameObject instance = Instantiate(definition.Prefab, transform, false);
                instance.name = $"Charge VFX {definition.AssetKey}";
                StrokeChargeVfxView view = instance.GetComponent<StrokeChargeVfxView>();
                if (view == null)
                {
                    DestroyImmediate(instance);
                    throw new ArgumentException(
                        $"Charge VFX prefab '{definition.AssetKey}' must contain " +
                        $"{nameof(StrokeChargeVfxView)} on its root.",
                        nameof(prefabs));
                }

                view.Initialize(sharedTrailMaterial, referenceSpace);
                chargeVfxAssetKeys[index] = definition.AssetKey;
                chargeVfxViews[index] = view;
            }
        }

        private StrokeChargeVfxView FindChargeVfx(string assetKey)
        {
            for (int index = 0; index < chargeVfxAssetKeys.Length; index++)
            {
                if (string.Equals(chargeVfxAssetKeys[index], assetKey, StringComparison.Ordinal))
                {
                    return chargeVfxViews[index];
                }
            }

            return null;
        }

        private void ValidateRendererTopology()
        {
            if (outerLineRenderer == null ||
                bodyLineRenderer == null ||
                coreLineRenderer == null ||
                branchLineRenderers == null ||
                branchLineRenderers.Length != BranchRendererCapacity)
            {
                throw new InvalidOperationException(
                    "Stroke trail prefab must bind outer, body, core and all branch renderers.");
            }

            if (outerLineRenderer.gameObject != gameObject)
            {
                throw new InvalidOperationException(
                    "The outer LineRenderer must be attached to the view root.");
            }

            ValidateChildRenderer(bodyLineRenderer, nameof(bodyLineRenderer));
            ValidateChildRenderer(coreLineRenderer, nameof(coreLineRenderer));
            for (int index = 0; index < branchLineRenderers.Length; index++)
            {
                ValidateChildRenderer(
                    branchLineRenderers[index],
                    $"{nameof(branchLineRenderers)}[{index}]");
            }
        }

        private void ValidateChildRenderer(LineRenderer renderer, string fieldName)
        {
            if (renderer == null || !renderer.transform.IsChildOf(transform))
            {
                throw new InvalidOperationException(
                    $"{fieldName} must reference a child LineRenderer.");
            }
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Stroke trail view is not initialized.");
            }
        }
    }
}
