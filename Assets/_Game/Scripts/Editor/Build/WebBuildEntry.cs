using System;
using System.IO;
using System.Linq;
using OneStrokeDemon.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OneStrokeDemon.Editor.Build
{
    public static class WebBuildEntry
    {
        public const string DefaultOutputPath = "Builds/WebGL";
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";
        public const string WebSmokeDefine = "T100_WEB_SMOKE";

        [MenuItem("One Stroke Demon/Build/Standard WebGL")]
        public static void BuildFromMenu()
        {
            Build(DefaultOutputPath, false, false);
        }

        public static void BuildFromCommandLine()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string outputPath = ReadArgument(arguments, "-buildOutput") ?? DefaultOutputPath;
            bool development = arguments.Contains("-developmentBuild");
            bool webSmoke = arguments.Contains("-webSmoke");
            Build(outputPath, development, webSmoke);
        }

        public static BuildPlayerOptions CreateBuildOptions(
            string outputPath,
            bool development,
            bool webSmoke = false)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Web build output path cannot be empty.", nameof(outputPath));
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes are configured in Build Settings.");
            }
            if (scenes[0] != BootstrapScenePath)
            {
                throw new BuildFailedException(
                    $"Build index 0 must be {BootstrapScenePath}, but was {scenes[0]}.");
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new BuildFailedException("Unable to resolve Unity project root.");
            string absoluteOutput = Path.GetFullPath(
                Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(projectRoot, outputPath));

            return new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = absoluteOutput,
                target = BuildTarget.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None,
                extraScriptingDefines = webSmoke ? new[] { WebSmokeDefine } : Array.Empty<string>()
            };
        }

        private static void Build(string outputPath, bool development, bool webSmoke)
        {
            BuildPlayerOptions options = CreateBuildOptions(outputPath, development, webSmoke);
            Directory.CreateDirectory(options.locationPathName);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"WebGL build {report.summary.result}: " +
                    $"errors={report.summary.totalErrors}, warnings={report.summary.totalWarnings}");
            }

            Debug.Log(
                $"WEB_BUILD_PASS output={options.locationPathName} " +
                $"size={report.summary.totalSize} duration={report.summary.totalTime} " +
                $"smoke={webSmoke}");
        }

        private static string ReadArgument(string[] arguments, string name)
        {
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name)
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }
    }
}
