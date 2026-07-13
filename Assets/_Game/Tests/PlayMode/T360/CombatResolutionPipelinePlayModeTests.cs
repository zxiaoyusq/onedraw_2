using System.Collections;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T360
{
    [Category("CombatResolutionPipeline")]
    public sealed class CombatResolutionPipelinePlayModeTests : InputTestFixture
    {
        private GameObject adapterObject;
        private GameObject referenceRoot;

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
                Object.DestroyImmediate(adapterObject);
            }

            if (referenceRoot != null)
            {
                Object.DestroyImmediate(referenceRoot);
            }

            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator MouseStrokeResolvesOrderedTargetsIntoConfiguredDamageScoreAndEnergy()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeRuleConfig samplingRule = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny);
            var classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
            StrokeHitResolverSettings resolverSettings =
                StrokeHitSettingsFactory.CreateResolverSettings(config);
            referenceRoot = new GameObject("T360 Reference Space");
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            ConfigureReferenceSpace(referenceRoot.transform, camera);
            var resolver = new StrokeHitResolver(
                resolverSettings,
                new Physics2DStrokeHitQuery(
                    resolverSettings.QueryCapacity,
                    Physics2D.AllLayers,
                    includeTriggers: true,
                    referenceRoot.transform));
            var hitBuffer = new HitRecord[resolverSettings.MaximumUniqueTargets];
            CreateTarget(101, new Vector2(650f, 540f), hasWeakpoint: true);
            CreateTarget(202, new Vector2(1200f, 540f), hasWeakpoint: false);
            Physics2D.SyncTransforms();

            ComboService combo = ComboService.FromConfig(config);
            DamageRuleSet rules = DamageRuleSetFactory.CreateForEnemy(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.Enemies.EnemyFireFish);
            var score = new ScoreService();
            var random = new FixedRandomSource();
            int hitCount = -1;
            DamageResult first = default;
            DamageResult second = default;

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystemPointerAdapter adapter = CreateAdapter();
            using var collector = new StrokeInputCollector(
                adapter,
                StrokeSamplingSettingsFactory.FromConfig(samplingRule));
            collector.StrokeCompleted += stroke =>
            {
                StrokeGeometryData geometry = StrokeGeometry.Process(
                    stroke,
                    StrokeGeometrySettingsFactory.FromConfig(samplingRule));
                GestureMatchResult gesture = classifier.Classify(geometry);
                StrokeHitRule hitRule = StrokeHitSettingsFactory.CreateRule(
                    config.GetStrokeRule(gesture.RuleId));
                hitCount = resolver.Resolve(geometry, gesture, hitRule, hitBuffer);
                for (int index = 0; index < hitCount; index++)
                {
                    ComboSnapshot comboState = combo.RegisterHit(hitBuffer[index].Timestamp);
                    DamageContext context = DamageContext.FromHitRecord(
                        hitBuffer[index],
                        ConfigIds.Stances.StanceBlade,
                        comboState.Count);
                    DamageResult result = DamageCalculator.Calculate(context, rules, random);
                    score.Record(result);
                    if (index == 0)
                    {
                        first = result;
                    }
                    else if (index == 1)
                    {
                        second = result;
                    }
                }
            };

            float y = Screen.height * 0.5f;
            Set(mouse.position, new Vector2(Screen.width * 0.15f, y), queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, new Vector2(Screen.width * 0.5f, y), queueEventOnly: true);
            yield return null;
            Set(mouse.position, new Vector2(Screen.width * 0.85f, y), queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(hitCount, Is.EqualTo(2));
            Assert.That(hitBuffer[0].TargetId, Is.EqualTo(101));
            Assert.That(hitBuffer[0].IsWeakpoint, Is.True);
            Assert.That(hitBuffer[1].TargetId, Is.EqualTo(202));
            Assert.That(hitBuffer[1].IsWeakpoint, Is.False);
            Assert.That(first.Damage, Is.EqualTo(48));
            Assert.That(first.ScoreAward, Is.EqualTo(398));
            Assert.That(first.EnergyAward, Is.EqualTo(11));
            Assert.That(second.ComboMultiplier, Is.EqualTo(1.1d).Within(0.000001d));
            Assert.That(second.Damage, Is.EqualTo(13));
            Assert.That(second.ScoreAward, Is.EqualTo(123));
            Assert.That(second.EnergyAward, Is.EqualTo(3));
            Assert.That(score.Current.TotalDamage, Is.EqualTo(61));
            Assert.That(score.Current.TotalScore, Is.EqualTo(521));
            Assert.That(score.Current.TotalEnergyEarned, Is.EqualTo(14));
            Assert.That(score.Current.HitCount, Is.EqualTo(2));
            Assert.That(combo.Current.Count, Is.EqualTo(2));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        private void CreateTarget(int targetId, Vector2 referencePosition, bool hasWeakpoint)
        {
            var targetObject = new GameObject("T360 Hittable Target");
            targetObject.transform.SetParent(referenceRoot.transform, false);
            targetObject.transform.localPosition = new Vector3(
                referencePosition.x,
                referencePosition.y,
                0f);
            var target = targetObject.AddComponent<TestHittable>();
            target.Initialize(targetId);
            var body = targetObject.AddComponent<BoxCollider2D>();
            body.size = new Vector2(100f, 100f);

            if (!hasWeakpoint)
            {
                return;
            }

            var weakpointObject = new GameObject("T360 Weakpoint");
            weakpointObject.transform.SetParent(targetObject.transform, false);
            var hitbox = weakpointObject.AddComponent<TestStrokeHitbox>();
            hitbox.Initialize(target);
            var weakpointCollider = weakpointObject.AddComponent<CircleCollider2D>();
            weakpointCollider.radius = 25f;
            weakpointCollider.isTrigger = true;
        }

        private InputSystemPointerAdapter CreateAdapter()
        {
            adapterObject = new GameObject("T360 Pointer Adapter");
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

        private static IEnumerator LoadRuntimeConfiguration()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != SceneNames.MainMenu &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));
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

            public bool IsWeakpoint => true;

            public bool IsStrokeHitboxActive => true;

            public void Initialize(IHittable hitTarget)
            {
                HitTarget = hitTarget;
            }
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            public double NextUnitInterval()
            {
                return 0.5d;
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
