using System.Collections;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OneStrokeDemon.Tests.PlayMode
{
    [Category("T698")]
    public sealed class T698LightningStrokeTrailPlayModeTests
    {
        [UnityTest]
        public IEnumerator ConfiguredPrefabRendersDeterministicLayersAndFadesAsOne()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            IConfigProvider config = GameplayConfigRuntime.Current;
            VfxCueConfig cue = config.GetVfxCue(ConfigIds.VfxCues.VfxSlash);
            GameObject prefab = AssetRegistryRuntime.Current.GetPrefab(cue.AssetKey);
            var root = new GameObject("T698 Lightning Trail Test Root");
            root.transform.localScale = new Vector3(0.01f, 0.01f, 1f);
            var pool = root.AddComponent<StrokeTrailPool>();
            Shader shader = Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            StrokeTrailStyle style = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.VfxCues.VfxSlash);
            pool.Initialize(
                StrokeTrailSettingsFactory.CreatePoolSettings(
                    config,
                    ConfigIds.VfxCues.VfxSlash),
                material,
                root.transform,
                prefab);
            var points = new[]
            {
                Vector2.zero,
                new Vector2(240f, 30f),
                new Vector2(480f, -20f),
                new Vector2(760f, 40f),
            };
            var path = new StrokeTrailPath(698, points);

            StrokeTrailView first = pool.Show(path, style);
            StrokeTrailView second = pool.Show(path, style);

            Assert.That(first.StyleId, Is.EqualTo(ConfigIds.StrokeTrailStyles.StrokeTrailLightningC));
            Assert.That(first.SourcePoints, Is.SameAs(points));
            Assert.That(first.BodyLineRenderer.positionCount, Is.EqualTo(points.Length));
            Assert.That(first.CoreLineRenderer.positionCount, Is.EqualTo(points.Length));
            Assert.That(first.ActiveBranchCount, Is.EqualTo(6));
            Assert.That(
                first.LineRenderer.startWidth,
                Is.EqualTo(
                    style.WidthReferencePixels *
                    style.OuterWidthMultiplier *
                    first.ReferencePixelWorldScale).Within(0.0001f));
            for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
            {
                Assert.That(
                    first.BodyLineRenderer.GetPosition(pointIndex),
                    Is.EqualTo(first.LineRenderer.GetPosition(pointIndex)));
                Assert.That(
                    first.CoreLineRenderer.GetPosition(pointIndex),
                    Is.EqualTo(first.LineRenderer.GetPosition(pointIndex)));
            }

            for (int branchIndex = 0; branchIndex < first.ActiveBranchCount; branchIndex++)
            {
                LineRenderer firstBranch = first.BranchLineRenderers[branchIndex];
                LineRenderer secondBranch = second.BranchLineRenderers[branchIndex];
                Assert.That(firstBranch.positionCount, Is.EqualTo(style.BranchSegmentCount + 1));
                Assert.That(secondBranch.positionCount, Is.EqualTo(firstBranch.positionCount));
                for (int pointIndex = 0; pointIndex < firstBranch.positionCount; pointIndex++)
                {
                    Assert.That(
                        secondBranch.GetPosition(pointIndex),
                        Is.EqualTo(firstBranch.GetPosition(pointIndex)));
                }
            }

            pool.Advance(style.LifetimeSeconds * 0.5f);
            Assert.That(first.LineRenderer.startColor.a, Is.EqualTo(style.OuterColor.a * 0.5f).Within(1f / 255f));
            Assert.That(first.BodyLineRenderer.startColor.a, Is.EqualTo(style.BodyColor.a * 0.5f).Within(1f / 255f));
            Assert.That(first.CoreLineRenderer.startColor.a, Is.EqualTo(style.CoreColor.a * 0.5f).Within(1f / 255f));
            Assert.That(
                first.BranchLineRenderers[0].startColor.a,
                Is.EqualTo(style.BranchColor.a * 0.5f).Within(1f / 255f));

            pool.Advance(style.LifetimeSeconds * 0.5f);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(first.ActiveBranchCount, Is.Zero);
            Assert.That(first.LineRenderer.enabled, Is.False);
            Assert.That(first.BodyLineRenderer.enabled, Is.False);
            Assert.That(first.CoreLineRenderer.enabled, Is.False);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(material);
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 6f;
            while (SceneManager.GetActiveScene().name != sceneName &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }
    }
}
