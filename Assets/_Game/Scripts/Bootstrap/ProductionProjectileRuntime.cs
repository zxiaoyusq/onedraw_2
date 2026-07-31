using System;
using System.Collections.Generic;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    /// <summary>
    /// 在生产参考像素空间中生成、显示、推进并回收配置投射物，同时处理玩家碰撞和真实笔迹交互。
    /// </summary>
    internal sealed class ProductionProjectileRuntime : IDisposable
    {
        private const int PlayerOwnerEntityId = -1;
        private const int FirstProjectileHitTargetId = -2;

        private readonly IConfigProvider config;
        private readonly IAssetRegistry assets;
        private readonly PlayerCombatController player;
        private readonly Collider2D playerBody;
        private readonly Transform referenceRoot;
        private readonly Transform poolRoot;
        private readonly ObjectPoolService pool = new ObjectPoolService();
        private readonly Dictionary<string, ProjectileRuleSet> rules =
            new Dictionary<string, ProjectileRuleSet>(StringComparer.Ordinal);
        private readonly HashSet<string> registeredPools =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly List<ActiveProjectile> active = new List<ActiveProjectile>();
        private readonly ProjectileOwner playerOwner =
            new ProjectileOwner(ProjectileFaction.Player, PlayerOwnerEntityId);
        private int nextHitTargetId = FirstProjectileHitTargetId;
        private double lastGameplayTimestamp;
        private bool hasGameplayClock;
        private bool disposed;

        /// <summary>创建只消费配置与资源注册表的生产投射物运行时。</summary>
        public ProductionProjectileRuntime(
            IConfigProvider configProvider,
            IAssetRegistry assetRegistry,
            PlayerCombatController playerController,
            Collider2D configuredPlayerBody,
            Transform configuredReferenceRoot)
        {
            config = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            assets = assetRegistry ?? throw new ArgumentNullException(nameof(assetRegistry));
            player = playerController ?? throw new ArgumentNullException(nameof(playerController));
            playerBody = configuredPlayerBody ??
                throw new ArgumentNullException(nameof(configuredPlayerBody));
            referenceRoot = configuredReferenceRoot ??
                throw new ArgumentNullException(nameof(configuredReferenceRoot));
            poolRoot = new GameObject("Production Projectile Pool").transform;
            poolRoot.SetParent(referenceRoot, false);
            pool.RegisterFamily(ObjectPoolConfiguration.CreateProjectileFamily(config));
        }

        /// <summary>获取当前仍处于活动生命周期的真实投射物数量。</summary>
        public int ActiveCount => active.Count;

        /// <summary>返回指定索引处的活动投射物，供生产集成测试读取可见状态。</summary>
        public ProjectileController GetActiveProjectile(int index)
        {
            ThrowIfDisposed();
            if (index < 0 || index >= active.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return active[index].Controller;
        }

        /// <summary>从攻击者当前位置朝玩家当前位置生成一个敌方归属投射物。</summary>
        public bool TrySpawn(
            string projectileId,
            EnemyController source,
            double gameplayTimestamp)
        {
            ThrowIfDisposed();
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!source.IsAlive || string.IsNullOrWhiteSpace(projectileId))
            {
                return false;
            }

            SynchronizeClock(gameplayTimestamp);
            ProjectileRuleSet rule = GetRules(projectileId);
            EnsurePoolRegistered(rule);
            PoolAcquireResult acquired = pool.Acquire(
                ObjectPoolConfiguration.GetProjectilePoolId(rule.ProjectileId));
            if (!acquired.IsAcquired)
            {
                return false;
            }

            var controller = acquired.Item as ProjectileController;
            if (controller == null)
            {
                pool.Release(acquired.Item, acquired.Lease, PoolReleaseReason.Manual);
                throw new InvalidOperationException(
                    $"Projectile pool '{rule.ProjectileId}' returned an unexpected item type.");
            }

            RemoveReusedOrStaleEntries(controller);
            Vector2 start = referenceRoot.InverseTransformPoint(source.transform.position);
            Vector2 target = referenceRoot.InverseTransformPoint(player.transform.position);
            Vector2 direction = target - start;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector2.left;
            }

            int hitTargetId = TakeNextHitTargetId();
            try
            {
                controller.Spawn(
                    rule,
                    hitTargetId,
                    new ProjectileOwner(
                        ProjectileFaction.Enemy,
                        source.Damage.HitTargetId),
                    referenceRoot,
                    start,
                    direction);
            }
            catch
            {
                pool.Release(controller, acquired.Lease, PoolReleaseReason.Manual);
                throw;
            }

            active.Add(new ActiveProjectile(controller, acquired.Lease));
            return true;
        }

        /// <summary>
        /// 按玩法时钟推进投射物；敌方弹体命中玩家才扣血，玩家归属弹体交由调用方解析敌人命中。
        /// </summary>
        public void Advance(
            double gameplayTimestamp,
            Func<ProjectileController, double, bool> reflectedImpactResolver)
        {
            ThrowIfDisposed();
            float deltaSeconds = SynchronizeClock(gameplayTimestamp);
            RemoveReusedOrStaleEntries(null);

            for (int index = active.Count - 1; index >= 0; index--)
            {
                ActiveProjectile item = active[index];
                ProjectileReleaseSnapshot release = item.Controller.Tick(deltaSeconds);
                if (release.IsValid)
                {
                    ReleaseAt(index, PoolReleaseReason.Completed);
                }
            }

            Physics2D.SyncTransforms();
            for (int index = active.Count - 1; index >= 0; index--)
            {
                ActiveProjectile item = active[index];
                ProjectileController controller = item.Controller;
                ProjectileFaction faction = controller.Ownership.CurrentOwner.Faction;
                if (faction == ProjectileFaction.Enemy && IsTouchingPlayer(controller))
                {
                    if (controller.TryResolveImpact(playerOwner, out ProjectileImpactResult impact))
                    {
                        player.ApplyDamage(
                            impact.DamageSource.Damage,
                            gameplayTimestamp,
                            impact.DamageSource.ProjectileId);
                        ReleaseAt(index, PoolReleaseReason.Completed);
                    }
                }
                else if (faction == ProjectileFaction.Player &&
                         reflectedImpactResolver != null &&
                         reflectedImpactResolver(controller, gameplayTimestamp))
                {
                    ReleaseAt(index, PoolReleaseReason.Completed);
                }
            }
        }

        /// <summary>若命中记录指向活动投射物，则按当前架势执行切断或反弹。</summary>
        public bool TryResolveStroke(
            in HitRecord hit,
            string stanceId,
            out ProjectileStrokeResult result)
        {
            ThrowIfDisposed();
            for (int index = active.Count - 1; index >= 0; index--)
            {
                ActiveProjectile item = active[index];
                if (item.Controller.HitTarget.HitTargetId != hit.TargetId ||
                    !ReferenceEquals(item.Controller.HitTarget, hit.Target))
                {
                    continue;
                }

                result = item.Controller.HitTarget.ResolveStrokeHit(
                    hit,
                    stanceId,
                    playerOwner);
                if (result.Release.IsValid)
                {
                    ReleaseAt(index, PoolReleaseReason.Completed);
                }

                return true;
            }

            result = default;
            return false;
        }

        /// <summary>清除全部仍由敌方拥有的活动投射物，并保留已反弹的玩家弹体。</summary>
        public int ClearHostileProjectiles()
        {
            ThrowIfDisposed();
            int cleared = 0;
            for (int index = active.Count - 1; index >= 0; index--)
            {
                ProjectileController controller = active[index].Controller;
                if (controller.Ownership.CurrentOwner.Faction != ProjectileFaction.Enemy)
                {
                    continue;
                }

                controller.Release(ProjectileReleaseReason.Manual);
                ReleaseAt(index, PoolReleaseReason.Manual);
                cleared++;
            }

            return cleared;
        }

        /// <summary>回收全部租约并销毁会话专属池根，防止重开后残留可见对象。</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            for (int index = active.Count - 1; index >= 0; index--)
            {
                active[index].Controller.Release(ProjectileReleaseReason.Manual);
                ReleaseAt(index, PoolReleaseReason.Restart);
            }

            pool.Dispose();
            Destroy(poolRoot.gameObject);
            disposed = true;
        }

        /// <summary>读取并缓存投射物规则，确保同一生命周期不重复映射配置。</summary>
        private ProjectileRuleSet GetRules(string projectileId)
        {
            if (!rules.TryGetValue(projectileId, out ProjectileRuleSet configured))
            {
                configured = ProjectileRuleSetFactory.Create(config, projectileId);
                rules.Add(projectileId, configured);
            }

            return configured;
        }

        /// <summary>首次使用某种弹体时按配置预热其池，不维护独立投射物ID清单。</summary>
        private void EnsurePoolRegistered(in ProjectileRuleSet rule)
        {
            if (!registeredPools.Add(rule.ProjectileId))
            {
                return;
            }

            ProjectileRuleSet configuredRule = rule;
            pool.RegisterPool(ObjectPoolConfiguration.CreateProjectilePool(
                config,
                configuredRule.ProjectileId,
                () => CreatePoolItem(configuredRule)));
        }

        /// <summary>创建根碰撞体与独立视觉子节点，使Sprite缩放不会放大玩法命中半径。</summary>
        private ProjectileController CreatePoolItem(in ProjectileRuleSet rule)
        {
            var item = new GameObject($"Projectile {rule.ProjectileId}");
            item.transform.SetParent(poolRoot, false);
            item.SetActive(false);
            ProjectileController controller = item.AddComponent<ProjectileController>();
            controller.enabled = false;

            var visual = new GameObject("Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(item.transform, false);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = assets.GetSprite(rule.AssetKey);
            renderer.sortingLayerName = "Projectiles";
            renderer.sortingOrder = 20;
            float spriteExtent = Mathf.Max(
                renderer.sprite.bounds.size.x,
                renderer.sprite.bounds.size.y);
            if (spriteExtent <= 0f)
            {
                throw new InvalidOperationException(
                    $"Projectile sprite '{rule.AssetKey}' must have positive bounds.");
            }

            float visualScale = (rule.HitRadiusReferencePixels * 2f) / spriteExtent;
            visual.transform.localScale = Vector3.one * visualScale;
            return controller;
        }

        /// <summary>使用缓存碰撞体判断敌方弹体是否真正接触玩家身体。</summary>
        private bool IsTouchingPlayer(ProjectileController controller)
        {
            return controller.HitCollider.enabled &&
                   playerBody.enabled &&
                   controller.HitCollider.bounds.Intersects(playerBody.bounds);
        }

        /// <summary>按玩法累计时间计算本帧delta，并拒绝时钟倒退或非有限输入。</summary>
        private float SynchronizeClock(double gameplayTimestamp)
        {
            if (double.IsNaN(gameplayTimestamp) ||
                double.IsInfinity(gameplayTimestamp) ||
                gameplayTimestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gameplayTimestamp),
                    "Projectile gameplay timestamp must be finite and non-negative.");
            }

            if (!hasGameplayClock)
            {
                hasGameplayClock = true;
                lastGameplayTimestamp = gameplayTimestamp;
                return 0f;
            }

            if (gameplayTimestamp < lastGameplayTimestamp)
            {
                throw new InvalidOperationException("Projectile gameplay clock cannot move backwards.");
            }

            double delta = gameplayTimestamp - lastGameplayTimestamp;
            lastGameplayTimestamp = gameplayTimestamp;
            return checked((float)delta);
        }

        /// <summary>从负数命名空间分配投射物目标ID，避免与正数敌人ID冲突。</summary>
        private int TakeNextHitTargetId()
        {
            int value = nextHitTargetId;
            nextHitTargetId = nextHitTargetId == int.MinValue
                ? FirstProjectileHitTargetId
                : nextHitTargetId - 1;
            return value;
        }

        /// <summary>池触发复用或外部释放后清除旧记录，防止旧租约释放新生命周期。</summary>
        private void RemoveReusedOrStaleEntries(ProjectileController reusedController)
        {
            for (int index = active.Count - 1; index >= 0; index--)
            {
                ProjectileController controller = active[index].Controller;
                if (ReferenceEquals(controller, reusedController) ||
                    !controller.IsPoolActive)
                {
                    active.RemoveAt(index);
                }
                else if (!controller.IsActive)
                {
                    ReleaseAt(index, PoolReleaseReason.Manual);
                }
            }
        }

        /// <summary>用精确租约结束池生命周期并移除活动记录。</summary>
        private void ReleaseAt(int index, PoolReleaseReason reason)
        {
            ActiveProjectile item = active[index];
            pool.Release(item.Controller, item.Lease, reason);
            active.RemoveAt(index);
        }

        /// <summary>按编辑器或运行模式销毁会话根。</summary>
        private static void Destroy(GameObject gameObject)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>拒绝在释放后继续访问会话专属投射物运行时。</summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ProductionProjectileRuntime));
            }
        }

        /// <summary>绑定活动控制器及其精确池租约。</summary>
        private readonly struct ActiveProjectile
        {
            /// <summary>创建一个可安全回收的活动投射物记录。</summary>
            public ActiveProjectile(ProjectileController controller, in PoolLease lease)
            {
                Controller = controller ?? throw new ArgumentNullException(nameof(controller));
                Lease = lease;
            }

            public ProjectileController Controller { get; }

            public PoolLease Lease { get; }
        }
    }
}
