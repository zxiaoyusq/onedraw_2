using System.Collections;
using NUnit.Framework;
using OneStrokeDemon.Bootstrap;
using OneStrokeDemon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode
{
    public sealed class SceneFlowSmokePlayModeTests
    {
        [UnityTest]
        public IEnumerator BootstrapLoadsMainMenuThenSceneFlowLoadsBattleGraybox()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            AssertSceneSkeleton(
                SceneNames.MainMenu,
                "MainMenuGraybox",
                expectedGrayboxActive: true);

            ISceneFlowService sceneFlow = new SceneFlowService();
            yield return sceneFlow.LoadBattle();

            AssertSceneSkeleton(
                SceneNames.Battle,
                "BattleGraybox",
                expectedGrayboxActive: false);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }

        private static void AssertSceneSkeleton(
            string sceneName,
            string grayboxRoot,
            bool expectedGrayboxActive)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Assert.That(activeScene.name, Is.EqualTo(sceneName));
            GameObject graybox = FindSceneObject(activeScene, grayboxRoot);
            Assert.That(graybox, Is.Not.Null);
            Assert.That(graybox.activeSelf, Is.EqualTo(expectedGrayboxActive));
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(GameObject.Find("Global Light 2D"), Is.Not.Null);
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index].scene == scene && objects[index].name == objectName)
                {
                    return objects[index];
                }
            }

            return null;
        }
    }
}
