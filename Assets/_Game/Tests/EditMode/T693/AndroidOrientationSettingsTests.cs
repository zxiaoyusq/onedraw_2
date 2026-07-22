using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode
{
    /// <summary>
    /// 锁定Android构建的横屏方向合同，防止Player Settings回退为允许竖屏。
    /// </summary>
    public sealed class AndroidOrientationSettingsTests
    {
        [Test]
        public void AutoRotationAllowsOnlyLandscapeOrientations()
        {
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.AutoRotation));
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.True);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.True);
        }
    }
}
