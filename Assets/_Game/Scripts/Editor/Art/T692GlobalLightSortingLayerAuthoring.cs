using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace OneStrokeDemon.Editor.Art
{
    /// <summary>统一修复生产场景的2D全局光层覆盖，避免Lit角色因未被照亮而显示全黑。</summary>
    public static class T692GlobalLightSortingLayerAuthoring
    {
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";
        public const string MainMenuScenePath = "Assets/_Game/Scenes/MainMenu.unity";
        public const string BattleScenePath = "Assets/_Game/Scenes/Battle.unity";

        private static readonly string[] ProductionScenePaths =
        {
            BootstrapScenePath,
            MainMenuScenePath,
            BattleScenePath,
        };

        /// <summary>返回需要保持灯光覆盖一致的生产场景路径。</summary>
        public static IReadOnlyList<string> ScenePaths => ProductionScenePaths;

        [MenuItem("One Stroke Demon/Art/Create or Repair T692 Global Light Coverage")]
        public static void CreateOrRepair()
        {
            int[] requiredLayerIds = SortingLayer.layers.Select(layer => layer.id).ToArray();
            if (requiredLayerIds.Length == 0)
            {
                throw new InvalidOperationException("Project must define at least one Sorting Layer.");
            }

            foreach (string scenePath in ProductionScenePaths)
            {
                ApplyToScene(scenePath, requiredLayerIds);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"T692_GLOBAL_LIGHT_COVERAGE_PASS scenes={ProductionScenePaths.Length} " +
                $"layers={requiredLayerIds.Length}");
        }

        /// <summary>只通过Unity场景API更新既有Global Light 2D，并保持当前打开场景不变。</summary>
        private static void ApplyToScene(string scenePath, int[] requiredLayerIds)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForRepair = !scene.IsValid() || !scene.isLoaded;
            if (openedForRepair)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            try
            {
                Light2D[] globalLights = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Light2D>(true))
                    .Where(light => light.lightType == Light2D.LightType.Global)
                    .ToArray();
                if (globalLights.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Scene '{scenePath}' must contain exactly one Global Light 2D, " +
                        $"but found {globalLights.Length}.");
                }

                Light2D globalLight = globalLights[0];
                globalLight.targetSortingLayers = requiredLayerIds;
                EditorUtility.SetDirty(globalLight);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    throw new InvalidOperationException($"Failed to save scene '{scenePath}'.");
                }
            }
            finally
            {
                if (openedForRepair && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
