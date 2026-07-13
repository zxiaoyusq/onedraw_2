using System;
using OneStrokeDemon.Core;
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
        private bool hasPoolParent;
        private bool isVisible;

        public bool IsPoolActive => poolLease.IsValid;

        public bool IsVisible => isVisible;

        public long Amount => amount;

        public int TargetId => targetId;

        public string SourceId => sourceId;

        public void Show(long displayedAmount, int displayedTargetId, string displayedSourceId, Vector3 worldPosition)
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
            amount = displayedAmount;
            targetId = displayedTargetId;
            sourceId = displayedSourceId;
            transform.position = worldPosition;
            isVisible = true;
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
                    "Damage-number world position must be finite.");
            }
        }
    }
}
