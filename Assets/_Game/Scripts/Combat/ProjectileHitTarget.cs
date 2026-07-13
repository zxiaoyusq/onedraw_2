using System;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    [DisallowMultipleComponent]
    public sealed class ProjectileHitTarget : MonoBehaviour, IHittable
    {
        private ProjectileController controller;

        public int HitTargetId { get; private set; }

        public bool CanReceiveStrokeHit =>
            HitTargetId != 0 && controller != null && controller.CanReceiveStrokeHit;

        public ProjectileController Controller => controller;

        public ProjectileStrokeResult ResolveStrokeHit(
            in HitRecord hit,
            string stanceId,
            ProjectileOwner reflector)
        {
            if (!ReferenceEquals(hit.Target, this) || hit.TargetId != HitTargetId)
            {
                throw new ArgumentException(
                    "Hit record does not describe this projectile target.",
                    nameof(hit));
            }

            if (!CanReceiveStrokeHit)
            {
                throw new InvalidOperationException(
                    "Projectile target is not accepting stroke hits.");
            }

            return controller.ResolveStroke(
                hit.StrokeId,
                hit.TargetId,
                stanceId,
                reflector);
        }

        internal void AttachController(ProjectileController projectileController)
        {
            controller = projectileController;
        }

        internal void Attach(ProjectileController projectileController, int hitTargetId)
        {
            if (projectileController == null)
            {
                throw new ArgumentNullException(nameof(projectileController));
            }

            if (hitTargetId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hitTargetId),
                    "Projectile hit target id must be non-zero.");
            }

            controller = projectileController;
            HitTargetId = hitTargetId;
        }

        internal void ResetRuntimeState()
        {
            HitTargetId = 0;
        }
    }
}
