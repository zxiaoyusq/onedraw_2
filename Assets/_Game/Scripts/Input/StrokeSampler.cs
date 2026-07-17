using System;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>一笔采样器的生命周期状态。</summary>
    public enum StrokeSamplingState
    {
        Idle,
        Sampling,
        Completed,
        Canceled
    }

    /// <summary>一次尝试加入采样点的结果。</summary>
    public enum StrokeSampleResult
    {
        Accepted,
        IgnoredBelowMinimumDistance,
        IgnoredNotSampling,
        CompletedMaximumLength,
        CompletedMaximumPointCount
    }

    /// <summary>使用固定缓冲执行低分配点距过滤、长度裁剪和点数上限控制。</summary>
    public sealed class StrokeSampler
    {
        private readonly Vector2[] pointBuffer;
        private readonly float minimumPointDistanceSquared;
        private int pointCount;
        private ulong strokeId;
        private float totalLength;
        private double startedAt;
        private double initialHoldDuration;
        private bool hasAcceptedMovement;
        private StrokeData completedStroke;

        /// <summary>按最大点数一次性分配缓冲并缓存最小点距平方。</summary>
        public StrokeSampler(StrokeSamplingSettings settings)
        {
            Settings = settings;
            pointBuffer = new Vector2[settings.MaximumPointCount];
            minimumPointDistanceSquared =
                settings.MinimumPointDistanceReferencePixels *
                settings.MinimumPointDistanceReferencePixels;
        }

        /// <summary>获取不可变采样设置。</summary>
        public StrokeSamplingSettings Settings { get; }

        /// <summary>获取当前采样生命周期状态。</summary>
        public StrokeSamplingState State { get; private set; }

        /// <summary>获取当前是否正在接收采样点。</summary>
        public bool IsSampling => State == StrokeSamplingState.Sampling;

        /// <summary>获取最近一次完成的冻结笔迹。</summary>
        public StrokeData CompletedStroke => completedStroke;

        /// <summary>以非零 ID、首点和时间戳开始一笔，并清除上一笔临时状态。</summary>
        public void Begin(ulong newStrokeId, Vector2 referencePosition, double timestamp)
        {
            if (State == StrokeSamplingState.Sampling)
            {
                throw new InvalidOperationException("Cannot begin a new stroke while another stroke is sampling.");
            }

            if (newStrokeId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newStrokeId), "Stroke IDs must be non-zero.");
            }

            ValidatePoint(referencePosition, nameof(referencePosition));
            ValidateTimestamp(timestamp, nameof(timestamp));

            strokeId = newStrokeId;
            pointBuffer[0] = referencePosition;
            pointCount = 1;
            totalLength = 0f;
            startedAt = timestamp;
            initialHoldDuration = 0d;
            hasAcceptedMovement = false;
            completedStroke = null;
            State = StrokeSamplingState.Sampling;
        }

        /// <summary>尝试接受一个点；按点距、最大长度和缓冲容量返回确定结果。</summary>
        public StrokeSampleResult AddPoint(Vector2 referencePosition, double timestamp)
        {
            if (State != StrokeSamplingState.Sampling)
            {
                return StrokeSampleResult.IgnoredNotSampling;
            }

            ValidatePoint(referencePosition, nameof(referencePosition));
            ValidateTimestamp(timestamp, nameof(timestamp));

            // 点距始终相对最后一个已接受点计算，短抖动不会积累成有效移动。
            Vector2 lastPoint = pointBuffer[pointCount - 1];
            Vector2 segment = referencePosition - lastPoint;
            float segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared < minimumPointDistanceSquared)
            {
                return StrokeSampleResult.IgnoredBelowMinimumDistance;
            }

            // 跨越最大长度时沿当前线段精确插值到剩余长度，避免简单丢点造成误差。
            float segmentLength = Mathf.Sqrt(segmentLengthSquared);
            float remainingLength = Settings.MaximumStrokeLengthReferencePixels - totalLength;
            if (segmentLength >= remainingLength)
            {
                RecordInitialHold(timestamp);
                Vector2 cutoffPoint = lastPoint + segment * (remainingLength / segmentLength);
                Append(cutoffPoint, Settings.MaximumStrokeLengthReferencePixels);
                Complete(timestamp, StrokeCompletionReason.MaximumLength);
                return StrokeSampleResult.CompletedMaximumLength;
            }

            RecordInitialHold(timestamp);
            Append(referencePosition, totalLength + segmentLength);
            if (pointCount == pointBuffer.Length)
            {
                Complete(timestamp, StrokeCompletionReason.MaximumPointCount);
                return StrokeSampleResult.CompletedMaximumPointCount;
            }

            return StrokeSampleResult.Accepted;
        }

        /// <summary>以抬起坐标结束活动笔迹；若采样器已自动完成则直接返回同一快照。</summary>
        public StrokeData End(Vector2 referencePosition, double timestamp)
        {
            if (State == StrokeSamplingState.Completed)
            {
                return completedStroke;
            }

            if (State != StrokeSamplingState.Sampling)
            {
                throw new InvalidOperationException("Only an active stroke can end.");
            }

            StrokeSampleResult result = AddPoint(referencePosition, timestamp);
            if (result != StrokeSampleResult.CompletedMaximumLength &&
                result != StrokeSampleResult.CompletedMaximumPointCount)
            {
                Complete(timestamp, StrokeCompletionReason.PointerEnded);
            }

            return completedStroke;
        }

        /// <summary>取消活动采样并清除临时数据，不创建可命中的完成笔迹。</summary>
        public bool Cancel()
        {
            if (State != StrokeSamplingState.Sampling)
            {
                return false;
            }

            State = StrokeSamplingState.Canceled;
            completedStroke = null;
            pointCount = 0;
            totalLength = 0f;
            initialHoldDuration = 0d;
            hasAcceptedMovement = false;
            return true;
        }

        /// <summary>仅在首次有效移动时记录起笔停留时长。</summary>
        private void RecordInitialHold(double timestamp)
        {
            if (hasAcceptedMovement)
            {
                return;
            }

            initialHoldDuration = Math.Max(0d, timestamp - startedAt);
            hasAcceptedMovement = true;
        }

        /// <summary>把点写入预分配缓冲并更新累计长度。</summary>
        private void Append(Vector2 point, float newTotalLength)
        {
            pointBuffer[pointCount] = point;
            pointCount++;
            totalLength = newTotalLength;
        }

        /// <summary>只在完成边界复制有效点，生成不受采样器复用影响的冻结快照。</summary>
        private void Complete(double timestamp, StrokeCompletionReason completionReason)
        {
            if (!hasAcceptedMovement)
            {
                initialHoldDuration = Math.Max(0d, timestamp - startedAt);
            }

            var frozenPoints = new Vector2[pointCount];
            Array.Copy(pointBuffer, frozenPoints, pointCount);
            completedStroke = new StrokeData(
                strokeId,
                frozenPoints,
                totalLength,
                startedAt,
                timestamp,
                initialHoldDuration,
                completionReason);
            State = StrokeSamplingState.Completed;
        }

        /// <summary>验证参考像素坐标两个分量均为有限值。</summary>
        private static void ValidatePoint(Vector2 point, string parameterName)
        {
            if (float.IsNaN(point.x) || float.IsInfinity(point.x) ||
                float.IsNaN(point.y) || float.IsInfinity(point.y))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Stroke points must be finite.");
            }
        }

        /// <summary>验证时间戳不是 NaN 或无穷。</summary>
        private static void ValidateTimestamp(double timestamp, string parameterName)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Stroke timestamps must be finite.");
            }
        }
    }
}
