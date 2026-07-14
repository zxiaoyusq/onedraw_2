using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    public static class BattleLaunchContext
    {
        private static string selectedLevelId;

        public static bool HasSelection => !string.IsNullOrEmpty(selectedLevelId);

        public static string SelectedLevelId => selectedLevelId ?? string.Empty;

        public static void Select(IConfigProvider configProvider, string levelId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (string.IsNullOrWhiteSpace(levelId) ||
                !string.Equals(levelId, levelId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Level id must be non-empty and trimmed.",
                    nameof(levelId));
            }

            selectedLevelId = configProvider.GetLevel(levelId).LevelId;
        }

        public static void Clear()
        {
            selectedLevelId = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Clear();
        }
    }

    public sealed class PlayerPrefsProgressSaveStore : IProgressSaveStore
    {
        public const string StorageKey = "one_stroke_demon.progress.v1";

        public bool TryRead(out string payload)
        {
            if (!PlayerPrefs.HasKey(StorageKey))
            {
                payload = null;
                return false;
            }

            payload = PlayerPrefs.GetString(StorageKey, string.Empty);
            return true;
        }

        public void Write(string payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            PlayerPrefs.SetString(StorageKey, payload);
            PlayerPrefs.Save();
        }
    }
}
