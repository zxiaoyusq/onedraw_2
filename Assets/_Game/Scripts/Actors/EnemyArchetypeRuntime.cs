using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Actors
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable), typeof(EnemyController))]
    // 定义 EnemyArchetypeActor 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class EnemyArchetypeActor : MonoBehaviour, IPoolable
    {
        private EnemyController controller;
        private EnemyStrategyRuntime strategy;
        private EnemyArchetypeDefinition archetype;
        private UnityObject sourceAsset;
        private PoolLease poolLease;

        public bool IsPoolActive => poolLease.IsValid && Controller.IsPoolActive;

        public EnemyController Controller
        {
            get
            {
                EnsureController();
                return controller;
            }
        }

        public EnemyStrategyRuntime Strategy => strategy ??
            throw new InvalidOperationException("Enemy archetype strategy is not active.");

        public EnemyArchetypeDefinition Archetype => archetype;

        public UnityObject SourceAsset => sourceAsset;

        public string AssetKey => archetype.IsConfigured
            ? archetype.Enemy.AssetKey
            : string.Empty;

        public string AssetType => archetype.IsConfigured
            ? archetype.AssetType
            : string.Empty;

        // 处理 BindAsset 对应的角色逻辑，并返回或发布一致的状态结果。
        internal void BindAsset(
            in EnemyArchetypeDefinition configuredArchetype,
            UnityObject configuredSourceAsset)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!configuredArchetype.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy archetype must be configured.",
                    nameof(configuredArchetype));
            }

            archetype = configuredArchetype;
            sourceAsset = configuredSourceAsset ??
                throw new ArgumentNullException(nameof(configuredSourceAsset));
            EnsureController();
        }

        // 生成 Spawn 对应的角色逻辑，并返回或发布一致的状态结果。
        internal void Spawn(
            IConfigProvider configProvider,
            int hitTargetId,
            double timestamp,
            IEnemyAttackWorld attackWorld)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!IsPoolActive)
            {
                throw new InvalidOperationException(
                    "Enemy archetype must be acquired from its pool before spawn.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!archetype.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Enemy archetype asset must be bound before spawn.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (strategy != null || Controller.IsSpawned)
            {
                throw new InvalidOperationException(
                    "Active enemy archetype must be released before reuse.");
            }

            controller.Spawn(
                configProvider,
                archetype.Enemy.EnemyId,
                hitTargetId,
                timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!controller.CompleteSpawn(timestamp))
            {
                throw new InvalidOperationException(
                    $"Enemy '{archetype.Enemy.EnemyId}' did not complete its spawn transition.");
            }

            strategy = new EnemyStrategyRuntime(
                controller,
                configProvider,
                attackWorld);
            ApplyMovement(strategy.SampleMovement(0d));
        }

        // 处理 AdvanceMovement 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyMovementSample AdvanceMovement(double movementElapsedSeconds)
        {
            EnemyMovementSample sample = Strategy.SampleMovement(movementElapsedSeconds);
            ApplyMovement(sample);
            return sample;
        }

        // 尝试执行 TryBeginAttack 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool TryBeginAttack(
            in EnemyAttackTriggerContext context,
            double unitSelection,
            double timestamp)
        {
            return Strategy.TryBeginAttack(context, unitSelection, timestamp);
        }

        // 按时间推进 Tick 对应的角色逻辑，并返回或发布一致的状态结果。
        public int Tick(double timestamp)
        {
            return Strategy.Tick(timestamp);
        }

        // 处理 OwnsLease 对应的角色逻辑，并返回或发布一致的状态结果。
        public bool OwnsLease(in PoolLease lease)
        {
            return poolLease.IsValid && poolLease == lease;
        }

        // 处理 AcquireFromPool 对应的角色逻辑，并返回或发布一致的状态结果。
        public void AcquireFromPool(in PoolLease lease)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!lease.IsValid)
            {
                throw new ArgumentException("A valid pool lease is required.", nameof(lease));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (poolLease.IsValid || strategy != null || Controller.IsSpawned)
            {
                throw new InvalidOperationException(
                    "Enemy archetype must be fully released before another acquisition.");
            }

            poolLease = lease;
            try
            {
                controller.AcquireFromPool(lease);
            }
            catch
            {
                poolLease = default;
                throw;
            }
        }

        // 释放 ReleaseToPool 对应的角色逻辑，并返回或发布一致的状态结果。
        public void ReleaseToPool(in PoolReleaseContext context)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (context.Lease.IsValid && poolLease.IsValid && context.Lease != poolLease)
            {
                throw new InvalidOperationException(
                    "Enemy archetype pool release used a stale lease.");
            }

            strategy?.Dispose();
            strategy = null;
            Controller.ReleaseToPool(context);
            poolLease = default;
        }

        // 应用 ApplyMovement 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ApplyMovement(in EnemyMovementSample sample)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!sample.IsValid)
            {
                throw new ArgumentException(
                    "Enemy movement sample must be configured.",
                    nameof(sample));
            }

            transform.localPosition = new Vector3(
                (float)sample.XReferencePixels,
                (float)sample.YReferencePixels,
                0f);
        }

        // 处理 EnsureController 对应的角色逻辑，并返回或发布一致的状态结果。
        private void EnsureController()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (controller == null)
            {
                controller = GetComponent<EnemyController>();
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (controller == null)
            {
                throw new InvalidOperationException(
                    "EnemyArchetypeActor requires an EnemyController component.");
            }
        }
    }

    // 定义 EnemyArchetypeSpawnResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyArchetypeSpawnResult
    {
        // 初始化 EnemyArchetypeSpawnResult，并建立角色运行时所需的初始状态。
        internal EnemyArchetypeSpawnResult(
            PoolAcquireStatus status,
            EnemyArchetypeActor actor,
            in PoolLease lease,
            bool reusedOldest)
        {
            Status = status;
            Actor = actor;
            Lease = lease;
            ReusedOldest = reusedOldest;
        }

        public PoolAcquireStatus Status { get; }

        public EnemyArchetypeActor Actor { get; }

        public PoolLease Lease { get; }

        public bool ReusedOldest { get; }

        public bool IsSpawned =>
            Status == PoolAcquireStatus.Acquired &&
            Actor != null &&
            Actor.OwnsLease(Lease) &&
            Actor.Controller.IsSpawned;
    }

    // 定义 EnemyArchetypePool 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class EnemyArchetypePool : IDisposable
    {
        private readonly IConfigProvider configProvider;
        private readonly IAssetRegistry assetRegistry;
        private readonly Transform poolRoot;
        private readonly ObjectPoolService poolService = new ObjectPoolService();
        private readonly IReadOnlyList<EnemyArchetypeDefinition> archetypes;
        private readonly Dictionary<string, EnemyArchetypeDefinition> archetypesById =
            new Dictionary<string, EnemyArchetypeDefinition>(StringComparer.Ordinal);
        private bool disposed;

        // 初始化 EnemyArchetypePool，并建立角色运行时所需的初始状态。
        public EnemyArchetypePool(
            IConfigProvider configuredProvider,
            IAssetRegistry configuredRegistry,
            Transform configuredPoolRoot = null)
        {
            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            assetRegistry = configuredRegistry ??
                throw new ArgumentNullException(nameof(configuredRegistry));
            poolRoot = configuredPoolRoot;
            archetypes = EnemyArchetypeCatalog.CreateCombatRoster(configProvider);
            poolService.RegisterFamily(
                ObjectPoolConfiguration.CreateEnemyFamily(configProvider));
            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            for (int index = 0; index < archetypes.Count; index++)
            {
                EnemyArchetypeDefinition archetype = archetypes[index];
                archetypesById.Add(archetype.Enemy.EnemyId, archetype);
                EnemyArchetypeDefinition captured = archetype;
                poolService.RegisterPool(ObjectPoolConfiguration.CreateEnemyPool(
                    configProvider,
                    captured.Enemy.EnemyId,
                    () => CreatePooledActor(captured)));
            }
        }

        public IReadOnlyList<EnemyArchetypeDefinition> Archetypes => archetypes;

        public PoolServiceSnapshot Snapshot => poolService.GetSnapshot();

        // 生成 Spawn 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyArchetypeSpawnResult Spawn(
            string enemyId,
            int hitTargetId,
            double timestamp,
            IEnemyAttackWorld attackWorld)
        {
            ThrowIfDisposed();
            ValidateTimestamp(timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!archetypesById.TryGetValue(
                    enemyId ?? string.Empty,
                    out EnemyArchetypeDefinition archetype))
            {
                throw new KeyNotFoundException(
                    $"Enemy archetype '{enemyId ?? "<null>"}' is not in the non-Boss combat roster.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (attackWorld == null)
            {
                throw new ArgumentNullException(nameof(attackWorld));
            }

            PoolAcquireResult acquired = poolService.Acquire(
                ObjectPoolConfiguration.GetEnemyPoolId(archetype.Enemy.EnemyId));
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!acquired.IsAcquired)
            {
                return new EnemyArchetypeSpawnResult(
                    acquired.Status,
                    null,
                    default,
                    acquired.ReusedOldest);
            }

            var actor = (EnemyArchetypeActor)acquired.Item;
            try
            {
                actor.Spawn(configProvider, hitTargetId, timestamp, attackWorld);
            }
            catch
            {
                poolService.Release(actor, acquired.Lease, PoolReleaseReason.Manual);
                throw;
            }

            return new EnemyArchetypeSpawnResult(
                acquired.Status,
                actor,
                acquired.Lease,
                acquired.ReusedOldest);
        }

        // 释放 Release 对应的角色逻辑，并返回或发布一致的状态结果。
        public PoolReleaseResult Release(
            in EnemyArchetypeSpawnResult spawned,
            PoolReleaseReason reason = PoolReleaseReason.Completed)
        {
            ThrowIfDisposed();
            return poolService.Release(spawned.Actor, spawned.Lease, reason);
        }

        // 处理 Restart 对应的角色逻辑，并返回或发布一致的状态结果。
        public PoolRestartReport Restart()
        {
            ThrowIfDisposed();
            return poolService.Restart();
        }

        // 处理 DetectLeaks 对应的角色逻辑，并返回或发布一致的状态结果。
        public PoolLeakReport DetectLeaks()
        {
            ThrowIfDisposed();
            return poolService.DetectLeaks();
        }

        // 处理 AssertNoLeaks 对应的角色逻辑，并返回或发布一致的状态结果。
        public void AssertNoLeaks()
        {
            ThrowIfDisposed();
            poolService.AssertNoLeaks();
        }

        // 获取 GetAllocatedCount 对应的角色逻辑，并返回或发布一致的状态结果。
        public int GetAllocatedCount(string enemyId)
        {
            ThrowIfDisposed();
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!archetypesById.ContainsKey(enemyId ?? string.Empty))
            {
                throw new KeyNotFoundException(
                    $"Enemy archetype '{enemyId ?? "<null>"}' is not registered.");
            }

            return poolService.GetPoolAllocatedCount(
                ObjectPoolConfiguration.GetEnemyPoolId(enemyId));
        }

        // 释放 Dispose 对应的角色逻辑，并返回或发布一致的状态结果。
        public void Dispose()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (disposed)
            {
                return;
            }

            poolService.Dispose();
            disposed = true;
        }

        // 创建 CreatePooledActor 对应的角色逻辑，并返回或发布一致的状态结果。
        private EnemyArchetypeActor CreatePooledActor(
            in EnemyArchetypeDefinition archetype)
        {
            UnityObject source;
            GameObject instance;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.Equals(archetype.AssetType, "Sprite", StringComparison.Ordinal))
            {
                Sprite sprite = assetRegistry.GetSprite(archetype.Enemy.AssetKey);
                source = sprite;
                instance = new GameObject($"Enemy {archetype.Enemy.EnemyId}");
                instance.SetActive(false);
                SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
            }
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            else if (string.Equals(archetype.AssetType, "Prefab", StringComparison.Ordinal))
            {
                GameObject prefab = assetRegistry.GetPrefab(archetype.Enemy.AssetKey);
                source = prefab;
                instance = UnityObject.Instantiate(prefab, poolRoot, false);
                instance.name = $"Enemy {archetype.Enemy.EnemyId}";
                instance.SetActive(false);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Enemy '{archetype.Enemy.EnemyId}' has unsupported asset type '{archetype.AssetType}'.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (poolRoot != null && instance.transform.parent != poolRoot)
            {
                instance.transform.SetParent(poolRoot, false);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (instance.GetComponent<Damageable>() == null)
            {
                instance.AddComponent<Damageable>();
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (instance.GetComponent<EnemyController>() == null)
            {
                instance.AddComponent<EnemyController>();
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (archetype.Enemy.Weakpoint.HasHitbox &&
                instance.GetComponentInChildren<WeakpointController>(true) == null)
            {
                var weakpointObject = new GameObject("Weakpoint");
                weakpointObject.transform.SetParent(instance.transform, false);
                weakpointObject.AddComponent<CircleCollider2D>();
                weakpointObject.AddComponent<WeakpointController>();
            }

            EnemyArchetypeActor actor = instance.GetComponent<EnemyArchetypeActor>() ??
                instance.AddComponent<EnemyArchetypeActor>();
            actor.BindAsset(archetype, source);
            return actor;
        }

        // 校验 ValidateTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateTimestamp(double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(timestamp) ||
                double.IsInfinity(timestamp) ||
                timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    "Enemy spawn timestamp must be finite and non-negative.");
            }
        }

        // 处理 ThrowIfDisposed 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ThrowIfDisposed()
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(EnemyArchetypePool));
            }
        }
    }
}
