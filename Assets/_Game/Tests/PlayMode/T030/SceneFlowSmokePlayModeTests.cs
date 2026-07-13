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

            AssertSceneSkeleton(SceneNames.MainMenu, "MainMenuGraybox");

            ISceneFlowService sceneFlow = new SceneFlowService();
            yield return sceneFlow.LoadBattle();

            AssertSceneSkeleton(SceneNames.Battle, "BattleGraybox");
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

        private static void AssertSceneSkeleton(string sceneName, string grayboxRoot)
        {
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
            Assert.That(GameObject.Find(grayboxRoot), Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(GameObject.Find("Global Light 2D"), Is.Not.Null);
        }
    }
}
