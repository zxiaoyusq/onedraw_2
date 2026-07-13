using System;
using System.Collections;
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

namespace OneStrokeDemon.Tests.PlayMode.T350
{
    [Category("StrokeHitResolver")]
    public sealed class MultiTargetHitPlayModeTests : InputTestFixture
    {
        private GameObject adapterObject;
        private GameObject referenceRoot;
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

            if (referenceRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(referenceRoot);
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
        public IEnumerator MouseStrokeSharesTrailPointsAndHitsMultipleTargetsInPathOrder()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeRuleConfig samplingRule = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny);
            var classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
            StrokeHitResolverSettings resolverSettings =
                StrokeHitSettingsFactory.CreateResolverSettings(config);
            referenceRoot = new GameObject("T350 Reference Space");
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            ConfigureReferenceSpace(referenceRoot.transform, camera);
            StrokeTrailPool trailPool = CreateTrailPool(config);
            var physicsQuery = new Physics2DStrokeHitQuery(
                resolverSettings.QueryCapacity,
                Physics2D.AllLayers,
                includeTriggers: true,
                referenceRoot.transform);
            var resolver = new StrokeHitResolver(resolverSettings, physicsQuery);
            var hitBuffer = new HitRecord[resolverSettings.MaximumUniqueTargets];
            CreateTarget(101, new Vector2(650f, 540f), hasWeakpoint: true);
            CreateTarget(202, new Vector2(1200f, 540f), hasWeakpoint: false);
            CreateTarget(303, new Vector2(900f, 760f), hasWeakpoint: false);
            Physics2D.SyncTransforms();

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystemPointerAdapter adapter = CreateAdapter();
            using var collector = new StrokeInputCollector(
                adapter,
                StrokeSamplingSettingsFactory.FromConfig(samplingRule));
            StrokeGeometryData geometry = null;
            GestureMatchResult gesture = null;
            StrokeTrailView trailView = null;
            int hitCount = -1;
            collector.StrokeCompleted += stroke =>
            {
                geometry = StrokeGeometry.Process(
                    stroke,
                    StrokeGeometrySettingsFactory.FromConfig(samplingRule));
                gesture = classifier.Classify(geometry);
                StrokeHitRule hitRule = StrokeHitSettingsFactory.CreateRule(
                    config.GetStrokeRule(gesture.RuleId));
                trailView = trailPool.Show(
                    StrokeTrailPath.FromGeometry(geometry),
                    StrokeTrailSettingsFactory.CreateStyle(
                        config,
                        ConfigIds.Stances.StanceBlade,
                        ConfigIds.VfxCues.VfxSlash));
                hitCount = resolver.Resolve(geometry, gesture, hitRule, hitBuffer);
            };

            float y = Screen.height * 0.5f;
            var begin = new Vector2(Screen.width * 0.15f, y);
            var move = new Vector2(Screen.width * 0.5f, y);
            var end = new Vector2(Screen.width * 0.85f, y);
            Set(mouse.position, begin, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, move, queueEventOnly: true);
            yield return null;
            Set(mouse.position, end, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(geometry, Is.Not.Null);
            Assert.That(gesture, Is.Not.Null);
            Assert.That(gesture.GestureType, Is.EqualTo(GestureType.Horizontal));
            Assert.That(trailView, Is.Not.Null);
            Assert.That(trailView.SourcePoints, Is.SameAs(geometry.Points));
            Assert.That(trailView.LineRenderer.positionCount, Is.EqualTo(geometry.PointCount));
            Assert.That(
                GeometryUtility.TestPlanesAABB(
                    GeometryUtility.CalculateFrustumPlanes(camera),
                    trailView.LineRenderer.bounds),
                Is.True);
            Assert.That(hitCount, Is.EqualTo(2));
            Assert.That(hitBuffer[0].TargetId, Is.EqualTo(101));
            Assert.That(hitBuffer[0].IsWeakpoint, Is.True);
            Assert.That(hitBuffer[1].TargetId, Is.EqualTo(202));
            Assert.That(hitBuffer[1].IsWeakpoint, Is.False);
            Assert.That(hitBuffer[0].PathParameter, Is.LessThan(hitBuffer[1].PathParameter));
            Assert.That(hitBuffer[0].StrokeId, Is.EqualTo(geometry.StrokeId));
            Assert.That(hitBuffer[0].Gesture, Is.SameAs(gesture));
            Assert.That(hitBuffer[0].Timestamp, Is.EqualTo(geometry.EndedAt));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        [UnityTest]
        public IEnumerator WarmPhysicsCircleCastAndResolveHotPathAllocatesNoManagedMemory()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            var classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
            StrokeHitResolverSettings resolverSettings =
                StrokeHitSettingsFactory.CreateResolverSettings(config);
            referenceRoot = new GameObject("T350 Allocation Reference Space");
            CreateTarget(404, new Vector2(100f, 0f), hasWeakpoint: true);
            Physics2D.SyncTransforms();
            var physicsQuery = new Physics2DStrokeHitQuery(
                resolverSettings.QueryCapacity,
                Physics2D.AllLayers,
                includeTriggers: true,
                referenceRoot.transform);
            var resolver = new StrokeHitResolver(resolverSettings, physicsQuery);
            StrokeGeometryData geometry = CreateGeometry(
                88,
                Vector2.zero,
                new Vector2(200f, 0f));
            GestureMatchResult gesture = classifier.Classify(geometry);
            StrokeHitRule rule = StrokeHitSettingsFactory.CreateRule(
                config.GetStrokeRule(gesture.RuleId));
            var results = new HitRecord[resolverSettings.MaximumUniqueTargets];
            for (int index = 0; index < 16; index++)
            {
                Assert.That(resolver.Resolve(geometry, gesture, rule, results), Is.EqualTo(1));
            }

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 128; index++)
            {
                resolver.Resolve(geometry, gesture, rule, results);
            }

            long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(allocatedAfter - allocatedBefore, Is.Zero);
        }

        private StrokeTrailPool CreateTrailPool(IConfigProvider config)
        {
            var trailPool = referenceRoot.AddComponent<StrokeTrailPool>();
            Shader shader = Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null);
            sharedMaterial = new Material(shader)
            {
                name = "T350 Shared Trail Material"
            };
            trailPool.Initialize(
                StrokeTrailSettingsFactory.CreatePoolSettings(
                    config,
                    ConfigIds.VfxCues.VfxSlash),
                sharedMaterial);
            return trailPool;
        }

        private TestHittable CreateTarget(
            int targetId,
            Vector2 referencePosition,
            bool hasWeakpoint)
        {
            var targetObject = new GameObject("T350 Hittable Target");
            targetObject.transform.SetParent(referenceRoot.transform, false);
            targetObject.transform.localPosition = new Vector3(
                referencePosition.x,
                referencePosition.y,
                0f);
            var target = targetObject.AddComponent<TestHittable>();
            target.Initialize(targetId);
            var body = targetObject.AddComponent<BoxCollider2D>();
            body.size = new Vector2(100f, 100f);

            if (hasWeakpoint)
            {
                var weakpointObject = new GameObject("T350 Weakpoint");
                weakpointObject.transform.SetParent(targetObject.transform, false);
                var hitbox = weakpointObject.AddComponent<TestStrokeHitbox>();
                hitbox.Initialize(target, isWeakpoint: true);
                var weakpointCollider = weakpointObject.AddComponent<CircleCollider2D>();
                weakpointCollider.radius = 25f;
                weakpointCollider.isTrigger = true;
            }

            return target;
        }

        private InputSystemPointerAdapter CreateAdapter()
        {
            adapterObject = new GameObject("T350 Pointer Adapter");
            var adapter = adapterObject.AddComponent<InputSystemPointerAdapter>();
            adapter.Initialize(
                new ReferencePixelConverter(new Vector2(1920f, 1080f)),
                new FixedSafeAreaProvider(new Rect(0f, 0f, Screen.width, Screen.height)),
                new NeverBlocked());
            return adapter;
        }

        private static void ConfigureReferenceSpace(Transform referenceSpace, Camera camera)
        {
            float distance = Vector3.Dot(
                Vector3.zero - camera.transform.position,
                camera.transform.forward);
            Assert.That(distance, Is.GreaterThan(camera.nearClipPlane));
            Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
            Vector3 bottomRight = camera.ViewportToWorldPoint(new Vector3(1f, 0f, distance));
            Vector3 topLeft = camera.ViewportToWorldPoint(new Vector3(0f, 1f, distance));
            referenceSpace.position = bottomLeft;
            referenceSpace.rotation = camera.transform.rotation;
            referenceSpace.localScale = new Vector3(
                Vector3.Distance(bottomLeft, bottomRight) / 1920f,
                Vector3.Distance(bottomLeft, topLeft) / 1080f,
                1f);
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

        private sealed class TestHittable : MonoBehaviour, IHittable
        {
            public int HitTargetId { get; private set; }

            public bool CanReceiveStrokeHit { get; private set; }

            public void Initialize(int targetId)
            {
                HitTargetId = targetId;
                CanReceiveStrokeHit = true;
            }
        }

        private sealed class TestStrokeHitbox : MonoBehaviour, IStrokeHitbox
        {
            public IHittable HitTarget { get; private set; }

            public bool IsWeakpoint { get; private set; }

            public bool IsStrokeHitboxActive { get; private set; }

            public void Initialize(IHittable hitTarget, bool isWeakpoint)
            {
                HitTarget = hitTarget;
                IsWeakpoint = isWeakpoint;
                IsStrokeHitboxActive = true;
            }
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
