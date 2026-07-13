using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode
{
    public sealed class WechatSdkIntegrationTests
    {
        private const string PackageName = "com.qq.weixin.minigame";
        private const string Commit = "ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228";
        private const string PackageUrl =
            "https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk.git#" + Commit;

        [Test]
        public void OfficialWechatSdkDependencyIsPinnedToReviewedCommit()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Assert.That(projectRoot, Is.Not.Null);

            string manifest = File.ReadAllText(Path.Combine(projectRoot, "Packages/manifest.json"));
            string lockFile = File.ReadAllText(Path.Combine(projectRoot, "Packages/packages-lock.json"));

            Assert.That(manifest, Does.Contain($"\"{PackageName}\": \"{PackageUrl}\""));
            Assert.That(lockFile, Does.Contain($"\"{PackageName}\""));
            Assert.That(lockFile, Does.Contain("\"version\": \"file:com.qq.weixin.minigame\""));
            Assert.That(lockFile, Does.Contain("\"source\": \"embedded\""));
        }

        [Test]
        public void ImportedWechatSdkMatchesReviewedReleaseAndLoadsAssemblies()
        {
            PackageInfo package = PackageInfo.GetAllRegisteredPackages()
                .SingleOrDefault(candidate => candidate.name == PackageName);

            Assert.That(package, Is.Not.Null, $"Missing installed package {PackageName}");
            Assert.That(package.source, Is.EqualTo(PackageSource.Embedded));
            Assert.That(package.version, Is.EqualTo("0.1.1"),
                "The pinned upstream package keeps stale UPM metadata; release truth is CHANGELOG v0.1.33.");
            Assert.That(File.ReadAllText(Path.Combine(package.resolvedPath, "CHANGELOG.md")),
                Does.Contain("2026-6-22 v0.1.33"));
            Assert.That(File.ReadAllText(Path.Combine(package.resolvedPath, "LICENSE")),
                Does.StartWith("MIT License"));
            Assert.That(File.ReadAllText(Path.Combine(package.resolvedPath, "UPSTREAM.md")),
                Does.Contain(Commit));
            Assert.That(File.ReadAllText(Path.Combine(
                    package.resolvedPath, "Runtime/WXRuntimeExtDef.cs")),
                Does.Contain("UNITY_6000_5_OR_NEWER"));
            Assert.That(Type.GetType("WeChatWASM.WX, Wx"), Is.Not.Null);
            Assert.That(Type.GetType("WeChatWASM.WXConvertCore, WxEditor"), Is.Not.Null);
        }
    }
}
