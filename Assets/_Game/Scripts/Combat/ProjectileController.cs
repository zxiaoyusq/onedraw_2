using System;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    public enum ProjectileReleaseReason
    {
        None = 0,
        Cut = 1,
        Impact = 2,
        LifetimeExpired = 3,
        Manual = 4
    }

    public readonly struct ProjectileReleaseSnapshot
    {
        internal ProjectileReleaseSnapshot(
            ProjectileReleaseReason reason,
            string projectileId,
            int hitTargetId,
            in ProjectileOwnership ownership,
            Vector2 referencePosition,
            float elapsedSeconds)
        {
            Reason = reason;
            ProjectileId = projectileId;
            HitTargetId = hitTargetId;
            Ownership = ownership;
            ReferencePosition = referencePosition;
            ElapsedSeconds = elapsedSeconds;
            IsValid = true;
        }

        public ProjectileReleaseReason Reason { get; }

        public string ProjectileId { get; }

        public int HitTargetId { get; }

        public ProjectileOwnership Ownership { get; }

        public Vector2 ReferencePosition { get; }

        public float ElapsedSeconds { get; }

        public bool IsValid { get; }
    }

    public readonly struct ProjectileStrokeResult
    {
        internal ProjectileStrokeResult(
            ulong strokeId,
            int hitTargetId,
            string projectileId,
            ProjectileStrokeOutcome outcome,
            in ProjectileOwnership ownershipBefore,
            in ProjectileOwnership ownershipAfter,
            Vector2 directionBefore,
            Vector2 directionAfter,
            in ProjectileReleaseSnapshot release)
        {
            StrokeId = strokeId;
            HitTargetId = hitTargetId;
            ProjectileId = projectileId;
            Outcome = outcome;
            OwnershipBefore = ownershipBefore;
            OwnershipAfter = ownershipAfter;
            DirectionBefore = directionBefore;
            DirectionAfter = directionAfter;
            Release = release;
            IsValid = true;
        }

        public ulong StrokeId { get; }

        public int HitTargetId { get; }

        public string ProjectileId { get; }

        public ProjectileStrokeOutcome Outcome { get; }

        public ProjectileOwnership OwnershipBefore { get; }

        public ProjectileOwnership OwnershipAfter { get; }

        public Vector2 DirectionBefore { get; }

        public Vector2 DirectionAfter { get; }

        public ProjectileReleaseSnapshot Release { get; }

        public bool IsValid { get; }
    }

    public readonly struct ProjectileImpactResult
    {
        internal ProjectileImpactResult(
            ProjectileOwner target,
            in ProjectileDamageSource damageSource,
            in ProjectileReleaseSnapshot release)
        {
            Target = target;
            DamageSource = damageSource;
            Release = release;
            IsValid = true;
        }

        public ProjectileOwner Target { get; }

        public ProjectileDamageSource DamageSource { get; }

        public ProjectileReleaseSnapshot Release { get; }

        public bool IsValid { get; }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectileHitTarget), typeof(CircleCollider2D))]
    public sealed class ProjectileController : MonoBehaviour
    {
        private ProjectileHitTarget hitTarget;
        private CircleCollider2D hitCollider;
        private ProjectileRuleSet rules;
        private ProjectileOwnership ownership;
        private Transform referenceSpace;
        private Vector2 referencePosition;
        private Vector2 travelDirection;
        private float elapsedSeconds;
        private bool isActive;

        public bool IsActive => isActive;

        public bool CanReceiveStrokeHit =>
            isActive && ownership.CurrentOwner.Faction == ProjectileFaction.Enemy;

        public ProjectileRuleSet Rules => rules;

        public ProjectileOwnership Ownership => ownership;

        public Transform ReferenceSpace => referenceSpace;

        public Vector2 ReferencePosition => referencePosition;

        public Vector2 TravelDirection => travelDirection;

        public float ElapsedSeconds => elapsedSeconds;

        public ProjectileHitTarget HitTarget
        {
            get
            {
                EnsureComponents();
                return hitTarget;
            }
        }

        public CircleCollider2D HitCollider
        {
            get
            {
                EnsureComponents();
                return hitCollider;
            }
        }

        private void Awake()
        {
            EnsureComponents();
            hitCollider.isTrigger = true;
            if (!isActive)
            {
                hitCollider.enabled = false;
            }
        }

        private void Update()
        {
            if (isActive)
            {
                Tick(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            if (isActive)
            {
                ClearRuntimeState(resetTransform: false);
            }
        }

        public void Spawn(
            in ProjectileRuleSet configuredRules,
            int hitTargetId,
            ProjectileOwner initialOwner,
            Transform referenceSpaceTransform,
            Vector2 startReferencePosition,
            Vector2 direction)
        {
            if (isActive)
            {
                throw new InvalidOperationException("An active projectile must be released before reuse.");
            }

            if (!configuredRules.IsConfigured)
            {
                throw new ArgumentException(
                    "Projectile rules must be configured.",
                    nameof(configuredRules));
            }

            if (hitTargetId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hitTargetId),
                    "Projectile hit target id must be non-zero.");
            }

            if (!initialOwner.IsValid)
            {
                throw new ArgumentException(
                    "Initial projectile owner must be initialized.",
                    nameof(initialOwner));
            }

            ValidateVector(startReferencePosition, nameof(startReferencePosition));
            ValidateVector(direction, nameof(direction));
            if (direction.sqrMagnitude <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    "Projectile travel direction must be non-zero.");
            }

            EnsureComponents();
            rules = configuredRules;
            ownership = ProjectileOwnership.FromInitialOwner(initialOwner);
            referenceSpace = referenceSpaceTransform;
            referencePosition = startReferencePosition;
            travelDirection = direction.normalized;
            elapsedSeconds = 0f;
            isActive = true;
            hitTarget.Attach(this, hitTargetId);
            hitCollider.radius = rules.HitRadiusReferencePixels;
            hitCollider.isTrigger = true;
            hitCollider.enabled = rules.HitRadiusReferencePixels > 0f;
            transform.SetParent(referenceSpace, false);
            transform.localScale = Vector3.one;
            ApplyTransform();
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public ProjectileReleaseSnapshot Tick(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds),
                    "Projectile delta time must be finite and non-negative.");
            }

            if (!isActive)
            {
                return default;
            }

            float remainingLifetime = Mathf.Max(0f, rules.LifetimeSeconds - elapsedSeconds);
            float appliedSeconds = Mathf.Min(deltaSeconds, remainingLifetime);
            Vector2 displacement =
                travelDirection * rules.SpeedReferencePixelsPerSecond * appliedSeconds;
            Vector2 nextPosition = referencePosition + displacement;
            ValidateVector(nextPosition, nameof(deltaSeconds));
            referencePosition = nextPosition;
            elapsedSeconds += appliedSeconds;
            ApplyTransform();

            if (deltaSeconds >= remainingLifetime)
            {
                return Release(ProjectileReleaseReason.LifetimeExpired);
            }

            return default;
        }

        public bool TryResolveImpact(
            ProjectileOwner target,
            out ProjectileImpactResult result)
        {
            result = default;
            if (!isActive || !ownership.CanDamage(target))
            {
                return false;
            }

            var damageSource = new ProjectileDamageSource(rules, ownership);
            ProjectileReleaseSnapshot release = Release(ProjectileReleaseReason.Impact);
            result = new ProjectileImpactResult(target, damageSource, release);
            return true;
        }

        public ProjectileReleaseSnapshot Release(ProjectileReleaseReason reason)
        {
            if (reason == ProjectileReleaseReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason),
                    "A concrete projectile release reason is required.");
            }

            if (!isActive)
            {
                return default;
            }

            var snapshot = new ProjectileReleaseSnapshot(
                reason,
                rules.ProjectileId,
                hitTarget.HitTargetId,
                ownership,
                referencePosition,
                elapsedSeconds);
            ClearRuntimeState(resetTransform: true);
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            return snapshot;
        }

        internal ProjectileStrokeResult ResolveStroke(
            ulong strokeId,
            int hitTargetId,
            string stanceId,
            ProjectileOwner reflector)
        {
            if (!isActive)
            {
                throw new InvalidOperationException("Inactive projectiles cannot resolve stroke hits.");
            }

            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId), "Stroke id must be positive.");
            }

            if (hitTargetId != hitTarget.HitTargetId)
            {
                throw new ArgumentException(
                    "Stroke target id does not match the active projectile target.",
                    nameof(hitTargetId));
            }

            string projectileId = rules.ProjectileId;
            ProjectileOwnership ownershipBefore = ownership;
            Vector2 directionBefore = travelDirection;
            ProjectileStrokeResolution resolution = ProjectileCutResolver.Resolve(
                rules,
                ownership,
                stanceId,
                reflector);
            ProjectileReleaseSnapshot release = default;
            if (resolution.ChangesOwnership)
            {
                ownership = ownership.ReflectTo(reflector);
                travelDirection = -travelDirection;
                ApplyTransform();
            }
            else if (resolution.ReleasesProjectile)
            {
                release = Release(ProjectileReleaseReason.Cut);
            }

            return new ProjectileStrokeResult(
                strokeId,
                hitTargetId,
                projectileId,
                resolution.Outcome,
                ownershipBefore,
                resolution.ReleasesProjectile ? ownershipBefore : ownership,
                directionBefore,
                resolution.ReleasesProjectile ? directionBefore : travelDirection,
                release);
        }

        private void EnsureComponents()
        {
            if (hitTarget == null)
            {
                hitTarget = GetComponent<ProjectileHitTarget>();
            }

            if (hitCollider == null)
            {
                hitCollider = GetComponent<CircleCollider2D>();
            }

            if (hitTarget == null || hitCollider == null)
            {
                throw new InvalidOperationException(
                    "ProjectileController requires ProjectileHitTarget and CircleCollider2D components.");
            }

            hitTarget.AttachController(this);
        }

        private void ApplyTransform()
        {
            transform.localPosition = new Vector3(referencePosition.x, referencePosition.y, 0f);
            transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg);
        }

        private void ClearRuntimeState(bool resetTransform)
        {
            isActive = false;
            rules = default;
            ownership = default;
            referenceSpace = null;
            referencePosition = Vector2.zero;
            travelDirection = Vector2.zero;
            elapsedSeconds = 0f;
            if (hitTarget != null)
            {
                hitTarget.ResetRuntimeState();
            }

            if (hitCollider != null)
            {
                hitCollider.enabled = false;
                hitCollider.radius = 0f;
                hitCollider.isTrigger = true;
            }

            if (resetTransform)
            {
                transform.SetParent(null, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }
        }

        private static void ValidateVector(Vector2 value, string parameterName)
        {
            if (float.IsNaN(value.x) || float.IsInfinity(value.x) ||
                float.IsNaN(value.y) || float.IsInfinity(value.y))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Projectile vectors must contain finite components.");
            }
        }
    }
}
