using System;
using OneStrokeDemon.Combat;
using UnityEngine;

namespace OneStrokeDemon.Actors
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    // 定义 WeakpointController 的角色领域数据与行为边界，供上层流程以明确契约使用。
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

        // 处理 Awake 对应的角色逻辑，并返回或发布一致的状态结果。
        private void Awake()
        {
            EnsureCollider();
            CloseWindow();
        }

        // 响应 OnDisable 对应的角色逻辑，并返回或发布一致的状态结果。
        private void OnDisable()
        {
            CloseWindow();
        }

        // 处理 Configure 对应的角色逻辑，并返回或发布一致的状态结果。
        internal void Configure(
            in EnemyWeakpointDefinition configuredDefinition,
            Damageable configuredHitTarget)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 开始 BeginAttack 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 按时间推进 Tick 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 处理 EndAttack 对应的角色逻辑，并返回或发布一致的状态结果。
        internal bool EndAttack()
        {
            bool changed = attackCycleActive || windowOpen;
            attackCycleActive = false;
            CloseWindow();
            return changed;
        }

        // 释放 Release 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 设置 SetWindow 对应的角色逻辑，并返回或发布一致的状态结果。
        private void SetWindow(bool open)
        {
            windowOpen = open && definition.HasHitbox;
            EnsureCollider();
            hitCollider.enabled = windowOpen;
        }

        // 处理 CloseWindow 对应的角色逻辑，并返回或发布一致的状态结果。
        private void CloseWindow()
        {
            windowOpen = false;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (hitCollider != null)
            {
                hitCollider.enabled = false;
            }
        }

        // 处理 EnsureCollider 对应的角色逻辑，并返回或发布一致的状态结果。
        private void EnsureCollider()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (hitCollider == null)
            {
                hitCollider = GetComponent<CircleCollider2D>();
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (hitCollider == null)
            {
                throw new InvalidOperationException(
                    "WeakpointController requires a CircleCollider2D.");
            }
        }

        // 处理 RequireConfigured 对应的角色逻辑，并返回或发布一致的状态结果。
        private void RequireConfigured()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!definition.IsConfigured || hitTarget == null)
            {
                throw new InvalidOperationException(
                    "WeakpointController must be configured before use.");
            }
        }

        // 处理 ObserveTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ObserveTimestamp(double timestamp)
        {
            ValidateTimestamp(timestamp, nameof(timestamp));
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 校验 ValidateTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateTimestamp(double timestamp, string parameterName)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
