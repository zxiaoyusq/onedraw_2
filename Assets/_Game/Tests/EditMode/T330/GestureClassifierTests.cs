using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T330
{
    [Category("GestureClassifier")]
    public sealed class GestureClassifierTests
    {
        private GameplayConfigService config;
        private GestureClassifier classifier;

        [SetUp]
        public void SetUp()
        {
            config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), RuntimeConfigTestFixture.Source);
            classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
        }

        [Test]
        public void RuntimeStrokeRulesMapAllRecognitionThresholdsIntoReadOnlyRules()
        {
            IReadOnlyList<StrokeRuleConfig> rows = config.GetStrokeRules();
            var mutableRows = rows as IList<StrokeRuleConfig>;
            Assert.That(rows.Count, Is.EqualTo(8));
            Assert.That(mutableRows, Is.Not.Null);
            Assert.That(mutableRows.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutableRows.Add(rows[0]));

            IReadOnlyList<GestureRule> rules = GestureRuleSetFactory.FromConfig(config);
            var mutableRules = rules as IList<GestureRule>;
            GestureRule circle = FindRule(rules, ConfigIds.StrokeRules.StrokeCircle);
            Assert.That(mutableRules, Is.Not.Null);
            Assert.That(mutableRules.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutableRules.RemoveAt(0));
            Assert.That(circle.GestureType, Is.EqualTo(GestureType.Circle));
            Assert.That(circle.MinimumLengthReferencePixels, Is.EqualTo(180f));
            Assert.That(circle.DirectionToleranceDegrees, Is.EqualTo(45f));
            Assert.That(circle.CloseDistanceReferencePixels, Is.EqualTo(75f));
            Assert.That(circle.MinimumAreaReferencePixelsSquared, Is.EqualTo(16000f));
            Assert.That(circle.MinimumNormalizedCurvature, Is.EqualTo(0.15f));
            Assert.That(circle.ChargeHoldSeconds, Is.Zero);
            GestureRule triangle = FindRule(rules, ConfigIds.StrokeRules.StrokeTriangle);
            Assert.That(triangle.GestureType, Is.EqualTo(GestureType.Triangle));
            Assert.That(triangle.CloseDistanceReferencePixels, Is.EqualTo(55f));
            Assert.That(triangle.MinimumAreaReferencePixelsSquared, Is.EqualTo(8000f));
            Assert.That(triangle.ShapeFitToleranceReferencePixels, Is.EqualTo(28f));
            Assert.That(triangle.MinimumCornerAngleDegrees, Is.EqualTo(22f));
        }

        [TestCase(200f, 0f, GestureType.Horizontal, 0f)]
        [TestCase(-200f, 0f, GestureType.Horizontal, 0f)]
        [TestCase(0f, 200f, GestureType.Vertical, 90f)]
        [TestCase(0f, -200f, GestureType.Vertical, 90f)]
        [TestCase(200f, 200f, GestureType.Diagonal, 45f)]
        [TestCase(-200f, 200f, GestureType.Diagonal, 135f)]
        public void StraightStrokesUseUndirectedConfiguredAngleClasses(
            float endX,
            float endY,
            GestureType expectedType,
            float expectedAngle)
        {
            Vector2 end = new Vector2(endX, endY);
            StrokeGeometryData geometry = CreateGeometry(
                new[] { Vector2.zero, end * 0.5f, end },
                new[] { 0d, 0.05d, 0.1d });

            GestureMatchResult result = classifier.Classify(geometry);

            Assert.That(result.IsMatch, Is.True);
            Assert.That(result.GestureType, Is.EqualTo(expectedType));
            Assert.That(result.DirectionAngleDegrees, Is.EqualTo(expectedAngle).Within(0.001f));
            Assert.That(result.NormalizedCurvature, Is.Zero.Within(0.0001f));
            Assert.That(
                result.AverageSpeedReferencePixelsPerSecond,
                Is.EqualTo(result.LengthReferencePixels / 0.1d).Within(0.001d));
            Assert.That(result.Confidence, Is.InRange(0f, 1f));
        }

        [Test]
        public void OpenArcMatchesArcBeforeItsDiagonalChord()
        {
            Vector2[] points = CreateArc(100f, 0f, 90f, 12, close: false);

            GestureMatchResult result = classifier.Classify(CreateGeometry(points));

            Assert.That(result.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeArc));
            Assert.That(result.GestureType, Is.EqualTo(GestureType.Arc));
            Assert.That(result.LengthReferencePixels, Is.GreaterThan(120f));
            Assert.That(result.NormalizedCurvature, Is.GreaterThan(0.22f));
            Assert.That(result.ClosureDistanceReferencePixels, Is.GreaterThan(75f));
        }

        [Test]
        public void LargeClosedLoopMatchesCircleBeforeArc()
        {
            Vector2[] points = CreateArc(100f, 0f, 360f, 32, close: true);

            GestureMatchResult result = classifier.Classify(CreateGeometry(points));

            Assert.That(result.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeCircle));
            Assert.That(result.GestureType, Is.EqualTo(GestureType.Circle));
            Assert.That(result.ClosureDistanceReferencePixels, Is.LessThan(0.001f));
            Assert.That(result.ClosureRatio, Is.LessThan(0.001f));
            Assert.That(result.AreaReferencePixelsSquared, Is.GreaterThan(16000f));
            Assert.That(result.NormalizedCurvature, Is.GreaterThan(1.5f));
        }

        [Test]
        [Category("T699H")]
        public void ImperfectClosedThreeSidedStrokeMatchesTriangleBeforeFallbacks()
        {
            StrokeGeometryData geometry = CreateGeometry(new[]
            {
                new Vector2(2f, 3f),
                new Vector2(70f, 7f),
                new Vector2(142f, 1f),
                new Vector2(108f, 66f),
                new Vector2(72f, 132f),
                new Vector2(35f, 68f),
                new Vector2(5f, 6f),
            });

            GestureMatchResult result = classifier.Classify(geometry);

            Assert.That(result.IsMatch, Is.True);
            Assert.That(result.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeTriangle));
            Assert.That(result.GestureType, Is.EqualTo(GestureType.Triangle));
            Assert.That(result.AreaReferencePixelsSquared, Is.GreaterThan(8000f));
            Assert.That(result.ClosureDistanceReferencePixels, Is.LessThan(55f));
            Assert.That(result.Confidence, Is.InRange(0.5f, 1f));
        }

        [Test]
        [Category("T699H")]
        public void CircleSquareAndOpenThreeSidesDoNotMatchTriangle()
        {
            GestureMatchResult circle = classifier.Classify(CreateGeometry(
                CreateArc(100f, 0f, 360f, 32, close: true)));
            GestureMatchResult square = classifier.Classify(CreateGeometry(new[]
            {
                Vector2.zero,
                new Vector2(120f, 0f),
                new Vector2(120f, 120f),
                new Vector2(0f, 120f),
                Vector2.zero,
            }));
            GestureMatchResult open = classifier.Classify(CreateGeometry(new[]
            {
                Vector2.zero,
                new Vector2(140f, 0f),
                new Vector2(70f, 130f),
                new Vector2(0f, 70f),
            }));

            Assert.That(circle.GestureType, Is.EqualTo(GestureType.Circle));
            Assert.That(square.GestureType, Is.Not.EqualTo(GestureType.Triangle));
            Assert.That(open.GestureType, Is.Not.EqualTo(GestureType.Triangle));
        }

        [Test]
        public void ChargeUsesInitialHoldInsteadOfWholeSlowDrawingDuration()
        {
            var heldSampler = new StrokeSampler(new StrokeSamplingSettings(8f, 1000f, 16));
            heldSampler.Begin(101, Vector2.zero, 0d);
            Assert.That(
                heldSampler.AddPoint(new Vector2(4f, 0f), 0.2d),
                Is.EqualTo(StrokeSampleResult.IgnoredBelowMinimumDistance));
            heldSampler.AddPoint(new Vector2(20f, 0f), 0.45d);
            StrokeData heldStroke = heldSampler.End(new Vector2(120f, 0f), 0.55d);

            var slowSampler = new StrokeSampler(new StrokeSamplingSettings(8f, 1000f, 16));
            slowSampler.Begin(102, Vector2.zero, 0d);
            slowSampler.AddPoint(new Vector2(20f, 0f), 0.1d);
            StrokeData slowStroke = slowSampler.End(new Vector2(120f, 0f), 1d);

            GestureMatchResult held = classifier.Classify(Process(heldStroke));
            GestureMatchResult slow = classifier.Classify(Process(slowStroke));

            Assert.That(heldStroke.InitialHoldDuration, Is.EqualTo(0.45d).Within(0.000001d));
            Assert.That(held.InitialHoldSeconds, Is.EqualTo(0.45d).Within(0.000001d));
            Assert.That(held.GestureType, Is.EqualTo(GestureType.Charged));
            Assert.That(held.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeCharged));
            Assert.That(slowStroke.Duration, Is.EqualTo(1d));
            Assert.That(slowStroke.InitialHoldDuration, Is.EqualTo(0.1d).Within(0.000001d));
            Assert.That(slow.GestureType, Is.EqualTo(GestureType.Horizontal));
        }

        [Test]
        [Category("T699G")]
        public void NormalCombatCollapsesOrdinaryShapesButKeepsChargedAndTriangle()
        {
            var normalCombat = new GestureClassifier(new[]
            {
                GestureRuleSetFactory.FromConfig(
                    config,
                    ConfigIds.StrokeRules.StrokeAny),
                GestureRuleSetFactory.FromConfig(
                    config,
                    ConfigIds.StrokeRules.StrokeCharged),
                GestureRuleSetFactory.FromConfig(
                    config,
                    ConfigIds.StrokeRules.StrokeTriangle),
            });
            StrokeGeometryData[] ordinaryShapes =
            {
                CreateGeometry(new[]
                {
                    Vector2.zero,
                    new Vector2(100f, 0f),
                    new Vector2(200f, 0f),
                }),
                CreateGeometry(new[]
                {
                    Vector2.zero,
                    new Vector2(0f, 100f),
                    new Vector2(0f, 200f),
                }),
                CreateGeometry(CreateArc(100f, 0f, 90f, 12, close: false)),
                CreateGeometry(CreateArc(100f, 0f, 360f, 32, close: true)),
            };
            for (int index = 0; index < ordinaryShapes.Length; index++)
            {
                GestureMatchResult result = normalCombat.Classify(ordinaryShapes[index]);
                Assert.That(result.GestureType, Is.EqualTo(GestureType.Any), $"shape {index}");
                Assert.That(result.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeAny));
            }

            var heldSampler = new StrokeSampler(new StrokeSamplingSettings(8f, 1000f, 16));
            heldSampler.Begin(501, Vector2.zero, 0d);
            heldSampler.AddPoint(new Vector2(20f, 0f), 0.45d);
            StrokeGeometryData held = Process(
                heldSampler.End(new Vector2(120f, 0f), 0.55d));

            GestureMatchResult charged = normalCombat.Classify(held);
            GestureMatchResult triangle = normalCombat.Classify(CreateGeometry(new[]
            {
                Vector2.zero,
                new Vector2(140f, 0f),
                new Vector2(70f, 130f),
                new Vector2(3f, 4f),
            }));
            GestureMatchResult ultimateCircle = classifier.Classify(ordinaryShapes[3]);
            Assert.That(charged.GestureType, Is.EqualTo(GestureType.Charged));
            Assert.That(charged.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeCharged));
            Assert.That(triangle.GestureType, Is.EqualTo(GestureType.Triangle));
            Assert.That(triangle.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeTriangle));
            Assert.That(ultimateCircle.GestureType, Is.EqualTo(GestureType.Circle));
        }

        [Test]
        public void AnyIsOnlyAConfiguredFallbackAndTooShortStrokeDoesNotMatch()
        {
            GestureMatchResult fallback = classifier.Classify(CreateGeometry(
                new[] { Vector2.zero, new Vector2(30f, 0f), new Vector2(60f, 0f) }));
            GestureMatchResult tooShort = classifier.Classify(CreateGeometry(
                new[] { Vector2.zero, new Vector2(20f, 0f), new Vector2(40f, 0f) }));

            Assert.That(fallback.GestureType, Is.EqualTo(GestureType.Any));
            Assert.That(fallback.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeAny));
            Assert.That(tooShort.IsMatch, Is.False);
            Assert.That(tooShort.GestureType, Is.EqualTo(GestureType.None));
            Assert.That(tooShort.RuleId, Is.Empty);
            Assert.That(tooShort.Confidence, Is.Zero);
            Assert.That(tooShort.LengthReferencePixels, Is.EqualTo(40f).Within(0.001f));
        }

        [Test]
        public void TableBoundariesPreventNearHorizontalAndSmallLoopMisclassification()
        {
            float angleRadians = 23f * Mathf.Deg2Rad;
            Vector2 end = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * 200f;
            GestureMatchResult nearHorizontal = classifier.Classify(CreateGeometry(
                new[] { Vector2.zero, end * 0.5f, end }));
            GestureMatchResult smallLoop = classifier.Classify(CreateGeometry(
                CreateArc(30f, 0f, 360f, 24, close: true)));
            GestureMatchResult straight = classifier.Classify(CreateGeometry(
                new[] { Vector2.zero, new Vector2(100f, 0f), new Vector2(200f, 0f) }));

            Assert.That(nearHorizontal.DirectionAngleDegrees, Is.EqualTo(23f).Within(0.001f));
            Assert.That(nearHorizontal.GestureType, Is.EqualTo(GestureType.Any));
            Assert.That(smallLoop.AreaReferencePixelsSquared, Is.LessThan(16000f));
            Assert.That(smallLoop.GestureType, Is.EqualTo(GestureType.Arc));
            Assert.That(straight.GestureType, Is.EqualTo(GestureType.Horizontal));
            Assert.That(straight.NormalizedCurvature, Is.Zero.Within(0.0001f));
        }

        [Test]
        public void ReplayProducesExactlyTheSameClassificationAndSummary()
        {
            StrokeGeometryData geometry = CreateGeometry(CreateArc(100f, 0f, 90f, 12, close: false));

            GestureMatchResult first = classifier.Classify(geometry);
            GestureMatchResult replay = classifier.Classify(geometry);

            Assert.That(replay.RuleId, Is.EqualTo(first.RuleId));
            Assert.That(replay.GestureType, Is.EqualTo(first.GestureType));
            Assert.That(replay.Confidence, Is.EqualTo(first.Confidence));
            Assert.That(replay.LengthReferencePixels, Is.EqualTo(first.LengthReferencePixels));
            Assert.That(
                replay.AverageSpeedReferencePixelsPerSecond,
                Is.EqualTo(first.AverageSpeedReferencePixelsPerSecond));
            Assert.That(replay.DirectionAngleDegrees, Is.EqualTo(first.DirectionAngleDegrees));
            Assert.That(replay.NormalizedCurvature, Is.EqualTo(first.NormalizedCurvature));
            Assert.That(replay.ClosureRatio, Is.EqualTo(first.ClosureRatio));
            Assert.That(replay.InitialHoldSeconds, Is.EqualTo(first.InitialHoldSeconds));
        }

        [Test]
        public void UnknownConfiguredTypeAndDuplicatePureRuleIdsFailExplicitly()
        {
            string invalidJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
                root["strokeRules"][0]["gestureType"] = "Spiral");
            var invalidConfig = new GameplayConfigService();
            invalidConfig.Load(invalidJson, "test:unknown-gesture-type");

            ArgumentException unknown = Assert.Throws<ArgumentException>(() =>
                GestureRuleSetFactory.FromConfig(invalidConfig));
            Assert.That(unknown.Message, Does.Contain("stroke_any"));
            Assert.That(unknown.Message, Does.Contain("Spiral"));

            var duplicate = new GestureRule(
                "duplicate",
                GestureType.Any,
                0f,
                0f,
                0f,
                0f,
                0f,
                0d,
                0f,
                0f);
            Assert.Throws<ArgumentException>(() =>
                new GestureClassifier(new[] { duplicate, duplicate }));
        }

        private static GestureRule FindRule(IReadOnlyList<GestureRule> rules, string ruleId)
        {
            for (int index = 0; index < rules.Count; index++)
            {
                if (string.Equals(rules[index].RuleId, ruleId, StringComparison.Ordinal))
                {
                    return rules[index];
                }
            }

            Assert.Fail($"Missing gesture rule '{ruleId}'.");
            return null;
        }

        private static StrokeGeometryData CreateGeometry(Vector2[] points, double[] timestamps = null)
        {
            if (timestamps == null)
            {
                timestamps = new double[points.Length];
                for (int index = 0; index < timestamps.Length; index++)
                {
                    timestamps[index] = index * 0.05d;
                }
            }

            var sampler = new StrokeSampler(new StrokeSamplingSettings(0.001f, 100000f, 256));
            sampler.Begin(77, points[0], timestamps[0]);
            for (int index = 1; index < points.Length - 1; index++)
            {
                sampler.AddPoint(points[index], timestamps[index]);
            }

            return Process(sampler.End(points[points.Length - 1], timestamps[timestamps.Length - 1]));
        }

        private static StrokeGeometryData Process(StrokeData stroke)
        {
            return StrokeGeometry.Process(stroke, new StrokeGeometrySettings(0f, 256));
        }

        private static Vector2[] CreateArc(
            float radius,
            float startDegrees,
            float endDegrees,
            int segmentCount,
            bool close)
        {
            int pointCount = segmentCount + 1;
            var points = new Vector2[pointCount];
            for (int index = 0; index <= segmentCount; index++)
            {
                float angle = Mathf.Lerp(startDegrees, endDegrees, index / (float)segmentCount) *
                              Mathf.Deg2Rad;
                points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            if (close)
            {
                points[points.Length - 1] = points[0];
            }

            return points;
        }
    }
}
