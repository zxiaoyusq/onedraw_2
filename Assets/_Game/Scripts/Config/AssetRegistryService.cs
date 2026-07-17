using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Config
{
    /// <summary>表示单个资源注册表服务实例的一次性装载状态。</summary>
    public enum AssetRegistryServiceState
    {
        /// <summary>尚未尝试装载。</summary>
        Uninitialized,

        /// <summary>清单覆盖与类型验证通过，注册表已发布。</summary>
        Ready,

        /// <summary>装载失败，实例不可重试。</summary>
        Failed,
    }

    /// <summary>
    /// 校验 AssetRegistrySO 与配置 AssetManifest 完全一致，并提供强类型资源查询。
    /// </summary>
    public sealed class AssetRegistryService : IAssetRegistry
    {
        private IReadOnlyDictionary<string, UnityObject> assets;

        /// <summary>获取当前一次性装载状态。</summary>
        public AssetRegistryServiceState State { get; private set; } = AssetRegistryServiceState.Uninitialized;

        /// <summary>获取成功装载摘要；未成功时为 null。</summary>
        public AssetRegistryLoadSummary Summary { get; private set; }

        /// <summary>获取已发布注册表的资源总数。</summary>
        public int Count => RequireAssets().Count;

        /// <summary>验证资源键、对象、类型和清单覆盖，并原子发布只读注册表。</summary>
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

                // 第一阶段只验证序列化注册项自身，并在局部字典中拒绝空键、空对象和重复键。
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

                // 第二阶段以配置 AssetManifest 为唯一真相，检查每个声明键存在且对象类型正确。
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

                // 反向检查禁止注册未在配置清单声明的额外资源。
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

                // 只有双向覆盖和类型校验全部成功后才冻结并发布注册表。
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
                // 失败实例不可重试，避免调用方使用来源不明的部分注册表。
                State = AssetRegistryServiceState.Failed;
                throw;
            }
        }

        /// <summary>按资源键获取不限定具体类型的 Unity 对象。</summary>
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

        /// <summary>按资源键获取指定 Unity 对象类型，类型不匹配时抛出明确异常。</summary>
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

        /// <summary>按资源键获取预制体。</summary>
        public GameObject GetPrefab(string assetKey) => Get<GameObject>(assetKey);

        /// <summary>按资源键获取精灵。</summary>
        public Sprite GetSprite(string assetKey) => Get<Sprite>(assetKey);

        /// <summary>按资源键获取音频片段。</summary>
        public AudioClip GetAudioClip(string assetKey) => Get<AudioClip>(assetKey);

        /// <summary>按资源键获取场景引用。</summary>
        public AssetSceneReference GetScene(string assetKey) => Get<AssetSceneReference>(assetKey);

        /// <summary>按 AssetManifest 声明的资源类型校验实际 Unity 对象。</summary>
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

        /// <summary>返回已发布只读注册表；服务未就绪时抛出生命周期异常。</summary>
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

        /// <summary>创建空值安全且带稳定错误码的资源注册表异常。</summary>
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
