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

        internal void BindAsset(
            in EnemyArchetypeDefinition configuredArchetype,
            UnityObject configuredSourceAsset)
        {
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

        internal void Spawn(
            IConfigProvider configProvider,
            int hitTargetId,
            double timestamp,
            IEnemyAttackWorld attackWorld)
        {
            if (!IsPoolActive)
            {
                throw new InvalidOperationException(
                    "Enemy archetype must be acquired from its pool before spawn.");
            }

            if (!archetype.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Enemy archetype asset must be bound before spawn.");
            }

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

        public EnemyMovementSample AdvanceMovement(double movementElapsedSeconds)
        {
            EnemyMovementSample sample = Strategy.SampleMovement(movementElapsedSeconds);
            ApplyMovement(sample);
            return sample;
        }

        public bool TryBeginAttack(
            in EnemyAttackTriggerContext context,
            double unitSelection,
            double timestamp)
        {
            return Strategy.TryBeginAttack(context, unitSelection, timestamp);
        }

        public int Tick(double timestamp)
        {
            return Strategy.Tick(timestamp);
        }

        public bool OwnsLease(in PoolLease lease)
        {
            return poolLease.IsValid && poolLease == lease;
        }

        public void AcquireFromPool(in PoolLease lease)
        {
            if (!lease.IsValid)
            {
                throw new ArgumentException("A valid pool lease is required.", nameof(lease));
            }

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

        public void ReleaseToPool(in PoolReleaseContext context)
        {
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

        private void ApplyMovement(in EnemyMovementSample sample)
        {
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

        private void EnsureController()
        {
            if (controller == null)
            {
                controller = GetComponent<EnemyController>();
            }

            if (controller == null)
            {
                throw new InvalidOperationException(
                    "EnemyArchetypeActor requires an EnemyController component.");
            }
        }
    }

    public readonly struct EnemyArchetypeSpawnResult
    {
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

        public EnemyArchetypeSpawnResult Spawn(
            string enemyId,
            int hitTargetId,
            double timestamp,
            IEnemyAttackWorld attackWorld)
        {
            ThrowIfDisposed();
            ValidateTimestamp(timestamp);
            if (!archetypesById.TryGetValue(
                    enemyId ?? string.Empty,
                    out EnemyArchetypeDefinition archetype))
            {
                throw new KeyNotFoundException(
                    $"Enemy archetype '{enemyId ?? "<null>"}' is not in the non-Boss combat roster.");
            }

            if (attackWorld == null)
            {
                throw new ArgumentNullException(nameof(attackWorld));
            }

            PoolAcquireResult acquired = poolService.Acquire(
                ObjectPoolConfiguration.GetEnemyPoolId(archetype.Enemy.EnemyId));
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

        public PoolReleaseResult Release(
            in EnemyArchetypeSpawnResult spawned,
            PoolReleaseReason reason = PoolReleaseReason.Completed)
        {
            ThrowIfDisposed();
            return poolService.Release(spawned.Actor, spawned.Lease, reason);
        }

        public PoolRestartReport Restart()
        {
            ThrowIfDisposed();
            return poolService.Restart();
        }

        public PoolLeakReport DetectLeaks()
        {
            ThrowIfDisposed();
            return poolService.DetectLeaks();
        }

        public void AssertNoLeaks()
        {
            ThrowIfDisposed();
            poolService.AssertNoLeaks();
        }

        public int GetAllocatedCount(string enemyId)
        {
            ThrowIfDisposed();
            if (!archetypesById.ContainsKey(enemyId ?? string.Empty))
            {
                throw new KeyNotFoundException(
                    $"Enemy archetype '{enemyId ?? "<null>"}' is not registered.");
            }

            return poolService.GetPoolAllocatedCount(
                ObjectPoolConfiguration.GetEnemyPoolId(enemyId));
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            poolService.Dispose();
            disposed = true;
        }

        private EnemyArchetypeActor CreatePooledActor(
            in EnemyArchetypeDefinition archetype)
        {
            UnityObject source;
            GameObject instance;
            if (string.Equals(archetype.AssetType, "Sprite", StringComparison.Ordinal))
            {
                Sprite sprite = assetRegistry.GetSprite(archetype.Enemy.AssetKey);
                source = sprite;
                instance = new GameObject($"Enemy {archetype.Enemy.EnemyId}");
                instance.SetActive(false);
                SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
            }
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

            if (poolRoot != null && instance.transform.parent != poolRoot)
            {
                instance.transform.SetParent(poolRoot, false);
            }

            if (instance.GetComponent<Damageable>() == null)
            {
                instance.AddComponent<Damageable>();
            }

            if (instance.GetComponent<EnemyController>() == null)
            {
                instance.AddComponent<EnemyController>();
            }

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

        private static void ValidateTimestamp(double timestamp)
        {
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

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(EnemyArchetypePool));
            }
        }
    }
}
