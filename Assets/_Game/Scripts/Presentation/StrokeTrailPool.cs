using System;
using OneStrokeDemon.Combat;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
    // 定义 StrokeTrailPool 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class StrokeTrailPool : MonoBehaviour
    {
        private StrokeTrailPoolSettings settings;
        private StrokeTrailView[] views;
        private ulong nextActivationSequence;

        public bool IsInitialized { get; private set; }

        public int Capacity => views?.Length ?? 0;

        public int ActiveCount { get; private set; }

        public int MaximumActiveTrailCount =>
            IsInitialized ? settings.MaximumActiveTrailCount : 0;

        // 处理 Initialize 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Initialize(
            StrokeTrailPoolSettings poolSettings,
            Material sharedMaterial,
            Transform referenceSpace = null,
            GameObject viewPrefab = null)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (IsInitialized)
            {
                throw new InvalidOperationException("Stroke trail pool is already initialized.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (sharedMaterial == null)
            {
                throw new ArgumentNullException(nameof(sharedMaterial));
            }

            settings = poolSettings;
            views = new StrokeTrailView[settings.Capacity];
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < views.Length; index++)
            {
                StrokeTrailView view = viewPrefab != null
                    ? InstantiatePrefabView(viewPrefab)
                    : CreateDebugFallbackView();
                view.gameObject.name = $"Stroke Trail {index + 1:00}";
                view.Initialize(
                    sharedMaterial,
                    referenceSpace != null ? referenceSpace : transform);
                views[index] = view;
            }

            nextActivationSequence = 1;
            IsInitialized = true;
        }

        // 显示 Show 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public StrokeTrailView Show(StrokeTrailPath path, StrokeTrailStyle style)
        {
            EnsureInitialized();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (path.PointCount > settings.MaximumPointCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(path),
                    $"Trail has {path.PointCount} points but the configured maximum is {settings.MaximumPointCount}.");
            }

            StrokeTrailView view = AcquireView();
            view.Show(path, style, NextActivationSequence());
            ActiveCount++;
            return view;
        }

        // 处理 BeginPreview 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public StrokeTrailView BeginPreview(
            ulong strokeId,
            Vector2 firstPoint,
            StrokeTrailStyle style)
        {
            EnsureInitialized();
            StrokeTrailView view = AcquireView();
            view.BeginPreview(strokeId, firstPoint, style, NextActivationSequence());
            ActiveCount++;
            return view;
        }

        // 尝试执行 TryAppendPreviewPoint 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool TryAppendPreviewPoint(ulong strokeId, Vector2 point)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!TryGetActiveView(strokeId, out StrokeTrailView view) ||
                !view.IsPreviewing ||
                view.LineRenderer.positionCount >= settings.MaximumPointCount)
            {
                return false;
            }

            view.AppendPreviewPoint(point);
            return true;
        }

        /// <summary>尝试把活动笔迹的停留时长映射为触点蓄力环，不参与手势或命中判定。</summary>
        public bool TryUpdateChargePreview(
            ulong strokeId,
            Vector2 referencePosition,
            float normalizedProgress,
            float radiusReferencePixels)
        {
            if (!TryGetActiveView(strokeId, out StrokeTrailView view) || !view.IsPreviewing)
            {
                return false;
            }

            view.UpdateChargePreview(
                referencePosition,
                normalizedProgress,
                radiusReferencePixels);
            return true;
        }

        // 处理 CompletePreview 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public StrokeTrailView CompletePreview(
            StrokeTrailPath path,
            StrokeTrailStyle style)
        {
            EnsureInitialized();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (path.PointCount > settings.MaximumPointCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(path),
                    $"Trail has {path.PointCount} points but the configured maximum is {settings.MaximumPointCount}.");
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (TryGetActiveView(path.StrokeId, out StrokeTrailView view) &&
                view.IsPreviewing)
            {
                ulong activationSequence = view.ActivationSequence;
                view.Show(path, style, activationSequence);
                return view;
            }

            return Show(path, style);
        }

        // 判断是否允许 CancelPreview 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool CancelPreview(ulong strokeId)
        {
            EnsureInitialized();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!TryGetActiveView(strokeId, out StrokeTrailView view) ||
                !view.IsPreviewing)
            {
                return false;
            }

            view.ResetForPool();
            ActiveCount--;
            return true;
        }

        // 处理 Advance 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Advance(float unscaledDeltaSeconds)
        {
            EnsureInitialized();
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < views.Length; index++)
            {
                StrokeTrailView view = views[index];
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (view.IsActive && view.Advance(unscaledDeltaSeconds))
                {
                    ActiveCount--;
                }
            }
        }

        // 尝试执行 TryGetActiveView 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool TryGetActiveView(ulong strokeId, out StrokeTrailView view)
        {
            EnsureInitialized();
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < views.Length; index++)
            {
                StrokeTrailView candidate = views[index];
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (candidate.IsActive && candidate.StrokeId == strokeId)
                {
                    view = candidate;
                    return true;
                }
            }

            view = null;
            return false;
        }

        // 清理 Clear 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public void Clear()
        {
            EnsureInitialized();
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < views.Length; index++)
            {
                views[index].ResetForPool();
            }

            ActiveCount = 0;
        }

        // 更新 Update 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void Update()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (IsInitialized)
            {
                Advance(Time.unscaledDeltaTime);
            }
        }

        // 处理 FindInactiveView 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private StrokeTrailView FindInactiveView()
        {
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < views.Length; index++)
            {
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (!views[index].IsActive)
                {
                    return views[index];
                }
            }

            return null;
        }

        // 处理 AcquireView 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private StrokeTrailView AcquireView()
        {
            StrokeTrailView view = FindInactiveView();
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (view == null || ActiveCount >= settings.MaximumActiveTrailCount)
            {
                view = FindOldestActiveView();
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (view == null)
                {
                    throw new InvalidOperationException("Stroke trail pool has no reusable view.");
                }

                view.ResetForPool();
                ActiveCount--;
            }

            return view;
        }

        // 处理 NextActivationSequence 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private ulong NextActivationSequence()
        {
            ulong activationSequence = nextActivationSequence++;
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (nextActivationSequence == 0)
            {
                nextActivationSequence = 1;
            }

            return activationSequence;
        }

        // 处理 FindOldestActiveView 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private StrokeTrailView FindOldestActiveView()
        {
            StrokeTrailView oldest = null;
            ulong oldestSequence = ulong.MaxValue;
            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 0; index < views.Length; index++)
            {
                StrokeTrailView candidate = views[index];
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (candidate.IsActive && candidate.ActivationSequence < oldestSequence)
                {
                    oldest = candidate;
                    oldestSequence = candidate.ActivationSequence;
                }
            }

            return oldest;
        }

        // 生产路径实例化VfxCues/AssetRegistry解析出的Prefab，并拒绝缺少视图根组件的资源。
        private StrokeTrailView InstantiatePrefabView(GameObject viewPrefab)
        {
            GameObject instance = Instantiate(viewPrefab, transform, false);
            StrokeTrailView view = instance.GetComponent<StrokeTrailView>();
            if (view != null)
            {
                return view;
            }

            DestroyImmediate(instance);
            throw new ArgumentException(
                "Stroke trail prefab must contain StrokeTrailView on its root.",
                nameof(viewPrefab));
        }

        // 仅供既有隔离测试使用；生产组合根始终传入配置Registry中的Prefab。
        private StrokeTrailView CreateDebugFallbackView()
        {
            var root = new GameObject("Stroke Trail");
            root.transform.SetParent(transform, false);
            LineRenderer outer = root.AddComponent<LineRenderer>();
            var view = root.AddComponent<StrokeTrailView>();
            LineRenderer body = CreateChildRenderer(root.transform, "Body");
            LineRenderer core = CreateChildRenderer(root.transform, "Core");
            var branches = new LineRenderer[StrokeTrailView.BranchRendererCapacity];
            var branchRoot = new GameObject("Branches");
            branchRoot.transform.SetParent(root.transform, false);
            for (int index = 0; index < branches.Length; index++)
            {
                branches[index] = CreateChildRenderer(
                    branchRoot.transform,
                    $"Branch {index + 1:00}");
            }

            view.ConfigureRenderersForAuthoring(outer, body, core, branches);
            return view;
        }

        private static LineRenderer CreateChildRenderer(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.AddComponent<LineRenderer>();
        }

        // 处理 EnsureInitialized 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void EnsureInitialized()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Stroke trail pool is not initialized.");
            }
        }
    }
}
