using System;
using OneStrokeDemon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneStrokeDemon.Bootstrap
{
    public sealed class SceneFlowService : ISceneFlowService
    {
        public AsyncOperation LoadMainMenu()
        {
            return Load(SceneNames.MainMenu);
        }

        public AsyncOperation LoadBattle()
        {
            return Load(SceneNames.Battle);
        }

        private static AsyncOperation Load(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new InvalidOperationException($"Scene '{sceneName}' is not enabled in build settings.");
            }

            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
    }
}
