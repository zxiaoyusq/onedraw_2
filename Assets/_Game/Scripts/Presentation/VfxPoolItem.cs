using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
    // 定义 VfxPoolItem 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class VfxPoolItem : MonoBehaviour, IPoolable
    {
        private Transform poolParent;
        private Transform followTarget;
        private PoolLease poolLease;
        private string vfxKey = string.Empty;
        private string assetKey = string.Empty;
        private string sortingLayer = string.Empty;
        private float lifetimeSeconds;
        private float elapsedSeconds;
        private int sortingOrder;
        private bool configuredFollowTarget;
        private bool hasPoolParent;
        private bool isConfigured;
        private bool isPlaying;
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Color[] originalColors = Array.Empty<Color>();
        private string[] originalSortingLayers = Array.Empty<string>();
        private int[] originalSortingOrders = Array.Empty<int>();
        private Color tint = Color.white;
        private float visualScale = 1f;

        public bool IsPoolActive => poolLease.IsValid;

        public bool IsConfigured => isConfigured;

        public bool IsPlaying => isPlaying;

        public bool HasCompleted => isPlaying && elapsedSeconds >= lifetimeSeconds;

        public string VfxKey => vfxKey;

        public string AssetKey => assetKey;

        public string SortingLayer => sortingLayer;

        public int SortingOrder => sortingOrder;

        public float LifetimeSeconds => lifetimeSeconds;

        public float ElapsedSeconds => elapsedSeconds;

        public bool FollowsTarget => configuredFollowTarget;

        public Transform FollowTarget => followTarget;

        public Color Tint => tint;

        public float VisualScale => visualScale;

        // 处理 Configure 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Configure(IConfigProvider configProvider, string configuredVfxKey)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (IsPoolActive || isPlaying)
            {
                throw new InvalidOperationException(
                    "VFX pool item cannot be reconfigured while leased or playing.");
            }

            VfxCueConfig row = configProvider.GetVfxCue(configuredVfxKey);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(row.VfxKey) ||
                string.IsNullOrWhiteSpace(row.AssetKey) ||
                string.IsNullOrWhiteSpace(row.SortingLayer))
            {
                throw new ArgumentException(
                    $"VFX cue '{configuredVfxKey}' has incomplete runtime fields.",
                    nameof(configuredVfxKey));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (float.IsNaN(row.LifeSec) || float.IsInfinity(row.LifeSec) || row.LifeSec <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredVfxKey),
                    "Configured VFX lifetime must be finite and positive.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (row.SortingOrder < int.MinValue || row.SortingOrder > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredVfxKey),
                    "Configured VFX sorting order exceeds the runtime integer range.");
            }

            vfxKey = row.VfxKey;
            assetKey = row.AssetKey;
            sortingLayer = row.SortingLayer;
            sortingOrder = (int)row.SortingOrder;
            lifetimeSeconds = row.LifeSec;
            configuredFollowTarget = row.FollowTarget;
            isConfigured = true;
        }

        // 播放 Play 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Play(Transform target, Vector3 worldPosition)
        {
            Play(target, worldPosition, Color.white, 1f);
        }

        // 播放 Play 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Play(Transform target, Vector3 worldPosition, Color configuredTint, float configuredScale)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsPoolActive)
            {
                throw new InvalidOperationException("VFX must hold a pool lease before playing.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!isConfigured)
            {
                throw new InvalidOperationException("VFX pool item must be configured before playing.");
            }

            ValidateVector(worldPosition, nameof(worldPosition));
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (float.IsNaN(configuredScale) || float.IsInfinity(configuredScale) || configuredScale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(configuredScale));
            }

            CacheRenderers();
            tint = configuredTint;
            visualScale = configuredScale;
            transform.localScale = Vector3.one;
            float sourceVisualSize = MeasureSourceVisualSize();
            transform.localScale = Vector3.one *
                (sourceVisualSize > Mathf.Epsilon
                    ? visualScale / sourceVisualSize
                    : visualScale);
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < renderers.Length; index += 1)
            {
                renderers[index].color = originalColors[index] * tint;
                renderers[index].sortingLayerName = sortingLayer;
                renderers[index].sortingOrder = sortingOrder;
            }

            followTarget = configuredFollowTarget ? target : null;
            transform.position = followTarget != null ? followTarget.position : worldPosition;
            elapsedSeconds = 0f;
            isPlaying = true;
        }

        // 处理 Advance 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool Advance(float deltaSeconds)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds),
                    "VFX delta time must be finite and non-negative.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!isPlaying)
            {
                return false;
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (followTarget != null)
            {
                transform.position = followTarget.position;
            }

            elapsedSeconds = Mathf.Min(lifetimeSeconds, elapsedSeconds + deltaSeconds);
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
                throw new InvalidOperationException("VFX already holds a pool lease.");
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
                throw new InvalidOperationException("VFX pool release used a stale lease.");
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
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < renderers.Length; index += 1)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (renderers[index] != null)
                {
                    renderers[index].color = originalColors[index];
                    renderers[index].sortingLayerName = originalSortingLayers[index];
                    renderers[index].sortingOrder = originalSortingOrders[index];
                }
            }

            followTarget = null;
            elapsedSeconds = 0f;
            isPlaying = false;
            tint = Color.white;
            visualScale = 1f;
            transform.SetParent(hasPoolParent ? poolParent : null, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        // 处理 CacheRenderers 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void CacheRenderers()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (renderers.Length > 0)
            {
                return;
            }

            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            originalColors = new Color[renderers.Length];
            originalSortingLayers = new string[renderers.Length];
            originalSortingOrders = new int[renderers.Length];
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < renderers.Length; index += 1)
            {
                originalColors[index] = renderers[index].color;
                originalSortingLayers[index] = renderers[index].sortingLayerName;
                originalSortingOrders[index] = renderers[index].sortingOrder;
            }
        }

        // 处理 MeasureSourceVisualSize 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private float MeasureSourceVisualSize()
        {
            bool hasBounds = false;
            Bounds combined = default;
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < renderers.Length; index += 1)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (renderers[index] == null)
                {
                    continue;
                }

                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (!hasBounds)
                {
                    combined = renderers[index].bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(renderers[index].bounds);
                }
            }

            return hasBounds
                ? Mathf.Max(combined.size.x, combined.size.y, combined.size.z)
                : 0f;
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
                    "VFX world position must be finite.");
            }
        }
    }
}
