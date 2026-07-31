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

namespace OneStrokeDemon.Tests.PlayMode.T370
{
    [Category("ProjectileReflect")]
    public sealed class ProjectileReflectPlayModeTests : InputTestFixture
    {
        private GameObject adapterObject;
        private GameObject projectileObject;
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

            if (projectileObject != null)
            {
                Object.DestroyImmediate(projectileObject);
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
        public IEnumerator MouseStrokeReflectsProjectileAndPreservesDamageAttributionAcrossReuse()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            referenceRoot = new GameObject("T370 Reference Space");
            ProjectileController projectile = CreateProjectile();
            ProjectileRuleSet ghostFire = ProjectileRuleSetFactory.Create(
                config,
                ConfigIds.Projectiles.ProjGhostFire);
            var originalEnemy = new ProjectileOwner(ProjectileFaction.Enemy, 7001);
            var player = new ProjectileOwner(ProjectileFaction.Player, 101);
            projectile.Spawn(
                ghostFire,
                5001,
                originalEnemy,
                referenceRoot.transform,
                new Vector2(960f, 540f),
                Vector2.left);
            Physics2D.SyncTransforms();

            StrokeRuleConfig samplingRule = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny);
            var classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
            StrokeHitResolverSettings resolverSettings =
                StrokeHitSettingsFactory.CreateResolverSettings(config);
            var resolver = new StrokeHitResolver(
                resolverSettings,
                new Physics2DStrokeHitQuery(
                    resolverSettings.QueryCapacity,
                    Physics2D.AllLayers,
                    includeTriggers: true,
                    referenceRoot.transform));
            var hitBuffer = new HitRecord[resolverSettings.MaximumUniqueTargets];
            ProjectileStrokeResult reflection = default;
            int hitCount = -1;

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
                hitCount = resolver.Resolve(
                    geometry,
                    gesture,
                    StrokeHitSettingsFactory.CreateRule(config.GetStrokeRule(gesture.RuleId)),
                    hitBuffer);
                if (hitCount > 0 && hitBuffer[0].Target is ProjectileHitTarget target)
                {
                    reflection = target.ResolveStrokeHit(
                        hitBuffer[0],
                        ConfigIds.Stances.StanceBlade,
                        player);
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

            Assert.That(hitCount, Is.EqualTo(1));
            Assert.That(reflection.IsValid, Is.True);
            Assert.That(reflection.Outcome, Is.EqualTo(ProjectileStrokeOutcome.Reflected));
            Assert.That(reflection.OwnershipBefore.CurrentOwner.Faction,
                Is.EqualTo(ProjectileFaction.Enemy));
            Assert.That(reflection.OwnershipAfter.CurrentOwner.Faction,
                Is.EqualTo(ProjectileFaction.Player));
            Assert.That(reflection.OwnershipAfter.CurrentOwner.EntityId, Is.EqualTo(101));
            Assert.That(reflection.OwnershipAfter.OriginalOwner.EntityId, Is.EqualTo(7001));
            Assert.That(reflection.OwnershipAfter.ReflectionCount, Is.EqualTo(1));
            Assert.That(reflection.DirectionBefore, Is.EqualTo(Vector2.left));
            Assert.That(reflection.DirectionAfter, Is.EqualTo(Vector2.right));
            Assert.That(projectile.HitTarget.CanReceiveStrokeHit, Is.False);

            Vector2 reflectedAt = projectile.ReferencePosition;
            ProjectileReleaseSnapshot earlyRelease = projectile.Tick(0.5f);
            Assert.That(earlyRelease.IsValid, Is.False);
            Assert.That(projectile.ReferencePosition.x,
                Is.EqualTo(reflectedAt.x + 90f).Within(0.001f));
            Assert.That(projectile.ReferencePosition.y,
                Is.EqualTo(reflectedAt.y).Within(0.001f));

            Assert.That(projectile.TryResolveImpact(originalEnemy, out ProjectileImpactResult impact),
                Is.True);
            Assert.That(impact.DamageSource.ProjectileId,
                Is.EqualTo(ConfigIds.Projectiles.ProjGhostFire));
            Assert.That(impact.DamageSource.Damage, Is.EqualTo(8));
            Assert.That(impact.DamageSource.CurrentOwner.Faction,
                Is.EqualTo(ProjectileFaction.Player));
            Assert.That(impact.DamageSource.CurrentOwner.EntityId, Is.EqualTo(101));
            Assert.That(impact.DamageSource.OriginalOwner.Faction,
                Is.EqualTo(ProjectileFaction.Enemy));
            Assert.That(impact.DamageSource.OriginalOwner.EntityId, Is.EqualTo(7001));
            Assert.That(impact.DamageSource.ReflectionCount, Is.EqualTo(1));
            Assert.That(impact.Target.EntityId, Is.EqualTo(7001));
            Assert.That(impact.Release.Reason, Is.EqualTo(ProjectileReleaseReason.Impact));
            AssertCompletelyReleased(projectile);

            ProjectileRuleSet rockfall = ProjectileRuleSetFactory.Create(
                config,
                ConfigIds.Projectiles.ProjRockfall);
            var nextEnemy = new ProjectileOwner(ProjectileFaction.Enemy, 9001);
            projectile.Spawn(
                rockfall,
                5002,
                nextEnemy,
                referenceRoot.transform,
                new Vector2(100f, 200f),
                Vector2.down);

            Assert.That(projectile.IsActive, Is.True);
            Assert.That(projectile.Rules.ProjectileId,
                Is.EqualTo(ConfigIds.Projectiles.ProjRockfall));
            Assert.That(projectile.Ownership.CurrentOwner.EntityId, Is.EqualTo(9001));
            Assert.That(projectile.Ownership.OriginalOwner.EntityId, Is.EqualTo(9001));
            Assert.That(projectile.Ownership.ReflectionCount, Is.Zero);
            Assert.That(projectile.TravelDirection, Is.EqualTo(Vector2.down));
            Assert.That(projectile.ElapsedSeconds, Is.Zero);
            Assert.That(projectile.HitTarget.HitTargetId, Is.EqualTo(5002));
            Assert.That(projectile.HitCollider.radius, Is.EqualTo(34f));
            Assert.That(projectile.Release(ProjectileReleaseReason.Manual).Reason,
                Is.EqualTo(ProjectileReleaseReason.Manual));
            AssertCompletelyReleased(projectile);
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        [UnityTest]
        public IEnumerator CutStanceGateAndLifetimeTransitionsUseRealStrokeColliders()
        {
            yield return LoadRuntimeConfiguration();
            IConfigProvider config = GameplayConfigRuntime.Current;
            referenceRoot = new GameObject("T370 Rule Reference Space");
            ProjectileController projectile = CreateProjectile();
            StrokeHitResolverSettings resolverSettings =
                StrokeHitSettingsFactory.CreateResolverSettings(config);
            var resolver = new StrokeHitResolver(
                resolverSettings,
                new Physics2DStrokeHitQuery(
                    resolverSettings.QueryCapacity,
                    Physics2D.AllLayers,
                    includeTriggers: true,
                    referenceRoot.transform));
            var classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
            var hits = new HitRecord[resolverSettings.MaximumUniqueTargets];
            var enemy = new ProjectileOwner(ProjectileFaction.Enemy, 801);
            var player = new ProjectileOwner(ProjectileFaction.Player, 201);

            projectile.Spawn(
                ProjectileRuleSetFactory.Create(config, ConfigIds.Projectiles.ProjRockfall),
                6001,
                enemy,
                referenceRoot.transform,
                new Vector2(100f, 0f),
                Vector2.left);
            ProjectileStrokeResult cut = ResolveSingleStroke(
                config,
                classifier,
                resolver,
                hits,
                71,
                ConfigIds.Stances.StanceBlade,
                player);

            Assert.That(cut.Outcome, Is.EqualTo(ProjectileStrokeOutcome.Cut));
            Assert.That(cut.Release.Reason, Is.EqualTo(ProjectileReleaseReason.Cut));
            Assert.That(cut.Release.ProjectileId,
                Is.EqualTo(ConfigIds.Projectiles.ProjRockfall));
            AssertCompletelyReleased(projectile);

            ProjectileRuleSet sealBolt = ProjectileRuleSetFactory.Create(
                config,
                ConfigIds.Projectiles.ProjSealBolt);
            projectile.Spawn(
                sealBolt,
                6002,
                enemy,
                referenceRoot.transform,
                new Vector2(100f, 0f),
                Vector2.left);
            ProjectileStrokeResult mismatch = ResolveSingleStroke(
                config,
                classifier,
                resolver,
                hits,
                72,
                ConfigIds.Stances.StanceBlade,
                player);

            Assert.That(mismatch.Outcome,
                Is.EqualTo(ProjectileStrokeOutcome.RequiredStanceMismatch));
            Assert.That(projectile.IsActive, Is.True);
            Assert.That(projectile.Ownership.CurrentOwner.Faction,
                Is.EqualTo(ProjectileFaction.Enemy));

            ProjectileStrokeResult reflected = ResolveSingleStroke(
                config,
                classifier,
                resolver,
                hits,
                73,
                ConfigIds.Stances.StanceTalisman,
                player);
            Assert.That(reflected.Outcome, Is.EqualTo(ProjectileStrokeOutcome.Reflected));
            Assert.That(projectile.Ownership.CurrentOwner.Faction,
                Is.EqualTo(ProjectileFaction.Player));

            ProjectileReleaseSnapshot expired = projectile.Tick(sealBolt.LifetimeSeconds);
            Assert.That(expired.IsValid, Is.True);
            Assert.That(expired.Reason, Is.EqualTo(ProjectileReleaseReason.LifetimeExpired));
            Assert.That(expired.Ownership.CurrentOwner.Faction,
                Is.EqualTo(ProjectileFaction.Player));
            Assert.That(expired.Ownership.OriginalOwner.Faction,
                Is.EqualTo(ProjectileFaction.Enemy));
            Assert.That(expired.Ownership.ReflectionCount, Is.EqualTo(1));
            AssertCompletelyReleased(projectile);
        }

        private ProjectileController CreateProjectile()
        {
            projectileObject = new GameObject("T370 Projectile");
            projectileObject.SetActive(false);
            ProjectileController controller = projectileObject.AddComponent<ProjectileController>();
            controller.enabled = false;
            return controller;
        }

        private InputSystemPointerAdapter CreateAdapter()
        {
            adapterObject = new GameObject("T370 Pointer Adapter");
            var adapter = adapterObject.AddComponent<InputSystemPointerAdapter>();
            adapter.Initialize(
                new ReferencePixelConverter(new Vector2(1920f, 1080f)),
                new FixedSafeAreaProvider(new Rect(0f, 0f, Screen.width, Screen.height)),
                new NeverBlocked());
            return adapter;
        }

        private static ProjectileStrokeResult ResolveSingleStroke(
            IConfigProvider config,
            GestureClassifier classifier,
            StrokeHitResolver resolver,
            HitRecord[] hits,
            ulong strokeId,
            string stanceId,
            ProjectileOwner player)
        {
            StrokeGeometryData geometry = CreateGeometry(
                strokeId,
                Vector2.zero,
                new Vector2(200f, 0f));
            GestureMatchResult gesture = classifier.Classify(geometry);
            int hitCount = resolver.Resolve(
                geometry,
                gesture,
                StrokeHitSettingsFactory.CreateRule(config.GetStrokeRule(gesture.RuleId)),
                hits);
            Assert.That(hitCount, Is.EqualTo(1));
            Assert.That(hits[0].Target, Is.TypeOf<ProjectileHitTarget>());
            return ((ProjectileHitTarget)hits[0].Target).ResolveStrokeHit(
                hits[0],
                stanceId,
                player);
        }

        private static StrokeGeometryData CreateGeometry(
            ulong strokeId,
            Vector2 start,
            Vector2 end)
        {
            var sampler = new StrokeSampler(
                new StrokeSamplingSettings(0.001f, 100000f, 256));
            sampler.Begin(strokeId, start, 1d);
            StrokeData stroke = sampler.End(end, 2d);
            return StrokeGeometry.Process(stroke, new StrokeGeometrySettings(0f, 96));
        }

        private static void AssertCompletelyReleased(ProjectileController projectile)
        {
            Assert.That(projectile.IsActive, Is.False);
            Assert.That(projectile.gameObject.activeSelf, Is.False);
            Assert.That(projectile.Rules.IsConfigured, Is.False);
            Assert.That(projectile.Ownership.IsValid, Is.False);
            Assert.That(projectile.ReferenceSpace, Is.Null);
            Assert.That(projectile.ReferencePosition, Is.EqualTo(Vector2.zero));
            Assert.That(projectile.TravelDirection, Is.EqualTo(Vector2.zero));
            Assert.That(projectile.ElapsedSeconds, Is.Zero);
            Assert.That(projectile.HitTarget.HitTargetId, Is.Zero);
            Assert.That(projectile.HitTarget.CanReceiveStrokeHit, Is.False);
            Assert.That(projectile.HitCollider.enabled, Is.False);
            Assert.That(projectile.transform.parent, Is.Null);
            Assert.That(projectile.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(projectile.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(projectile.transform.localScale, Is.EqualTo(Vector3.one));
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
