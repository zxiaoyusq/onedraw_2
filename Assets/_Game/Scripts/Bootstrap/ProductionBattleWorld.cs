using System;
using System.Collections.Generic;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Skills;
using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    // 定义 ProductionBattleWorld 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    public sealed class ProductionBattleWorld : IBossLevelWorld, IDisposable
    {
        private readonly IConfigProvider config;
        private readonly IAssetRegistry assets;
        private readonly PlayerCombatController player;
        private readonly Transform root;
        private readonly EnemyArchetypePool pool;
        private readonly Dictionary<long, ActiveEnemy> active =
            new Dictionary<long, ActiveEnemy>();
        private readonly float referenceWidth;
        private readonly float referenceHeight;
        private readonly AudioSource audioSource;
        private SkillService skills;
        private BattleFlowCoordinator flow;
        private ISkillEffectTarget primaryTarget;
        private long nextEntityId = 1L;
        private int pendingProjectileCount;
        private float nextStrokeDamageMultiplier = 1f;
        private bool disposed;

        // 初始化 ProductionBattleWorld，并建立生产入口或战斗会话的依赖关系。
        public ProductionBattleWorld(
            IConfigProvider configProvider,
            IAssetRegistry assetRegistry,
            PlayerCombatController playerController,
            Transform configuredRoot)
        {
            config = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            assets = assetRegistry ?? throw new ArgumentNullException(nameof(assetRegistry));
            player = playerController ?? throw new ArgumentNullException(nameof(playerController));
            root = configuredRoot ?? throw new ArgumentNullException(nameof(configuredRoot));
            referenceWidth = ReadReference(config, ConfigIds.GlobalKeys.ReferenceWidth);
            referenceHeight = ReadReference(config, ConfigIds.GlobalKeys.ReferenceHeight);
            pool = new EnemyArchetypePool(config, assets, root);
            audioSource = root.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        public event Action<long, EnemyController, SpriteRenderer[]> EnemySpawned;

        public event Action<int> EnemyReleased;

        public event Action<EnemyAttackAction> AttackExecuted;

        public int ActiveCount => active.Count;

        public int PendingProjectileCount => pendingProjectileCount;

        public IReadOnlyList<ISkillEffectTarget> Targets
        {
            get
            {
                long[] ids = GetActiveIds();
                var targets = new List<ISkillEffectTarget>(ids.Length);
                // 逐项装配或释放会话资源，保持创建与回收顺序一致。
                for (int index = 0; index < ids.Length; index++)
                {
                    ActiveEnemy item = active[ids[index]];
                    // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                    if (item.Target.IsAlive)
                    {
                        targets.Add(item.Target);
                    }
                }

                return targets.AsReadOnly();
            }
        }

        public ISkillEffectTarget PrimaryTarget
        {
            get
            {
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (primaryTarget != null && primaryTarget.IsAlive)
                {
                    return primaryTarget;
                }

                long[] ids = GetActiveIds();
                // 逐项装配或释放会话资源，保持创建与回收顺序一致。
                for (int index = 0; index < ids.Length; index++)
                {
                    ActiveEnemy item = active[ids[index]];
                    // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                    if (item.Target.IsAlive)
                    {
                        return item.Target;
                    }
                }

                return null;
            }
        }

        // 处理 Bind 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void Bind(BattleFlowCoordinator battleFlow, SkillService skillService)
        {
            flow = battleFlow ?? throw new ArgumentNullException(nameof(battleFlow));
            skills = skillService ?? throw new ArgumentNullException(nameof(skillService));
        }

        // 尝试执行 TrySpawn 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
        {
            ThrowIfDisposed();
            long candidate = nextEntityId++;
            int hitTargetId = checked((int)candidate);
            double timestamp = ActorTimestamp();
            ActiveEnemy item;
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (request.IsBoss)
            {
                EnemyController controller = CreateBoss(
                    request.EnemyId,
                    hitTargetId,
                    timestamp);
                item = new ActiveEnemy(
                    request,
                    controller,
                    new EnemySkillEffectTarget(controller),
                    false,
                    default,
                    controller.gameObject);
            }
            else
            {
                EnemyArchetypeSpawnResult spawned = pool.Spawn(
                    request.EnemyId,
                    hitTargetId,
                    timestamp,
                    this);
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (!spawned.IsSpawned)
                {
                    entityId = 0L;
                    return false;
                }

                item = new ActiveEnemy(
                    request,
                    spawned.Actor.Controller,
                    new EnemySkillEffectTarget(spawned.Actor.Controller),
                    true,
                    spawned,
                    spawned.Actor.gameObject);
            }

            ConfigurePresentation(item.GameObject, request);
            item.GameObject.transform.localPosition = new Vector3(
                (float)(request.Position.X * referenceWidth),
                (float)(request.Position.Y * referenceHeight),
                0f);
            entityId = candidate;
            active.Add(candidate, item);
            SpriteRenderer[] renderers = item.GameObject.GetComponentsInChildren<SpriteRenderer>(true);
            EnemySpawned?.Invoke(candidate, item.Controller, renderers);
            return true;
        }

        // 尝试执行 TryGetEnemyController 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public bool TryGetEnemyController(long entityId, out EnemyController controller)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (active.TryGetValue(entityId, out ActiveEnemy item))
            {
                controller = item.Controller;
                return true;
            }

            controller = null;
            return false;
        }

        // 尝试执行 TryGetByHitTarget 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public bool TryGetByHitTarget(int hitTargetId, out long entityId, out EnemyController controller)
        {
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            foreach (KeyValuePair<long, ActiveEnemy> pair in active)
            {
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (pair.Value.Controller.Damage.HitTargetId == hitTargetId)
                {
                    entityId = pair.Key;
                    controller = pair.Value.Controller;
                    return true;
                }
            }

            entityId = 0L;
            controller = null;
            return false;
        }

        // 处理 Advance 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void Advance(double movementElapsedSeconds, double gameplayTimestamp)
        {
            ThrowIfDisposed();
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (flow == null || skills == null ||
                (flow.Flow.State != BattleFlowState.Playing &&
                 flow.Flow.State != BattleFlowState.UltimateDrawing))
            {
                return;
            }

            long[] ids = GetActiveIds();
            string supportTargetId = FindSupportTargetId(ids);
            double actorTimestamp = ActorTimestamp();
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            for (int index = 0; index < ids.Length; index++)
            {
                ActiveEnemy item = active[ids[index]];
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (!item.Controller.IsAlive)
                {
                    continue;
                }

                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (item.IsPooled)
                {
                    item.Pooled.Actor.AdvanceMovement(movementElapsedSeconds);
                    item.Pooled.Actor.TryBeginAttack(
                        new EnemyAttackTriggerContext(
                            cooldownReady: true,
                            targetInDistance: true,
                            hpThresholdReached: true,
                            supportTargetId),
                        UnitSelection(ids[index]),
                        actorTimestamp);
                }
            }
        }

        // 处理 AdvanceBoss 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void AdvanceBoss(BossPhaseController phases)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (phases == null || phases.HasEnded)
            {
                return;
            }

            long[] ids = GetActiveIds();
            double timestamp = ActorTimestamp();
            phases.TryBeginAttack(
                new EnemyAttackTriggerContext(
                    cooldownReady: true,
                    targetInDistance: true,
                    hpThresholdReached: true,
                    FindSupportTargetId(ids)),
                UnitSelection(ids.Length > 0 ? ids[0] : 1L),
                timestamp);
            phases.Tick(timestamp);
        }

        // 处理 ExecuteAttack 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void ExecuteAttack(in EnemyAttackAction action, double timestamp)
        {
            ThrowIfDisposed();
            double gameplayTimestamp = flow?.Flow.Time.Current.GameplayElapsedSeconds ?? 0d;
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (action.Damage > 0L)
            {
                player.ApplyDamage(action.Damage, gameplayTimestamp, action.AttackId);
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!string.IsNullOrEmpty(action.ProjectileId))
            {
                checked
                {
                    pendingProjectileCount += 1;
                }
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!string.IsNullOrEmpty(action.EffectGroupId) && skills != null)
            {
                primaryTarget = ResolveSupportTarget(action.SupportTargetId);
                try
                {
                    skills.ExecuteEffectGroup(
                        action.EffectGroupId,
                        action.AttackId,
                        new SkillEffectContext(this, gameplayTimestamp));
                }
                finally
                {
                    primaryTarget = null;
                }
            }

            AttackExecuted?.Invoke(action);
        }

        // 尝试执行 TryCutProjectile 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public bool TryCutProjectile()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (pendingProjectileCount <= 0)
            {
                return false;
            }

            pendingProjectileCount -= 1;
            return true;
        }

        // 清理 ClearStrokeSelection 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void ClearStrokeSelection()
        {
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            foreach (ActiveEnemy item in active.Values)
            {
                item.Target.SetSelectionFlags(false, false, false);
            }
        }

        // 处理 MarkStrokeTarget 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void MarkStrokeTarget(int hitTargetId, bool insideGesture)
        {
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            foreach (ActiveEnemy item in active.Values)
            {
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (item.Controller.Damage.HitTargetId == hitTargetId)
                {
                    item.Target.SetSelectionFlags(
                        inEffectRadius: true,
                        hitByLastStroke: true,
                        insideGesture);
                    return;
                }
            }
        }

        // 释放 Release 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public bool Release(long entityId, PoolReleaseReason reason = PoolReleaseReason.Completed)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!active.TryGetValue(entityId, out ActiveEnemy item))
            {
                return false;
            }

            int targetId = item.Controller.Damage.HitTargetId;
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (item.IsPooled)
            {
                pool.Release(item.Pooled, reason);
            }
            else
            {
                double timestamp = Math.Max(ActorTimestamp(), item.Controller.State.LastTimestamp);
                item.Controller.Release(EnemyReleaseReason.Manual, timestamp);
                Destroy(item.GameObject);
            }

            active.Remove(entityId);
            EnemyReleased?.Invoke(targetId);
            return true;
        }

        // 处理 RepeatLastStroke 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public int RepeatLastStroke(
            float damageMultiplier,
            float delaySeconds,
            string sourceId,
            double timestamp)
        {
            return 0;
        }

        // 设置 SetTimeScale 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public int SetTimeScale(
            float scale,
            float durationSeconds,
            string sourceId,
            double timestamp)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (flow == null)
            {
                return 0;
            }

            flow.ApplyGameplayScale(scale, durationSeconds);
            return 1;
        }

        // 设置 SetNextStrokeDamageMultiplier 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public int SetNextStrokeDamageMultiplier(
            float multiplier,
            string sourceId,
            double timestamp)
        {
            nextStrokeDamageMultiplier = multiplier;
            return 1;
        }

        // 处理 ConsumeNextStrokeDamageMultiplier 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public float ConsumeNextStrokeDamageMultiplier()
        {
            float value = nextStrokeDamageMultiplier;
            nextStrokeDamageMultiplier = 1f;
            return value;
        }

        // 清理 ClearHostileProjectiles 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public int ClearHostileProjectiles(string sourceId, double timestamp)
        {
            int cleared = pendingProjectileCount;
            pendingProjectileCount = 0;
            return cleared;
        }

        // 处理 PlayVfx 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void PlayVfx(
            string vfxKey,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId,
            double timestamp)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (string.IsNullOrEmpty(vfxKey))
            {
                return;
            }

            GameObject prefab = assets.GetPrefab(vfxKey);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (targets == null || targets.Count == 0)
            {
                UnityEngine.Object.Instantiate(prefab, root, false);
                return;
            }

            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            for (int index = 0; index < targets.Count; index++)
            {
                GameObject instance = UnityEngine.Object.Instantiate(prefab, root, false);
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (targets[index] is EnemySkillEffectTarget enemyTarget)
                {
                    instance.transform.position = enemyTarget.Enemy.transform.position;
                }
            }
        }

        // 处理 PlayAudio 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void PlayAudio(string audioKey, string sourceId, double timestamp)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!string.IsNullOrEmpty(audioKey))
            {
                AudioCueConfig cue = config.GetAudioCue(audioKey);
                audioSource.PlayOneShot(assets.GetAudioClip(cue.AssetKey));
            }
        }

        // 释放 Dispose 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void Dispose()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (disposed)
            {
                return;
            }

            long[] ids = GetActiveIds();
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            for (int index = 0; index < ids.Length; index++)
            {
                Release(ids[index], PoolReleaseReason.Restart);
            }

            pool.Dispose();
            EnemySpawned = null;
            EnemyReleased = null;
            AttackExecuted = null;
            disposed = true;
        }

        // 创建 CreateBoss 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private EnemyController CreateBoss(
            string enemyId,
            int hitTargetId,
            double timestamp)
        {
            EnemyConfig enemy = config.GetEnemy(enemyId);
            AssetManifestConfig asset = config.GetAsset(enemy.AssetKey);
            GameObject instance;
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (string.Equals(asset.AssetType, "Prefab", StringComparison.Ordinal))
            {
                instance = UnityEngine.Object.Instantiate(
                    assets.GetPrefab(enemy.AssetKey),
                    root,
                    false);
            }
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            else if (string.Equals(asset.AssetType, "Sprite", StringComparison.Ordinal))
            {
                instance = new GameObject($"Boss {enemyId}", typeof(SpriteRenderer));
                instance.transform.SetParent(root, false);
                instance.GetComponent<SpriteRenderer>().sprite = assets.GetSprite(enemy.AssetKey);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Boss '{enemyId}' uses unsupported asset type '{asset.AssetType}'.");
            }

            instance.name = $"Boss {enemyId}";
            instance.SetActive(false);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (instance.GetComponent<Damageable>() == null)
            {
                instance.AddComponent<Damageable>();
            }

            EnemyController controller = instance.GetComponent<EnemyController>();
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (controller == null)
            {
                controller = instance.AddComponent<EnemyController>();
            }
            WeakpointController weakpoint =
                instance.GetComponentInChildren<WeakpointController>(true);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (weakpoint == null)
            {
                var weakpointObject = new GameObject("Weakpoint", typeof(CircleCollider2D));
                weakpointObject.transform.SetParent(instance.transform, false);
                weakpoint = weakpointObject.AddComponent<WeakpointController>();
            }

            controller.Spawn(config, enemyId, hitTargetId, timestamp, weakpoint);
            controller.CompleteSpawn(timestamp);
            return controller;
        }

        // 处理 ConfigurePresentation 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void ConfigurePresentation(GameObject actor, in LevelSpawnRequest request)
        {
            SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer primary = null;
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            for (int index = 0; index < renderers.Length; index++)
            {
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (renderers[index].sprite == null)
                {
                    continue;
                }

                renderers[index].sortingOrder = 10;
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (ColorUtility.TryParseHtmlString(request.Modifier.TintHex, out Color tint))
                {
                    renderers[index].color = tint;
                }

                primary ??= renderers[index];
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (primary == null)
            {
                throw new InvalidOperationException(
                    $"Enemy '{request.EnemyId}' must expose a configured SpriteRenderer.");
            }

            actor.transform.localScale = Vector3.one * primary.sprite.pixelsPerUnit;
            BoxCollider2D body = actor.GetComponent<BoxCollider2D>();
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (body == null)
            {
                body = actor.AddComponent<BoxCollider2D>();
            }
            body.isTrigger = true;
            body.size = primary.sprite.bounds.size;
        }

        // 处理 ResolveSupportTarget 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private ISkillEffectTarget ResolveSupportTarget(string targetId)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (string.IsNullOrEmpty(targetId))
            {
                return null;
            }

            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            foreach (ActiveEnemy item in active.Values)
            {
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (string.Equals(item.Target.TargetId, targetId, StringComparison.Ordinal))
                {
                    return item.Target;
                }
            }

            return null;
        }

        // 处理 FindSupportTargetId 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private string FindSupportTargetId(long[] ids)
        {
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            for (int index = 0; index < ids.Length; index++)
            {
                ActiveEnemy item = active[ids[index]];
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (item.Target.IsAlive)
                {
                    return item.Target.TargetId;
                }
            }

            return string.Empty;
        }

        // 获取 GetActiveIds 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private long[] GetActiveIds()
        {
            var ids = new long[active.Count];
            active.Keys.CopyTo(ids, 0);
            Array.Sort(ids);
            return ids;
        }

        // 处理 UnitSelection 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static double UnitSelection(long value)
        {
            unchecked
            {
                ulong mixed = ((ulong)value * 11400714819323198485UL) >> 11;
                return mixed * (1d / 9007199254740992d);
            }
        }

        // 处理 ActorTimestamp 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static double ActorTimestamp()
        {
            return Time.timeAsDouble;
        }

        // 处理 ReadReference 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static float ReadReference(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!row.IntValue.HasValue || row.IntValue.Value <= 0L)
            {
                throw new ArgumentException(
                    $"Global '{key}' must define a positive reference dimension.",
                    nameof(configProvider));
            }

            return row.IntValue.Value;
        }

        // 处理 Destroy 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static void Destroy(GameObject gameObject)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        // 处理 ThrowIfDisposed 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void ThrowIfDisposed()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ProductionBattleWorld));
            }
        }

        // 定义 ActiveEnemy 的入口装配契约，集中管理场景、服务与战斗会话所有权。
        private sealed class ActiveEnemy
        {
            // 初始化 ActiveEnemy，并建立生产入口或战斗会话的依赖关系。
            public ActiveEnemy(
                in LevelSpawnRequest request,
                EnemyController controller,
                EnemySkillEffectTarget target,
                bool isPooled,
                in EnemyArchetypeSpawnResult pooled,
                GameObject gameObject)
            {
                Request = request;
                Controller = controller;
                Target = target;
                IsPooled = isPooled;
                Pooled = pooled;
                GameObject = gameObject;
            }

            public LevelSpawnRequest Request { get; }
            public EnemyController Controller { get; }
            public EnemySkillEffectTarget Target { get; }
            public bool IsPooled { get; }
            public EnemyArchetypeSpawnResult Pooled { get; }
            public GameObject GameObject { get; }
        }
    }
}
