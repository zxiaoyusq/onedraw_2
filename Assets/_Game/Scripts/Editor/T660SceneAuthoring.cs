using OneStrokeDemon.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneStrokeDemon.Editor
{
    // 定义 T660SceneAuthoring 的编辑器工具职责，集中管理资源生成、验证或构建入口。
    public static class T660SceneAuthoring
    {
        private const string MainMenuPath = "Assets/_Game/Scenes/MainMenu.unity";
        private const string BattlePath = "Assets/_Game/Scenes/Battle.unity";

        [MenuItem("One Stroke Demon/Scenes/Apply T660 Production Roots")]
        // 应用 Apply 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public static void Apply()
        {
            EnsureProductionRoot<MainMenuCompositionRoot>(
                MainMenuPath,
                "Production Main Menu Root");
            EnsureProductionRoot<BattleCompositionRoot>(
                BattlePath,
                "Production Battle Root");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("T660_SCENE_AUTHORING_PASS scenes=MainMenu,Battle");
        }

        private static void EnsureProductionRoot<T>(string path, string rootName)
            where T : Component
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GameObject root = null;
            GameObject[] roots = scene.GetRootGameObjects();
            // 逐项处理资源或配置条目，保证生成与验证顺序稳定。
            for (int index = 0; index < roots.Length; index++)
            {
                // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
                if (roots[index].name == rootName)
                {
                    root = roots[index];
                }

                T[] staleRoots = roots[index].GetComponentsInChildren<T>(true);
                // 逐项处理资源或配置条目，保证生成与验证顺序稳定。
                for (int componentIndex = 0; componentIndex < staleRoots.Length; componentIndex++)
                {
                    // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
                    if (root == null || staleRoots[componentIndex].gameObject != root)
                    {
                        Object.DestroyImmediate(staleRoots[componentIndex]);
                    }
                }
            }

            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (root == null)
            {
                root = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (root.GetComponent<T>() == null)
            {
                root.AddComponent<T>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
