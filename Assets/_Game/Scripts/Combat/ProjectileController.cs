using System;
using OneStrokeDemon.Core;
using UnityEngine;

namespace OneStrokeDemon.Combat
{
    /// <summary>投射物离开活动状态的原因。</summary>
    public enum ProjectileReleaseReason
    {
        None = 0,
        Cut = 1,
        Impact = 2,
        LifetimeExpired = 3,
        Manual = 4
    }

    /// <summary>保存投射物回收前的 ID、归属、位置和寿命快照。</summary>
    public readonly struct ProjectileReleaseSnapshot
    {
        /// <summary>创建有效回收快照。</summary>
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

        // 默认结构 IsValid=false，调用方可区分“本次没有发生回收”。
        public ProjectileReleaseReason Reason { get; }

        public string ProjectileId { get; }

        public int HitTargetId { get; }

        public ProjectileOwnership Ownership { get; }

        public Vector2 ReferencePosition { get; }

        public float ElapsedSeconds { get; }

        public bool IsValid { get; }
    }

    /// <summary>保存一笔切弹交互前后的归属、方向和可选回收事实。</summary>
    public readonly struct ProjectileStrokeResult
    {
        /// <summary>创建有效切弹结果。</summary>
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

        // 前后快照让反馈和测试无需读取已可能回收的 MonoBehaviour 状态。
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

    /// <summary>保存投射物有效撞击目标时的伤害来源和回收事实。</summary>
    public readonly struct ProjectileImpactResult
    {
        /// <summary>创建有效撞击结果。</summary>
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

        // 结果在控制器清空状态后仍保留完整伤害归属。
        public ProjectileOwner Target { get; }

        public ProjectileDamageSource DamageSource { get; }

        public ProjectileReleaseSnapshot Release { get; }

        public bool IsValid { get; }
    }

    /// <summary>管理配置投射物的确定移动、阵营、切弹、撞击和对象池重置生命周期。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProjectileHitTarget), typeof(CircleCollider2D))]
    public sealed class ProjectileController : MonoBehaviour, IPoolable
    {
        private ProjectileHitTarget hitTarget;
        private CircleCollider2D hitCollider;
        private ProjectileRuleSet rules;
        private ProjectileOwnership ownership;
        private Transform poolParent;
        private Transform referenceSpace;
        private Vector2 referencePosition;
        private Vector2 travelDirection;
        private float elapsedSeconds;
        private PoolLease poolLease;
        private bool hasPoolParent;
        private bool isActive;

        /// <summary>获取投射物玩法生命周期是否活动。</summary>
        public bool IsActive => isActive;

        /// <summary>获取对象是否持有有效池租约。</summary>
        public bool IsPoolActive => poolLease.IsValid;

        /// <summary>获取当前是否为可被玩家笔迹处理的敌方投射物。</summary>
        public bool CanReceiveStrokeHit =>
            isActive && ownership.CurrentOwner.Faction == ProjectileFaction.Enemy;

        /// <summary>获取当前规则快照。</summary>
        public ProjectileRuleSet Rules => rules;

        /// <summary>获取当前与原始归属快照。</summary>
        public ProjectileOwnership Ownership => ownership;

        /// <summary>获取参考像素空间根。</summary>
        public Transform ReferenceSpace => referenceSpace;

        /// <summary>获取参考像素位置。</summary>
        public Vector2 ReferencePosition => referencePosition;

        /// <summary>获取归一化运动方向。</summary>
        public Vector2 TravelDirection => travelDirection;

        /// <summary>获取当前生命周期已推进秒数。</summary>
        public float ElapsedSeconds => elapsedSeconds;

        /// <summary>获取并确保存在命中目标组件。</summary>
        public ProjectileHitTarget HitTarget
        {
            get
            {
                EnsureComponents();
                return hitTarget;
            }
        }

        /// <summary>获取并确保存在圆形碰撞体。</summary>
        public CircleCollider2D HitCollider
        {
            get
            {
                EnsureComponents();
                return hitCollider;
            }
        }

        /// <summary>缓存必需组件并把未生成对象的碰撞体保持禁用。</summary>
        private void Awake()
        {
            EnsureComponents();
            hitCollider.isTrigger = true;
            if (!isActive)
            {
                hitCollider.enabled = false;
            }
        }

        /// <summary>活动时使用 Unity 传入的 delta 推进确定运动。</summary>
        private void Update()
        {
            if (isActive)
            {
                Tick(Time.deltaTime);
            }
        }

        /// <summary>对象被外部禁用时清空运行状态，防止池外残留。</summary>
        private void OnDisable()
        {
            if (isActive)
            {
                ClearRuntimeState(resetTransform: false);
            }
        }

        /// <summary>用完整规则、目标 ID、归属、参考空间、位置和方向生成投射物。</summary>
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

            // 生成时完整覆盖所有可变运行状态，不能依赖上一个池生命周期的值。
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

        /// <summary>按外部非负时间推进参考像素位置，并在寿命边界自动回收。</summary>
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

            // 超大 delta 只推进剩余寿命对应的位移，避免越过配置生命终点。
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

        /// <summary>若当前归属可伤目标，则冻结伤害来源并以 Impact 原因回收。</summary>
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

        /// <summary>以明确原因冻结回收快照、清空状态并停用对象。</summary>
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

            // 快照必须在清空 rules、目标 ID 和归属之前创建。
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

        /// <summary>取得新池租约，记住池父级并将对象恢复为干净待生成状态。</summary>
        public void AcquireFromPool(in PoolLease lease)
        {
            if (!lease.IsValid)
            {
                throw new ArgumentException("A valid pool lease is required.", nameof(lease));
            }

            if (poolLease.IsValid || isActive)
            {
                throw new InvalidOperationException(
                    "Projectile must be fully released before acquiring another pool lease.");
            }

            poolParent = transform.parent;
            hasPoolParent = true;
            poolLease = lease;
            ClearRuntimeState(resetTransform: true);
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        /// <summary>校验租约、必要时手动回收，并完整清理后归还对象池。</summary>
        public void ReleaseToPool(in PoolReleaseContext context)
        {
            if (!hasPoolParent)
            {
                poolParent = transform.parent;
                hasPoolParent = true;
            }

            if (context.Lease.IsValid && poolLease.IsValid && context.Lease != poolLease)
            {
                throw new InvalidOperationException("Projectile pool release used a stale lease.");
            }

            if (isActive)
            {
                Release(ProjectileReleaseReason.Manual);
            }

            ClearRuntimeState(resetTransform: true);
            poolLease = default;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>解析一笔对活动投射物的交互，并原子应用反弹或切断。</summary>
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

            // 先保留前态；反弹改变归属与方向，切断则在创建结果前回收。
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

        /// <summary>缓存并验证必需组件，同时建立命中目标到控制器的引用。</summary>
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

        /// <summary>把参考像素位置与运动方向同步到局部 Transform。</summary>
        private void ApplyTransform()
        {
            transform.localPosition = new Vector3(referencePosition.x, referencePosition.y, 0f);
            transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg);
        }

        /// <summary>清除全部玩法、碰撞和可选 Transform 状态，供回收与异常禁用复用。</summary>
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

            // Transform 重置时回到记录的池父级，避免跨会话继承参考空间。
            if (resetTransform)
            {
                transform.SetParent(hasPoolParent ? poolParent : null, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }
        }

        /// <summary>验证二维向量的两个分量均为有限值。</summary>
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
