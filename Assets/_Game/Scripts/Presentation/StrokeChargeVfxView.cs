using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    /// <summary>驱动独立蓄力Prefab中的雷核、同心环、径向电弧与粒子，不参与输入或命中判定。</summary>
    [DisallowMultipleComponent]
    public sealed class StrokeChargeVfxView : MonoBehaviour
    {
        public const int RingRendererCount = 4;
        public const int RadialRendererCount = 8;
        public const int ParticleSystemCount = 3;
        public const int RingSegmentCount = 32;

        private static readonly Color TransparentWhite = new Color(1f, 1f, 1f, 0f);

        [SerializeField] private LineRenderer[] ringRenderers = Array.Empty<LineRenderer>();
        [SerializeField] private LineRenderer[] radialRenderers = Array.Empty<LineRenderer>();
        [SerializeField] private ParticleSystem[] particleSystems = Array.Empty<ParticleSystem>();

        private Material sharedLineMaterial;
        private Transform referenceSpace;
        private ParticleSystemRenderer[] particleRenderers = Array.Empty<ParticleSystemRenderer>();
        private float previousProgress;

        public bool IsInitialized { get; private set; }

        public bool IsVisible { get; private set; }

        public float NormalizedProgress { get; private set; }

        public float RadiusReferencePixels { get; private set; }

        public IReadOnlyList<LineRenderer> RingRenderers => ringRenderers;

        public IReadOnlyList<LineRenderer> RadialRenderers => radialRenderers;

        public IReadOnlyList<ParticleSystem> ParticleSystems => particleSystems;

        /// <summary>由Unity作者工具绑定Prefab拓扑；这里只保存组件引用，不保存玩法或表现数值。</summary>
        public void ConfigureForAuthoring(
            LineRenderer[] rings,
            LineRenderer[] radials,
            ParticleSystem[] particles)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException(
                    "Charge VFX references cannot change after initialization.");
            }

            ringRenderers = CloneExact(rings, RingRendererCount, nameof(rings));
            radialRenderers = CloneExact(radials, RadialRendererCount, nameof(radials));
            particleSystems = CloneExact(particles, ParticleSystemCount, nameof(particles));
            ValidateTopology();
        }

        /// <summary>初始化Prefab内预建组件并清空作者态预览，后续更新不创建对象。</summary>
        public void Initialize(Material lineMaterial, Transform configuredReferenceSpace)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Charge VFX view is already initialized.");
            }

            sharedLineMaterial = lineMaterial != null
                ? lineMaterial
                : throw new ArgumentNullException(nameof(lineMaterial));
            referenceSpace = configuredReferenceSpace != null
                ? configuredReferenceSpace
                : throw new ArgumentNullException(nameof(configuredReferenceSpace));
            ValidateTopology();
            for (int index = 0; index < ringRenderers.Length; index++)
            {
                ConfigureLineRenderer(ringRenderers[index], capVertices: 2);
            }

            for (int index = 0; index < radialRenderers.Length; index++)
            {
                ConfigureLineRenderer(radialRenderers[index], capVertices: 1);
            }

            particleRenderers = new ParticleSystemRenderer[particleSystems.Length];
            for (int index = 0; index < particleSystems.Length; index++)
            {
                particleRenderers[index] = particleSystems[index]
                    .GetComponent<ParticleSystemRenderer>();
                if (particleRenderers[index] == null)
                {
                    throw new InvalidOperationException(
                        $"Particle system {index} has no ParticleSystemRenderer.");
                }
            }

            IsInitialized = true;
            Hide();
        }

        /// <summary>按当前配置进度刷新独立Prefab；命中半径只作为视觉尺寸输入，不反向改变规则。</summary>
        public void Show(
            Vector2 referencePosition,
            float normalizedProgress,
            float radiusReferencePixels,
            StrokeTrailStyle style,
            float referencePixelWorldScale)
        {
            EnsureInitialized();
            if (float.IsNaN(normalizedProgress) || float.IsInfinity(normalizedProgress))
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedProgress));
            }

            if (!IsFinitePositive(radiusReferencePixels))
            {
                throw new ArgumentOutOfRangeException(nameof(radiusReferencePixels));
            }

            if (!IsFinitePositive(referencePixelWorldScale))
            {
                throw new ArgumentOutOfRangeException(nameof(referencePixelWorldScale));
            }

            float progress = Mathf.Clamp01(normalizedProgress);
            if (!IsVisible || progress < previousProgress)
            {
                ResetParticles();
                previousProgress = 0f;
                for (int index = 0; index < particleSystems.Length; index++)
                {
                    particleSystems[index].Play(withChildren: false);
                }
            }

            transform.position = referenceSpace.TransformPoint(
                new Vector3(referencePosition.x, referencePosition.y, 0f));
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            float visualRadiusReferencePixels = Mathf.Max(
                radiusReferencePixels,
                style.BranchLengthReferencePixels);
            float worldBaseWidth = style.WidthReferencePixels * referencePixelWorldScale;
            float worldBranchWidth = worldBaseWidth * style.BranchWidthMultiplier;
            float coreRadius = radiusReferencePixels * referencePixelWorldScale / 3f;
            float visualRadius = visualRadiusReferencePixels * referencePixelWorldScale;
            RenderCoreAndRings(
                progress,
                coreRadius,
                visualRadius,
                worldBranchWidth * style.OuterWidthMultiplier,
                worldBranchWidth * style.BodyWidthMultiplier,
                worldBaseWidth * style.CoreWidthMultiplier,
                style);
            RenderRadials(
                progress,
                coreRadius,
                visualRadius,
                style.BranchLengthReferencePixels * referencePixelWorldScale,
                style.BranchJitterReferencePixels * referencePixelWorldScale,
                worldBaseWidth * style.CoreWidthMultiplier,
                worldBranchWidth,
                style);
            ConfigureParticles(
                progress,
                visualRadius,
                worldBranchWidth,
                style.BranchLengthReferencePixels * referencePixelWorldScale,
                style);
            EmitProgressParticles(progress);

            previousProgress = progress;
            NormalizedProgress = progress;
            RadiusReferencePixels = radiusReferencePixels;
            IsVisible = true;
        }

        /// <summary>停止并清空粒子及全部渲染器，保证池化笔迹复用时没有残影。</summary>
        public void Hide()
        {
            if (!IsInitialized)
            {
                return;
            }

            for (int index = 0; index < ringRenderers.Length; index++)
            {
                ResetRenderer(ringRenderers[index]);
            }

            for (int index = 0; index < radialRenderers.Length; index++)
            {
                ResetRenderer(radialRenderers[index]);
            }

            ResetParticles();
            previousProgress = 0f;
            NormalizedProgress = 0f;
            RadiusReferencePixels = 0f;
            IsVisible = false;
        }

        private void RenderCoreAndRings(
            float progress,
            float coreRadius,
            float visualRadius,
            float outerWidth,
            float bodyWidth,
            float coreWidth,
            StrokeTrailStyle style)
        {
            float coreProgress = Mathf.Clamp01(progress * 3f);
            float middleProgress = Mathf.Clamp01(progress * 3f - 1f);
            float outerProgress = Mathf.Clamp01(progress * 3f - 2f);
            WriteRing(ringRenderers[0], coreRadius, coreProgress, outerWidth, style.OuterColor);
            WriteRing(ringRenderers[1], coreRadius, coreProgress, coreWidth, style.CoreColor);
            WriteRing(
                ringRenderers[2],
                visualRadius * 2f / 3f,
                middleProgress,
                bodyWidth,
                style.BodyColor);
            WriteRing(ringRenderers[3], visualRadius, outerProgress, outerWidth, style.OuterColor);
        }

        private void RenderRadials(
            float progress,
            float coreRadius,
            float visualRadius,
            float branchLength,
            float branchJitter,
            float coreWidth,
            float branchWidth,
            StrokeTrailStyle style)
        {
            int visibleCount = Mathf.CeilToInt(progress * RadialRendererCount);
            float actualBranchLength = Mathf.Min(branchLength, visualRadius);
            float jitter = Mathf.Min(branchJitter, actualBranchLength / 2f);
            float endRadius = visualRadius + actualBranchLength * progress;
            for (int index = 0; index < radialRenderers.Length; index++)
            {
                LineRenderer renderer = radialRenderers[index];
                if (index >= visibleCount)
                {
                    ResetRenderer(renderer);
                    continue;
                }

                float angle = 360f * index / RadialRendererCount * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new Vector2(-direction.y, direction.x);
                float signedJitter = (index & 1) == 0 ? jitter : -jitter;
                renderer.positionCount = 3;
                renderer.startWidth = coreWidth;
                renderer.endWidth = branchWidth;
                renderer.startColor = style.CoreColor;
                renderer.endColor = style.BranchColor;
                renderer.SetPosition(0, direction * coreRadius);
                renderer.SetPosition(
                    1,
                    direction * ((coreRadius + visualRadius) / 2f) + tangent * signedJitter);
                renderer.SetPosition(2, direction * endRadius);
                renderer.enabled = true;
            }
        }

        private void WriteRing(
            LineRenderer renderer,
            float radius,
            float progress,
            float width,
            Color color)
        {
            if (progress <= 0f)
            {
                ResetRenderer(renderer);
                return;
            }

            int visibleSegments = Mathf.Max(1, Mathf.CeilToInt(progress * RingSegmentCount));
            renderer.positionCount = visibleSegments + 1;
            renderer.startWidth = width;
            renderer.endWidth = width;
            renderer.startColor = color;
            renderer.endColor = color;
            for (int index = 0; index <= visibleSegments; index++)
            {
                float angle = (90f - 360f * index / RingSegmentCount) * Mathf.Deg2Rad;
                renderer.SetPosition(
                    index,
                    new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            renderer.enabled = true;
        }

        private void ConfigureParticles(
            float progress,
            float visualRadius,
            float branchWidth,
            float branchLength,
            StrokeTrailStyle style)
        {
            for (int index = 0; index < particleSystems.Length; index++)
            {
                Color color;
                float size;
                float speed;
                if (index == 0)
                {
                    color = style.CoreColor;
                    size = Mathf.Max(branchWidth * 2.5f, visualRadius * 0.06f);
                    speed = 0f;
                }
                else
                {
                    color = index == 1 ? style.BodyColor : style.BranchColor;
                    size = index == 1
                        ? Mathf.Max(branchWidth * 1.6f, visualRadius * 0.035f)
                        : Mathf.Max(branchWidth * 1.2f, visualRadius * 0.025f);
                    speed = Mathf.Max(branchLength, visualRadius * 0.25f) /
                        Mathf.Max(style.LifetimeSeconds, 0.1f);
                    if (index == 1)
                    {
                        speed = -speed;
                    }
                }

                ParticleSystem system = particleSystems[index];
                ParticleSystem.MainModule main = system.main;
                main.startLifetime = Mathf.Max(style.LifetimeSeconds, 0.1f);
                main.startColor = color;
                main.startSize = size * Mathf.Lerp(0.6f, 1.2f, progress);
                main.startSpeed = speed;
                ParticleSystem.ShapeModule shape = system.shape;
                shape.radius = index == 0 ? visualRadius / 3f : visualRadius;
                ParticleSystemRenderer renderer = particleRenderers[index];
                renderer.sortingLayerID = style.SortingLayerId;
                renderer.sortingOrder = style.SortingOrder + 4 + index;
            }
        }

        private void EmitProgressParticles(float progress)
        {
            int previousStep = Mathf.FloorToInt(previousProgress * 20f);
            int currentStep = Mathf.FloorToInt(progress * 20f);
            int advancedSteps = Mathf.Max(0, currentStep - previousStep);
            if (advancedSteps == 0)
            {
                return;
            }

            particleSystems[0].Emit(Mathf.Max(1, advancedSteps / 2));
            particleSystems[1].Emit(advancedSteps);
            particleSystems[2].Emit(advancedSteps * 2);
        }

        private void ConfigureLineRenderer(LineRenderer renderer, int capVertices)
        {
            renderer.sharedMaterial = sharedLineMaterial;
            renderer.useWorldSpace = false;
            renderer.loop = false;
            renderer.alignment = LineAlignment.View;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.numCapVertices = capVertices;
            renderer.numCornerVertices = 1;
            renderer.generateLightingData = false;
        }

        private void ResetParticles()
        {
            for (int index = 0; index < particleSystems.Length; index++)
            {
                particleSystems[index].Stop(
                    withChildren: false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystems[index].Clear(withChildren: false);
            }
        }

        private static void ResetRenderer(LineRenderer renderer)
        {
            renderer.enabled = false;
            renderer.positionCount = 0;
            renderer.startWidth = 0f;
            renderer.endWidth = 0f;
            renderer.startColor = TransparentWhite;
            renderer.endColor = TransparentWhite;
        }

        private void ValidateTopology()
        {
            ValidateComponents(ringRenderers, RingRendererCount, nameof(ringRenderers));
            ValidateComponents(radialRenderers, RadialRendererCount, nameof(radialRenderers));
            ValidateComponents(particleSystems, ParticleSystemCount, nameof(particleSystems));
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Charge VFX view is not initialized.");
            }
        }

        private static T[] CloneExact<T>(T[] source, int count, string parameterName)
            where T : Component
        {
            ValidateComponents(source, count, parameterName);
            return (T[])source.Clone();
        }

        private static void ValidateComponents<T>(T[] components, int count, string fieldName)
            where T : Component
        {
            if (components == null || components.Length != count)
            {
                throw new InvalidOperationException(
                    $"{fieldName} must contain exactly {count} components.");
            }

            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] == null)
                {
                    throw new InvalidOperationException($"{fieldName}[{index}] is missing.");
                }
            }
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
