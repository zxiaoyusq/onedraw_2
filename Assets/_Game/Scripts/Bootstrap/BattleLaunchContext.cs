using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Levels;
using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    // 定义 BattleLaunchContext 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    public static class BattleLaunchContext
    {
        private static string selectedLevelId;

        public static bool HasSelection => !string.IsNullOrEmpty(selectedLevelId);

        public static string SelectedLevelId => selectedLevelId ?? string.Empty;

        // 处理 Select 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public static void Select(IConfigProvider configProvider, string levelId)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (string.IsNullOrWhiteSpace(levelId) ||
                !string.Equals(levelId, levelId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Level id must be non-empty and trimmed.",
                    nameof(levelId));
            }

            selectedLevelId = configProvider.GetLevel(levelId).LevelId;
        }

        // 清理 Clear 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public static void Clear()
        {
            selectedLevelId = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        // 重置 ResetOnSubsystemRegistration 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static void ResetOnSubsystemRegistration()
        {
            Clear();
        }
    }

    // 定义 PlayerPrefsProgressSaveStore 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    public sealed class PlayerPrefsProgressSaveStore : IProgressSaveStore
    {
        public const string StorageKey = "one_stroke_demon.progress.v1";

        // 尝试执行 TryRead 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public bool TryRead(out string payload)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!PlayerPrefs.HasKey(StorageKey))
            {
                payload = null;
                return false;
            }

            payload = PlayerPrefs.GetString(StorageKey, string.Empty);
            return true;
        }

        // 处理 Write 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void Write(string payload)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            PlayerPrefs.SetString(StorageKey, payload);
            PlayerPrefs.Save();
        }
    }
}
