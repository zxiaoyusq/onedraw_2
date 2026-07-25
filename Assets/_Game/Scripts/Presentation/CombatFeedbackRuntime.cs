using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Levels;
using TMPro;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Presentation
{
    // 定义 CombatFeedbackRuntime 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class CombatFeedbackRuntime : ICombatFeedbackOutput, IDisposable
    {
        public const string DamageNumberFontResourcePath = "Fonts/OneStrokeDemon UI Latin SDF";

        private static readonly CombatFeedbackType[] FeedbackTypes =
        {
            CombatFeedbackType.EnemyHit,
            CombatFeedbackType.WeakpointHit,
            CombatFeedbackType.ArmorBreak,
            CombatFeedbackType.ProjectileReflect,
            CombatFeedbackType.PlayerHit,
            CombatFeedbackType.EnemyDeath,
        };

        // 定义 TargetState 的表现层契约，隔离战斗状态与具体Unity视图实现。
        private sealed class TargetState
        {
            // 初始化 TargetState，并建立表现层所需的引用与初始显示状态。
            internal TargetState(Transform target, SpriteRenderer[] targetRenderers)
            {
                Transform = target;
                Renderers = targetRenderers;
                BaseColors = new Color[targetRenderers.Length];
                // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
                for (int index = 0; index < targetRenderers.Length; index += 1)
                {
                    BaseColors[index] = targetRenderers[index].color;
                }
            }

            internal Transform Transform { get; }
            internal SpriteRenderer[] Renderers { get; }
            internal Color[] BaseColors { get; }
            internal float FlashRemainingSeconds { get; set; }
        }

        // 定义 ActiveVfx 的表现层契约，隔离战斗状态与具体Unity视图实现。
        private sealed class ActiveVfx
        {
            // 初始化 ActiveVfx，并建立表现层所需的引用与初始显示状态。
            internal ActiveVfx(VfxPoolItem item, in PoolLease lease)
            {
                Item = item;
                Lease = lease;
            }

            internal VfxPoolItem Item { get; }
            internal PoolLease Lease { get; }
        }

        // 定义 ActiveDamageNumber 的表现层契约，隔离战斗状态与具体Unity视图实现。
        private sealed class ActiveDamageNumber
        {
            // 初始化 ActiveDamageNumber，并建立表现层所需的引用与初始显示状态。
            internal ActiveDamageNumber(DamageNumberPoolItem item, in PoolLease lease)
            {
                Item = item;
                Lease = lease;
            }

            internal DamageNumberPoolItem Item { get; }
            internal PoolLease Lease { get; }
        }

        // 定义 AudioChannel 的表现层契约，隔离战斗状态与具体Unity视图实现。
        private sealed class AudioChannel
        {
            private readonly AudioSource[] sources;
            private readonly float cooldownSeconds;
            private double lastPlayedTimestamp = double.NegativeInfinity;

            // 初始化 AudioChannel，并建立表现层所需的引用与初始显示状态。
            internal AudioChannel(AudioCueConfig cue, AudioClip clip, Transform parent)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (cue.MaxConcurrent < 1L || cue.MaxConcurrent > int.MaxValue)
                {
                    throw new ArgumentException(
                        $"AudioCues row '{cue.AudioKey}' has invalid maxConcurrent.",
                        "configProvider");
                }

                cooldownSeconds = cue.CooldownSec;
                sources = new AudioSource[(int)cue.MaxConcurrent];
                // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
                for (int index = 0; index < sources.Length; index += 1)
                {
                    var sourceObject = new GameObject($"Audio {cue.AudioKey} {index + 1}");
                    sourceObject.transform.SetParent(parent, false);
                    AudioSource source = sourceObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = false;
                    source.clip = clip;
                    source.volume = cue.Volume;
                    source.pitch = (cue.PitchMin + cue.PitchMax) * 0.5f;
                    sources[index] = source;
                }
            }

            internal int PlayCount { get; private set; }

            // 尝试执行 TryPlay 对应的表现逻辑，使视图与只读战斗状态保持同步。
            internal bool TryPlay(double timestamp)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (timestamp - lastPlayedTimestamp < cooldownSeconds)
                {
                    return false;
                }

                // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
                for (int index = 0; index < sources.Length; index += 1)
                {
                    // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                    if (sources[index].isPlaying)
                    {
                        continue;
                    }

                    sources[index].Play();
                    lastPlayedTimestamp = timestamp;
                    PlayCount += 1;
                    return true;
                }

                return false;
            }

            // 停止 Stop 对应的表现逻辑，使视图与只读战斗状态保持同步。
            internal void Stop()
            {
                // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
                for (int index = 0; index < sources.Length; index += 1)
                {
                    sources[index].Stop();
                }

                lastPlayedTimestamp = double.NegativeInfinity;
            }
        }

        private readonly IConfigProvider configProvider;
        private readonly IAssetRegistry assetRegistry;
        private readonly BattleTimeSource battleTime;
        private readonly Camera feedbackCamera;
        private readonly Transform root;
        private readonly ObjectPoolService pools;
        private readonly CombatFeedbackSettings settings;
        private readonly TMP_FontAsset damageNumberFont;
        private readonly float referenceHeight;
        private readonly Dictionary<int, TargetState> targets = new Dictionary<int, TargetState>();
        private readonly Dictionary<string, AudioChannel> audioChannels =
            new Dictionary<string, AudioChannel>(StringComparer.Ordinal);
        private readonly List<ActiveVfx> activeVfx = new List<ActiveVfx>();
        private readonly List<ActiveDamageNumber> activeDamageNumbers = new List<ActiveDamageNumber>();
        private Vector3 cameraBasePosition;
        private float shakeRemainingSeconds;
        private float shakeTotalSeconds;
        private float shakeStrengthReferencePixels;
        private float shakeElapsedSeconds;
        private bool disposed;

        // 初始化 CombatFeedbackRuntime，并建立表现层所需的引用与初始显示状态。
        private CombatFeedbackRuntime(
            IConfigProvider configuredProvider,
            IAssetRegistry configuredRegistry,
            BattleTimeSource configuredBattleTime,
            Camera configuredCamera,
            Transform parent)
        {
            configProvider = configuredProvider ?? throw new ArgumentNullException(nameof(configuredProvider));
            assetRegistry = configuredRegistry ?? throw new ArgumentNullException(nameof(configuredRegistry));
            battleTime = configuredBattleTime ?? throw new ArgumentNullException(nameof(configuredBattleTime));
            feedbackCamera = configuredCamera ?? throw new ArgumentNullException(nameof(configuredCamera));
            settings = CombatFeedbackSettings.Create(configProvider);
            referenceHeight = ReadPositiveReferenceDimension(
                configProvider,
                ConfigIds.GlobalKeys.ReferenceHeight);
            damageNumberFont = Resources.Load<TMP_FontAsset>(DamageNumberFontResourcePath);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (damageNumberFont == null)
            {
                throw new InvalidOperationException(
                    $"Damage-number font is missing from Resources: {DamageNumberFontResourcePath}");
            }

            var rootObject = new GameObject("Combat Feedback Runtime");
            rootObject.layer = parent != null ? parent.gameObject.layer : 0;
            root = rootObject.transform;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (parent != null)
            {
                root.SetParent(parent, false);
            }

            cameraBasePosition = feedbackCamera.transform.localPosition;
            pools = new ObjectPoolService();
            BuildCachedOutputs();
        }

        public CombatFeedbackSettings Settings => settings;

        public int ActiveVfxCount => activeVfx.Count;

        public int ActiveDamageNumberCount => activeDamageNumbers.Count;

        public int EmittedCount { get; private set; }

        public int AudioPlayCount { get; private set; }

        public PoolServiceSnapshot PoolSnapshot => pools.GetSnapshot();

        // 创建 Create 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public static CombatFeedbackRuntime Create(
            IConfigProvider configProvider,
            IAssetRegistry assetRegistry,
            BattleTimeSource battleTime,
            Camera feedbackCamera,
            Transform parent = null) =>
            new CombatFeedbackRuntime(
                configProvider,
                assetRegistry,
                battleTime,
                feedbackCamera,
                parent);

        // 处理 RegisterTarget 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void RegisterTarget(int targetId, Transform target, params SpriteRenderer[] renderers)
        {
            ThrowIfDisposed();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (targetId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetId));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (renderers == null || renderers.Length == 0)
            {
                renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            }

            targets[targetId] = new TargetState(target, renderers);
        }

        // 处理 UnregisterTarget 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void UnregisterTarget(int targetId)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (targets.TryGetValue(targetId, out TargetState target))
            {
                RestoreTarget(target);
                targets.Remove(targetId);
            }
        }

        // 处理 Emit 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Emit(in CombatFeedbackCommand command)
        {
            ThrowIfDisposed();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!command.Event.IsValid || command.Profile == null)
            {
                throw new ArgumentException("Feedback command must be initialized.", nameof(command));
            }

            targets.TryGetValue(command.Event.TargetId, out TargetState target);
            Vector3 position = target?.Transform != null ? target.Transform.position : Vector3.zero;
            battleTime.ApplyGameplayScale(command.Profile.TimeScale, command.Profile.TimeScaleSeconds);
            EmitVfx(command.Profile, target?.Transform, position);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (command.Event.SignedAmount != 0L)
            {
                EmitDamageNumber(command, position);
            }

            ApplyFlash(target, command.Profile.FlashSeconds);
            ApplyShake(command.Profile);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (audioChannels[command.Profile.AudioKey].TryPlay(command.Event.Timestamp))
            {
                AudioPlayCount += 1;
            }

            EmittedCount += 1;
        }

        // 处理 Advance 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Advance(float unscaledDeltaSeconds)
        {
            ThrowIfDisposed();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (float.IsNaN(unscaledDeltaSeconds) ||
                float.IsInfinity(unscaledDeltaSeconds) ||
                unscaledDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaSeconds));
            }

            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = activeVfx.Count - 1; index >= 0; index -= 1)
            {
                ActiveVfx active = activeVfx[index];
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (active.Item.Advance(unscaledDeltaSeconds))
                {
                    pools.Release(active.Item, active.Lease, PoolReleaseReason.Completed);
                    activeVfx.RemoveAt(index);
                }
            }

            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = activeDamageNumbers.Count - 1; index >= 0; index -= 1)
            {
                ActiveDamageNumber active = activeDamageNumbers[index];
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (active.Item.Advance(unscaledDeltaSeconds))
                {
                    pools.Release(active.Item, active.Lease, PoolReleaseReason.Completed);
                    activeDamageNumbers.RemoveAt(index);
                }
            }

            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            foreach (TargetState target in targets.Values)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (target.FlashRemainingSeconds <= 0f)
                {
                    continue;
                }

                target.FlashRemainingSeconds = Mathf.Max(0f, target.FlashRemainingSeconds - unscaledDeltaSeconds);
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (target.FlashRemainingSeconds == 0f)
                {
                    RestoreTarget(target);
                }
            }

            AdvanceShake(unscaledDeltaSeconds);
        }

        // 处理 Restart 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Restart()
        {
            ThrowIfDisposed();
            pools.Restart();
            activeVfx.Clear();
            activeDamageNumbers.Clear();
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            foreach (TargetState target in targets.Values)
            {
                RestoreTarget(target);
            }

            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            foreach (AudioChannel channel in audioChannels.Values)
            {
                channel.Stop();
            }

            ResetShake();
        }

        // 释放 Dispose 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Dispose()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (disposed)
            {
                return;
            }

            Restart();
            pools.Dispose();
            disposed = true;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (root != null)
            {
                UnityObject.Destroy(root.gameObject);
            }
        }

        // 构建 BuildCachedOutputs 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void BuildCachedOutputs()
        {
            pools.RegisterFamily(ObjectPoolConfiguration.CreateVfxFamily(configProvider));
            pools.RegisterFamily(ObjectPoolConfiguration.CreateDamageNumberFamily(configProvider));
            var registeredVfx = new HashSet<string>(StringComparer.Ordinal);
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            foreach (CombatFeedbackType type in FeedbackTypes)
            {
                CombatFeedbackProfile profile = settings.Get(type);
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (registeredVfx.Add(profile.VfxKey))
                {
                    string vfxKey = profile.VfxKey;
                    pools.RegisterPool(ObjectPoolConfiguration.CreateVfxPool(
                        configProvider,
                        vfxKey,
                        () => CreateVfx(vfxKey)));
                }

                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (!audioChannels.ContainsKey(profile.AudioKey))
                {
                    AudioCueConfig cue = configProvider.GetAudioCue(profile.AudioKey);
                    AudioClip clip = assetRegistry.GetAudioClip(cue.AssetKey);
                    audioChannels.Add(profile.AudioKey, new AudioChannel(cue, clip, root));
                }
            }

            pools.RegisterPool(ObjectPoolConfiguration.CreateDamageNumberPool(
                configProvider,
                CreateDamageNumber));
        }

        // 创建 CreateVfx 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private VfxPoolItem CreateVfx(string vfxKey)
        {
            VfxCueConfig cue = configProvider.GetVfxCue(vfxKey);
            GameObject prefab = assetRegistry.GetPrefab(cue.AssetKey);
            GameObject instance = UnityObject.Instantiate(prefab, root, false);
            instance.name = $"Feedback VFX {vfxKey}";
            SetLayerRecursively(instance, root.gameObject.layer);
            VfxPoolItem item = instance.GetComponent<VfxPoolItem>();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (item == null)
            {
                item = instance.AddComponent<VfxPoolItem>();
            }

            instance.SetActive(false);
            item.Configure(configProvider, vfxKey);
            return item;
        }

        // 创建 CreateDamageNumber 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private DamageNumberPoolItem CreateDamageNumber()
        {
            var instance = new GameObject("Feedback Damage Number");
            instance.layer = root.gameObject.layer;
            instance.transform.SetParent(root, false);
            DamageNumberPoolItem item = instance.AddComponent<DamageNumberPoolItem>();
            item.ConfigureVisual(damageNumberFont);
            instance.SetActive(false);
            return item;
        }

        // 处理 EmitVfx 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void EmitVfx(
            CombatFeedbackProfile profile,
            Transform target,
            Vector3 position)
        {
            PoolAcquireResult result = pools.Acquire(ObjectPoolConfiguration.GetVfxPoolId(profile.VfxKey));
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!result.IsAcquired)
            {
                return;
            }

            RemovePreviousLease(result.Item, activeVfx);
            var item = (VfxPoolItem)result.Item;
            item.Play(
                target,
                position,
                ParseColor(profile.VfxTintColorHex),
                ReferencePixelsToWorld(profile.VfxScaleReferencePixels));
            activeVfx.Add(new ActiveVfx(item, result.Lease));
        }

        // 处理 EmitDamageNumber 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void EmitDamageNumber(in CombatFeedbackCommand command, Vector3 position)
        {
            PoolAcquireResult result = pools.Acquire(ObjectPoolConfiguration.DamageNumberPoolId);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!result.IsAcquired)
            {
                return;
            }

            RemovePreviousLease(result.Item, activeDamageNumbers);
            var item = (DamageNumberPoolItem)result.Item;
            float fontHeightWorldUnits = ReferencePixelsToWorld(
                command.Profile.DamageNumberFontSizeReferencePixels);
            float initialVerticalOffset =
                (ReferencePixelsToWorld(command.Profile.VfxScaleReferencePixels) * 0.5f) +
                (fontHeightWorldUnits * 0.5f);
            item.Show(
                command.Event.SignedAmount,
                command.Event.TargetId,
                command.Event.SourceId,
                position + (Vector3.up * initialVerticalOffset),
                ParseColor(command.Profile.DamageNumberColorHex),
                command.Profile.DamageNumberFontSizeReferencePixels,
                fontHeightWorldUnits,
                command.Profile.DamageNumberLifeSeconds,
                ReferencePixelsToWorld(command.Profile.DamageNumberRiseReferencePixels));
            activeDamageNumbers.Add(new ActiveDamageNumber(item, result.Lease));
        }

        // 移除 RemovePreviousLease 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void RemovePreviousLease(IPoolable item, List<ActiveVfx> entries)
        {
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = entries.Count - 1; index >= 0; index -= 1)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (ReferenceEquals(entries[index].Item, item))
                {
                    entries.RemoveAt(index);
                }
            }
        }

        // 移除 RemovePreviousLease 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void RemovePreviousLease(IPoolable item, List<ActiveDamageNumber> entries)
        {
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = entries.Count - 1; index >= 0; index -= 1)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (ReferenceEquals(entries[index].Item, item))
                {
                    entries.RemoveAt(index);
                }
            }
        }

        // 应用 ApplyFlash 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void ApplyFlash(TargetState target, float durationSeconds)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (target == null || durationSeconds <= 0f)
            {
                return;
            }

            target.FlashRemainingSeconds = Mathf.Max(target.FlashRemainingSeconds, durationSeconds);
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < target.Renderers.Length; index += 1)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (target.Renderers[index] != null)
                {
                    target.Renderers[index].color = Color.white;
                }
            }
        }

        // 应用 ApplyShake 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void ApplyShake(CombatFeedbackProfile profile)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (profile.ShakeSeconds <= 0f || profile.ShakeStrengthReferencePixels <= 0f)
            {
                return;
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (shakeRemainingSeconds <= 0f)
            {
                cameraBasePosition = feedbackCamera.transform.localPosition;
            }

            shakeRemainingSeconds = Mathf.Max(shakeRemainingSeconds, profile.ShakeSeconds);
            shakeTotalSeconds = Mathf.Max(shakeTotalSeconds, profile.ShakeSeconds);
            shakeStrengthReferencePixels = Mathf.Max(
                shakeStrengthReferencePixels,
                profile.ShakeStrengthReferencePixels);
        }

        // 处理 AdvanceShake 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void AdvanceShake(float deltaSeconds)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (shakeRemainingSeconds <= 0f)
            {
                return;
            }

            shakeRemainingSeconds = Mathf.Max(0f, shakeRemainingSeconds - deltaSeconds);
            shakeElapsedSeconds += deltaSeconds;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (shakeRemainingSeconds == 0f)
            {
                ResetShake();
                return;
            }

            float envelope = shakeTotalSeconds > 0f ? shakeRemainingSeconds / shakeTotalSeconds : 0f;
            float strength = ReferencePixelsToWorld(shakeStrengthReferencePixels) * envelope;
            var offset = new Vector3(
                Mathf.Sin(shakeElapsedSeconds * 91f),
                Mathf.Cos(shakeElapsedSeconds * 73f),
                0f) * strength;
            feedbackCamera.transform.localPosition = cameraBasePosition + offset;
        }

        // 重置 ResetShake 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void ResetShake()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (feedbackCamera != null)
            {
                feedbackCamera.transform.localPosition = cameraBasePosition;
            }

            shakeRemainingSeconds = 0f;
            shakeTotalSeconds = 0f;
            shakeStrengthReferencePixels = 0f;
            shakeElapsedSeconds = 0f;
        }

        // 处理 RestoreTarget 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void RestoreTarget(TargetState target)
        {
            target.FlashRemainingSeconds = 0f;
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < target.Renderers.Length; index += 1)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (target.Renderers[index] != null)
                {
                    target.Renderers[index].color = target.BaseColors[index];
                }
            }
        }

        // 处理 ReferencePixelsToWorld 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private float ReferencePixelsToWorld(float referencePixels)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (feedbackCamera.orthographic)
            {
                return referencePixels * ((feedbackCamera.orthographicSize * 2f) / referenceHeight);
            }

            return referencePixels / referenceHeight;
        }

        // 处理 ReadPositiveReferenceDimension 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static float ReadPositiveReferenceDimension(
            IConfigProvider provider,
            string key)
        {
            GlobalConfig row = provider.GetGlobal(key);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!string.Equals(row.ValueType, "int", StringComparison.Ordinal) ||
                !row.IntValue.HasValue ||
                row.IntValue.Value < 1L ||
                row.IntValue.Value > int.MaxValue)
            {
                throw new ArgumentException(
                    $"Global '{key}' must define a positive runtime reference dimension.",
                    nameof(provider));
            }

            return row.IntValue.Value;
        }

        // 处理 ParseColor 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static Color ParseColor(string value)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!ColorUtility.TryParseHtmlString(value, out Color color))
            {
                throw new ArgumentException($"Configured feedback color '{value}' is invalid.", "configProvider");
            }

            return color;
        }

        // 设置 SetLayerRecursively 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            Transform targetTransform = target.transform;
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < targetTransform.childCount; index += 1)
            {
                SetLayerRecursively(targetTransform.GetChild(index).gameObject, layer);
            }
        }

        // 处理 ThrowIfDisposed 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void ThrowIfDisposed()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(CombatFeedbackRuntime));
            }
        }
    }
}
