using OneStrokeDemon.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneStrokeDemon.Editor
{
    public static class T660SceneAuthoring
    {
        private const string MainMenuPath = "Assets/_Game/Scenes/MainMenu.unity";
        private const string BattlePath = "Assets/_Game/Scenes/Battle.unity";

        [MenuItem("One Stroke Demon/Scenes/Apply T660 Production Roots")]
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
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == rootName)
                {
                    root = roots[index];
                }

                T[] staleRoots = roots[index].GetComponentsInChildren<T>(true);
                for (int componentIndex = 0; componentIndex < staleRoots.Length; componentIndex++)
                {
                    if (root == null || staleRoots[componentIndex].gameObject != root)
                    {
                        Object.DestroyImmediate(staleRoots[componentIndex]);
                    }
                }
            }

            if (root == null)
            {
                root = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            if (root.GetComponent<T>() == null)
            {
                root.AddComponent<T>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
