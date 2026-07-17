using UnityEngine;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 保存当前进程唯一的资源注册表，并控制一次性初始化生命周期。
    /// </summary>
    public static class AssetRegistryRuntime
    {
        private static IAssetRegistry current;
        private static AssetRegistryLoadSummary currentSummary;

        /// <summary>获取资源注册表运行时是否已成功初始化。</summary>
        public static bool IsReady => current != null;

        /// <summary>获取已发布的资源注册表；尚未初始化时抛出明确生命周期异常。</summary>
        public static IAssetRegistry Current
        {
            get
            {
                if (current == null)
                {
                    throw new AssetRegistryException(
                        "ARREG001",
                        "Runtime asset registry has not been initialized.",
                        "runtime",
                        "lifecycle");
                }

                return current;
            }
        }

        /// <summary>获取已发布的注册表装载摘要；尚未初始化时抛出明确生命周期异常。</summary>
        public static AssetRegistryLoadSummary CurrentSummary
        {
            get
            {
                if (currentSummary == null)
                {
                    throw new AssetRegistryException(
                        "ARREG001",
                        "Runtime asset registry summary is unavailable.",
                        "runtime",
                        "lifecycle");
                }

                return currentSummary;
            }
        }

        /// <summary>校验注册表与配置清单，并在全部验证成功后一次性发布全局状态。</summary>
        public static AssetRegistryLoadSummary Initialize(
            AssetRegistrySO registry,
            IConfigProvider config,
            string source)
        {
            if (current != null)
            {
                throw new AssetRegistryException(
                    "ARREG001",
                    "Runtime asset registry was already initialized.",
                    source,
                    "lifecycle");
            }

            // 先在局部服务中验证完整注册表，避免失败时发布部分资源键。
            var service = new AssetRegistryService();
            AssetRegistryLoadSummary summary = service.Load(registry, config, source);
            current = service;
            currentSummary = summary;
            return summary;
        }

        /// <summary>Unity 子系统重新注册时清理静态状态，兼容关闭域重载的编辑器设置。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            ResetForTests();
        }

        /// <summary>为测试隔离清理全局注册表状态。</summary>
        internal static void ResetForTests()
        {
            current = null;
            currentSummary = null;
        }
    }
}
