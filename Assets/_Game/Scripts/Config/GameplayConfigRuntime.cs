using UnityEngine;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 保存当前进程唯一的玩法配置提供者，并控制一次性初始化生命周期。
    /// </summary>
    public static class GameplayConfigRuntime
    {
        private static IConfigProvider current;
        private static GameplayConfigLoadSummary currentSummary;

        /// <summary>获取配置运行时是否已成功初始化。</summary>
        public static bool IsReady => current != null;

        /// <summary>获取已发布的配置提供者；尚未初始化时抛出明确生命周期异常。</summary>
        public static IConfigProvider Current
        {
            get
            {
                if (current == null)
                {
                    throw new GameplayConfigException(
                        "CFGRT001",
                        "Runtime configuration has not been initialized.",
                        "runtime",
                        "lifecycle");
                }

                return current;
            }
        }

        /// <summary>获取已发布的配置装载摘要；尚未初始化时抛出明确生命周期异常。</summary>
        public static GameplayConfigLoadSummary CurrentSummary
        {
            get
            {
                if (currentSummary == null)
                {
                    throw new GameplayConfigException(
                        "CFGRT001",
                        "Runtime configuration summary is unavailable.",
                        "runtime",
                        "lifecycle");
                }

                return currentSummary;
            }
        }

        /// <summary>严格装载 JSON，并在全部验证成功后一次性发布全局配置与摘要。</summary>
        public static GameplayConfigLoadSummary Initialize(string json, string source)
        {
            if (current != null)
            {
                throw new GameplayConfigException(
                    "CFGRT001",
                    "Runtime configuration was already initialized.",
                    source,
                    "lifecycle");
            }

            // 先在局部服务中完成完整装载，避免失败时向全局暴露半成品状态。
            var service = new GameplayConfigService();
            GameplayConfigLoadSummary summary = service.Load(json, source);
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

        /// <summary>为测试隔离清理全局配置状态。</summary>
        internal static void ResetForTests()
        {
            current = null;
            currentSummary = null;
        }
    }
}
