using System;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    /// <summary>把投射物 Collider 暴露为稳定 IHittable，并把命中转发给控制器。</summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileHitTarget : MonoBehaviour, IHittable
    {
        private ProjectileController controller;

        /// <summary>获取当前投射物生命周期内的目标 ID。</summary>
        public int HitTargetId { get; private set; }

        /// <summary>获取敌方活动投射物当前是否接受玩家笔迹。</summary>
        public bool CanReceiveStrokeHit =>
            HitTargetId != 0 && controller != null && controller.CanReceiveStrokeHit;

        /// <summary>获取所属投射物控制器。</summary>
        public ProjectileController Controller => controller;

        /// <summary>验证命中记录属于本目标，并交给控制器解析切断或反弹。</summary>
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

        /// <summary>组件初始化时建立控制器反向引用，不分配目标 ID。</summary>
        internal void AttachController(ProjectileController projectileController)
        {
            controller = projectileController;
        }

        /// <summary>投射物生成时绑定控制器和非零生命周期目标 ID。</summary>
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

        /// <summary>回收时清除目标 ID，使旧命中记录立即失效。</summary>
        internal void ResetRuntimeState()
        {
            HitTargetId = 0;
        }
    }
}
