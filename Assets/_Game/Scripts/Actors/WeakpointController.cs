using System;
using OneStrokeDemon.Combat;
using UnityEngine;

namespace OneStrokeDemon.Actors
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class WeakpointController : MonoBehaviour, IStrokeHitbox
    {
        private CircleCollider2D hitCollider;
        private Damageable hitTarget;
        private EnemyWeakpointDefinition definition;
        private double attackStartedAt;
        private double lastTimestamp;
        private bool attackCycleActive;
        private bool windowOpen;
        private bool hasTimestamp;

        public IHittable HitTarget => hitTarget;

        public bool IsWeakpoint => true;

        public bool IsStrokeHitboxActive =>
            windowOpen &&
            enabled &&
            gameObject.activeInHierarchy &&
            hitCollider != null &&
            hitCollider.enabled &&
            hitTarget != null &&
            hitTarget.CanReceiveStrokeHit;

        public EnemyWeakpointDefinition Definition => definition;

        public bool IsWindowOpen => windowOpen;

        public bool IsAttackCycleActive => attackCycleActive;

        public CircleCollider2D HitCollider
        {
            get
            {
                EnsureCollider();
                return hitCollider;
            }
        }

        private void Awake()
        {
            EnsureCollider();
            CloseWindow();
        }

        private void OnDisable()
        {
            CloseWindow();
        }

        internal void Configure(
            in EnemyWeakpointDefinition configuredDefinition,
            Damageable configuredHitTarget)
        {
            if (!configuredDefinition.IsConfigured)
            {
                throw new ArgumentException(
                    "Weakpoint definition must be configured.",
                    nameof(configuredDefinition));
            }

            hitTarget = configuredHitTarget ??
                throw new ArgumentNullException(nameof(configuredHitTarget));
            definition = configuredDefinition;
            attackStartedAt = 0d;
            lastTimestamp = 0d;
            attackCycleActive = false;
            windowOpen = false;
            hasTimestamp = false;
            EnsureCollider();
            hitCollider.isTrigger = true;
            hitCollider.radius = definition.RadiusReferencePixels;
            hitCollider.enabled = false;
        }

        internal void BeginAttack(double timestamp)
        {
            RequireConfigured();
            ValidateTimestamp(timestamp, nameof(timestamp));
            attackStartedAt = timestamp;
            lastTimestamp = timestamp;
            hasTimestamp = true;
            attackCycleActive = true;
            SetWindow(definition.IsOpenAt(0d));
        }

        internal bool Tick(double timestamp, bool attackMayExposeWeakpoint)
        {
            RequireConfigured();
            ObserveTimestamp(timestamp);
            bool shouldOpen = attackCycleActive &&
                              attackMayExposeWeakpoint &&
                              definition.IsOpenAt(timestamp - attackStartedAt);
            bool changed = shouldOpen != windowOpen;
            SetWindow(shouldOpen);
            return changed;
        }

        internal bool EndAttack()
        {
            bool changed = attackCycleActive || windowOpen;
            attackCycleActive = false;
            CloseWindow();
            return changed;
        }

        internal bool Release()
        {
            bool hadState = definition.IsConfigured || hitTarget != null || attackCycleActive;
            definition = default;
            hitTarget = null;
            attackStartedAt = 0d;
            lastTimestamp = 0d;
            attackCycleActive = false;
            windowOpen = false;
            hasTimestamp = false;
            EnsureCollider();
            hitCollider.enabled = false;
            hitCollider.radius = 0f;
            hitCollider.isTrigger = true;
            return hadState;
        }

        private void SetWindow(bool open)
        {
            windowOpen = open && definition.HasHitbox;
            EnsureCollider();
            hitCollider.enabled = windowOpen;
        }

        private void CloseWindow()
        {
            windowOpen = false;
            if (hitCollider != null)
            {
                hitCollider.enabled = false;
            }
        }

        private void EnsureCollider()
        {
            if (hitCollider == null)
            {
                hitCollider = GetComponent<CircleCollider2D>();
            }

            if (hitCollider == null)
            {
                throw new InvalidOperationException(
                    "WeakpointController requires a CircleCollider2D.");
            }
        }

        private void RequireConfigured()
        {
            if (!definition.IsConfigured || hitTarget == null)
            {
                throw new InvalidOperationException(
                    "WeakpointController must be configured before use.");
            }
        }

        private void ObserveTimestamp(double timestamp)
        {
            ValidateTimestamp(timestamp, nameof(timestamp));
            if (hasTimestamp && timestamp < lastTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    $"Weakpoint timestamp cannot move backwards from {lastTimestamp}.");
            }

            lastTimestamp = timestamp;
            hasTimestamp = true;
        }

        private static void ValidateTimestamp(double timestamp, string parameterName)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    timestamp,
                    "Weakpoint timestamp must be finite and non-negative.");
            }
        }
    }
}
