using System;

namespace OneStrokeDemon.Levels
{
    public interface IBattleSession : IDisposable
    {
        string LevelId { get; }
    }

    public interface IBattleSessionFactory
    {
        IBattleSession Create(string levelId);
    }

    public sealed class BattleResultNavigation : IDisposable
    {
        private readonly IBattleSessionFactory sessionFactory;
        private bool disposed;

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

        public IBattleSession Restart()
        {
            RequireActive();
            return Replace(Current.LevelId);
        }

        public IBattleSession GoNext(ResultReceipt result)
        {
            RequireActive();
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!string.Equals(result.LevelId, Current.LevelId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Result level '{result.LevelId}' does not match current level '{Current.LevelId}'.");
            }

            if (!result.CanGoNext || string.IsNullOrWhiteSpace(result.NextLevelId))
            {
                throw new InvalidOperationException("The result does not allow next-level navigation.");
            }

            return Replace(result.NextLevelId);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Current?.Dispose();
            Current = null;
        }

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

        private IBattleSession Create(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId) ||
                !string.Equals(levelId, levelId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Level id must be non-empty and trimmed.",
                    nameof(levelId));
            }

            IBattleSession session = sessionFactory.Create(levelId);
            if (session == null)
            {
                throw new InvalidOperationException(
                    $"Battle session factory returned null for '{levelId}'.");
            }

            if (!string.Equals(session.LevelId, levelId, StringComparison.Ordinal))
            {
                session.Dispose();
                throw new InvalidOperationException(
                    $"Battle session level '{session.LevelId}' does not match requested level '{levelId}'.");
            }

            return session;
        }

        private void RequireActive()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BattleResultNavigation));
            }

            if (Current == null)
            {
                throw new InvalidOperationException("No active battle session is available.");
            }
        }
    }
}
