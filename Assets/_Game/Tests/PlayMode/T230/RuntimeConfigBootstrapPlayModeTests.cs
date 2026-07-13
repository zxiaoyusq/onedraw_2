using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneStrokeDemon.Bootstrap;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T230
{
    public sealed class RuntimeConfigBootstrapPlayModeTests
    {
        private const string IncompatibleSchemaJson =
            "{\"schemaVersion\":999,\"contentVersion\":\"0.1.1-sample\"," +
            "\"contentHash\":\"0000000000000000000000000000000000000000000000000000000000000000\"," +
            "\"global\":[],\"players\":[],\"stances\":[],\"strokeRules\":[]," +
            "\"damageFormulas\":[],\"defenseRules\":[],\"weakpointRules\":[],\"movePatterns\":[]," +
            "\"enemies\":[],\"enemyAttacks\":[],\"projectiles\":[],\"buffs\":[],\"skills\":[]," +
            "\"skillEffects\":[],\"levels\":[],\"waves\":[],\"spawnPoints\":[],\"enemyModifiers\":[]," +
            "\"spawns\":[],\"bossPhases\":[],\"rewards\":[],\"tutorials\":[],\"texts\":[]," +
            "\"audioCues\":[],\"vfxCues\":[],\"assetManifest\":[],\"enums\":[],\"fieldDictionary\":[]}";

        [SetUp]
        public void SetUp()
        {
            GameplayConfigRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator BootstrapLoadsAndIndexesGeneratedSnapshotBeforeMainMenu()
        {
            LogAssert.Expect(
                LogType.Log,
                new Regex("CONFIG_RUNTIME_READY.*schema=1.*content=0\\.1\\.1-sample.*tables=28.*records=645"));

            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            Assert.That(GameplayConfigRuntime.IsReady, Is.True);
            Assert.That(GameplayConfigRuntime.CurrentSummary.RecordCount, Is.EqualTo(645));
            Assert.That(GameplayConfigRuntime.Current.ContentHash, Is.EqualTo(
                "16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c"));
            Assert.That(GameplayConfigRuntime.Current.GetEnemy("boss_tomb_king").Tier, Is.EqualTo("Boss"));
        }

        [UnityTest]
        public IEnumerator IncompatibleVersionLogsContextAndBlocksSceneAdvance()
        {
            Scene testScene = SceneManager.CreateScene("T230InvalidConfigScene");
            SceneManager.SetActiveScene(testScene);
            var root = new GameObject("InvalidConfigBootstrap");
            BootstrapController controller = root.AddComponent<BootstrapController>();
            var invalidAsset = new TextAsset(IncompatibleSchemaJson);
            typeof(BootstrapController)
                .GetField("gameplayConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, invalidAsset);
            LogAssert.Expect(
                LogType.Error,
                new Regex("CONFIG_RUNTIME_FAILED.*CFGRT003.*schemaVersion"));

            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("T230InvalidConfigScene"));
            Assert.That(GameplayConfigRuntime.IsReady, Is.False);
            Object.Destroy(root);
            Object.Destroy(invalidAsset);
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
