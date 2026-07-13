using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
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

        public void Configure(IConfigProvider configProvider, string configuredVfxKey)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (IsPoolActive || isPlaying)
            {
                throw new InvalidOperationException(
                    "VFX pool item cannot be reconfigured while leased or playing.");
            }

            VfxCueConfig row = configProvider.GetVfxCue(configuredVfxKey);
            if (string.IsNullOrWhiteSpace(row.VfxKey) ||
                string.IsNullOrWhiteSpace(row.AssetKey) ||
                string.IsNullOrWhiteSpace(row.SortingLayer))
            {
                throw new ArgumentException(
                    $"VFX cue '{configuredVfxKey}' has incomplete runtime fields.",
                    nameof(configuredVfxKey));
            }

            if (float.IsNaN(row.LifeSec) || float.IsInfinity(row.LifeSec) || row.LifeSec <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredVfxKey),
                    "Configured VFX lifetime must be finite and positive.");
            }

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

        public void Play(Transform target, Vector3 worldPosition)
        {
            if (!IsPoolActive)
            {
                throw new InvalidOperationException("VFX must hold a pool lease before playing.");
            }

            if (!isConfigured)
            {
                throw new InvalidOperationException("VFX pool item must be configured before playing.");
            }

            ValidateVector(worldPosition, nameof(worldPosition));
            followTarget = configuredFollowTarget ? target : null;
            transform.position = followTarget != null ? followTarget.position : worldPosition;
            elapsedSeconds = 0f;
            isPlaying = true;
        }

        public bool Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds),
                    "VFX delta time must be finite and non-negative.");
            }

            if (!isPlaying)
            {
                return false;
            }

            if (followTarget != null)
            {
                transform.position = followTarget.position;
            }

            elapsedSeconds = Mathf.Min(lifetimeSeconds, elapsedSeconds + deltaSeconds);
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
                throw new InvalidOperationException("VFX already holds a pool lease.");
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
                throw new InvalidOperationException("VFX pool release used a stale lease.");
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
            followTarget = null;
            elapsedSeconds = 0f;
            isPlaying = false;
            transform.SetParent(hasPoolParent ? poolParent : null, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static void ValidateVector(Vector3 value, string parameterName)
        {
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
