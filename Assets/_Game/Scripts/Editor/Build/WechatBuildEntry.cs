using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using WeChatWASM;

namespace OneStrokeDemon.Editor.Build
{
    public readonly struct WechatBuildPolicy
    {
        public WechatBuildPolicy(bool development)
        {
            AppId = string.Empty;
            Orientation = "landscape";
            MemorySizeMb = WechatBuildEntry.MemorySizeMb;
            Development = development;
            RenderThread = false;
            DisableMultiTouch = false;
            PerformanceAnalysis = false;
            CleanBuild = true;
            MultiThreadedBrotli = true;
        }

        public string AppId { get; }
        public string Orientation { get; }
        public int MemorySizeMb { get; }
        public bool Development { get; }
        public bool RenderThread { get; }
        public bool DisableMultiTouch { get; }
        public bool PerformanceAnalysis { get; }
        public bool CleanBuild { get; }
        public bool MultiThreadedBrotli { get; }
    }

    public static class WechatBuildEntry
    {
        public const string DefaultOutputPath = "Builds/WeChat/T120";
        public const string ProjectName = "OneStrokeDemon-T120-Spike";
        public const int MemorySizeMb = 256;

        public static readonly IReadOnlyList<string> RequiredMiniGameFiles = new[]
        {
            "minigame/game.js",
            "minigame/game.json",
            "minigame/project.config.json",
            "minigame/unity-namespace.js"
        };

        [MenuItem("One Stroke Demon/Build/WeChat T120 Spike")]
        public static void BuildFromMenu()
        {
            Build(DefaultOutputPath, false);
        }

        public static void BuildFromCommandLine()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string outputPath = ReadArgument(arguments, "-buildOutput") ?? DefaultOutputPath;
            bool development = arguments.Contains("-developmentBuild");
            Build(outputPath, development);
        }

        public static string ResolveOutputPath(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("WeChat build output path cannot be empty.", nameof(outputPath));
            }

            string projectRoot = GetProjectRoot();
            string allowedRoot = Path.GetFullPath(Path.Combine(projectRoot, "Builds/WeChat"));
            string absoluteOutput = Path.GetFullPath(
                Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(projectRoot, outputPath));
            string allowedPrefix = allowedRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!absoluteOutput.StartsWith(allowedPrefix, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"WeChat Spike output must be below {allowedRoot}, but was {absoluteOutput}.");
            }

            return absoluteOutput;
        }

        public static WechatBuildPolicy CreatePolicy(bool development)
        {
            return new WechatBuildPolicy(development);
        }

        private static void ApplyConfiguration(
            WXEditorScriptObject config,
            string absoluteOutput,
            WechatBuildPolicy policy)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.ProjectConf ??= new WXProjectConf();
            config.SDKOptions ??= new SDKOptions();
            config.CompileOptions ??= new CompileOptions();
            config.CompressTexture ??= new CompressTexture();
            config.FontOptions ??= new FontOptions();
            config.PlayerPrefsKeys ??= new List<string>();

            string projectRoot = GetProjectRoot();
            config.ProjectConf.projectName = ProjectName;
            config.ProjectConf.Appid = policy.AppId;
            config.ProjectConf.CDN = string.Empty;
            config.ProjectConf.AssetsUrl = string.Empty;
            config.ProjectConf.StreamCDN = string.Empty;
            config.ProjectConf.VideoUrl = string.Empty;
            config.ProjectConf.DST = absoluteOutput;
            config.ProjectConf.relativeDST = Path.GetRelativePath(projectRoot, absoluteOutput)
                .Replace('\\', '/');
            config.ProjectConf.Orientation = policy.Orientation == "landscape"
                ? WXScreenOritation.Landscape
                : throw new BuildFailedException($"Unsupported WeChat orientation: {policy.Orientation}");
            config.ProjectConf.MemorySize = policy.MemorySizeMb;
            config.ProjectConf.assetLoadType = 0;
            config.ProjectConf.compressDataPackage = false;
            config.ProjectConf.bgImageSrc = WXConvertCore.defaultImgSrc;

            config.SDKOptions.UseCompressedTexture = false;
            config.SDKOptions.PreloadWXFont = false;
            config.SDKOptions.disableMultiTouch = policy.DisableMultiTouch;

            config.CompileOptions.DevelopBuild = policy.Development;
            config.CompileOptions.AutoProfile = false;
            config.CompileOptions.ScriptOnly = false;
            config.CompileOptions.CleanBuild = policy.CleanBuild;
            config.CompileOptions.Il2CppOptimizeSize = true;
            config.CompileOptions.profilingFuncs = policy.Development;
            config.CompileOptions.ProfilingMemory = false;
            config.CompileOptions.DeleteStreamingAssets = true;
            config.CompileOptions.fbslim = false;
            config.CompileOptions.enablePerfAnalysis = policy.PerformanceAnalysis;
            config.CompileOptions.enableProfileStats = false;
            config.CompileOptions.enableRenderAnalysis = false;
            config.CompileOptions.enableIOSPerformancePlus = false;
            config.CompileOptions.enableiOSMetal = false;
            config.CompileOptions.enableEmscriptenGLX = false;
            config.CompileOptions.enableRenderThread = policy.RenderThread;
            config.CompileOptions.enableWasm2023 = false;
            config.CompileOptions.brotliMT = policy.MultiThreadedBrotli;
            config.CompileOptions.showMonitorSuggestModal = false;
        }

        private static void Build(string outputPath, bool development)
        {
            ValidateScenes();
            string absoluteOutput = ResolveOutputPath(outputPath);
            if (Directory.Exists(absoluteOutput))
            {
                Directory.Delete(absoluteOutput, true);
            }
            Directory.CreateDirectory(absoluteOutput);

            WXEditorScriptObject config = UnityUtil.GetEditorConf();
            if (config == null)
            {
                throw new BuildFailedException("WXSDK MiniGameConfig could not be loaded.");
            }

            string originalConfig = EditorJsonUtility.ToJson(config);
            try
            {
                ApplyConfiguration(config, absoluteOutput, CreatePolicy(development));
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                WXConvertCore.RefreshEnableRenderThread();
                WXConvertCore.WXExportError result = WXConvertCore.DoExport(true);
                if (result != WXConvertCore.WXExportError.SUCCEED)
                {
                    throw new BuildFailedException($"WXSDK conversion failed with {result}.");
                }

                ValidateOutput(absoluteOutput);
                long totalBytes = Directory.EnumerateFiles(absoluteOutput, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
                Debug.Log(
                    $"WECHAT_CONVERSION_PASS output={absoluteOutput} bytes={totalBytes} " +
                    $"development={development} appid=EMPTY");
            }
            finally
            {
                EditorJsonUtility.FromJsonOverwrite(originalConfig, config);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        private static void ValidateScenes()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes are configured in Build Settings.");
            }
            if (scenes[0] != WebBuildEntry.BootstrapScenePath)
            {
                throw new BuildFailedException(
                    $"Build index 0 must be {WebBuildEntry.BootstrapScenePath}, but was {scenes[0]}.");
            }
        }

        private static void ValidateOutput(string absoluteOutput)
        {
            foreach (string relativePath in RequiredMiniGameFiles)
            {
                string path = Path.Combine(absoluteOutput, relativePath);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    throw new BuildFailedException($"WXSDK conversion output is missing: {path}");
                }
            }

            string webBuildDirectory = Path.Combine(absoluteOutput, "webgl", "Build");
            if (!Directory.Exists(webBuildDirectory) ||
                !Directory.EnumerateFiles(webBuildDirectory).Any())
            {
                throw new BuildFailedException(
                    $"WXSDK WebGL intermediate output is missing: {webBuildDirectory}");
            }
        }

        private static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new BuildFailedException("Unable to resolve Unity project root.");
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
