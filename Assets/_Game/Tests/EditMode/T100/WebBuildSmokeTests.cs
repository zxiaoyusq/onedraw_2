using System;
using System.IO;
using NUnit.Framework;
using OneStrokeDemon.Editor.Build;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode
{
    public sealed class WebBuildSmokeTests
    {
        [Test]
        public void WebSmokeDefineIsIncludedOnlyWhenExplicitlyRequested()
        {
            BuildPlayerOptions normal = WebBuildEntry.CreateBuildOptions("Builds/WebGL", false);
            BuildPlayerOptions smoke = WebBuildEntry.CreateBuildOptions("Builds/WebGL", false, true);

            Assert.That(normal.target, Is.EqualTo(BuildTarget.WebGL));
            Assert.That(normal.extraScriptingDefines, Is.Empty);
            Assert.That(smoke.target, Is.EqualTo(BuildTarget.WebGL));
            Assert.That(smoke.extraScriptingDefines, Is.EqualTo(new[] { WebBuildEntry.WebSmokeDefine }));
            Assert.That(smoke.scenes, Is.EqualTo(normal.scenes));
        }

        [Test]
        public void WebSmokeProbeAndBridgeRemainTestBuildOnly()
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            Assert.That(root, Is.Not.Null);

            string probe = File.ReadAllText(Path.Combine(
                root, "Assets/_Game/Scripts/Platform/T100/WebSmokeProbe.cs"));
            string bridge = File.ReadAllText(Path.Combine(
                root, "Assets/Plugins/WebGL/T100WebSmoke.jslib"));
            string wrapper = File.ReadAllText(Path.Combine(root, "Tools/CI/build-web.sh"));
            string server = File.ReadAllText(Path.Combine(root, "Tools/CI/serve-web-build.py"));

            Assert.That(probe, Does.StartWith("#if UNITY_WEBGL && T100_WEB_SMOKE"));
            Assert.That(probe, Does.Contain("PlayerPrefs.Save()"));
            Assert.That(probe, Does.Contain("Pointer.current"));
            Assert.That(probe, Does.Contain("AudioClip.Create"));
            Assert.That(bridge, Does.Contain("window.__oneStrokeWebSmoke"));
            Assert.That(bridge, Does.Contain("UTF8ToString"));
            Assert.That(wrapper, Does.Contain("--smoke"));
            Assert.That(wrapper, Does.Contain("-webSmoke"));
            Assert.That(server, Does.Contain("Content-Encoding"));
            Assert.That(server, Does.Contain("application/wasm"));
        }
    }
}
