using System;
using OneStrokeDemon.Core;
using TMPro;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DamageNumberPoolItem : MonoBehaviour, IPoolable
    {
        private Transform poolParent;
        private PoolLease poolLease;
        private string sourceId = string.Empty;
        private long amount;
        private int targetId;
        private TextMeshPro textMesh;
        private Color configuredColor = Color.white;
        private Vector3 startPosition;
        private float lifetimeSeconds = 1f;
        private float elapsedSeconds;
        private float riseWorldUnits;
        private bool hasPoolParent;
        private bool isVisible;

        public bool IsPoolActive => poolLease.IsValid;

        public bool IsVisible => isVisible;

        public long Amount => amount;

        public int TargetId => targetId;

        public string SourceId => sourceId;

        public bool HasCompleted => isVisible && elapsedSeconds >= lifetimeSeconds;

        public float ElapsedSeconds => elapsedSeconds;

        public Color ConfiguredColor => configuredColor;

        public TextMeshPro TextMesh => textMesh;

        public void ConfigureVisual(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                throw new ArgumentNullException(nameof(fontAsset));
            }

            textMesh = GetComponent<TextMeshPro>();
            if (textMesh == null)
            {
                textMesh = gameObject.AddComponent<TextMeshPro>();
            }

            textMesh.font = fontAsset;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.textWrappingMode = TextWrappingModes.NoWrap;
            textMesh.sortingOrder = 100;
        }

        public void Show(long displayedAmount, int displayedTargetId, string displayedSourceId, Vector3 worldPosition)
        {
            Show(
                displayedAmount,
                displayedTargetId,
                displayedSourceId,
                worldPosition,
                Color.white,
                24f,
                1f,
                0f);
        }

        public void Show(
            long displayedAmount,
            int displayedTargetId,
            string displayedSourceId,
            Vector3 worldPosition,
            Color color,
            float fontSize,
            float lifeSeconds,
            float configuredRiseWorldUnits)
        {
            Show(
                displayedAmount,
                displayedTargetId,
                displayedSourceId,
                worldPosition,
                color,
                fontSize,
                0f,
                lifeSeconds,
                configuredRiseWorldUnits);
        }

        public void Show(
            long displayedAmount,
            int displayedTargetId,
            string displayedSourceId,
            Vector3 worldPosition,
            Color color,
            float fontSize,
            float fontHeightWorldUnits,
            float lifeSeconds,
            float configuredRiseWorldUnits)
        {
            if (!IsPoolActive)
            {
                throw new InvalidOperationException(
                    "Damage number must hold a pool lease before it can be shown.");
            }

            if (displayedAmount == 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayedAmount),
                    "Displayed damage amount must be non-zero.");
            }

            if (displayedTargetId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayedTargetId),
                    "Displayed damage target id must be non-zero.");
            }

            if (string.IsNullOrWhiteSpace(displayedSourceId))
            {
                throw new ArgumentException(
                    "Displayed damage source id must be non-empty.",
                    nameof(displayedSourceId));
            }

            ValidateVector(worldPosition, nameof(worldPosition));
            ValidatePositive(fontSize, nameof(fontSize));
            if (float.IsNaN(fontHeightWorldUnits) ||
                float.IsInfinity(fontHeightWorldUnits) ||
                fontHeightWorldUnits < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fontHeightWorldUnits));
            }

            ValidatePositive(lifeSeconds, nameof(lifeSeconds));
            if (float.IsNaN(configuredRiseWorldUnits) ||
                float.IsInfinity(configuredRiseWorldUnits) ||
                configuredRiseWorldUnits < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(configuredRiseWorldUnits));
            }

            amount = displayedAmount;
            targetId = displayedTargetId;
            sourceId = displayedSourceId;
            transform.position = worldPosition;
            startPosition = worldPosition;
            configuredColor = color;
            lifetimeSeconds = lifeSeconds;
            riseWorldUnits = configuredRiseWorldUnits;
            elapsedSeconds = 0f;
            if (textMesh != null)
            {
                textMesh.text = displayedAmount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                textMesh.fontSize = fontSize;
                textMesh.color = configuredColor;
                if (fontHeightWorldUnits > 0f)
                {
                    textMesh.ForceMeshUpdate(ignoreActiveState: false, forceTextReparsing: true);
                    float preferredHeight = textMesh.GetPreferredValues(textMesh.text).y;
                    ValidatePositive(preferredHeight, "preferredTextHeight");
                    transform.localScale = Vector3.one * (fontHeightWorldUnits / preferredHeight);
                }
            }

            isVisible = true;
        }

        public bool Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }

            if (!isVisible)
            {
                return false;
            }

            elapsedSeconds = Mathf.Min(lifetimeSeconds, elapsedSeconds + deltaSeconds);
            float progress = Mathf.Clamp01(elapsedSeconds / lifetimeSeconds);
            transform.position = startPosition + (Vector3.up * riseWorldUnits * progress);
            if (textMesh != null)
            {
                Color fading = configuredColor;
                fading.a *= 1f - progress;
                textMesh.color = fading;
            }

            return HasCompleted;
        }

        public void AcquireFromPool(in PoolLease lease)
        {
            if (!lease.IsValid)
            {
                throw new ArgumentException("A valid pool lease is required.", nameof(lease));
            }

            if (poolLease.IsValid)
            {
                throw new InvalidOperationException("Damage number already holds a pool lease.");
            }

            poolParent = transform.parent;
            hasPoolParent = true;
            ResetRuntimeState();
            poolLease = lease;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void ReleaseToPool(in PoolReleaseContext context)
        {
            if (!hasPoolParent)
            {
                poolParent = transform.parent;
                hasPoolParent = true;
            }

            if (context.Lease.IsValid && poolLease.IsValid && context.Lease != poolLease)
            {
                throw new InvalidOperationException("Damage-number pool release used a stale lease.");
            }

            ResetRuntimeState();
            poolLease = default;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void ResetRuntimeState()
        {
            amount = 0L;
            targetId = 0;
            sourceId = string.Empty;
            isVisible = false;
            configuredColor = Color.white;
            startPosition = Vector3.zero;
            lifetimeSeconds = 1f;
            elapsedSeconds = 0f;
            riseWorldUnits = 0f;
            if (textMesh != null)
            {
                textMesh.text = string.Empty;
                textMesh.color = Color.white;
            }
            transform.SetParent(hasPoolParent ? poolParent : null, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static void ValidatePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void ValidateVector(Vector3 value, string parameterName)
        {
            if (float.IsNaN(value.x) || float.IsInfinity(value.x) ||
                float.IsNaN(value.y) || float.IsInfinity(value.y) ||
                float.IsNaN(value.z) || float.IsInfinity(value.z))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Damage-number world position must be finite.");
            }
        }
    }
}
