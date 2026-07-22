using System.Linq;
using NUnit.Framework;
using OneStrokeDemon.Editor.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace OneStrokeDemon.Tests.EditMode.T692
{
    /// <summary>验证生产场景的2D全局光覆盖Lit角色使用的全部Sorting Layer。</summary>
    [Category("T692")]
    public sealed class GlobalLightSortingLayerTests
    {
        [TestCase(T692GlobalLightSortingLayerAuthoring.BootstrapScenePath)]
        [TestCase(T692GlobalLightSortingLayerAuthoring.MainMenuScenePath)]
        [TestCase(T692GlobalLightSortingLayerAuthoring.BattleScenePath)]
        public void ProductionGlobalLightCoversEverySortingLayer(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                Light2D[] globalLights = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Light2D>(true))
                    .Where(light => light.lightType == Light2D.LightType.Global)
                    .ToArray();
                Assert.That(globalLights, Has.Length.EqualTo(1));
                Assert.That(
                    globalLights[0].targetSortingLayers,
                    Is.EqualTo(SortingLayer.layers.Select(layer => layer.id).ToArray()));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void FireFishKeepsActorsLayerAndLitMaterial()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                T690FireFishAnimationAuthoring.PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sortingLayerName, Is.EqualTo("Actors"));
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.shader.name,
                Is.EqualTo("Universal Render Pipeline/2D/Sprite-Lit-Default"));
        }
    }
}
