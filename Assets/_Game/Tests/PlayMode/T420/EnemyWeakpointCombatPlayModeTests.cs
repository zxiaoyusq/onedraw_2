using System.Collections;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T420
{
    [Category("T420")]
    public sealed class EnemyWeakpointCombatPlayModeTests : InputTestFixture
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
        public IEnumerator DiagonalMouseStrokeHitsConfiguredBossWeakpointAndInterruptsAttack()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeRuleConfig samplingRule = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny);
            var classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
            StrokeHitResolverSettings resolverSettings =
                StrokeHitSettingsFactory.CreateResolverSettings(config);
            referenceRoot = new GameObject("T420 Reference Space");
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            ConfigureReferenceSpace(referenceRoot.transform, camera);

            EnemyController enemy = CreateBoss(config, referenceRoot.transform);
            Assert.That(enemy.CompleteSpawn(0d), Is.True);
            Assert.That(
                enemy.BeginAttack(ConfigIds.EnemyAttacks.AtkBossRockfall, 0d),
                Is.True);
            enemy.Tick(0.5d);
            Assert.That(enemy.State.State, Is.EqualTo(EnemyState.Windup));
            Assert.That(enemy.Weakpoint.IsWindowOpen, Is.True);

            var resolver = new StrokeHitResolver(
                resolverSettings,
                new Physics2DStrokeHitQuery(
                    resolverSettings.QueryCapacity,
                    Physics2D.AllLayers,
                    includeTriggers: true,
                    referenceRoot.transform));
            var hitBuffer = new HitRecord[resolverSettings.MaximumUniqueTargets];
            DamageRuleSet damageRules = DamageRuleSetFactory.CreateForEnemy(
                config,
                ConfigIds.Stances.StanceBlade,
                ConfigIds.Enemies.BossTombKing);
            Physics2D.SyncTransforms();

            int hitCount = -1;
            GestureMatchResult completedGesture = null;
            EnemyHitResolution resolution = default;
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
                completedGesture = classifier.Classify(geometry);
                StrokeHitRule hitRule = StrokeHitSettingsFactory.CreateRule(
                    config.GetStrokeRule(completedGesture.RuleId));
                hitCount = resolver.Resolve(
                    geometry,
                    completedGesture,
                    hitRule,
                    hitBuffer);
                if (hitCount == 1)
                {
                    DamageContext context = DamageContext.FromHitRecord(
                        hitBuffer[0],
                        ConfigIds.Stances.StanceBlade,
                        comboCount: 1);
                    DamageResult damage = DamageCalculator.Calculate(
                        context,
                        damageRules,
                        new NonCriticalRandom());
                    resolution = enemy.ApplyStrokeDamage(
                        damage,
                        completedGesture.GestureType.ToString(),
                        0.5d,
                        $"stroke:{geometry.StrokeId}");
                }
            };

            var begin = new Vector2(Screen.width * 0.30f, Screen.height * 0.20f);
            var middle = new Vector2(Screen.width * 0.50f, Screen.height * 0.50f);
            var end = new Vector2(Screen.width * 0.70f, Screen.height * 0.80f);
            Set(mouse.position, begin, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, middle, queueEventOnly: true);
            yield return null;
            Set(mouse.position, end, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(completedGesture, Is.Not.Null);
            Assert.That(completedGesture.GestureType, Is.EqualTo(GestureType.Diagonal));
            Assert.That(hitCount, Is.EqualTo(1));
            Assert.That(hitBuffer[0].TargetId, Is.EqualTo(42001));
            Assert.That(hitBuffer[0].IsWeakpoint, Is.True);
            Assert.That(resolution.IsValid, Is.True);
            Assert.That(resolution.Damage.AppliedArmorDamage, Is.EqualTo(1));
            Assert.That(resolution.Interrupt.Status, Is.EqualTo(EnemyInterruptStatus.Interrupted));
            Assert.That(enemy.Damage.CurrentArmor, Is.EqualTo(119));
            Assert.That(enemy.State.State, Is.EqualTo(EnemyState.Stun));
            Assert.That(enemy.Weakpoint.IsWindowOpen, Is.False);
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        private static EnemyController CreateBoss(
            IConfigProvider config,
            Transform referenceSpace)
        {
            var enemyObject = new GameObject("T420 Boss");
            enemyObject.transform.SetParent(referenceSpace, false);
            enemyObject.transform.localPosition = new Vector3(960f, 540f, 0f);
            enemyObject.SetActive(false);
            enemyObject.AddComponent<Damageable>();
            EnemyController enemy = enemyObject.AddComponent<EnemyController>();
            enemy.enabled = false;
            var weakpointObject = new GameObject("T420 Boss Weakpoint");
            weakpointObject.transform.SetParent(enemyObject.transform, false);
            WeakpointController weakpoint = weakpointObject.AddComponent<WeakpointController>();
            enemy.Spawn(
                config,
                ConfigIds.Enemies.BossTombKing,
                42001,
                0d,
                weakpoint);
            return enemy;
        }

        private InputSystemPointerAdapter CreateAdapter()
        {
            adapterObject = new GameObject("T420 Pointer Adapter");
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

        private sealed class NonCriticalRandom : IRandomSource
        {
            public double NextUnitInterval()
            {
                return 0.99d;
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
