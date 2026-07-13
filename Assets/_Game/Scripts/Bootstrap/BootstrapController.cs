using System;
using OneStrokeDemon.Config;
using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    public sealed class BootstrapController : MonoBehaviour
    {
        [SerializeField]
        private TextAsset gameplayConfig;

        [SerializeField]
        private AssetRegistrySO assetRegistry;

        private void Start()
        {
            const string configSource = "TextAsset:gameplay_config";
            const string registrySource = "AssetRegistry:asset_registry";
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

            if (assetRegistry == null)
            {
                Debug.LogError($"ASSET_REGISTRY_FAILED source={registrySource} reason=missing_registry_asset");
                return;
            }

            try
            {
                AssetRegistryLoadSummary registrySummary = AssetRegistryRuntime.IsReady
                    ? AssetRegistryRuntime.CurrentSummary
                    : AssetRegistryRuntime.Initialize(
                        assetRegistry,
                        GameplayConfigRuntime.Current,
                        registrySource);
                Debug.Log(configSummary.ToLogMessage());
                Debug.Log(registrySummary.ToLogMessage());
                new SceneFlowService().LoadMainMenu();
            }
            catch (Exception exception)
            {
                Debug.LogError($"ASSET_REGISTRY_FAILED error={exception.Message}");
            }
        }
    }
}
