using System;
using OneStrokeDemon.Core;
using TMPro;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
    // 定义 DamageNumberPoolItem 的表现层契约，隔离战斗状态与具体Unity视图实现。
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

        // 处理 ConfigureVisual 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void ConfigureVisual(TMP_FontAsset fontAsset)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (fontAsset == null)
            {
                throw new ArgumentNullException(nameof(fontAsset));
            }

            textMesh = GetComponent<TextMeshPro>();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (textMesh == null)
            {
                textMesh = gameObject.AddComponent<TextMeshPro>();
            }

            textMesh.font = fontAsset;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.textWrappingMode = TextWrappingModes.NoWrap;
            textMesh.sortingOrder = 100;
        }

        // 显示 Show 对应的表现逻辑，使视图与只读战斗状态保持同步。
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

        // 显示 Show 对应的表现逻辑，使视图与只读战斗状态保持同步。
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

        // 显示 Show 对应的表现逻辑，使视图与只读战斗状态保持同步。
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
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsPoolActive)
            {
                throw new InvalidOperationException(
                    "Damage number must hold a pool lease before it can be shown.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (displayedAmount == 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayedAmount),
                    "Displayed damage amount must be non-zero.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (displayedTargetId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayedTargetId),
                    "Displayed damage target id must be non-zero.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(displayedSourceId))
            {
                throw new ArgumentException(
                    "Displayed damage source id must be non-empty.",
                    nameof(displayedSourceId));
            }

            ValidateVector(worldPosition, nameof(worldPosition));
            ValidatePositive(fontSize, nameof(fontSize));
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (float.IsNaN(fontHeightWorldUnits) ||
                float.IsInfinity(fontHeightWorldUnits) ||
                fontHeightWorldUnits < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fontHeightWorldUnits));
            }

            ValidatePositive(lifeSeconds, nameof(lifeSeconds));
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (textMesh != null)
            {
                textMesh.text = displayedAmount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                textMesh.fontSize = fontSize;
                textMesh.color = configuredColor;
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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

        // 处理 Advance 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool Advance(float deltaSeconds)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!isVisible)
            {
                return false;
            }

            elapsedSeconds = Mathf.Min(lifetimeSeconds, elapsedSeconds + deltaSeconds);
            float progress = Mathf.Clamp01(elapsedSeconds / lifetimeSeconds);
            transform.position = startPosition + (Vector3.up * riseWorldUnits * progress);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (textMesh != null)
            {
                Color fading = configuredColor;
                fading.a *= 1f - progress;
                textMesh.color = fading;
            }

            return HasCompleted;
        }

        // 处理 AcquireFromPool 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void AcquireFromPool(in PoolLease lease)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!lease.IsValid)
            {
                throw new ArgumentException("A valid pool lease is required.", nameof(lease));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (poolLease.IsValid)
            {
                throw new InvalidOperationException("Damage number already holds a pool lease.");
            }

            poolParent = transform.parent;
            hasPoolParent = true;
            ResetRuntimeState();
            poolLease = lease;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        // 处理 ReleaseToPool 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void ReleaseToPool(in PoolReleaseContext context)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!hasPoolParent)
            {
                poolParent = transform.parent;
                hasPoolParent = true;
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (context.Lease.IsValid && poolLease.IsValid && context.Lease != poolLease)
            {
                throw new InvalidOperationException("Damage-number pool release used a stale lease.");
            }

            ResetRuntimeState();
            poolLease = default;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        // 重置 ResetRuntimeState 对应的表现逻辑，使视图与只读战斗状态保持同步。
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
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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

        // 校验 ValidatePositive 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void ValidatePositive(float value, string parameterName)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        // 校验 ValidateVector 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void ValidateVector(Vector3 value, string parameterName)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
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
