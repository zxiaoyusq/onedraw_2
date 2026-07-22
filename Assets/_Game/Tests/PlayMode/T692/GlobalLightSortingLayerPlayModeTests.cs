using System.Collections;
using System.Linq;
using NUnit.Framework;
using OneStrokeDemon.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T692
{
    /// <summary>验证Battle运行时的2D全局光确实包含火鱼所在的Actors层。</summary>
    [Category("T692")]
    public sealed class GlobalLightSortingLayerPlayModeTests
    {
        [UnityTest]
        public IEnumerator BattleGlobalLightIncludesActorsLayerAtRuntime()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Battle, LoadSceneMode.Single);

            Light2D[] globalLights = Object
                .FindObjectsByType<Light2D>(FindObjectsInactive.Include)
                .Where(light => light.lightType == Light2D.LightType.Global)
                .ToArray();
            Assert.That(globalLights, Has.Length.EqualTo(1));
            Assert.That(
                globalLights[0].targetSortingLayers,
                Does.Contain(SortingLayer.NameToID("Actors")));
        }
    }
}
