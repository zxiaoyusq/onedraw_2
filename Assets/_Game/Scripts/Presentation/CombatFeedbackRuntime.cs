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
        };

        private sealed class TargetState
        {
            internal TargetState(Transform target, SpriteRenderer[] targetRenderers)
            {
                Transform = target;
                Renderers = targetRenderers;
                BaseColors = new Color[targetRenderers.Length];
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

        private sealed class ActiveVfx
        {
            internal ActiveVfx(VfxPoolItem item, in PoolLease lease)
            {
                Item = item;
                Lease = lease;
            }

            internal VfxPoolItem Item { get; }
            internal PoolLease Lease { get; }
        }

        private sealed class ActiveDamageNumber
        {
            internal ActiveDamageNumber(DamageNumberPoolItem item, in PoolLease lease)
            {
                Item = item;
                Lease = lease;
            }

            internal DamageNumberPoolItem Item { get; }
            internal PoolLease Lease { get; }
        }

        private sealed class AudioChannel
        {
            private readonly AudioSource[] sources;
            private readonly float cooldownSeconds;
            private double lastPlayedTimestamp = double.NegativeInfinity;

            internal AudioChannel(AudioCueConfig cue, AudioClip clip, Transform parent)
            {
                if (cue.MaxConcurrent < 1L || cue.MaxConcurrent > int.MaxValue)
                {
                    throw new ArgumentException(
                        $"AudioCues row '{cue.AudioKey}' has invalid maxConcurrent.",
                        "configProvider");
                }

                cooldownSeconds = cue.CooldownSec;
                sources = new AudioSource[(int)cue.MaxConcurrent];
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

            internal bool TryPlay(double timestamp)
            {
                if (timestamp - lastPlayedTimestamp < cooldownSeconds)
                {
                    return false;
                }

                for (int index = 0; index < sources.Length; index += 1)
                {
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

            internal void Stop()
            {
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
            if (damageNumberFont == null)
            {
                throw new InvalidOperationException(
                    $"Damage-number font is missing from Resources: {DamageNumberFontResourcePath}");
            }

            var rootObject = new GameObject("Combat Feedback Runtime");
            rootObject.layer = parent != null ? parent.gameObject.layer : 0;
            root = rootObject.transform;
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

        public void RegisterTarget(int targetId, Transform target, params SpriteRenderer[] renderers)
        {
            ThrowIfDisposed();
            if (targetId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetId));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            }

            targets[targetId] = new TargetState(target, renderers);
        }

        public void UnregisterTarget(int targetId)
        {
            if (targets.TryGetValue(targetId, out TargetState target))
            {
                RestoreTarget(target);
                targets.Remove(targetId);
            }
        }

        public void Emit(in CombatFeedbackCommand command)
        {
            ThrowIfDisposed();
            if (!command.Event.IsValid || command.Profile == null)
            {
                throw new ArgumentException("Feedback command must be initialized.", nameof(command));
            }

            targets.TryGetValue(command.Event.TargetId, out TargetState target);
            Vector3 position = target?.Transform != null ? target.Transform.position : Vector3.zero;
            battleTime.ApplyGameplayScale(command.Profile.TimeScale, command.Profile.TimeScaleSeconds);
            EmitVfx(command.Profile, target?.Transform, position);
            if (command.Event.SignedAmount != 0L)
            {
                EmitDamageNumber(command, position);
            }

            ApplyFlash(target, command.Profile.FlashSeconds);
            ApplyShake(command.Profile);
            if (audioChannels[command.Profile.AudioKey].TryPlay(command.Event.Timestamp))
            {
                AudioPlayCount += 1;
            }

            EmittedCount += 1;
        }

        public void Advance(float unscaledDeltaSeconds)
        {
            ThrowIfDisposed();
            if (float.IsNaN(unscaledDeltaSeconds) ||
                float.IsInfinity(unscaledDeltaSeconds) ||
                unscaledDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaSeconds));
            }

            for (int index = activeVfx.Count - 1; index >= 0; index -= 1)
            {
                ActiveVfx active = activeVfx[index];
                if (active.Item.Advance(unscaledDeltaSeconds))
                {
                    pools.Release(active.Item, active.Lease, PoolReleaseReason.Completed);
                    activeVfx.RemoveAt(index);
                }
            }

            for (int index = activeDamageNumbers.Count - 1; index >= 0; index -= 1)
            {
                ActiveDamageNumber active = activeDamageNumbers[index];
                if (active.Item.Advance(unscaledDeltaSeconds))
                {
                    pools.Release(active.Item, active.Lease, PoolReleaseReason.Completed);
                    activeDamageNumbers.RemoveAt(index);
                }
            }

            foreach (TargetState target in targets.Values)
            {
                if (target.FlashRemainingSeconds <= 0f)
                {
                    continue;
                }

                target.FlashRemainingSeconds = Mathf.Max(0f, target.FlashRemainingSeconds - unscaledDeltaSeconds);
                if (target.FlashRemainingSeconds == 0f)
                {
                    RestoreTarget(target);
                }
            }

            AdvanceShake(unscaledDeltaSeconds);
        }

        public void Restart()
        {
            ThrowIfDisposed();
            pools.Restart();
            activeVfx.Clear();
            activeDamageNumbers.Clear();
            foreach (TargetState target in targets.Values)
            {
                RestoreTarget(target);
            }

            foreach (AudioChannel channel in audioChannels.Values)
            {
                channel.Stop();
            }

            ResetShake();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            Restart();
            pools.Dispose();
            disposed = true;
            if (root != null)
            {
                UnityObject.Destroy(root.gameObject);
            }
        }

        private void BuildCachedOutputs()
        {
            pools.RegisterFamily(ObjectPoolConfiguration.CreateVfxFamily(configProvider));
            pools.RegisterFamily(ObjectPoolConfiguration.CreateDamageNumberFamily(configProvider));
            var registeredVfx = new HashSet<string>(StringComparer.Ordinal);
            foreach (CombatFeedbackType type in FeedbackTypes)
            {
                CombatFeedbackProfile profile = settings.Get(type);
                if (registeredVfx.Add(profile.VfxKey))
                {
                    string vfxKey = profile.VfxKey;
                    pools.RegisterPool(ObjectPoolConfiguration.CreateVfxPool(
                        configProvider,
                        vfxKey,
                        () => CreateVfx(vfxKey)));
                }

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

        private VfxPoolItem CreateVfx(string vfxKey)
        {
            VfxCueConfig cue = configProvider.GetVfxCue(vfxKey);
            GameObject prefab = assetRegistry.GetPrefab(cue.AssetKey);
            GameObject instance = UnityObject.Instantiate(prefab, root, false);
            instance.name = $"Feedback VFX {vfxKey}";
            SetLayerRecursively(instance, root.gameObject.layer);
            VfxPoolItem item = instance.GetComponent<VfxPoolItem>();
            if (item == null)
            {
                item = instance.AddComponent<VfxPoolItem>();
            }

            instance.SetActive(false);
            item.Configure(configProvider, vfxKey);
            return item;
        }

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

        private void EmitVfx(
            CombatFeedbackProfile profile,
            Transform target,
            Vector3 position)
        {
            PoolAcquireResult result = pools.Acquire(ObjectPoolConfiguration.GetVfxPoolId(profile.VfxKey));
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

        private void EmitDamageNumber(in CombatFeedbackCommand command, Vector3 position)
        {
            PoolAcquireResult result = pools.Acquire(ObjectPoolConfiguration.DamageNumberPoolId);
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

        private static void RemovePreviousLease(IPoolable item, List<ActiveVfx> entries)
        {
            for (int index = entries.Count - 1; index >= 0; index -= 1)
            {
                if (ReferenceEquals(entries[index].Item, item))
                {
                    entries.RemoveAt(index);
                }
            }
        }

        private static void RemovePreviousLease(IPoolable item, List<ActiveDamageNumber> entries)
        {
            for (int index = entries.Count - 1; index >= 0; index -= 1)
            {
                if (ReferenceEquals(entries[index].Item, item))
                {
                    entries.RemoveAt(index);
                }
            }
        }

        private static void ApplyFlash(TargetState target, float durationSeconds)
        {
            if (target == null || durationSeconds <= 0f)
            {
                return;
            }

            target.FlashRemainingSeconds = Mathf.Max(target.FlashRemainingSeconds, durationSeconds);
            for (int index = 0; index < target.Renderers.Length; index += 1)
            {
                if (target.Renderers[index] != null)
                {
                    target.Renderers[index].color = Color.white;
                }
            }
        }

        private void ApplyShake(CombatFeedbackProfile profile)
        {
            if (profile.ShakeSeconds <= 0f || profile.ShakeStrengthReferencePixels <= 0f)
            {
                return;
            }

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

        private void AdvanceShake(float deltaSeconds)
        {
            if (shakeRemainingSeconds <= 0f)
            {
                return;
            }

            shakeRemainingSeconds = Mathf.Max(0f, shakeRemainingSeconds - deltaSeconds);
            shakeElapsedSeconds += deltaSeconds;
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

        private void ResetShake()
        {
            if (feedbackCamera != null)
            {
                feedbackCamera.transform.localPosition = cameraBasePosition;
            }

            shakeRemainingSeconds = 0f;
            shakeTotalSeconds = 0f;
            shakeStrengthReferencePixels = 0f;
            shakeElapsedSeconds = 0f;
        }

        private static void RestoreTarget(TargetState target)
        {
            target.FlashRemainingSeconds = 0f;
            for (int index = 0; index < target.Renderers.Length; index += 1)
            {
                if (target.Renderers[index] != null)
                {
                    target.Renderers[index].color = target.BaseColors[index];
                }
            }
        }

        private float ReferencePixelsToWorld(float referencePixels)
        {
            if (feedbackCamera.orthographic)
            {
                return referencePixels * ((feedbackCamera.orthographicSize * 2f) / referenceHeight);
            }

            return referencePixels / referenceHeight;
        }

        private static float ReadPositiveReferenceDimension(
            IConfigProvider provider,
            string key)
        {
            GlobalConfig row = provider.GetGlobal(key);
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

        private static Color ParseColor(string value)
        {
            if (!ColorUtility.TryParseHtmlString(value, out Color color))
            {
                throw new ArgumentException($"Configured feedback color '{value}' is invalid.", "configProvider");
            }

            return color;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            Transform targetTransform = target.transform;
            for (int index = 0; index < targetTransform.childCount; index += 1)
            {
                SetLayerRecursively(targetTransform.GetChild(index).gameObject, layer);
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(CombatFeedbackRuntime));
            }
        }
    }
}
