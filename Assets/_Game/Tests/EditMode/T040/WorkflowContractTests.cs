using System.IO;
using NUnit.Framework;
using OneStrokeDemon.Editor.Build;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode
{
    public sealed class WorkflowContractTests
    {
        [Test]
        public void WebBuildEntryUsesEnabledScenesAndWebGlWithoutMutatingSettings()
        {
            string[] scenesBefore = EnabledScenePaths();

            BuildPlayerOptions options = WebBuildEntry.CreateBuildOptions("Builds/WebGL", false);

            Assert.That(options.target, Is.EqualTo(BuildTarget.WebGL));
            Assert.That(options.options, Is.EqualTo(BuildOptions.None));
            Assert.That(options.scenes, Is.EqualTo(scenesBefore));
            Assert.That(options.scenes[0], Is.EqualTo(WebBuildEntry.BootstrapScenePath));
            Assert.That(Path.IsPathRooted(options.locationPathName), Is.True);
            Assert.That(options.locationPathName, Does.EndWith(WebBuildEntry.DefaultOutputPath));
            Assert.That(EnabledScenePaths(), Is.EqualTo(scenesBefore));
        }

        [Test]
        public void WorkflowFilesExposeTestEvidenceAndWhitelistContracts()
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            Assert.That(root, Is.Not.Null);

            AssertFileContains(root, "Tools/CI/run-unity-tests.sh", "-testResults");
            AssertFileContains(root, "Tools/CI/run-unity-tests.sh", "check-unity-test-results.py");
            AssertFileContains(root, "Tools/CI/build-web.sh", "WebBuildEntry.BuildFromCommandLine");
            AssertFileContains(root, "templates/verification.md", "结论");
            AssertFileContains(root, "templates/change-whitelist.md", "预计改动白名单");
            AssertFileContains(root, "docs/WORKFLOW.md", "一个任务一个提交");
        }

        private static string[] EnabledScenePaths()
        {
            var scenes = EditorBuildSettings.scenes;
            var enabled = new System.Collections.Generic.List<string>(scenes.Length);
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.enabled)
                {
                    enabled.Add(scene.path);
                }
            }

            return enabled.ToArray();
        }

        private static void AssertFileContains(string root, string relativePath, string expected)
        {
            string path = Path.Combine(root, relativePath);
            Assert.That(File.Exists(path), Is.True, $"Missing workflow file: {relativePath}");
            Assert.That(File.ReadAllText(path), Does.Contain(expected), relativePath);
        }
    }
}
