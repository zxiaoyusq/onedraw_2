using UnityEngine;

namespace OneStrokeDemon.Config
{
    public static class AssetRegistryRuntime
    {
        private static IAssetRegistry current;
        private static AssetRegistryLoadSummary currentSummary;

        public static bool IsReady => current != null;

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

            var service = new AssetRegistryService();
            AssetRegistryLoadSummary summary = service.Load(registry, config, source);
            current = service;
            currentSummary = summary;
            return summary;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            ResetForTests();
        }

        internal static void ResetForTests()
        {
            current = null;
            currentSummary = null;
        }
    }
}
