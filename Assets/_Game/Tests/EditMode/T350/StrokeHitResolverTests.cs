using System;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T350
{
    [Category("StrokeHitResolver")]
    public sealed class StrokeHitResolverTests
    {
        private GameplayConfigService config;
        private GestureClassifier classifier;
        private StrokeHitResolverSettings resolverSettings;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), RuntimeConfigTestFixture.Source);
            classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
            resolverSettings = StrokeHitSettingsFactory.CreateResolverSettings(config);
        }

        [Test]
        public void RuntimeConfigMapsRuleRadiusAndActiveTargetCapacity()
        {
            StrokeHitRule any = StrokeHitSettingsFactory.CreateRule(
                config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny));
            StrokeHitRule circle = StrokeHitSettingsFactory.CreateRule(
                config.GetStrokeRule(ConfigIds.StrokeRules.StrokeCircle));

            Assert.That(any.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeAny));
            Assert.That(any.RadiusReferencePixels, Is.EqualTo(18f));
            Assert.That(circle.RadiusReferencePixels, Is.EqualTo(28f));
            Assert.That(resolverSettings.MaximumUniqueTargets, Is.EqualTo(58));
            Assert.That(resolverSettings.QueryCapacity, Is.EqualTo(117));
        }

        [Test]
        public void ResolveSortsByFirstPathContactAndAggregatesWeakpointPerTarget()
        {
            var first = new TestHittable(101);
            var second = new TestHittable(202);
            var third = new TestHittable(303);
            StrokeGeometryData geometry = CreateGeometry(
                71,
                new Vector2(0f, 0f),
                new Vector2(100f, 0f),
                new Vector2(200f, 20f));
            GestureMatchResult gesture = classifier.Classify(geometry);
            var query = new ScriptedQuery(
                new[]
                {
                    new StrokeHitCandidate(second, false, 0.8f),
                    new StrokeHitCandidate(first, false, 0.2f),
                    new StrokeHitCandidate(first, false, 0.4f)
                },
                new[]
                {
                    new StrokeHitCandidate(first, true, 0.2f),
                    new StrokeHitCandidate(third, false, 0.5f)
                });
            var resolver = new StrokeHitResolver(resolverSettings, query);
            var results = new HitRecord[resolverSettings.MaximumUniqueTargets];
            StrokeHitRule rule = RuleFor(gesture);

            int count = resolver.Resolve(geometry, gesture, rule, results);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(query.CallCount, Is.EqualTo(2));
            Assert.That(results[0].Target, Is.SameAs(first));
            Assert.That(results[0].TargetId, Is.EqualTo(101));
            Assert.That(results[0].IsWeakpoint, Is.True);
            Assert.That(results[0].PathDistanceReferencePixels, Is.EqualTo(20f).Within(0.001f));
            Assert.That(results[1].Target, Is.SameAs(second));
            Assert.That(results[1].PathDistanceReferencePixels, Is.EqualTo(80f).Within(0.001f));
            Assert.That(results[2].Target, Is.SameAs(third));
            Assert.That(results[2].PathDistanceReferencePixels, Is.GreaterThan(150f));
            Assert.That(results[0].PathParameter, Is.LessThan(results[1].PathParameter));
            Assert.That(results[1].PathParameter, Is.LessThan(results[2].PathParameter));
            Assert.That(results[0].StrokeId, Is.EqualTo(geometry.StrokeId));
            Assert.That(results[0].Gesture, Is.SameAs(gesture));
            Assert.That(results[0].GestureRuleId, Is.EqualTo(gesture.RuleId));
            Assert.That(results[0].GestureType, Is.EqualTo(gesture.GestureType));
            Assert.That(results[0].Timestamp, Is.EqualTo(geometry.EndedAt));
        }

        [Test]
        public void DisabledTargetsAreSkippedAndEqualPathUsesStableTargetIdOrder()
        {
            var laterId = new TestHittable(20);
            var earlierId = new TestHittable(10);
            var disabled = new TestHittable(5, canReceive: false);
            StrokeGeometryData geometry = CreateGeometry(
                72,
                Vector2.zero,
                new Vector2(200f, 0f));
            GestureMatchResult gesture = classifier.Classify(geometry);
            var query = new ScriptedQuery(new[]
            {
                new StrokeHitCandidate(laterId, false, 0.5f),
                new StrokeHitCandidate(disabled, true, 0.1f),
                new StrokeHitCandidate(earlierId, false, 0.5f)
            });
            var resolver = new StrokeHitResolver(resolverSettings, query);
            var results = new HitRecord[resolverSettings.MaximumUniqueTargets];

            int count = resolver.Resolve(geometry, gesture, RuleFor(gesture), results);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(results[0].TargetId, Is.EqualTo(10));
            Assert.That(results[1].TargetId, Is.EqualTo(20));
        }

        [Test]
        public void NoGestureDoesNotQueryAndMismatchedRuleOrSmallBufferFails()
        {
            StrokeGeometryData shortGeometry = CreateGeometry(
                73,
                Vector2.zero,
                new Vector2(40f, 0f));
            GestureMatchResult noGesture = classifier.Classify(shortGeometry);
            var query = new ScriptedQuery(Array.Empty<StrokeHitCandidate>());
            var resolver = new StrokeHitResolver(resolverSettings, query);
            var results = new HitRecord[resolverSettings.MaximumUniqueTargets];

            int noHitCount = resolver.Resolve(shortGeometry, noGesture, default, results);

            Assert.That(noGesture.IsMatch, Is.False);
            Assert.That(noHitCount, Is.Zero);
            Assert.That(query.CallCount, Is.Zero);

            StrokeGeometryData geometry = CreateGeometry(
                74,
                Vector2.zero,
                new Vector2(200f, 0f));
            GestureMatchResult gesture = classifier.Classify(geometry);
            StrokeHitRule wrongRule = StrokeHitSettingsFactory.CreateRule(
                config.GetStrokeRule(ConfigIds.StrokeRules.StrokeCircle));
            Assert.Throws<ArgumentException>(() =>
                resolver.Resolve(geometry, gesture, wrongRule, results));
            Assert.Throws<ArgumentException>(() =>
                resolver.Resolve(
                    geometry,
                    gesture,
                    RuleFor(gesture),
                    new HitRecord[resolverSettings.MaximumUniqueTargets - 1]));
        }

        [Test]
        public void ReplayProducesSameSortedRecordsWithoutRetainingInternalTargets()
        {
            var target = new TestHittable(801);
            StrokeGeometryData geometry = CreateGeometry(
                75,
                Vector2.zero,
                new Vector2(200f, 0f));
            GestureMatchResult gesture = classifier.Classify(geometry);
            var query = new RepeatingSingleCandidateQuery(
                new StrokeHitCandidate(target, true, 0.25f));
            var resolver = new StrokeHitResolver(resolverSettings, query);
            var first = new HitRecord[resolverSettings.MaximumUniqueTargets];
            var replay = new HitRecord[resolverSettings.MaximumUniqueTargets];
            StrokeHitRule rule = RuleFor(gesture);

            int firstCount = resolver.Resolve(geometry, gesture, rule, first);
            int replayCount = resolver.Resolve(geometry, gesture, rule, replay);

            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(replayCount, Is.EqualTo(1));
            Assert.That(replay[0].TargetId, Is.EqualTo(first[0].TargetId));
            Assert.That(replay[0].IsWeakpoint, Is.EqualTo(first[0].IsWeakpoint));
            Assert.That(replay[0].PathParameter, Is.EqualTo(first[0].PathParameter));
            Assert.That(replay[0].Timestamp, Is.EqualTo(first[0].Timestamp));
        }

        [Test]
        public void WarmResolveHotPathAllocatesNoManagedMemory()
        {
            var target = new TestHittable(901);
            StrokeGeometryData geometry = CreateGeometry(
                76,
                Vector2.zero,
                new Vector2(200f, 0f));
            GestureMatchResult gesture = classifier.Classify(geometry);
            var query = new RepeatingSingleCandidateQuery(
                new StrokeHitCandidate(target, false, 0.5f));
            var resolver = new StrokeHitResolver(resolverSettings, query);
            var results = new HitRecord[resolverSettings.MaximumUniqueTargets];
            StrokeHitRule rule = RuleFor(gesture);
            for (int index = 0; index < 16; index++)
            {
                resolver.Resolve(geometry, gesture, rule, results);
            }

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 128; index++)
            {
                resolver.Resolve(geometry, gesture, rule, results);
            }

            long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(allocatedAfter - allocatedBefore, Is.Zero);
        }

        private StrokeHitRule RuleFor(GestureMatchResult gesture)
        {
            return StrokeHitSettingsFactory.CreateRule(config.GetStrokeRule(gesture.RuleId));
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

        private sealed class TestHittable : IHittable
        {
            public TestHittable(int targetId, bool canReceive = true)
            {
                HitTargetId = targetId;
                CanReceiveStrokeHit = canReceive;
            }

            public int HitTargetId { get; }

            public bool CanReceiveStrokeHit { get; }
        }

        private sealed class ScriptedQuery : IStrokeHitQuery
        {
            private readonly StrokeHitCandidate[][] segments;

            public ScriptedQuery(params StrokeHitCandidate[][] scriptedSegments)
            {
                segments = scriptedSegments;
            }

            public int CallCount { get; private set; }

            public int QuerySegment(
                Vector2 startReferencePixels,
                Vector2 endReferencePixels,
                float radiusReferencePixels,
                StrokeHitCandidate[] results)
            {
                StrokeHitCandidate[] source = segments[CallCount++];
                Array.Copy(source, results, source.Length);
                return source.Length;
            }
        }

        private sealed class RepeatingSingleCandidateQuery : IStrokeHitQuery
        {
            private readonly StrokeHitCandidate candidate;

            public RepeatingSingleCandidateQuery(StrokeHitCandidate hitCandidate)
            {
                candidate = hitCandidate;
            }

            public int QuerySegment(
                Vector2 startReferencePixels,
                Vector2 endReferencePixels,
                float radiusReferencePixels,
                StrokeHitCandidate[] results)
            {
                results[0] = candidate;
                return 1;
            }
        }
    }
}
