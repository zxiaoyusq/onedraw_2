using System;
using System.Collections.Generic;
using OneStrokeDemon.Combat;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
    // 定义 StrokeTrailView 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class StrokeTrailView : MonoBehaviour
    {
        private static readonly Color OpaqueWhite = Color.white;
        private static readonly Color TransparentWhite = new Color(1f, 1f, 1f, 0f);

        private LineRenderer lineRendererComponent;
        private Material sharedTrailMaterial;
        private Transform referenceSpace;
        private IReadOnlyList<Vector2> sourcePoints;
        private float elapsedSeconds;
        private float lifetimeSeconds;

        public bool IsInitialized { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsPreviewing { get; private set; }

        public ulong StrokeId { get; private set; }

        public ulong ActivationSequence { get; private set; }

        public string StanceId { get; private set; }

        public IReadOnlyList<Vector2> SourcePoints => sourcePoints;

        public LineRenderer LineRenderer => lineRendererComponent;

        public float ReferencePixelWorldScale { get; private set; }

        public float NormalizedLifetime =>
            IsActive && lifetimeSeconds > 0f ? Mathf.Clamp01(elapsedSeconds / lifetimeSeconds) : 0f;

        // 处理 Initialize 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Initialize(
            LineRenderer lineRenderer,
            Material sharedMaterial,
            Transform configuredReferenceSpace = null)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (IsInitialized)
            {
                throw new InvalidOperationException("Stroke trail view is already initialized.");
            }

            lineRendererComponent = lineRenderer != null
                ? lineRenderer
                : throw new ArgumentNullException(nameof(lineRenderer));
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (lineRenderer.gameObject != gameObject)
            {
                throw new ArgumentException(
                    "The LineRenderer must be attached to the same GameObject.",
                    nameof(lineRenderer));
            }

            sharedTrailMaterial = sharedMaterial != null
                ? sharedMaterial
                : throw new ArgumentNullException(nameof(sharedMaterial));
            referenceSpace = configuredReferenceSpace != null
                ? configuredReferenceSpace
                : transform;
            lineRendererComponent.sharedMaterial = sharedTrailMaterial;
            lineRendererComponent.useWorldSpace = true;
            lineRendererComponent.loop = false;
            lineRendererComponent.alignment = LineAlignment.View;
            lineRendererComponent.textureMode = LineTextureMode.Stretch;
            lineRendererComponent.numCapVertices = 4;
            lineRendererComponent.numCornerVertices = 2;
            IsInitialized = true;
            ResetForPool();
        }

        // 显示 Show 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Show(
            StrokeTrailPath path,
            StrokeTrailStyle style,
            ulong activationSequence)
        {
            EnsureInitialized();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (path.PointCount < 2)
            {
                throw new ArgumentException("A visible trail needs at least two points.", nameof(path));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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
            sourcePoints = path.Points;
            lifetimeSeconds = style.LifetimeSeconds;

            Configure(style);
            lineRendererComponent.positionCount = path.PointCount;
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < path.PointCount; index++)
            {
                Vector2 point = path.Points[index];
                lineRendererComponent.SetPosition(index, ReferenceToWorld(point));
            }

            IsActive = true;
            IsPreviewing = false;
            lineRendererComponent.enabled = true;
        }

        // 处理 BeginPreview 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void BeginPreview(
            ulong strokeId,
            Vector2 firstPoint,
            StrokeTrailStyle style,
            ulong activationSequence)
        {
            EnsureInitialized();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (activationSequence == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(activationSequence));
            }

            ResetForPool();
            StrokeId = strokeId;
            ActivationSequence = activationSequence;
            StanceId = style.StanceId;
            lifetimeSeconds = style.LifetimeSeconds;
            Configure(style);
            lineRendererComponent.positionCount = 1;
            lineRendererComponent.SetPosition(0, ReferenceToWorld(firstPoint));
            IsActive = true;
            IsPreviewing = true;
            lineRendererComponent.enabled = false;
        }

        // 处理 AppendPreviewPoint 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void AppendPreviewPoint(Vector2 point)
        {
            EnsureInitialized();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsActive || !IsPreviewing)
            {
                throw new InvalidOperationException(
                    "Only an active preview can append a stroke point.");
            }

            int index = lineRendererComponent.positionCount;
            lineRendererComponent.positionCount = index + 1;
            lineRendererComponent.SetPosition(index, ReferenceToWorld(point));
            lineRendererComponent.enabled = true;
        }

        // 处理 Advance 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool Advance(float unscaledDeltaSeconds)
        {
            EnsureInitialized();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsActive)
            {
                return false;
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (float.IsNaN(unscaledDeltaSeconds) ||
                float.IsInfinity(unscaledDeltaSeconds) ||
                unscaledDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unscaledDeltaSeconds),
                    "Delta time must be finite and non-negative.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (IsPreviewing)
            {
                return false;
            }

            elapsedSeconds += unscaledDeltaSeconds;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (elapsedSeconds >= lifetimeSeconds)
            {
                ResetForPool();
                return true;
            }

            float alpha = 1f - (elapsedSeconds / lifetimeSeconds);
            var fadedColor = new Color(1f, 1f, 1f, alpha);
            lineRendererComponent.startColor = fadedColor;
            lineRendererComponent.endColor = fadedColor;
            return false;
        }

        // 重置 ResetForPool 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void ResetForPool()
        {
            EnsureInitialized();
            IsActive = false;
            IsPreviewing = false;
            StrokeId = 0;
            ActivationSequence = 0;
            StanceId = null;
            sourcePoints = null;
            elapsedSeconds = 0f;
            lifetimeSeconds = 0f;
            ReferencePixelWorldScale = 0f;

            lineRendererComponent.enabled = false;
            lineRendererComponent.positionCount = 0;
            lineRendererComponent.startWidth = 0f;
            lineRendererComponent.endWidth = 0f;
            lineRendererComponent.startColor = TransparentWhite;
            lineRendererComponent.endColor = TransparentWhite;
            lineRendererComponent.sortingLayerID = 0;
            lineRendererComponent.sortingOrder = 0;
            lineRendererComponent.useWorldSpace = true;
        }

        // 处理 Configure 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void Configure(StrokeTrailStyle style)
        {
            Transform ownTransform = transform;
            ownTransform.localPosition = Vector3.zero;
            ownTransform.localRotation = Quaternion.identity;
            ownTransform.localScale = Vector3.one;
            lineRendererComponent.sharedMaterial = sharedTrailMaterial;
            ReferencePixelWorldScale = Mathf.Max(
                Mathf.Abs(referenceSpace.lossyScale.x),
                Mathf.Abs(referenceSpace.lossyScale.y));
            float worldWidth = style.WidthReferencePixels * ReferencePixelWorldScale;
            lineRendererComponent.startWidth = worldWidth;
            lineRendererComponent.endWidth = worldWidth;
            lineRendererComponent.startColor = OpaqueWhite;
            lineRendererComponent.endColor = OpaqueWhite;
            lineRendererComponent.sortingLayerID = style.SortingLayerId;
            lineRendererComponent.sortingOrder = style.SortingOrder;
        }

        // 处理 ReferenceToWorld 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private Vector3 ReferenceToWorld(Vector2 point)
        {
            return referenceSpace.TransformPoint(new Vector3(point.x, point.y, 0f));
        }

        // 处理 EnsureInitialized 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void EnsureInitialized()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Stroke trail view is not initialized.");
            }
        }
    }
}
