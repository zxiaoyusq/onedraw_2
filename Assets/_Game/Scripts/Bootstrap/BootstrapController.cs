using System;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    // 定义 BootstrapController 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    public sealed class BootstrapController : MonoBehaviour
    {
        [SerializeField]
        private TextAsset gameplayConfig;

        [SerializeField]
        private AssetRegistrySO assetRegistry;

        // 启动 Start 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void Start()
        {
            const string configSource = "TextAsset:gameplay_config";
            const string registrySource = "AssetRegistry:asset_registry";
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (gameplayConfig == null)
            {
                Debug.LogError($"CONFIG_RUNTIME_FAILED source={configSource} reason=missing_text_asset");
                return;
            }

            GameplayConfigLoadSummary configSummary;
            try
            {
                configSummary = GameplayConfigRuntime.IsReady
                    ? GameplayConfigRuntime.CurrentSummary
                    : GameplayConfigRuntime.Initialize(gameplayConfig.text, configSource);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CONFIG_RUNTIME_FAILED error={exception.Message}");
                return;
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (assetRegistry == null)
            {
                Debug.LogError($"ASSET_REGISTRY_FAILED source={registrySource} reason=missing_registry_asset");
                return;
            }

            AssetRegistryLoadSummary registrySummary;
            try
            {
                registrySummary = AssetRegistryRuntime.IsReady
                    ? AssetRegistryRuntime.CurrentSummary
                    : AssetRegistryRuntime.Initialize(
                        assetRegistry,
                        GameplayConfigRuntime.Current,
                        registrySource);
            }
            catch (Exception exception)
            {
                Debug.LogError($"ASSET_REGISTRY_FAILED error={exception.Message}");
                return;
            }

            PointerInputRuntimeSummary pointerSummary;
            try
            {
                pointerSummary = PointerInputRuntime.Initialize(ReadReferenceResolution());
            }
            catch (Exception exception)
            {
                Debug.LogError($"POINTER_INPUT_FAILED error={exception.Message}");
                return;
            }

            Debug.Log(configSummary.ToLogMessage());
            Debug.Log(registrySummary.ToLogMessage());
            Debug.Log(pointerSummary.ToLogMessage());
            new SceneFlowService().LoadMainMenu();
        }

        // 处理 ReadReferenceResolution 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static Vector2 ReadReferenceResolution()
        {
            float width = ReadPositiveIntegerGlobal(ConfigIds.GlobalKeys.ReferenceWidth);
            float height = ReadPositiveIntegerGlobal(ConfigIds.GlobalKeys.ReferenceHeight);
            return new Vector2(width, height);
        }

        // 处理 ReadPositiveIntegerGlobal 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static float ReadPositiveIntegerGlobal(string key)
        {
            GlobalConfig value = GameplayConfigRuntime.Current.GetGlobal(key);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!string.Equals(value.ValueType, "int", StringComparison.Ordinal) ||
                !value.IntValue.HasValue || value.IntValue.Value <= 0)
            {
                throw new InvalidOperationException(
                    $"Global '{key}' must provide a positive int reference-pixel value.");
            }

            return value.IntValue.Value;
        }
    }
}
