using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T330
{
    [Category("GestureClassifier")]
    public sealed class GestureClassificationPipelinePlayModeTests : InputTestFixture
    {
        private GameObject adapterObject;

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

            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator MousePathUsesRuntimeStrokeRulesForSamplingGeometryAndClassification()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            IConfigProvider config = GameplayConfigRuntime.Current;
            StrokeRuleConfig anyRule = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny);
            var classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystemPointerAdapter adapter = CreateAdapter();
            using var collector = new StrokeInputCollector(
                adapter,
                StrokeSamplingSettingsFactory.FromConfig(anyRule));
            var results = new List<GestureMatchResult>();
            collector.StrokeCompleted += stroke => results.Add(classifier.Classify(
                StrokeGeometry.Process(
                    stroke,
                    StrokeGeometrySettingsFactory.FromConfig(anyRule))));

            float y = Screen.height * 0.5f;
            var begin = new Vector2(Screen.width * 0.2f, y);
            var move = new Vector2(Screen.width * 0.225f, y);
            var end = new Vector2(Screen.width * 0.25f, y);
            Set(mouse.position, begin, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, move, queueEventOnly: true);
            yield return null;
            Set(mouse.position, end, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(results.Count, Is.EqualTo(1));
            GestureMatchResult result = results[0];
            Assert.That(result.StrokeId, Is.EqualTo(1));
            Assert.That(result.RuleId, Is.EqualTo(ConfigIds.StrokeRules.StrokeHorizontal));
            Assert.That(result.GestureType, Is.EqualTo(GestureType.Horizontal));
            Assert.That(result.LengthReferencePixels, Is.InRange(90f, 100f));
            Assert.That(result.DirectionAngleDegrees, Is.EqualTo(0f).Within(0.001f));
            Assert.That(result.NormalizedCurvature, Is.Zero.Within(0.001f));
            Assert.That(result.Confidence, Is.InRange(0.5f, 1f));
            Assert.That(adapter.IsPointerActive, Is.False);
        }

        private InputSystemPointerAdapter CreateAdapter()
        {
            adapterObject = new GameObject("T330 Pointer Adapter");
            var adapter = adapterObject.AddComponent<InputSystemPointerAdapter>();
            adapter.Initialize(
                new ReferencePixelConverter(new Vector2(1920f, 1080f)),
                new FixedSafeAreaProvider(new Rect(0f, 0f, Screen.width, Screen.height)),
                new NeverBlocked());
            return adapter;
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
