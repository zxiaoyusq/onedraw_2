using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T240
{
    [Category("ConfigPipeline")]
    public sealed class AssetRegistryBootstrapPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator BootstrapValidatesRegistryBeforeEnteringMainMenu()
        {
            LogAssert.Expect(
                LogType.Log,
                new Regex("CONFIG_RUNTIME_READY.*hash=5c3b73b.*tables=30.*records=763"));
            LogAssert.Expect(
                LogType.Log,
                new Regex("ASSET_REGISTRY_READY.*configHash=5c3b73b.*entries=77.*prefabs=43.*sprites=16.*audioClips=17.*scenes=1"));

            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            Assert.That(GameplayConfigRuntime.IsReady, Is.True);
            Assert.That(AssetRegistryRuntime.IsReady, Is.True);
            Assert.That(AssetRegistryRuntime.Current.Count, Is.EqualTo(77));
            Assert.That(AssetRegistryRuntime.Current.GetPrefab("boss_tomb_armor_king"), Is.Not.Null);
            Assert.That(AssetRegistryRuntime.Current.GetPrefab("char_moyan_idle"), Is.Not.Null);
            Assert.That(AssetRegistryRuntime.Current.GetAudioClip("audio_sfx_slash"), Is.Not.Null);
            Assert.That(AssetRegistryRuntime.Current.GetScene("scene_battle").SceneName, Is.EqualTo("Battle"));
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
