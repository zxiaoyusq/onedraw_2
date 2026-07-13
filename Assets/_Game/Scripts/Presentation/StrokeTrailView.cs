using System;
using System.Collections.Generic;
using OneStrokeDemon.Combat;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
    public sealed class StrokeTrailView : MonoBehaviour
    {
        private static readonly Color OpaqueWhite = Color.white;
        private static readonly Color TransparentWhite = new Color(1f, 1f, 1f, 0f);

        private LineRenderer lineRendererComponent;
        private Material sharedTrailMaterial;
        private IReadOnlyList<Vector2> sourcePoints;
        private float elapsedSeconds;
        private float lifetimeSeconds;

        public bool IsInitialized { get; private set; }

        public bool IsActive { get; private set; }

        public ulong StrokeId { get; private set; }

        public ulong ActivationSequence { get; private set; }

        public string StanceId { get; private set; }

        public IReadOnlyList<Vector2> SourcePoints => sourcePoints;

        public LineRenderer LineRenderer => lineRendererComponent;

        public float NormalizedLifetime =>
            IsActive && lifetimeSeconds > 0f ? Mathf.Clamp01(elapsedSeconds / lifetimeSeconds) : 0f;

        public void Initialize(LineRenderer lineRenderer, Material sharedMaterial)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Stroke trail view is already initialized.");
            }

            lineRendererComponent = lineRenderer != null
                ? lineRenderer
                : throw new ArgumentNullException(nameof(lineRenderer));
            if (lineRenderer.gameObject != gameObject)
            {
                throw new ArgumentException(
                    "The LineRenderer must be attached to the same GameObject.",
                    nameof(lineRenderer));
            }

            sharedTrailMaterial = sharedMaterial != null
                ? sharedMaterial
                : throw new ArgumentNullException(nameof(sharedMaterial));
            lineRendererComponent.sharedMaterial = sharedTrailMaterial;
            lineRendererComponent.useWorldSpace = false;
            lineRendererComponent.loop = false;
            lineRendererComponent.alignment = LineAlignment.View;
            lineRendererComponent.textureMode = LineTextureMode.Stretch;
            lineRendererComponent.numCapVertices = 0;
            lineRendererComponent.numCornerVertices = 0;
            IsInitialized = true;
            ResetForPool();
        }

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
            sourcePoints = path.Points;
            lifetimeSeconds = style.LifetimeSeconds;

            Transform ownTransform = transform;
            ownTransform.localPosition = Vector3.zero;
            ownTransform.localRotation = Quaternion.identity;
            ownTransform.localScale = Vector3.one;
            lineRendererComponent.sharedMaterial = sharedTrailMaterial;
            lineRendererComponent.startWidth = style.WidthReferencePixels;
            lineRendererComponent.endWidth = style.WidthReferencePixels;
            lineRendererComponent.startColor = OpaqueWhite;
            lineRendererComponent.endColor = OpaqueWhite;
            lineRendererComponent.sortingLayerID = style.SortingLayerId;
            lineRendererComponent.sortingOrder = style.SortingOrder;
            lineRendererComponent.positionCount = path.PointCount;
            for (int index = 0; index < path.PointCount; index++)
            {
                Vector2 point = path.Points[index];
                lineRendererComponent.SetPosition(index, new Vector3(point.x, point.y, 0f));
            }

            IsActive = true;
            lineRendererComponent.enabled = true;
        }

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

            elapsedSeconds += unscaledDeltaSeconds;
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

        public void ResetForPool()
        {
            EnsureInitialized();
            IsActive = false;
            StrokeId = 0;
            ActivationSequence = 0;
            StanceId = null;
            sourcePoints = null;
            elapsedSeconds = 0f;
            lifetimeSeconds = 0f;

            lineRendererComponent.enabled = false;
            lineRendererComponent.positionCount = 0;
            lineRendererComponent.startWidth = 0f;
            lineRendererComponent.endWidth = 0f;
            lineRendererComponent.startColor = TransparentWhite;
            lineRendererComponent.endColor = TransparentWhite;
            lineRendererComponent.sortingLayerID = 0;
            lineRendererComponent.sortingOrder = 0;
            lineRendererComponent.useWorldSpace = false;
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
