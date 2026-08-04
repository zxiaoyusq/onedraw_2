using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using OneStrokeDemon.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T340
{
    [Category("StrokeTrail")]
    public sealed class StrokeTrailPoolPlayModeTests : InputTestFixture
    {
        private GameObject adapterObject;
        private GameObject poolObject;
        private Material sharedMaterial;

        [SetUp]
        public override void Setup()
        {
            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
            base.Setup();
        }

        [TearDown]
        public override void TearDown()
        {
            if (adapterObject != null)
            {
                UnityEngine.Object.DestroyImmediate(adapterObject);
            }

            if (poolObject != null)
            {
                UnityEngine.Object.DestroyImmediate(poolObject);
            }

            if (sharedMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(sharedMaterial);
            }

            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ConfigurationAndGeometryUseOneSharedPointCollection()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeTrailPoolSettings poolSettings = StrokeTrailSettingsFactory.CreatePoolSettings(
                config,
                ConfigIds.VfxCues.VfxSlash);
            StrokeTrailStyle bladeStyle = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.VfxCues.VfxSlash);
            StrokeTrailStyle talismanStyle = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceTalisman,
                ConfigIds.VfxCues.VfxSlash);
            VfxCueConfig vfxCue = config.GetVfxCue(ConfigIds.VfxCues.VfxSlash);
            StrokeGeometryData geometry = CreateGeometry(
                41,
                new Vector2(100f, 200f),
                new Vector2(500f, 600f),
                new Vector2(900f, 300f));
            StrokeTrailPath path = StrokeTrailPath.FromGeometry(geometry);
            StrokeTrailPool pool = CreatePool(poolSettings, config);

            StrokeTrailView view = pool.Show(path, bladeStyle);

            Assert.That(poolSettings.Capacity, Is.EqualTo(12));
            Assert.That(poolSettings.MaximumActiveTrailCount, Is.EqualTo(3));
            Assert.That(poolSettings.MaximumPointCount, Is.EqualTo(80));
            Assert.That(bladeStyle.WidthReferencePixels, Is.EqualTo(18f));
            Assert.That(talismanStyle.WidthReferencePixels, Is.EqualTo(28f));
            Assert.That(talismanStyle.WidthReferencePixels, Is.GreaterThan(bladeStyle.WidthReferencePixels));
            Assert.That(bladeStyle.LifetimeSeconds, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(bladeStyle.SortingLayerId, Is.EqualTo(SortingLayer.NameToID(vfxCue.SortingLayer)));
            Assert.That(bladeStyle.SortingLayerId, Is.Not.Zero);
            Assert.That(bladeStyle.SortingOrder, Is.EqualTo(20));
            Assert.That(path.Points, Is.SameAs(geometry.Points));
            Assert.That(view.SourcePoints, Is.SameAs(geometry.Points));
            Assert.That(view.LineRenderer.positionCount, Is.EqualTo(geometry.PointCount));
            Assert.That(
                view.LineRenderer.startWidth,
                Is.EqualTo(
                    bladeStyle.WidthReferencePixels *
                    bladeStyle.OuterWidthMultiplier));
            Assert.That(
                view.BodyLineRenderer.startWidth,
                Is.EqualTo(
                    bladeStyle.WidthReferencePixels *
                    bladeStyle.BodyWidthMultiplier));
            Assert.That(
                view.CoreLineRenderer.startWidth,
                Is.EqualTo(
                    bladeStyle.WidthReferencePixels *
                    bladeStyle.CoreWidthMultiplier));
            Assert.That(view.StyleId, Is.EqualTo(ConfigIds.StrokeTrailStyles.StrokeTrailLightningC));
            Assert.That(view.LineRenderer.sharedMaterial, Is.SameAs(sharedMaterial));
            for (int index = 0; index < geometry.PointCount; index++)
            {
                Vector2 expected = geometry.Points[index];
                Vector3 actual = view.LineRenderer.GetPosition(index);
                Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
                Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
                Assert.That(actual.z, Is.Zero);
            }

        }

        [UnityTest]
        public IEnumerator FadeExpiryAndReuseCompletelyResetViewState()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeTrailStyle bladeStyle = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.VfxCues.VfxSlash);
            StrokeTrailStyle talismanStyle = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceTalisman,
                ConfigIds.VfxCues.VfxSlash);
            StrokeTrailPool pool = CreatePool(StrokeTrailSettingsFactory.CreatePoolSettings(
                config,
                ConfigIds.VfxCues.VfxSlash), config);
            var bladePath = new StrokeTrailPath(
                1,
                new[] { Vector2.zero, new Vector2(100f, 50f), new Vector2(200f, 0f) });
            var talismanPath = new StrokeTrailPath(
                2,
                new[] { new Vector2(20f, 30f), new Vector2(80f, 90f) });

            StrokeTrailView firstUse = pool.Show(bladePath, bladeStyle);
            pool.Advance(bladeStyle.LifetimeSeconds * 0.5f);
            Assert.That(firstUse.LineRenderer.startColor.a, Is.EqualTo(0.5f).Within(1f / 255f));
            Assert.That(firstUse.NormalizedLifetime, Is.EqualTo(0.5f).Within(0.001f));

            pool.Advance(bladeStyle.LifetimeSeconds * 0.5f);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(firstUse.IsActive, Is.False);
            Assert.That(firstUse.StrokeId, Is.Zero);
            Assert.That(firstUse.ActivationSequence, Is.Zero);
            Assert.That(firstUse.StanceId, Is.Null);
            Assert.That(firstUse.SourcePoints, Is.Null);
            Assert.That(firstUse.LineRenderer.enabled, Is.False);
            Assert.That(firstUse.LineRenderer.positionCount, Is.Zero);
            Assert.That(firstUse.LineRenderer.startWidth, Is.Zero);
            Assert.That(firstUse.LineRenderer.sortingLayerID, Is.Zero);
            Assert.That(firstUse.LineRenderer.sortingOrder, Is.Zero);
            Assert.That(firstUse.LineRenderer.sharedMaterial, Is.SameAs(sharedMaterial));

            StrokeTrailView secondUse = pool.Show(talismanPath, talismanStyle);
            Assert.That(secondUse, Is.SameAs(firstUse));
            Assert.That(secondUse.StrokeId, Is.EqualTo(2));
            Assert.That(secondUse.StanceId, Is.EqualTo(ConfigIds.Stances.StanceTalisman));
            Assert.That(secondUse.SourcePoints, Is.SameAs(talismanPath.Points));
            Assert.That(secondUse.LineRenderer.positionCount, Is.EqualTo(2));
            Assert.That(
                secondUse.LineRenderer.startWidth,
                Is.EqualTo(28f * talismanStyle.OuterWidthMultiplier));
            Assert.That(secondUse.LineRenderer.startColor.a, Is.EqualTo(1f));

        }

        [UnityTest]
        public IEnumerator RapidStrokesRecycleOldestAndKeepThreeSharedMaterialViews()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeTrailPool pool = CreatePool(StrokeTrailSettingsFactory.CreatePoolSettings(
                config,
                ConfigIds.VfxCues.VfxSlash), config);
            StrokeTrailStyle bladeStyle = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.VfxCues.VfxSlash);
            StrokeTrailStyle talismanStyle = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceTalisman,
                ConfigIds.VfxCues.VfxSlash);
            var points = new[] { Vector2.zero, new Vector2(120f, 40f) };

            pool.Show(new StrokeTrailPath(1, points), bladeStyle);
            pool.Show(new StrokeTrailPath(2, points), bladeStyle);
            pool.Show(new StrokeTrailPath(3, points), bladeStyle);
            StrokeTrailView newest = pool.Show(new StrokeTrailPath(4, points), talismanStyle);

            Assert.That(pool.Capacity, Is.EqualTo(12));
            Assert.That(pool.ActiveCount, Is.EqualTo(3));
            Assert.That(pool.TryGetActiveView(1, out _), Is.False);
            Assert.That(pool.TryGetActiveView(2, out StrokeTrailView second), Is.True);
            Assert.That(pool.TryGetActiveView(3, out StrokeTrailView third), Is.True);
            Assert.That(pool.TryGetActiveView(4, out StrokeTrailView fourth), Is.True);
            Assert.That(newest, Is.SameAs(fourth));
            Assert.That(
                fourth.LineRenderer.startWidth,
                Is.EqualTo(28f * talismanStyle.OuterWidthMultiplier));
            Assert.That(second.LineRenderer.sharedMaterial, Is.SameAs(sharedMaterial));
            Assert.That(third.LineRenderer.sharedMaterial, Is.SameAs(sharedMaterial));
            Assert.That(fourth.LineRenderer.sharedMaterial, Is.SameAs(sharedMaterial));

        }

        [UnityTest]
        public IEnumerator WarmPoolShowAndReclaimHotPathAllocatesNoManagedMemory()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeTrailPool pool = CreatePool(StrokeTrailSettingsFactory.CreatePoolSettings(
                config,
                ConfigIds.VfxCues.VfxSlash), config);
            StrokeTrailStyle style = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.VfxCues.VfxSlash);
            var points = new[]
            {
                Vector2.zero,
                new Vector2(40f, 20f),
                new Vector2(80f, 10f),
                new Vector2(120f, 30f)
            };
            var paths = new StrokeTrailPath[4];
            for (int index = 0; index < paths.Length; index++)
            {
                paths[index] = new StrokeTrailPath((ulong)(index + 1), points);
            }

            for (int index = 0; index < 16; index++)
            {
                pool.Show(paths[index & 3], style);
                pool.Advance(style.LifetimeSeconds);
            }

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 128; index++)
            {
                pool.Show(paths[index & 3], style);
                pool.Advance(style.LifetimeSeconds);
            }

            long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(allocatedAfter - allocatedBefore, Is.Zero);

        }

        [UnityTest]
        public IEnumerator StationaryHoldDrawsConfiguredThunderCoreThenMovementBecomesTrail()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeTrailStyle style = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.VfxCues.VfxSlash);
            StrokeRuleConfig chargedRule = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeCharged);
            StrokeTrailPool pool = CreatePool(StrokeTrailSettingsFactory.CreatePoolSettings(
                config,
                ConfigIds.VfxCues.VfxSlash), config);
            var origin = new Vector2(500f, 400f);
            StrokeTrailView view = pool.BeginPreview(91, origin, style);

            Assert.That(
                pool.TryUpdateChargePreview(
                    91,
                    origin,
                    0.5f,
                    chargedRule.HitRadiusRefPx),
                Is.True);
            Assert.That(view.IsChargePreviewVisible, Is.True);
            Assert.That(view.ChargePreviewProgress, Is.EqualTo(0.5f));
            Assert.That(
                view.ChargePreviewRadiusReferencePixels,
                Is.EqualTo((float)chargedRule.HitRadiusRefPx));
            StrokeChargeVfxView chargeVfx = view.ActiveChargeVfx;
            Assert.That(chargeVfx, Is.Not.Null);
            Assert.That(chargeVfx.IsVisible, Is.True);
            Assert.That(chargeVfx.ParticleSystems.Count, Is.EqualTo(3));
            Assert.That(chargeVfx.ParticleSystems[0].particleCount, Is.GreaterThan(0));
            Assert.That(chargeVfx.RingRenderers[0].enabled, Is.True);
            Assert.That(
                chargeVfx.RingRenderers[0].positionCount,
                Is.EqualTo(StrokeChargeVfxView.RingSegmentCount + 1));
            Assert.That(
                chargeVfx.RingRenderers[0].startWidth,
                Is.EqualTo(
                    style.WidthReferencePixels *
                    style.BranchWidthMultiplier *
                    style.OuterWidthMultiplier));
            Assert.That(chargeVfx.RingRenderers[1].enabled, Is.True);
            Assert.That(
                chargeVfx.RingRenderers[1].startColor,
                Is.EqualTo(style.CoreColor));
            Assert.That(
                chargeVfx.RingRenderers[1].startWidth,
                Is.EqualTo(view.CoreLineRenderer.startWidth));
            Assert.That(chargeVfx.RingRenderers[2].enabled, Is.True);
            Assert.That(
                chargeVfx.RingRenderers[2].positionCount,
                Is.EqualTo(StrokeChargeVfxView.RingSegmentCount / 2 + 1));
            Assert.That(chargeVfx.RingRenderers[3].enabled, Is.False);
            for (int index = 0; index < StrokeChargeVfxView.RadialRendererCount; index++)
            {
                LineRenderer radial = chargeVfx.RadialRenderers[index];
                Assert.That(radial.enabled, Is.EqualTo(index < 4));
                Assert.That(radial.positionCount, Is.EqualTo(index < 4 ? 3 : 0));
                if (index < 4)
                {
                    Assert.That(radial.startWidth, Is.GreaterThan(radial.endWidth));
                }
            }

            pool.TryUpdateChargePreview(91, origin, 1f, chargedRule.HitRadiusRefPx);
            Assert.That(
                chargeVfx.RingRenderers[3].positionCount,
                Is.EqualTo(StrokeChargeVfxView.RingSegmentCount + 1));
            for (int index = 0; index < chargeVfx.RingRenderers.Count; index++)
            {
                Assert.That(chargeVfx.RingRenderers[index].enabled, Is.True);
            }

            for (int index = 0; index < chargeVfx.RadialRenderers.Count; index++)
            {
                Assert.That(chargeVfx.RadialRenderers[index].enabled, Is.True);
            }

            Assert.That(pool.TryAppendPreviewPoint(91, origin + Vector2.right * 120f), Is.True);
            Assert.That(view.IsChargePreviewVisible, Is.False);
            Assert.That(chargeVfx.IsVisible, Is.False);
            for (int index = 0; index < chargeVfx.RingRenderers.Count; index++)
            {
                Assert.That(chargeVfx.RingRenderers[index].enabled, Is.False);
                Assert.That(chargeVfx.RingRenderers[index].positionCount, Is.Zero);
            }

            for (int index = 0; index < view.BranchLineRenderers.Count; index++)
            {
                Assert.That(view.BranchLineRenderers[index].enabled, Is.False);
                Assert.That(view.BranchLineRenderers[index].positionCount, Is.Zero);
            }

            Assert.That(view.LineRenderer.enabled, Is.True);
            Assert.That(view.LineRenderer.positionCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator MousePlayerPathDisplaysTheProcessedRuntimeConfiguredStroke()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeRuleConfig anyRule = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny);
            StrokeTrailPool pool = CreatePool(StrokeTrailSettingsFactory.CreatePoolSettings(
                config,
                ConfigIds.VfxCues.VfxSlash), config);
            StrokeTrailStyle style = StrokeTrailSettingsFactory.CreateStyle(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.VfxCues.VfxSlash);
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystemPointerAdapter adapter = CreateAdapter();
            using var collector = new StrokeInputCollector(
                adapter,
                StrokeSamplingSettingsFactory.FromConfig(anyRule));
            StrokeGeometryData geometry = null;
            StrokeTrailView view = null;
            collector.StrokeCompleted += stroke =>
            {
                geometry = StrokeGeometry.Process(
                    stroke,
                    StrokeGeometrySettingsFactory.FromConfig(anyRule));
                view = pool.Show(StrokeTrailPath.FromGeometry(geometry), style);
            };
            float y = Screen.height * 0.5f;
            var begin = new Vector2(Screen.width * 0.2f, y);
            var move = new Vector2(Screen.width * 0.45f, Screen.height * 0.7f);
            var end = new Vector2(Screen.width * 0.75f, Screen.height * 0.35f);

            Set(mouse.position, begin, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, move, queueEventOnly: true);
            yield return null;
            Set(mouse.position, end, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(geometry, Is.Not.Null);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsActive, Is.True);
            Assert.That(view.StrokeId, Is.EqualTo(geometry.StrokeId));
            Assert.That(view.SourcePoints, Is.SameAs(geometry.Points));
            Assert.That(view.LineRenderer.positionCount, Is.EqualTo(geometry.PointCount));
            Assert.That(view.LineRenderer.enabled, Is.True);
            Assert.That(
                view.LineRenderer.startWidth,
                Is.EqualTo(18f * style.OuterWidthMultiplier));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        private StrokeTrailPool CreatePool(
            StrokeTrailPoolSettings settings,
            IConfigProvider config)
        {
            poolObject = new GameObject("T340 Stroke Trail Pool");
            var pool = poolObject.AddComponent<StrokeTrailPool>();
            Shader shader = Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null, "Sprites/Default shader must be available for trail tests.");
            sharedMaterial = new Material(shader)
            {
                name = "T340 Shared Trail Material"
            };
            StrokeTrailStyleConfig style = config.GetStrokeTrailStyle(
                ConfigIds.StrokeTrailStyles.StrokeTrailLightningC);
            GameObject chargePrefab = AssetRegistryRuntime.Current.GetPrefab(
                style.ChargeVfxAssetKey);
            pool.Initialize(
                settings,
                sharedMaterial,
                chargeVfxPrefabs: new[]
                {
                    new StrokeChargeVfxPrefab(style.ChargeVfxAssetKey, chargePrefab),
                });
            return pool;
        }

        private InputSystemPointerAdapter CreateAdapter()
        {
            adapterObject = new GameObject("T340 Pointer Adapter");
            var adapter = adapterObject.AddComponent<InputSystemPointerAdapter>();
            adapter.Initialize(
                new ReferencePixelConverter(new Vector2(1920f, 1080f)),
                new FixedSafeAreaProvider(new Rect(0f, 0f, Screen.width, Screen.height)),
                new NeverBlocked());
            return adapter;
        }

        private static StrokeGeometryData CreateGeometry(ulong strokeId, params Vector2[] points)
        {
            var sampler = new StrokeSampler(new StrokeSamplingSettings(0.001f, 100000f, 256));
            sampler.Begin(strokeId, points[0], 1d);
            for (int index = 1; index < points.Length - 1; index++)
            {
                sampler.AddPoint(points[index], index + 1d);
            }

            StrokeData stroke = sampler.End(points[points.Length - 1], points.Length + 1d);
            return StrokeGeometry.Process(stroke, new StrokeGeometrySettings(0f, 96));
        }

        private static IEnumerator LoadRuntimeConfiguration()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != sceneName &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }

        private sealed class FixedSafeAreaProvider : ISafeAreaProvider
        {
            public FixedSafeAreaProvider(Rect safeArea)
            {
                SafeArea = safeArea;
            }

            public Rect SafeArea { get; }
        }

        private sealed class NeverBlocked : IPointerUiBlocker
        {
            public bool IsBlocked(Vector2 screenPosition, int pointerId)
            {
                return false;
            }
        }
    }
}
