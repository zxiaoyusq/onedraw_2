using System;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    public sealed class Physics2DStrokeHitQuery : IStrokeHitQuery
    {
        private static readonly Type HitboxType = typeof(IStrokeHitbox);
        private static readonly Type HittableType = typeof(IHittable);

        private readonly ContactFilter2D contactFilter;
        private readonly Transform referenceSpace;
        private readonly RaycastHit2D[] hitBuffer;

        public Physics2DStrokeHitQuery(
            int queryCapacity,
            int layerMask,
            bool includeTriggers,
            Transform referenceSpaceTransform = null)
        {
            if (queryCapacity < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(queryCapacity),
                    "Physics query capacity must include result and saturation room.");
            }

            hitBuffer = new RaycastHit2D[queryCapacity];
            referenceSpace = referenceSpaceTransform;
            contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = layerMask,
                useTriggers = includeTriggers
            };
        }

        public int QuerySegment(
            Vector2 startReferencePixels,
            Vector2 endReferencePixels,
            float radiusReferencePixels,
            StrokeHitCandidate[] results)
        {
            ValidatePoint(startReferencePixels, nameof(startReferencePixels));
            ValidatePoint(endReferencePixels, nameof(endReferencePixels));
            if (float.IsNaN(radiusReferencePixels) ||
                float.IsInfinity(radiusReferencePixels) ||
                radiusReferencePixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radiusReferencePixels),
                    "Stroke radius must be finite and positive.");
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (results.Length < hitBuffer.Length)
            {
                throw new ArgumentException(
                    $"Candidate buffer must contain at least {hitBuffer.Length} entries.",
                    nameof(results));
            }

            Vector2 startWorld = TransformPoint(startReferencePixels);
            Vector2 endWorld = TransformPoint(endReferencePixels);
            Vector2 segment = endWorld - startWorld;
            float distance = segment.magnitude;
            if (distance <= 0f)
            {
                return 0;
            }

            float worldRadius = radiusReferencePixels * MaximumReferenceScale();
            int rawHitCount = Physics2D.CircleCast(
                startWorld,
                worldRadius,
                segment / distance,
                contactFilter,
                hitBuffer,
                distance);
            if (rawHitCount >= hitBuffer.Length)
            {
                throw new InvalidOperationException(
                    "Physics2D stroke query saturated its configured collider buffer.");
            }

            int resultCount = 0;
            for (int index = 0; index < rawHitCount; index++)
            {
                Collider2D collider = hitBuffer[index].collider;
                if (collider == null || !TryResolveTarget(
                        collider,
                        out IHittable target,
                        out bool isWeakpoint))
                {
                    continue;
                }

                results[resultCount++] = new StrokeHitCandidate(
                    target,
                    isWeakpoint,
                    Mathf.Clamp01(hitBuffer[index].fraction));
            }

            return resultCount;
        }

        private bool TryResolveTarget(
            Collider2D collider,
            out IHittable target,
            out bool isWeakpoint)
        {
            Component hitboxComponent = collider.GetComponent(HitboxType) ??
                                        collider.GetComponentInParent(HitboxType, true);
            if (hitboxComponent is IStrokeHitbox hitbox)
            {
                target = hitbox.HitTarget;
                isWeakpoint = hitbox.IsWeakpoint;
                return hitbox.IsStrokeHitboxActive && !IsUnityNull(target);
            }

            Component targetComponent = collider.GetComponent(HittableType) ??
                                        collider.GetComponentInParent(HittableType, true);
            target = targetComponent as IHittable;
            isWeakpoint = false;
            return !IsUnityNull(target);
        }

        private Vector2 TransformPoint(Vector2 referencePoint)
        {
            if (referenceSpace == null)
            {
                return referencePoint;
            }

            Vector3 world = referenceSpace.TransformPoint(
                new Vector3(referencePoint.x, referencePoint.y, 0f));
            return new Vector2(world.x, world.y);
        }

        private float MaximumReferenceScale()
        {
            if (referenceSpace == null)
            {
                return 1f;
            }

            Vector3 scale = referenceSpace.lossyScale;
            float maximumScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            if (maximumScale <= 0f || float.IsNaN(maximumScale) || float.IsInfinity(maximumScale))
            {
                throw new InvalidOperationException(
                    "Reference space must have finite non-zero X/Y scale.");
            }

            return maximumScale;
        }

        private static bool IsUnityNull(IHittable target)
        {
            if (target == null)
            {
                return true;
            }

            return target is UnityEngine.Object unityObject && unityObject == null;
        }

        private static void ValidatePoint(Vector2 point, string parameterName)
        {
            if (float.IsNaN(point.x) || float.IsInfinity(point.x) ||
                float.IsNaN(point.y) || float.IsInfinity(point.y))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Stroke query points must be finite.");
            }
        }
    }
}
