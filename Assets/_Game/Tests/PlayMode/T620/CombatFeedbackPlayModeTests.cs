using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode.T620
{
    [Category("T620")]
    public sealed class CombatFeedbackPlayModeTests
    {
        private readonly List<GameObject> targets = new List<GameObject>();
        private GameObject root;
        private Camera feedbackCamera;
        private CombatFeedbackRuntime runtime;

        [SetUp]
        public void SetUp()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            runtime?.Dispose();
            runtime = null;
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            targets.Clear();
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator FiveFeedbackEventsRemainPerceptuallyDistinctAndPoolCleanly()
        {
            LogAssert.Expect(LogType.Log, new Regex("CONFIG_RUNTIME_READY.*schema=5.*records=745"));
            LogAssert.Expect(LogType.Log, new Regex("ASSET_REGISTRY_READY.*entries=76"));
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            root = new GameObject("T620 Feedback Gallery");
            root.layer = LayerMask.NameToLayer("Ignore Raycast");
            Assert.That(root.layer, Is.GreaterThanOrEqualTo(0));
            feedbackCamera = CreateCamera(root.transform);
            var battleTime = new BattleTimeSource();
            runtime = CombatFeedbackRuntime.Create(
                GameplayConfigRuntime.Current,
                AssetRegistryRuntime.Current,
                battleTime,
                feedbackCamera,
                root.transform);
            var vibration = new RecordingVibration();
            var service = new CombatFeedbackService(runtime.Settings, runtime, vibration);
            Color[] baseColors =
            {
                new Color32(60, 110, 190, 255),
                new Color32(120, 70, 175, 255),
                new Color32(174, 96, 45, 255),
                new Color32(42, 145, 150, 255),
                new Color32(176, 55, 65, 255),
            };
            CombatFeedbackType[] types =
            {
                CombatFeedbackType.EnemyHit,
                CombatFeedbackType.WeakpointHit,
                CombatFeedbackType.ArmorBreak,
                CombatFeedbackType.ProjectileReflect,
                CombatFeedbackType.PlayerHit,
            };

            for (int index = 0; index < types.Length; index += 1)
            {
                GameObject target = CreateTarget(index, baseColors[index]);
                targets.Add(target);
                int targetId = 62001 + index;
                SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
                runtime.RegisterTarget(targetId, target.transform, renderer);
                CreateFeedbackLabel(index, runtime.Settings.Get(types[index]).FeedbackId);
                service.Publish(new CombatFeedbackEvent(
                    types[index],
                    targetId,
                    $"T620_{types[index]}",
                    types[index] == CombatFeedbackType.ProjectileReflect ? 0L : -10L * (index + 1),
                    index + 1d));
                Assert.That(renderer.color, Is.EqualTo(Color.white), types[index].ToString());
            }

            Assert.That(runtime.EmittedCount, Is.EqualTo(5));
            Assert.That(runtime.ActiveVfxCount, Is.EqualTo(5));
            Assert.That(runtime.ActiveDamageNumberCount, Is.EqualTo(4));
            Assert.That(runtime.AudioPlayCount, Is.EqualTo(5));
            Assert.That(runtime.PoolSnapshot.ActiveCount, Is.EqualTo(9));
            Assert.That(vibration.Patterns, Has.Count.EqualTo(5));
            Assert.That(battleTime.Current.GameplayScale, Is.EqualTo(0.15d).Within(0.000001d));

            Vector3 cameraBeforeShake = feedbackCamera.transform.localPosition;
            runtime.Advance(0.02f);
            Assert.That(feedbackCamera.transform.localPosition, Is.Not.EqualTo(cameraBeforeShake));
            runtime.Advance(0.18f);

            string screenshotPath = Environment.GetEnvironmentVariable("ONEDRAW_T620_SCREENSHOT");
            if (!string.IsNullOrWhiteSpace(screenshotPath))
            {
                Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                    "Screenshot run requires a graphics device; omit -nographics.");
                Capture(feedbackCamera, screenshotPath);
                Assert.That(new FileInfo(screenshotPath).Length, Is.GreaterThan(10_000));
            }

            int vibrationCount = vibration.Patterns.Count;
            service.VibrationEnabled = false;
            service.Publish(new CombatFeedbackEvent(
                CombatFeedbackType.EnemyHit,
                62001,
                "T620_disabled_vibration",
                -1L,
                10d));
            Assert.That(runtime.EmittedCount, Is.EqualTo(6));
            Assert.That(vibration.Patterns, Has.Count.EqualTo(vibrationCount));

            runtime.Advance(4f);
            Assert.That(runtime.ActiveVfxCount, Is.Zero);
            Assert.That(runtime.ActiveDamageNumberCount, Is.Zero);
            Assert.That(runtime.PoolSnapshot.ActiveCount, Is.Zero);
            Assert.That(feedbackCamera.transform.localPosition, Is.EqualTo(cameraBeforeShake));
            for (int index = 0; index < targets.Count; index += 1)
            {
                Assert.That(targets[index].GetComponent<SpriteRenderer>().color,
                    Is.EqualTo(baseColors[index]));
            }

            runtime.Restart();
            Assert.That(runtime.PoolSnapshot.ActiveCount, Is.Zero);
            yield return null;
        }

        private GameObject CreateTarget(int index, Color color)
        {
            var target = new GameObject($"Feedback Target {index + 1}", typeof(SpriteRenderer));
            target.layer = root.layer;
            target.transform.SetParent(root.transform, false);
            target.transform.position = new Vector3(-5f + (index * 2.5f), 0f, 0f);
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetRegistryRuntime.Current.GetSprite(ConfigIds.Assets.EnemySkeletonGhost);
            float largestSpriteDimension = Mathf.Max(
                renderer.sprite.bounds.size.x,
                renderer.sprite.bounds.size.y);
            Assert.That(largestSpriteDimension, Is.GreaterThan(0f));
            target.transform.localScale = Vector3.one * (0.8f / largestSpriteDimension);
            renderer.color = color;
            renderer.sortingOrder = 1;
            return target;
        }

        private void CreateFeedbackLabel(int index, string configuredFeedbackId)
        {
            var labelObject = new GameObject($"Feedback Label {index + 1}", typeof(TextMeshPro));
            labelObject.layer = root.layer;
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.position = new Vector3(-5f + (index * 2.5f), -0.85f, 0f);
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            label.font = Resources.Load<TMP_FontAsset>(CombatFeedbackRuntime.DamageNumberFontResourcePath);
            Assert.That(label.font, Is.Not.Null);
            label.text = configuredFeedbackId.Replace("feedback_", string.Empty);
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = Color.white;
            label.sortingOrder = 200;
            label.ForceMeshUpdate(ignoreActiveState: false, forceTextReparsing: true);
            float preferredHeight = label.GetPreferredValues(label.text).y;
            Assert.That(preferredHeight, Is.GreaterThan(0f));
            GlobalConfig referenceHeight = GameplayConfigRuntime.Current.GetGlobal(
                ConfigIds.GlobalKeys.ReferenceHeight);
            Assert.That(referenceHeight.IntValue, Is.GreaterThan(0L));
            float labelWorldHeight = 18f *
                ((feedbackCamera.orthographicSize * 2f) / referenceHeight.IntValue.Value);
            labelObject.transform.localScale = Vector3.one * (labelWorldHeight / preferredHeight);
        }

        private static Camera CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("T620 Feedback Camera", typeof(Camera));
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 16, 29, 255);
            camera.orthographic = true;
            camera.orthographicSize = 4.4f;
            camera.cullingMask = 1 << parent.gameObject.layer;
            camera.enabled = false;
            return camera;
        }

        private static void Capture(Camera camera, string outputPath)
        {
            var texture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            texture.Create();
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, mipChain: false);
            image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
            image.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }

        private sealed class RecordingVibration : ICombatFeedbackVibration
        {
            internal List<FeedbackVibrationPattern> Patterns { get; } =
                new List<FeedbackVibrationPattern>();

            public void Request(FeedbackVibrationPattern pattern)
            {
                Patterns.Add(pattern);
            }
        }
    }
}
