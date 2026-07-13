using UnityEngine;

namespace OneStrokeDemon.Config
{
    public static class GameplayConfigRuntime
    {
        private static IConfigProvider current;
        private static GameplayConfigLoadSummary currentSummary;

        public static bool IsReady => current != null;

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

            var service = new GameplayConfigService();
            GameplayConfigLoadSummary summary = service.Load(json, source);
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
