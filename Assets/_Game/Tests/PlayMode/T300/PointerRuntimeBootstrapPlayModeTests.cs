using System.Collections;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T300
{
    [Category("PointerInput")]
    public sealed class PointerRuntimeBootstrapPlayModeTests
    {
        [SetUp]
        public void ResetRuntimeServices()
        {
            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
        }

        [TearDown]
        public void TearDownRuntimeServices()
        {
            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator BootstrapInitializesPointerRuntimeFromConfigBeforeMainMenu()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            Assert.That(PointerInputRuntime.IsReady, Is.True);
            GlobalConfig width = GameplayConfigRuntime.Current.GetGlobal(ConfigIds.GlobalKeys.ReferenceWidth);
            GlobalConfig height = GameplayConfigRuntime.Current.GetGlobal(ConfigIds.GlobalKeys.ReferenceHeight);
            Vector2 referenceResolution = PointerInputRuntime.CurrentSummary.ReferenceResolution;
            Assert.That(referenceResolution.x, Is.EqualTo((float)width.IntValue.Value));
            Assert.That(referenceResolution.y, Is.EqualTo((float)height.IntValue.Value));
            Assert.That(PointerInputRuntime.Current, Is.TypeOf<InputSystemPointerAdapter>());
            Assert.That(
                PointerInputRuntime.CurrentSummary.ToLogMessage(),
                Does.Contain("safeArea=dynamic uiBeginBlock=true maxActivePointers=1"));
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
    }
}
