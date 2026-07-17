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
    // 定义 WebBuildEntry 的编辑器工具职责，集中管理资源生成、验证或构建入口。
    public static class WebBuildEntry
    {
        public const string DefaultOutputPath = "Builds/WebGL";
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";
        public const string WebSmokeDefine = "T100_WEB_SMOKE";

        [MenuItem("One Stroke Demon/Build/Standard WebGL")]
        // 构建 BuildFromMenu 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public static void BuildFromMenu()
        {
            Build(DefaultOutputPath, false, false);
        }

        // 构建 BuildFromCommandLine 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public static void BuildFromCommandLine()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string outputPath = ReadArgument(arguments, "-buildOutput") ?? DefaultOutputPath;
            bool development = arguments.Contains("-developmentBuild");
            bool webSmoke = arguments.Contains("-webSmoke");
            Build(outputPath, development, webSmoke);
        }

        // 创建 CreateBuildOptions 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        public static BuildPlayerOptions CreateBuildOptions(
            string outputPath,
            bool development,
            bool webSmoke = false)
        {
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Web build output path cannot be empty.", nameof(outputPath));
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes are configured in Build Settings.");
            }
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
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

        // 构建 Build 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static void Build(string outputPath, bool development, bool webSmoke)
        {
            BuildPlayerOptions options = CreateBuildOptions(outputPath, development, webSmoke);
            Directory.CreateDirectory(options.locationPathName);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
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

        // 处理 ReadArgument 对应的编辑器流程，并保持资源写入与校验结果可追踪。
        private static string ReadArgument(string[] arguments, string name)
        {
            // 逐项处理资源或配置条目，保证生成与验证顺序稳定。
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                // 检查编辑器输入、资源状态或写入边界，避免生成不完整资产。
                if (arguments[i] == name)
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }
    }
}
