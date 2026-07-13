using System;
using OneStrokeDemon.Config;
using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    public sealed class BootstrapController : MonoBehaviour
    {
        [SerializeField]
        private TextAsset gameplayConfig;

        private void Start()
        {
            const string source = "TextAsset:gameplay_config";
            if (gameplayConfig == null)
            {
                Debug.LogError($"CONFIG_RUNTIME_FAILED source={source} reason=missing_text_asset");
                return;
            }

            try
            {
                GameplayConfigLoadSummary summary = GameplayConfigRuntime.IsReady
                    ? GameplayConfigRuntime.CurrentSummary
                    : GameplayConfigRuntime.Initialize(gameplayConfig.text, source);
                Debug.Log(summary.ToLogMessage());
                new SceneFlowService().LoadMainMenu();
            }
            catch (Exception exception)
            {
                Debug.LogError($"CONFIG_RUNTIME_FAILED source={source} error={exception.Message}");
            }
        }
    }
}
