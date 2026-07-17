using System;

namespace OneStrokeDemon.Levels
{
    // 定义 IBattleSession 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public interface IBattleSession : IDisposable
    {
        string LevelId { get; }
    }

    // 定义 IBattleSessionFactory 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public interface IBattleSessionFactory
    {
        IBattleSession Create(string levelId);
    }

    // 定义 BattleResultNavigation 的关卡领域契约，用于描述时间线、流程或持久化边界。
    public sealed class BattleResultNavigation : IDisposable
    {
        private readonly IBattleSessionFactory sessionFactory;
        private bool disposed;

        // 初始化 BattleResultNavigation，并建立关卡流程所需的初始状态。
        public BattleResultNavigation(
            IBattleSessionFactory sessionFactory,
            string initialLevelId)
        {
            this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            Current = Create(initialLevelId);
            Generation = 1U;
        }

        public IBattleSession Current { get; private set; }

        public uint Generation { get; private set; }

        // 处理 Restart 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public IBattleSession Restart()
        {
            RequireActive();
            return Replace(Current.LevelId);
        }

        // 处理 GoNext 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public IBattleSession GoNext(ResultReceipt result)
        {
            RequireActive();
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!string.Equals(result.LevelId, Current.LevelId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Result level '{result.LevelId}' does not match current level '{Current.LevelId}'.");
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!result.CanGoNext || string.IsNullOrWhiteSpace(result.NextLevelId))
            {
                throw new InvalidOperationException("The result does not allow next-level navigation.");
            }

            return Replace(result.NextLevelId);
        }

        // 释放 Dispose 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        public void Dispose()
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (disposed)
            {
                return;
            }

            disposed = true;
            Current?.Dispose();
            Current = null;
        }

        // 处理 Replace 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private IBattleSession Replace(string levelId)
        {
            IBattleSession previous = Current;
            Current = null;
            previous.Dispose();
            IBattleSession next = Create(levelId);
            Current = next;
            checked
            {
                Generation += 1U;
            }

            return next;
        }

        // 创建 Create 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private IBattleSession Create(string levelId)
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (string.IsNullOrWhiteSpace(levelId) ||
                !string.Equals(levelId, levelId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Level id must be non-empty and trimmed.",
                    nameof(levelId));
            }

            IBattleSession session = sessionFactory.Create(levelId);
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (session == null)
            {
                throw new InvalidOperationException(
                    $"Battle session factory returned null for '{levelId}'.");
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (!string.Equals(session.LevelId, levelId, StringComparison.Ordinal))
            {
                session.Dispose();
                throw new InvalidOperationException(
                    $"Battle session level '{session.LevelId}' does not match requested level '{levelId}'.");
            }

            return session;
        }

        // 处理 RequireActive 对应的关卡逻辑，并保持时间线、进度与结算状态一致。
        private void RequireActive()
        {
            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BattleResultNavigation));
            }

            // 检查关卡条件或生命周期边界，避免流程进入不一致状态。
            if (Current == null)
            {
                throw new InvalidOperationException("No active battle session is available.");
            }
        }
    }
}
