using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Config
{
    public enum AssetRegistryServiceState
    {
        Uninitialized,
        Ready,
        Failed,
    }

    public sealed class AssetRegistryService : IAssetRegistry
    {
        private IReadOnlyDictionary<string, UnityObject> assets;

        public AssetRegistryServiceState State { get; private set; } = AssetRegistryServiceState.Uninitialized;

        public AssetRegistryLoadSummary Summary { get; private set; }

        public int Count => RequireAssets().Count;

        public AssetRegistryLoadSummary Load(
            AssetRegistrySO registry,
            IConfigProvider config,
            string source)
        {
            if (State != AssetRegistryServiceState.Uninitialized)
            {
                throw Failure(
                    "ARREG001",
                    $"A registry service may load exactly once; current state is {State}.",
                    source ?? string.Empty,
                    "lifecycle");
            }

            try
            {
                if (registry == null)
                {
                    throw Failure("ARREG001", "AssetRegistrySO is missing.", source, "registry");
                }

                if (config == null)
                {
                    throw Failure("ARREG001", "IConfigProvider is missing.", source, "config");
                }

                if (string.IsNullOrWhiteSpace(source))
                {
                    throw new ArgumentException("A non-empty asset registry source is required.", nameof(source));
                }

                IReadOnlyList<AssetRegistryEntry> entries = registry.Entries;
                var candidate = new Dictionary<string, UnityObject>(entries.Count, StringComparer.Ordinal);
                for (int index = 0; index < entries.Count; index += 1)
                {
                    AssetRegistryEntry entry = entries[index];
                    if (entry == null || string.IsNullOrEmpty(entry.AssetKey))
                    {
                        throw Failure("ARREG002", "Registry key is empty.", source, $"entries[{index}].assetKey");
                    }

                    if (entry.Asset == null)
                    {
                        throw Failure(
                            "ARREG003",
                            $"Registry object for '{entry.AssetKey}' is missing.",
                            source,
                            entry.AssetKey);
                    }

                    if (candidate.ContainsKey(entry.AssetKey))
                    {
                        throw Failure(
                            "ARREG004",
                            $"Duplicate registry key '{entry.AssetKey}'.",
                            source,
                            entry.AssetKey);
                    }

                    candidate.Add(entry.AssetKey, entry.Asset);
                }

                int prefabCount = 0;
                int spriteCount = 0;
                int audioClipCount = 0;
                int sceneCount = 0;
                IReadOnlyList<AssetManifestConfig> manifest = config.GetAssetManifest();
                var expectedKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (AssetManifestConfig expected in manifest)
                {
                    expectedKeys.Add(expected.AssetKey);
                    if (!candidate.TryGetValue(expected.AssetKey, out UnityObject asset))
                    {
                        throw Failure(
                            "ARREG005",
                            $"Required AssetManifest key '{expected.AssetKey}' is missing from the registry.",
                            source,
                            expected.AssetKey);
                    }

                    ValidateExpectedType(expected, asset, source);
                    switch (expected.AssetType)
                    {
                        case "Prefab":
                            prefabCount += 1;
                            break;
                        case "Sprite":
                            spriteCount += 1;
                            break;
                        case "AudioClip":
                            audioClipCount += 1;
                            break;
                        case "Scene":
                            sceneCount += 1;
                            break;
                    }
                }

                foreach (string registryKey in candidate.Keys)
                {
                    if (!expectedKeys.Contains(registryKey))
                    {
                        throw Failure(
                            "ARREG006",
                            $"Registry key '{registryKey}' is not declared by AssetManifest.",
                            source,
                            registryKey);
                    }
                }

                var summary = new AssetRegistryLoadSummary(
                    source,
                    config.ContentHash,
                    candidate.Count,
                    prefabCount,
                    spriteCount,
                    audioClipCount,
                    sceneCount);
                assets = new ReadOnlyDictionary<string, UnityObject>(candidate);
                Summary = summary;
                State = AssetRegistryServiceState.Ready;
                return summary;
            }
            catch
            {
                State = AssetRegistryServiceState.Failed;
                throw;
            }
        }

        public UnityObject GetObject(string assetKey)
        {
            IReadOnlyDictionary<string, UnityObject> current = RequireAssets();
            if (assetKey != null && current.TryGetValue(assetKey, out UnityObject asset))
            {
                return asset;
            }

            throw Failure(
                "ARREG009",
                $"Unknown asset key '{assetKey ?? "<null>"}'.",
                Summary.Source,
                assetKey ?? "<null>");
        }

        public T Get<T>(string assetKey) where T : UnityObject
        {
            UnityObject asset = GetObject(assetKey);
            if (asset is T typed)
            {
                return typed;
            }

            throw Failure(
                "ARREG008",
                $"Asset '{assetKey}' is {asset.GetType().Name}, not {typeof(T).Name}.",
                Summary.Source,
                assetKey);
        }

        public GameObject GetPrefab(string assetKey) => Get<GameObject>(assetKey);

        public Sprite GetSprite(string assetKey) => Get<Sprite>(assetKey);

        public AudioClip GetAudioClip(string assetKey) => Get<AudioClip>(assetKey);

        public AssetSceneReference GetScene(string assetKey) => Get<AssetSceneReference>(assetKey);

        private static void ValidateExpectedType(
            AssetManifestConfig expected,
            UnityObject asset,
            string source)
        {
            bool valid = expected.AssetType switch
            {
                "Prefab" => asset is GameObject,
                "Sprite" => asset is Sprite,
                "AudioClip" => asset is AudioClip,
                "Scene" => asset is AssetSceneReference,
                _ => throw Failure(
                    "ARREG007",
                    $"Unsupported AssetManifest type '{expected.AssetType}'.",
                    source,
                    expected.AssetKey),
            };
            if (!valid)
            {
                throw Failure(
                    "ARREG007",
                    $"Asset '{expected.AssetKey}' expects {expected.AssetType}, but registry object is " +
                    $"{asset.GetType().Name}.",
                    source,
                    expected.AssetKey);
            }
        }

        private IReadOnlyDictionary<string, UnityObject> RequireAssets()
        {
            if (State != AssetRegistryServiceState.Ready || assets == null)
            {
                throw Failure(
                    "ARREG001",
                    $"Asset registry is unavailable while service state is {State}.",
                    Summary?.Source ?? "uninitialized",
                    "lifecycle");
            }

            return assets;
        }

        private static AssetRegistryException Failure(
            string code,
            string message,
            string source,
            string context)
        {
            return new AssetRegistryException(code, message, source ?? string.Empty, context ?? string.Empty);
        }
    }
}
