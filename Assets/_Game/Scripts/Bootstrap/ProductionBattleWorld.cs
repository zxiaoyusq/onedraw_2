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
                for (int index = 0; index < ids.Length; index++)
                {
                    ActiveEnemy item = active[ids[index]];
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
                if (primaryTarget != null && primaryTarget.IsAlive)
                {
                    return primaryTarget;
                }

                long[] ids = GetActiveIds();
                for (int index = 0; index < ids.Length; index++)
                {
                    ActiveEnemy item = active[ids[index]];
                    if (item.Target.IsAlive)
                    {
                        return item.Target;
                    }
                }

                return null;
            }
        }

        public void Bind(BattleFlowCoordinator battleFlow, SkillService skillService)
        {
            flow = battleFlow ?? throw new ArgumentNullException(nameof(battleFlow));
            skills = skillService ?? throw new ArgumentNullException(nameof(skillService));
        }

        public bool TrySpawn(in LevelSpawnRequest request, out long entityId)
        {
            ThrowIfDisposed();
            long candidate = nextEntityId++;
            int hitTargetId = checked((int)candidate);
            double timestamp = ActorTimestamp();
            ActiveEnemy item;
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

        public bool TryGetEnemyController(long entityId, out EnemyController controller)
        {
            if (active.TryGetValue(entityId, out ActiveEnemy item))
            {
                controller = item.Controller;
                return true;
            }

            controller = null;
            return false;
        }

        public bool TryGetByHitTarget(int hitTargetId, out long entityId, out EnemyController controller)
        {
            foreach (KeyValuePair<long, ActiveEnemy> pair in active)
            {
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

        public void Advance(double movementElapsedSeconds, double gameplayTimestamp)
        {
            ThrowIfDisposed();
            if (flow == null || skills == null ||
                (flow.Flow.State != BattleFlowState.Playing &&
                 flow.Flow.State != BattleFlowState.UltimateDrawing))
            {
                return;
            }

            long[] ids = GetActiveIds();
            string supportTargetId = FindSupportTargetId(ids);
            double actorTimestamp = ActorTimestamp();
            for (int index = 0; index < ids.Length; index++)
            {
                ActiveEnemy item = active[ids[index]];
                if (!item.Controller.IsAlive)
                {
                    continue;
                }

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

        public void AdvanceBoss(BossPhaseController phases)
        {
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

        public void ExecuteAttack(in EnemyAttackAction action, double timestamp)
        {
            ThrowIfDisposed();
            double gameplayTimestamp = flow?.Flow.Time.Current.GameplayElapsedSeconds ?? 0d;
            if (action.Damage > 0L)
            {
                player.ApplyDamage(action.Damage, gameplayTimestamp, action.AttackId);
            }

            if (!string.IsNullOrEmpty(action.ProjectileId))
            {
                checked
                {
                    pendingProjectileCount += 1;
                }
            }

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

        public bool TryCutProjectile()
        {
            if (pendingProjectileCount <= 0)
            {
                return false;
            }

            pendingProjectileCount -= 1;
            return true;
        }

        public void ClearStrokeSelection()
        {
            foreach (ActiveEnemy item in active.Values)
            {
                item.Target.SetSelectionFlags(false, false, false);
            }
        }

        public void MarkStrokeTarget(int hitTargetId, bool insideGesture)
        {
            foreach (ActiveEnemy item in active.Values)
            {
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

        public bool Release(long entityId, PoolReleaseReason reason = PoolReleaseReason.Completed)
        {
            if (!active.TryGetValue(entityId, out ActiveEnemy item))
            {
                return false;
            }

            int targetId = item.Controller.Damage.HitTargetId;
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

        public int RepeatLastStroke(
            float damageMultiplier,
            float delaySeconds,
            string sourceId,
            double timestamp)
        {
            return 0;
        }

        public int SetTimeScale(
            float scale,
            float durationSeconds,
            string sourceId,
            double timestamp)
        {
            if (flow == null)
            {
                return 0;
            }

            flow.ApplyGameplayScale(scale, durationSeconds);
            return 1;
        }

        public int SetNextStrokeDamageMultiplier(
            float multiplier,
            string sourceId,
            double timestamp)
        {
            nextStrokeDamageMultiplier = multiplier;
            return 1;
        }

        public float ConsumeNextStrokeDamageMultiplier()
        {
            float value = nextStrokeDamageMultiplier;
            nextStrokeDamageMultiplier = 1f;
            return value;
        }

        public int ClearHostileProjectiles(string sourceId, double timestamp)
        {
            int cleared = pendingProjectileCount;
            pendingProjectileCount = 0;
            return cleared;
        }

        public void PlayVfx(
            string vfxKey,
            IReadOnlyList<ISkillEffectTarget> targets,
            string sourceId,
            double timestamp)
        {
            if (string.IsNullOrEmpty(vfxKey))
            {
                return;
            }

            GameObject prefab = assets.GetPrefab(vfxKey);
            if (targets == null || targets.Count == 0)
            {
                UnityEngine.Object.Instantiate(prefab, root, false);
                return;
            }

            for (int index = 0; index < targets.Count; index++)
            {
                GameObject instance = UnityEngine.Object.Instantiate(prefab, root, false);
                if (targets[index] is EnemySkillEffectTarget enemyTarget)
                {
                    instance.transform.position = enemyTarget.Enemy.transform.position;
                }
            }
        }

        public void PlayAudio(string audioKey, string sourceId, double timestamp)
        {
            if (!string.IsNullOrEmpty(audioKey))
            {
                AudioCueConfig cue = config.GetAudioCue(audioKey);
                audioSource.PlayOneShot(assets.GetAudioClip(cue.AssetKey));
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            long[] ids = GetActiveIds();
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

        private EnemyController CreateBoss(
            string enemyId,
            int hitTargetId,
            double timestamp)
        {
            EnemyConfig enemy = config.GetEnemy(enemyId);
            AssetManifestConfig asset = config.GetAsset(enemy.AssetKey);
            GameObject instance;
            if (string.Equals(asset.AssetType, "Prefab", StringComparison.Ordinal))
            {
                instance = UnityEngine.Object.Instantiate(
                    assets.GetPrefab(enemy.AssetKey),
                    root,
                    false);
            }
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
            if (instance.GetComponent<Damageable>() == null)
            {
                instance.AddComponent<Damageable>();
            }

            EnemyController controller = instance.GetComponent<EnemyController>();
            if (controller == null)
            {
                controller = instance.AddComponent<EnemyController>();
            }
            WeakpointController weakpoint =
                instance.GetComponentInChildren<WeakpointController>(true);
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

        private void ConfigurePresentation(GameObject actor, in LevelSpawnRequest request)
        {
            SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer primary = null;
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index].sprite == null)
                {
                    continue;
                }

                renderers[index].sortingOrder = 10;
                if (ColorUtility.TryParseHtmlString(request.Modifier.TintHex, out Color tint))
                {
                    renderers[index].color = tint;
                }

                primary ??= renderers[index];
            }

            if (primary == null)
            {
                throw new InvalidOperationException(
                    $"Enemy '{request.EnemyId}' must expose a configured SpriteRenderer.");
            }

            actor.transform.localScale = Vector3.one * primary.sprite.pixelsPerUnit;
            BoxCollider2D body = actor.GetComponent<BoxCollider2D>();
            if (body == null)
            {
                body = actor.AddComponent<BoxCollider2D>();
            }
            body.isTrigger = true;
            body.size = primary.sprite.bounds.size;
        }

        private ISkillEffectTarget ResolveSupportTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return null;
            }

            foreach (ActiveEnemy item in active.Values)
            {
                if (string.Equals(item.Target.TargetId, targetId, StringComparison.Ordinal))
                {
                    return item.Target;
                }
            }

            return null;
        }

        private string FindSupportTargetId(long[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
            {
                ActiveEnemy item = active[ids[index]];
                if (item.Target.IsAlive)
                {
                    return item.Target.TargetId;
                }
            }

            return string.Empty;
        }

        private long[] GetActiveIds()
        {
            var ids = new long[active.Count];
            active.Keys.CopyTo(ids, 0);
            Array.Sort(ids);
            return ids;
        }

        private static double UnitSelection(long value)
        {
            unchecked
            {
                ulong mixed = ((ulong)value * 11400714819323198485UL) >> 11;
                return mixed * (1d / 9007199254740992d);
            }
        }

        private static double ActorTimestamp()
        {
            return Time.timeAsDouble;
        }

        private static float ReadReference(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            if (!row.IntValue.HasValue || row.IntValue.Value <= 0L)
            {
                throw new ArgumentException(
                    $"Global '{key}' must define a positive reference dimension.",
                    nameof(configProvider));
            }

            return row.IntValue.Value;
        }

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

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ProductionBattleWorld));
            }
        }

        private sealed class ActiveEnemy
        {
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
