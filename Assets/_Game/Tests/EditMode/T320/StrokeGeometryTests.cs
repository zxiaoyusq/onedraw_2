using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Tests.EditMode.T230;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T320
{
    [Category("StrokeGeometry")]
    public sealed class StrokeGeometryTests
    {
        [Test]
        public void RdpSimplifiesACollinearStrokeAndPreservesItsEndpoints()
        {
            Vector2[] points =
            {
                new Vector2(0f, 0f),
                new Vector2(2f, 0f),
                new Vector2(4f, 0f),
                new Vector2(6f, 0f)
            };

            Vector2[] simplified = StrokeGeometry.SimplifyRdp(points, 0f);

            Assert.That(simplified.Length, Is.EqualTo(2));
            AssertVector(simplified[0], points[0]);
            AssertVector(simplified[1], points[points.Length - 1]);
        }

        [Test]
        public void RdpUsesAnInclusiveToleranceAndKeepsAnOutsideCorner()
        {
            Vector2[] points =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(2f, 0f)
            };

            Vector2[] onBoundary = StrokeGeometry.SimplifyRdp(points, 1f);
            Vector2[] outsideBoundary = StrokeGeometry.SimplifyRdp(points, 0.99f);

            Assert.That(onBoundary.Length, Is.EqualTo(2));
            Assert.That(outsideBoundary.Length, Is.EqualTo(3));
            AssertVector(outsideBoundary[1], points[1]);
        }

        [Test]
        public void ResamplePlacesPointsAtUniformPathDistancesAcrossACorner()
        {
            Vector2[] points =
            {
                new Vector2(0f, 0f),
                new Vector2(6f, 0f),
                new Vector2(6f, 4f)
            };

            Vector2[] resampled = StrokeGeometry.Resample(points, 6);

            Assert.That(resampled.Length, Is.EqualTo(6));
            AssertVector(resampled[0], new Vector2(0f, 0f));
            AssertVector(resampled[1], new Vector2(2f, 0f));
            AssertVector(resampled[2], new Vector2(4f, 0f));
            AssertVector(resampled[3], new Vector2(6f, 0f));
            AssertVector(resampled[4], new Vector2(6f, 2f));
            AssertVector(resampled[5], new Vector2(6f, 4f));
        }

        [Test]
        public void ProcessSimplifiesThenCapsPointsWithArcLengthResampling()
        {
            StrokeData source = CreateStroke(
                21,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(2f, 0f),
                new Vector2(3f, 1f),
                new Vector2(4f, 0f),
                new Vector2(5f, 1f),
                new Vector2(6f, 0f));

            StrokeGeometryData processed = StrokeGeometry.Process(
                source,
                new StrokeGeometrySettings(0f, 4));

            Assert.That(processed.Source, Is.SameAs(source));
            Assert.That(processed.StrokeId, Is.EqualTo(21));
            Assert.That(processed.SourcePointCount, Is.EqualTo(7));
            Assert.That(processed.PointCount, Is.EqualTo(4));
            AssertVector(processed.Points[0], source.Points[0]);
            AssertVector(processed.Points[3], source.Points[6]);
            Assert.That(
                processed.LengthReferencePixels,
                Is.EqualTo(StrokeGeometry.CalculateLength(processed.Points)).Within(0.0001f));
            Assert.That(
                processed.AreaReferencePixelsSquared,
                Is.EqualTo(StrokeGeometry.CalculateArea(processed.Points)).Within(0.0001f));
        }

        [Test]
        public void RectangleMetricsUseTheSameClosedPointSet()
        {
            Vector2[] counterClockwiseRectangle =
            {
                new Vector2(0f, 0f),
                new Vector2(4f, 0f),
                new Vector2(4f, 3f),
                new Vector2(0f, 3f),
                new Vector2(0f, 0f)
            };
            Vector2[] clockwiseRectangle =
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 3f),
                new Vector2(4f, 3f),
                new Vector2(4f, 0f),
                new Vector2(0f, 0f)
            };

            Rect bounds = StrokeGeometry.CalculateBounds(counterClockwiseRectangle);

            Assert.That(
                StrokeGeometry.CalculateLength(counterClockwiseRectangle),
                Is.EqualTo(14f).Within(0.0001f));
            Assert.That(bounds.xMin, Is.EqualTo(0f));
            Assert.That(bounds.yMin, Is.EqualTo(0f));
            Assert.That(bounds.width, Is.EqualTo(4f));
            Assert.That(bounds.height, Is.EqualTo(3f));
            Assert.That(
                StrokeGeometry.CalculateSignedArea(counterClockwiseRectangle),
                Is.EqualTo(12f).Within(0.0001f));
            Assert.That(
                StrokeGeometry.CalculateSignedArea(clockwiseRectangle),
                Is.EqualTo(-12f).Within(0.0001f));
            Assert.That(StrokeGeometry.CalculateArea(clockwiseRectangle), Is.EqualTo(12f));
            Assert.That(StrokeGeometry.CalculateClosureDistance(counterClockwiseRectangle), Is.Zero);
            Assert.That(StrokeGeometry.CalculateClosureRatio(counterClockwiseRectangle), Is.Zero);
        }

        [Test]
        public void CurvatureSeparatesDirectionFromTotalTurning()
        {
            Vector2[] leftTurn =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f)
            };
            Vector2[] rightTurn =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, -1f)
            };
            Vector2[] sTurnWithDuplicate =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(2f, 1f)
            };

            Assert.That(
                StrokeGeometry.CalculateSignedCurvatureRadians(leftTurn),
                Is.EqualTo(Mathf.PI * 0.5f).Within(0.0001f));
            Assert.That(
                StrokeGeometry.CalculateSignedCurvatureRadians(rightTurn),
                Is.EqualTo(-Mathf.PI * 0.5f).Within(0.0001f));
            Assert.That(
                StrokeGeometry.CalculateSignedCurvatureRadians(sTurnWithDuplicate),
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(
                StrokeGeometry.CalculateTotalCurvatureRadians(sTurnWithDuplicate),
                Is.EqualTo(Mathf.PI).Within(0.0001f));
            Assert.That(
                StrokeGeometry.CalculateNormalizedCurvature(sTurnWithDuplicate),
                Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void QuarterArcCurvatureIsStableAcrossReferencePixelScale()
        {
            Vector2[] smallArc = CreateQuarterArc(10f, 8);
            Vector2[] largeArc = CreateQuarterArc(100f, 8);

            float smallCurvature = StrokeGeometry.CalculateNormalizedCurvature(smallArc);
            float largeCurvature = StrokeGeometry.CalculateNormalizedCurvature(largeArc);

            Assert.That(smallCurvature, Is.EqualTo(7f / 16f).Within(0.0001f));
            Assert.That(largeCurvature, Is.EqualTo(smallCurvature).Within(0.0001f));
            Assert.That(
                StrokeGeometry.CalculateSignedCurvatureRadians(smallArc),
                Is.GreaterThan(0f));
        }

        [Test]
        public void CircleMetricsRemainStableAndReplayableAfterProcessing()
        {
            const int segmentCount = 32;
            var points = new Vector2[segmentCount + 1];
            for (int index = 0; index <= segmentCount; index++)
            {
                float angle = Mathf.PI * 2f * index / segmentCount;
                points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            StrokeData source = CreateStroke(31, points);
            var settings = new StrokeGeometrySettings(0.01f, 24);
            StrokeGeometryData first = StrokeGeometry.Process(source, settings);
            StrokeGeometryData replay = StrokeGeometry.Process(source, settings);

            Assert.That(first.PointCount, Is.LessThanOrEqualTo(24));
            Assert.That(first.AreaReferencePixelsSquared, Is.EqualTo(Mathf.PI).Within(0.15f));
            Assert.That(first.ClosureDistanceReferencePixels, Is.LessThan(0.25f));
            Assert.That(first.ClosureRatio, Is.LessThan(0.05f));
            Assert.That(first.NormalizedCurvature, Is.GreaterThan(1.5f));
            Assert.That(replay.PointCount, Is.EqualTo(first.PointCount));
            for (int index = 0; index < first.PointCount; index++)
            {
                AssertVector(replay.Points[index], first.Points[index]);
            }

            Assert.That(replay.LengthReferencePixels, Is.EqualTo(first.LengthReferencePixels));
            Assert.That(replay.TotalCurvatureRadians, Is.EqualTo(first.TotalCurvatureRadians));
        }

        [Test]
        public void EmptyDuplicateAndSinglePointInputsHaveDefinedDegenerateResults()
        {
            Vector2[] empty = Array.Empty<Vector2>();
            Vector2[] repeated =
            {
                new Vector2(5f, 7f),
                new Vector2(5f, 7f),
                new Vector2(5f, 7f)
            };

            Assert.That(StrokeGeometry.SimplifyRdp(empty, 1f), Is.Empty);
            Assert.That(StrokeGeometry.Resample(empty, 8), Is.Empty);
            Assert.That(StrokeGeometry.CalculateLength(empty), Is.Zero);
            Assert.That(StrokeGeometry.CalculateBounds(empty), Is.EqualTo(Rect.zero));
            Assert.That(StrokeGeometry.CalculateArea(empty), Is.Zero);
            Assert.That(StrokeGeometry.CalculateClosureRatio(empty), Is.Zero);
            Assert.That(StrokeGeometry.CalculateNormalizedCurvature(empty), Is.Zero);

            Vector2[] simplified = StrokeGeometry.SimplifyRdp(repeated, 1f);
            Vector2[] resampled = StrokeGeometry.Resample(repeated, 8);
            Assert.That(simplified.Length, Is.EqualTo(1));
            Assert.That(resampled.Length, Is.EqualTo(1));
            AssertVector(simplified[0], repeated[0]);
            AssertVector(resampled[0], repeated[0]);
            Rect singleBounds = StrokeGeometry.CalculateBounds(simplified);
            Assert.That(singleBounds.position, Is.EqualTo(repeated[0]));
            Assert.That(singleBounds.size, Is.EqualTo(Vector2.zero));

            StrokeGeometryData singlePointStroke = StrokeGeometry.Process(
                CreateStroke(9, repeated[0]),
                new StrokeGeometrySettings(1f, 8));
            Assert.That(singlePointStroke.PointCount, Is.EqualTo(1));
            Assert.That(singlePointStroke.LengthReferencePixels, Is.Zero);
            Assert.That(singlePointStroke.AreaReferencePixelsSquared, Is.Zero);
            Assert.That(singlePointStroke.ClosureRatio, Is.Zero);
            Assert.That(singlePointStroke.NormalizedCurvature, Is.Zero);
        }

        [Test]
        public void ProcessedPointsAreImmutableAndPreserveSourceMetadata()
        {
            StrokeData source = CreateStroke(
                42,
                new Vector2(-1f, -2f),
                new Vector2(0f, 2f),
                new Vector2(3f, 4f));
            StrokeGeometryData processed = StrokeGeometry.Process(
                source,
                new StrokeGeometrySettings(0f, 8));
            var mutableView = processed.Points as IList<Vector2>;

            Assert.That(mutableView, Is.Not.Null);
            Assert.That(mutableView.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutableView[0] = Vector2.zero);
            Assert.That(processed.StrokeId, Is.EqualTo(42));
            Assert.That(processed.StartedAt, Is.EqualTo(source.StartedAt));
            Assert.That(processed.EndedAt, Is.EqualTo(source.EndedAt));
            Assert.That(processed.CompletionReason, Is.EqualTo(source.CompletionReason));
        }

        [Test]
        public void GeometrySettingsAreMappedFromTheSelectedStrokeRule()
        {
            var config = new GameplayConfigService();
            config.Load(RuntimeConfigTestFixture.LoadJson(), RuntimeConfigTestFixture.Source);
            StrokeRuleConfig circleRule = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeCircle);

            StrokeGeometrySettings settings = StrokeGeometrySettingsFactory.FromConfig(circleRule);

            Assert.That(
                settings.RdpEpsilonReferencePixels,
                Is.EqualTo((float)circleRule.RdpEpsilonRefPx));
            Assert.That(
                settings.MaximumProcessedPointCount,
                Is.EqualTo((int)circleRule.MaxPointCount));
            Assert.That(settings.RdpEpsilonReferencePixels, Is.EqualTo(5f));
            Assert.That(settings.MaximumProcessedPointCount, Is.EqualTo(80));
        }

        [Test]
        public void InvalidToleranceCountsAndCoordinatesFailExplicitly()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StrokeGeometrySettings(float.NaN, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StrokeGeometrySettings(1f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StrokeGeometry.SimplifyRdp(Array.Empty<Vector2>(), -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StrokeGeometry.Resample(Array.Empty<Vector2>(), 1));
            Assert.Throws<ArgumentException>(() =>
                StrokeGeometry.CalculateLength(new[] { new Vector2(float.PositiveInfinity, 0f) }));
        }

        private static StrokeData CreateStroke(ulong strokeId, params Vector2[] points)
        {
            Assert.That(points, Is.Not.Empty);
            var sampler = new StrokeSampler(new StrokeSamplingSettings(0.001f, 100000f, 256));
            sampler.Begin(strokeId, points[0], 1d);
            for (int index = 1; index < points.Length - 1; index++)
            {
                sampler.AddPoint(points[index], index + 1d);
            }

            return sampler.End(points[points.Length - 1], points.Length + 1d);
        }

        private static Vector2[] CreateQuarterArc(float radius, int segmentCount)
        {
            var points = new Vector2[segmentCount + 1];
            for (int index = 0; index <= segmentCount; index++)
            {
                float angle = Mathf.PI * 0.5f * index / segmentCount;
                points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            return points;
        }

        private static void AssertVector(Vector2 actual, Vector2 expected)
        {
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(0.0001f));
        }
    }
}
