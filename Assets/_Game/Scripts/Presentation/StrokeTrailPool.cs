using System;
using OneStrokeDemon.Combat;
using UnityEngine;

namespace OneStrokeDemon.Presentation
{
    [DisallowMultipleComponent]
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

        public void Initialize(
            StrokeTrailPoolSettings poolSettings,
            Material sharedMaterial,
            Transform referenceSpace = null)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Stroke trail pool is already initialized.");
            }

            if (sharedMaterial == null)
            {
                throw new ArgumentNullException(nameof(sharedMaterial));
            }

            settings = poolSettings;
            views = new StrokeTrailView[settings.Capacity];
            for (int index = 0; index < views.Length; index++)
            {
                var child = new GameObject("Stroke Trail");
                child.transform.SetParent(transform, false);
                var lineRenderer = child.AddComponent<LineRenderer>();
                var view = child.AddComponent<StrokeTrailView>();
                view.Initialize(
                    lineRenderer,
                    sharedMaterial,
                    referenceSpace != null ? referenceSpace : transform);
                views[index] = view;
            }

            nextActivationSequence = 1;
            IsInitialized = true;
        }

        public StrokeTrailView Show(StrokeTrailPath path, StrokeTrailStyle style)
        {
            EnsureInitialized();
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

        public bool TryAppendPreviewPoint(ulong strokeId, Vector2 point)
        {
            if (!TryGetActiveView(strokeId, out StrokeTrailView view) ||
                !view.IsPreviewing ||
                view.LineRenderer.positionCount >= settings.MaximumPointCount)
            {
                return false;
            }

            view.AppendPreviewPoint(point);
            return true;
        }

        public StrokeTrailView CompletePreview(
            StrokeTrailPath path,
            StrokeTrailStyle style)
        {
            EnsureInitialized();
            if (path.PointCount > settings.MaximumPointCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(path),
                    $"Trail has {path.PointCount} points but the configured maximum is {settings.MaximumPointCount}.");
            }

            if (TryGetActiveView(path.StrokeId, out StrokeTrailView view) &&
                view.IsPreviewing)
            {
                ulong activationSequence = view.ActivationSequence;
                view.Show(path, style, activationSequence);
                return view;
            }

            return Show(path, style);
        }

        public bool CancelPreview(ulong strokeId)
        {
            EnsureInitialized();
            if (!TryGetActiveView(strokeId, out StrokeTrailView view) ||
                !view.IsPreviewing)
            {
                return false;
            }

            view.ResetForPool();
            ActiveCount--;
            return true;
        }

        public void Advance(float unscaledDeltaSeconds)
        {
            EnsureInitialized();
            for (int index = 0; index < views.Length; index++)
            {
                StrokeTrailView view = views[index];
                if (view.IsActive && view.Advance(unscaledDeltaSeconds))
                {
                    ActiveCount--;
                }
            }
        }

        public bool TryGetActiveView(ulong strokeId, out StrokeTrailView view)
        {
            EnsureInitialized();
            for (int index = 0; index < views.Length; index++)
            {
                StrokeTrailView candidate = views[index];
                if (candidate.IsActive && candidate.StrokeId == strokeId)
                {
                    view = candidate;
                    return true;
                }
            }

            view = null;
            return false;
        }

        public void Clear()
        {
            EnsureInitialized();
            for (int index = 0; index < views.Length; index++)
            {
                views[index].ResetForPool();
            }

            ActiveCount = 0;
        }

        private void Update()
        {
            if (IsInitialized)
            {
                Advance(Time.unscaledDeltaTime);
            }
        }

        private StrokeTrailView FindInactiveView()
        {
            for (int index = 0; index < views.Length; index++)
            {
                if (!views[index].IsActive)
                {
                    return views[index];
                }
            }

            return null;
        }

        private StrokeTrailView AcquireView()
        {
            StrokeTrailView view = FindInactiveView();
            if (view == null || ActiveCount >= settings.MaximumActiveTrailCount)
            {
                view = FindOldestActiveView();
                if (view == null)
                {
                    throw new InvalidOperationException("Stroke trail pool has no reusable view.");
                }

                view.ResetForPool();
                ActiveCount--;
            }

            return view;
        }

        private ulong NextActivationSequence()
        {
            ulong activationSequence = nextActivationSequence++;
            if (nextActivationSequence == 0)
            {
                nextActivationSequence = 1;
            }

            return activationSequence;
        }

        private StrokeTrailView FindOldestActiveView()
        {
            StrokeTrailView oldest = null;
            ulong oldestSequence = ulong.MaxValue;
            for (int index = 0; index < views.Length; index++)
            {
                StrokeTrailView candidate = views[index];
                if (candidate.IsActive && candidate.ActivationSequence < oldestSequence)
                {
                    oldest = candidate;
                    oldestSequence = candidate.ActivationSequence;
                }
            }

            return oldest;
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Stroke trail pool is not initialized.");
            }
        }
    }
}
