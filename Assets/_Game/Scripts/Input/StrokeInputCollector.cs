using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>表示一笔实时预览中的起点或新增有效采样点。</summary>
    public readonly struct StrokePreviewPointEvent
    {
        /// <summary>创建带非零笔迹 ID 的预览点事件。</summary>
        public StrokePreviewPointEvent(ulong strokeId, Vector2 referencePosition)
        {
            if (strokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeId));
            }

            StrokeId = strokeId;
            ReferencePosition = referencePosition;
        }

        /// <summary>获取预览所属笔迹 ID。</summary>
        public ulong StrokeId { get; }

        /// <summary>获取预览点的参考像素坐标。</summary>
        public Vector2 ReferencePosition { get; }
    }

    /// <summary>表示一笔因输入生命周期中断而被丢弃。</summary>
    public readonly struct StrokeCanceledEvent
    {
        /// <summary>创建带明确取消原因的笔迹取消事件。</summary>
        public StrokeCanceledEvent(
            ulong strokeId,
            double timestamp,
            PointerCancelReason reason)
        {
            if (reason == PointerCancelReason.None)
            {
                throw new ArgumentException("A canceled stroke requires a cancellation reason.", nameof(reason));
            }

            StrokeId = strokeId;
            Timestamp = timestamp;
            Reason = reason;
        }

        /// <summary>获取被取消的笔迹 ID。</summary>
        public ulong StrokeId { get; }

        /// <summary>获取取消时间戳。</summary>
        public double Timestamp { get; }

        /// <summary>获取取消原因。</summary>
        public PointerCancelReason Reason { get; }
    }

    /// <summary>把统一指针事件桥接为采样器生命周期、实时预览和不可变完成笔迹。</summary>
    public sealed class StrokeInputCollector : IDisposable
    {
        private readonly IPointerInput pointerInput;
        private readonly StrokeSampler sampler;
        private ulong nextStrokeId;
        private int activePointerId;
        private PointerSource activeSource;
        private int previewPointCount;
        private bool awaitingPointerTerminal;
        private bool disposed;

        /// <summary>创建采集器并订阅统一指针输入。</summary>
        public StrokeInputCollector(
            IPointerInput pointerInput,
            StrokeSamplingSettings settings,
            ulong initialStrokeId = 0)
        {
            this.pointerInput = pointerInput ?? throw new ArgumentNullException(nameof(pointerInput));
            sampler = new StrokeSampler(settings);
            nextStrokeId = initialStrokeId;
            pointerInput.PointerChanged += OnPointerChanged;
        }

        /// <summary>一笔成功完成时发布冻结数据。</summary>
        public event Action<StrokeData> StrokeCompleted;

        /// <summary>一笔被生命周期取消时发布取消事实。</summary>
        public event Action<StrokeCanceledEvent> StrokeCanceled;

        /// <summary>合法起笔时立即发布第一个预览点。</summary>
        public event Action<StrokePreviewPointEvent> StrokeStarted;

        /// <summary>采样器接受新点或补齐裁剪终点时发布预览点。</summary>
        public event Action<StrokePreviewPointEvent> StrokePointAdded;

        /// <summary>获取当前是否正在采样。</summary>
        public bool IsCollecting => sampler.IsSampling;

        /// <summary>幂等解除输入订阅并丢弃尚未完成的笔迹。</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            pointerInput.PointerChanged -= OnPointerChanged;
            sampler.Cancel();
            previewPointCount = 0;
            awaitingPointerTerminal = false;
            disposed = true;
        }

        /// <summary>按统一指针阶段分派采样生命周期操作。</summary>
        private void OnPointerChanged(PointerInputEvent pointerEvent)
        {
            if (disposed)
            {
                return;
            }

            switch (pointerEvent.Phase)
            {
                case PointerPhase.Began:
                    Begin(pointerEvent);
                    break;
                case PointerPhase.Moved:
                    Move(pointerEvent);
                    break;
                case PointerPhase.Ended:
                    End(pointerEvent);
                    break;
                case PointerPhase.Canceled:
                    Cancel(pointerEvent);
                    break;
            }
        }

        /// <summary>为新的活动物理指针分配单调笔迹 ID 并启动采样与预览。</summary>
        private void Begin(PointerInputEvent pointerEvent)
        {
            if (sampler.IsSampling || awaitingPointerTerminal)
            {
                return;
            }

            if (nextStrokeId == ulong.MaxValue)
            {
                throw new InvalidOperationException("Stroke ID space was exhausted.");
            }

            activePointerId = pointerEvent.PointerId;
            activeSource = pointerEvent.Source;
            nextStrokeId++;
            sampler.Begin(nextStrokeId, pointerEvent.ReferencePosition, pointerEvent.Timestamp);
            previewPointCount = 1;
            StrokeStarted?.Invoke(new StrokePreviewPointEvent(
                nextStrokeId,
                pointerEvent.ReferencePosition));
        }

        /// <summary>把当前物理指针移动交给采样器，并同步接受点或自动完成结果。</summary>
        private void Move(PointerInputEvent pointerEvent)
        {
            if (!MatchesActivePointer(pointerEvent) || !sampler.IsSampling)
            {
                return;
            }

            StrokeSampleResult result = sampler.AddPoint(
                pointerEvent.ReferencePosition,
                pointerEvent.Timestamp);
            if (result == StrokeSampleResult.Accepted)
            {
                PublishPreviewPoint(pointerEvent.ReferencePosition);
            }
            else if (result == StrokeSampleResult.CompletedMaximumLength ||
                result == StrokeSampleResult.CompletedMaximumPointCount)
            {
                // 达到长度或点数上限后笔迹已经冻结，但仍等待原物理指针终止，禁止接管新笔。
                awaitingPointerTerminal = true;
                PublishMissingPreviewPoints(sampler.CompletedStroke);
                StrokeCompleted?.Invoke(sampler.CompletedStroke);
            }
        }

        /// <summary>在物理指针抬起时完成仍在采样的笔迹，并释放接管门。</summary>
        private void End(PointerInputEvent pointerEvent)
        {
            if (!MatchesActivePointer(pointerEvent))
            {
                return;
            }

            if (sampler.IsSampling)
            {
                StrokeData stroke = sampler.End(
                    pointerEvent.ReferencePosition,
                    pointerEvent.Timestamp);
                PublishMissingPreviewPoints(stroke);
                StrokeCompleted?.Invoke(stroke);
            }

            previewPointCount = 0;
            awaitingPointerTerminal = false;
        }

        /// <summary>取消当前笔迹且不生成 StrokeData，再释放终止等待门。</summary>
        private void Cancel(PointerInputEvent pointerEvent)
        {
            if (!MatchesActivePointer(pointerEvent))
            {
                return;
            }

            if (sampler.IsSampling)
            {
                ulong canceledStrokeId = nextStrokeId;
                sampler.Cancel();
                previewPointCount = 0;
                StrokeCanceled?.Invoke(new StrokeCanceledEvent(
                    canceledStrokeId,
                    pointerEvent.Timestamp,
                    pointerEvent.CancelReason));
            }

            awaitingPointerTerminal = false;
        }

        /// <summary>补发自动裁剪产生、但原始指针事件中不存在的最终预览点。</summary>
        private void PublishMissingPreviewPoints(StrokeData stroke)
        {
            while (previewPointCount < stroke.PointCount)
            {
                PublishPreviewPoint(stroke.Points[previewPointCount]);
            }
        }

        /// <summary>递增预览计数并发布一个新增参考像素点。</summary>
        private void PublishPreviewPoint(Vector2 referencePosition)
        {
            previewPointCount += 1;
            StrokePointAdded?.Invoke(new StrokePreviewPointEvent(
                nextStrokeId,
                referencePosition));
        }

        /// <summary>判断事件是否属于当前采样或等待终止的物理指针。</summary>
        private bool MatchesActivePointer(PointerInputEvent pointerEvent)
        {
            return (sampler.IsSampling || awaitingPointerTerminal) &&
                   pointerEvent.PointerId == activePointerId &&
                   pointerEvent.Source == activeSource;
        }
    }
}
