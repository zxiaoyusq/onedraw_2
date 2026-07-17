using System;
using OneStrokeDemon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneStrokeDemon.Bootstrap
{
    // 定义 SceneFlowService 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    public sealed class SceneFlowService : ISceneFlowService
    {
        // 加载 LoadMainMenu 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public AsyncOperation LoadMainMenu()
        {
            return Load(SceneNames.MainMenu);
        }

        // 加载 LoadBattle 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public AsyncOperation LoadBattle()
        {
            return Load(SceneNames.Battle);
        }

        // 加载 Load 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static AsyncOperation Load(string sceneName)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new InvalidOperationException($"Scene '{sceneName}' is not enabled in build settings.");
            }

            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
    }
}
