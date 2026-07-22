using System.IO;
using NUnit.Framework;
using OneStrokeDemon.Editor.Build;

namespace OneStrokeDemon.Tests.EditMode
{
    public sealed class WechatBuildEntryTests
    {
        [Test]
        public void DefaultOutputIsIsolatedUnderWechatBuildDirectory()
        {
            string output = WechatBuildEntry.ResolveOutputPath(WechatBuildEntry.DefaultOutputPath);

            Assert.That(output, Does.EndWith(Path.Combine("Builds", "WeChat", "T120")));
            Assert.That(WechatBuildEntry.RequiredMiniGameFiles, Does.Contain("minigame/game.js"));
            Assert.That(WechatBuildEntry.RequiredMiniGameFiles,
                Does.Contain("minigame/project.config.json"));
        }

        [Test]
        public void OutputOutsideWechatBuildDirectoryIsRejected()
        {
            Assert.Throws<UnityEditor.Build.BuildFailedException>(() =>
                WechatBuildEntry.ResolveOutputPath(Path.GetTempPath()));
        }

        [Test]
        public void SpikeConfigurationContainsNoAppIdAndUsesLandscapeMultiThreadedBrotli()
        {
            WechatBuildPolicy policy = WechatBuildEntry.CreatePolicy(true);

            Assert.That(policy.AppId, Is.Empty);
            Assert.That(policy.Orientation, Is.EqualTo("landscape"));
            Assert.That(policy.MemorySizeMb, Is.EqualTo(WechatBuildEntry.MemorySizeMb));
            Assert.That(policy.DisableMultiTouch, Is.False);
            Assert.That(policy.Development, Is.True);
            Assert.That(policy.RenderThread, Is.False);
            Assert.That(policy.PerformanceAnalysis, Is.False);
            Assert.That(policy.CleanBuild, Is.True);
            Assert.That(policy.MultiThreadedBrotli, Is.True,
                "Use the SDK-bundled Brotli path; Unity 6000.5 has no Unity.app/PlaybackEngines Brotli binary.");
        }
    }
}
