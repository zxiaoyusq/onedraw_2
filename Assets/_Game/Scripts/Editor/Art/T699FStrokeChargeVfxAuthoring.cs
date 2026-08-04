using System;
using System.Linq;
using OneStrokeDemon.Config;
using OneStrokeDemon.Editor.AssetRegistry;
using OneStrokeDemon.Presentation;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Editor.Art
{
    /// <summary>通过Unity序列化API创建并绑定独立粒子蓄力Prefab，禁止手工维护YAML。</summary>
    public static class T699FStrokeChargeVfxAuthoring
    {
        public const string AssetKey = "vfx_stroke_charge";
        public const string PrefabPath = "Assets/_Game/Art/VFX/vfx_stroke_charge.prefab";
        public const string MenuPath = "One Stroke Demon/T699F/Create Stroke Charge Particle Prefab";
        private const string CompatibilitySpritePath =
            "Assets/_Game/Art/VFX/Sprites/vfx_slash_arc.png";

        [MenuItem(MenuPath)]
        /// <summary>幂等重建雷核环、八向电弧和三组粒子，并把新资源键绑定到Canonical Registry。</summary>
        public static void CreateOrRepairPrefab()
        {
            GameObject root = null;
            try
            {
                root = new GameObject(AssetKey);
                root.AddComponent<VfxPoolItem>();
                var view = root.AddComponent<StrokeChargeVfxView>();
                CreateCompatibilitySprite(root.transform);

                var ringsRoot = new GameObject("Rings");
                ringsRoot.transform.SetParent(root.transform, false);
                var rings = new LineRenderer[StrokeChargeVfxView.RingRendererCount];
                for (int index = 0; index < rings.Length; index++)
                {
                    rings[index] = CreateLineRenderer(
                        ringsRoot.transform,
                        $"Ring {index + 1:00}");
                }

                var radialsRoot = new GameObject("Radial Arcs");
                radialsRoot.transform.SetParent(root.transform, false);
                var radials = new LineRenderer[StrokeChargeVfxView.RadialRendererCount];
                for (int index = 0; index < radials.Length; index++)
                {
                    radials[index] = CreateLineRenderer(
                        radialsRoot.transform,
                        $"Radial Arc {index + 1:00}");
                }

                var particlesRoot = new GameObject("Particles");
                particlesRoot.transform.SetParent(root.transform, false);
                var particles = new[]
                {
                    CreateParticleSystem(particlesRoot.transform, "Core Pulses", 64),
                    CreateParticleSystem(particlesRoot.transform, "Converging Sparks", 96),
                    CreateParticleSystem(particlesRoot.transform, "Radial Sparks", 128),
                };

                view.ConfigureForAuthoring(rings, radials, particles);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PrefabPath,
                    out bool success);
                if (!success || prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Unity failed to save charge VFX prefab at '{PrefabPath}'.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                BindRegistry(prefab);
                Selection.activeObject = prefab;
                Debug.Log(
                    $"T699F_STROKE_CHARGE_PREFAB_PASS path={PrefabPath} " +
                    $"rings={rings.Length} radials={radials.Length} particles={particles.Length}");
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static LineRenderer CreateLineRenderer(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            LineRenderer renderer = child.AddComponent<LineRenderer>();
            renderer.enabled = false;
            renderer.positionCount = 0;
            renderer.useWorldSpace = false;
            renderer.loop = false;
            return renderer;
        }

        // 粒子Prefab只固定组件拓扑与技术上限，颜色、尺寸、速度、寿命和排序由运行时配置覆盖。
        private static ParticleSystem CreateParticleSystem(
            Transform parent,
            string name,
            int maximumParticleCount)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            ParticleSystem system = child.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = maximumParticleCount;
            main.startLifetime = 0.3f;
            main.startSpeed = 0f;
            main.startSize = 0.1f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1f;
            shape.radiusThickness = 0.2f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var alpha = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 1f),
                new GradientAlphaKey(1f, 0f),
            };
            var color = new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f),
            };
            var gradient = new Gradient();
            gradient.SetKeys(color, alpha);
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.EaseInOut(0f, 0.35f, 1f, 0f));

            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingLayerName = "VFX";
            Material particleMaterial = AssetDatabase.GetBuiltinExtraResource<Material>(
                "Default-ParticleSystem.mat");
            if (particleMaterial != null)
            {
                renderer.sharedMaterial = particleMaterial;
            }

            system.Stop(withChildren: false, ParticleSystemStopBehavior.StopEmittingAndClear);
            return system;
        }

        private static void CreateCompatibilitySprite(Transform parent)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(CompatibilitySpritePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Missing compatibility sprite at '{CompatibilitySpritePath}'.");
            }

            var child = new GameObject("Compatibility Sprite");
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "VFX";
            renderer.enabled = false;
        }

        private static void BindRegistry(GameObject prefab)
        {
            GameplayConfigService config = AssetRegistryEditorValidator.LoadCanonicalConfig();
            AssetManifestConfig expected = config.GetAsset(AssetKey);
            if (!string.Equals(expected.AssetType, "Prefab", StringComparison.Ordinal) ||
                !string.Equals(expected.AddressOrPath, PrefabPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{AssetKey} must be configured as Prefab at '{PrefabPath}'.");
            }

            AssetRegistryAuthoring.CreateOrRepairCanonicalRegistry();
            AssetRegistrySO registry = AssetDatabase.LoadAssetAtPath<AssetRegistrySO>(
                AssetRegistryPaths.CanonicalRegistry);
            if (registry == null)
            {
                throw new InvalidOperationException("Canonical AssetRegistry is missing.");
            }

            AssetRegistryEntry[] entries = registry.Entries
                .Select(entry => string.Equals(entry.AssetKey, AssetKey, StringComparison.Ordinal)
                    ? new AssetRegistryEntry(entry.AssetKey, prefab)
                    : new AssetRegistryEntry(entry.AssetKey, entry.Asset))
                .ToArray();
            if (entries.Count(entry =>
                    string.Equals(entry.AssetKey, AssetKey, StringComparison.Ordinal)) != 1)
            {
                throw new InvalidOperationException(
                    $"AssetRegistry must contain exactly one '{AssetKey}' entry.");
            }

            registry.ReplaceEntriesForEditor(entries);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetRegistryEditorValidator.ValidateCanonical();
        }
    }
}
