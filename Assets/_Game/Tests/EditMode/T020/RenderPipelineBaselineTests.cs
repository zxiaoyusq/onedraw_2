using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OneStrokeDemon.Tests.EditMode
{
    public sealed class RenderPipelineBaselineTests
    {
        private const string PipelinePath = "Assets/Settings/UniversalRP.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [Test]
        public void GraphicsAndLowHighQualityProfilesUseUrp2DRenderer()
        {
            var expectedPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            Assert.That(expectedPipeline, Is.Not.Null, $"Missing URP asset at {PipelinePath}");
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(expectedPipeline));

            var serializedPipeline = new SerializedObject(expectedPipeline);
            var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            Assert.That(rendererList, Is.Not.Null);
            Assert.That(rendererList.arraySize, Is.GreaterThan(0));
            Assert.That(rendererList.GetArrayElementAtIndex(0).objectReferenceValue, Is.TypeOf<Renderer2DData>());

            int originalQuality = QualitySettings.GetQualityLevel();
            try
            {
                AssertQualityUsesPipeline("Low", expectedPipeline);
                AssertQualityUsesPipeline("High", expectedPipeline);
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, false);
            }
        }

        [Test]
        public void InputActionsDeclareMouseAndTouchPointerBindings()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.That(actions, Is.Not.Null, $"Missing Input Actions at {InputActionsPath}");

            var point = actions.FindAction("UI/Point", true);
            var click = actions.FindAction("UI/Click", true);
            Assert.That(point.bindings.Any(binding => binding.path == "<Mouse>/position"), Is.True);
            Assert.That(point.bindings.Any(binding => binding.path == "<Touchscreen>/touch*/position"), Is.True);
            Assert.That(click.bindings.Any(binding => binding.path == "<Mouse>/leftButton"), Is.True);
            Assert.That(click.bindings.Any(binding => binding.path == "<Touchscreen>/touch*/press"), Is.True);
            Assert.That(actions.controlSchemes.Any(scheme => scheme.name == "Touch"), Is.True);
        }

        [Test]
        public void TmpAndTestFrameworkAssembliesAreLoaded()
        {
            Assert.That(typeof(TMP_Text).Assembly.GetName().Name, Is.EqualTo("Unity.TextMeshPro"));
            Assert.That(typeof(TestAttribute).Assembly.GetName().Name, Is.EqualTo("nunit.framework"));
        }

        private static void AssertQualityUsesPipeline(string qualityName, RenderPipelineAsset expectedPipeline)
        {
            int qualityIndex = Array.IndexOf(QualitySettings.names, qualityName);
            Assert.That(qualityIndex, Is.GreaterThanOrEqualTo(0), $"Missing {qualityName} quality profile");
            QualitySettings.SetQualityLevel(qualityIndex, false);
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(expectedPipeline),
                $"Quality profile {qualityName} must use {PipelinePath}");
        }
    }
}
