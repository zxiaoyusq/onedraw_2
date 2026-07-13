using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    public static class EnemyMovementPatternTypes
    {
        public const string Linear = "Linear";
        public const string Sine = "Sine";
        public const string Dive = "Dive";
        public const string Hover = "Hover";
        public const string Boss = "Boss";
    }

    public readonly struct EnemyMovementDefinition
    {
        internal EnemyMovementDefinition(
            string movePatternId,
            string patternType,
            double startXReferencePixels,
            double startYReferencePixels,
            double endXReferencePixels,
            double endYReferencePixels,
            double speedReferencePixelsPerSecond,
            double amplitudeReferencePixels,
            double frequency,
            bool loop)
        {
            MovePatternId = movePatternId;
            PatternType = patternType;
            StartXReferencePixels = startXReferencePixels;
            StartYReferencePixels = startYReferencePixels;
            EndXReferencePixels = endXReferencePixels;
            EndYReferencePixels = endYReferencePixels;
            SpeedReferencePixelsPerSecond = speedReferencePixelsPerSecond;
            AmplitudeReferencePixels = amplitudeReferencePixels;
            Frequency = frequency;
            Loop = loop;
            IsConfigured = true;
        }

        public string MovePatternId { get; }

        public string PatternType { get; }

        public double StartXReferencePixels { get; }

        public double StartYReferencePixels { get; }

        public double EndXReferencePixels { get; }

        public double EndYReferencePixels { get; }

        public double SpeedReferencePixelsPerSecond { get; }

        public double AmplitudeReferencePixels { get; }

        public double Frequency { get; }

        public bool Loop { get; }

        public bool IsConfigured { get; }

        public double DirectDistanceReferencePixels
        {
            get
            {
                double x = EndXReferencePixels - StartXReferencePixels;
                double y = EndYReferencePixels - StartYReferencePixels;
                return Math.Sqrt((x * x) + (y * y));
            }
        }
    }

    public readonly struct EnemyMovementSample
    {
        internal EnemyMovementSample(
            double xReferencePixels,
            double yReferencePixels,
            double progress,
            bool completed)
        {
            XReferencePixels = xReferencePixels;
            YReferencePixels = yReferencePixels;
            Progress = progress;
            Completed = completed;
            IsValid = true;
        }

        public double XReferencePixels { get; }

        public double YReferencePixels { get; }

        public double Progress { get; }

        public bool Completed { get; }

        public bool IsValid { get; }
    }

    public interface IEnemyMovementStrategy
    {
        string PatternType { get; }

        EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds);
    }

    public sealed class MovementStrategyRegistry
    {
        private readonly Dictionary<string, IEnemyMovementStrategy> strategies =
            new Dictionary<string, IEnemyMovementStrategy>(StringComparer.Ordinal);

        public MovementStrategyRegistry(IEnumerable<IEnemyMovementStrategy> configuredStrategies)
        {
            if (configuredStrategies == null)
            {
                throw new ArgumentNullException(nameof(configuredStrategies));
            }

            foreach (IEnemyMovementStrategy strategy in configuredStrategies)
            {
                if (strategy == null || string.IsNullOrWhiteSpace(strategy.PatternType))
                {
                    throw new ArgumentException(
                        "Movement strategies and their pattern types must be non-null.",
                        nameof(configuredStrategies));
                }

                if (!strategies.TryAdd(strategy.PatternType, strategy))
                {
                    throw new ArgumentException(
                        $"Movement strategy '{strategy.PatternType}' is registered more than once.",
                        nameof(configuredStrategies));
                }
            }
        }

        public static MovementStrategyRegistry CreateDefault()
        {
            return new MovementStrategyRegistry(new IEnemyMovementStrategy[]
            {
                new LinearMovementStrategy(),
                new SineMovementStrategy(),
                new DiveMovementStrategy(),
                new HoverMovementStrategy(),
                new BossMovementStrategy(),
            });
        }

        public IEnemyMovementStrategy Get(string patternType)
        {
            if (patternType != null && strategies.TryGetValue(patternType, out IEnemyMovementStrategy strategy))
            {
                return strategy;
            }

            throw new KeyNotFoundException(
                $"No enemy movement strategy is registered for pattern type '{patternType}'.");
        }

        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            if (!definition.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy movement definition must be configured.",
                    nameof(definition));
            }

            return Get(definition.PatternType).Sample(definition, elapsedSeconds);
        }
    }

    public static class EnemyMovementDefinitionFactory
    {
        public static EnemyMovementDefinition Create(
            IConfigProvider configProvider,
            string enemyId,
            MovementStrategyRegistry registry = null)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            EnemyDefinition enemy = EnemyDefinitionFactory.Create(configProvider, enemyId);
            return Create(configProvider, enemy, registry);
        }

        public static EnemyMovementDefinition Create(
            IConfigProvider configProvider,
            in EnemyDefinition enemy,
            MovementStrategyRegistry registry = null)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (!enemy.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy definition must be configured.",
                    nameof(enemy));
            }

            MovePatternConfig pattern = configProvider.GetMovePattern(enemy.MovePatternId);
            (registry ?? MovementStrategyRegistry.CreateDefault()).Get(pattern.PatternType);

            long referenceWidth = RequirePositiveGlobalInt(configProvider, "reference_width");
            long referenceHeight = RequirePositiveGlobalInt(configProvider, "reference_height");
            RequireFiniteRange(pattern.MovePatternId, nameof(pattern.SpeedMultiplier), pattern.SpeedMultiplier, 0d, double.MaxValue);
            RequireFiniteRange(pattern.MovePatternId, nameof(pattern.AmplitudeRefPx), pattern.AmplitudeRefPx, 0d, double.MaxValue);
            RequireFiniteRange(pattern.MovePatternId, nameof(pattern.Frequency), pattern.Frequency, 0d, double.MaxValue);
            RequireNormalized(pattern.MovePatternId, nameof(pattern.StartXNorm), pattern.StartXNorm);
            RequireNormalized(pattern.MovePatternId, nameof(pattern.EndXNorm), pattern.EndXNorm);
            RequireNormalized(pattern.MovePatternId, nameof(pattern.StartYNorm), pattern.StartYNorm);
            RequireNormalized(pattern.MovePatternId, nameof(pattern.EndYNorm), pattern.EndYNorm);
            RequireFiniteRange(
                enemy.EnemyId,
                nameof(enemy.MoveSpeedReferencePixelsPerSecond),
                enemy.MoveSpeedReferencePixelsPerSecond,
                0d,
                double.MaxValue);

            return new EnemyMovementDefinition(
                pattern.MovePatternId,
                pattern.PatternType,
                pattern.StartXNorm * referenceWidth,
                pattern.StartYNorm * referenceHeight,
                pattern.EndXNorm * referenceWidth,
                pattern.EndYNorm * referenceHeight,
                enemy.MoveSpeedReferencePixelsPerSecond * pattern.SpeedMultiplier,
                pattern.AmplitudeRefPx,
                pattern.Frequency,
                pattern.Loop);
        }

        private static long RequirePositiveGlobalInt(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            if (!string.Equals(row.ValueType, "int", StringComparison.Ordinal) ||
                !row.IntValue.HasValue ||
                row.IntValue.Value <= 0L)
            {
                throw new ArgumentException(
                    $"Global '{key}' must contain a positive int value.",
                    nameof(configProvider));
            }

            return row.IntValue.Value;
        }

        private static void RequireNormalized(string rowId, string field, double value)
        {
            RequireFiniteRange(rowId, field, value, 0d, 1d);
        }

        private static void RequireFiniteRange(
            string rowId,
            string field,
            double value,
            double minimum,
            double maximum)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value < minimum ||
                value > maximum)
            {
                throw new ArgumentOutOfRangeException(
                    field,
                    value,
                    $"Movement definition '{rowId}.{field}' is outside its supported range.");
            }
        }
    }

    internal static class EnemyMovementMath
    {
        public static void ValidateElapsed(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) ||
                double.IsInfinity(elapsedSeconds) ||
                elapsedSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "Movement elapsed time must be finite and non-negative.");
            }
        }

        public static double Progress(
            in EnemyMovementDefinition definition,
            double elapsedSeconds,
            out bool completed)
        {
            ValidateElapsed(elapsedSeconds);
            double distance = definition.DirectDistanceReferencePixels;
            if (distance <= 0d || definition.SpeedReferencePixelsPerSecond <= 0d)
            {
                completed = !definition.Loop;
                return 0d;
            }

            double raw = elapsedSeconds * definition.SpeedReferencePixelsPerSecond / distance;
            if (!definition.Loop)
            {
                completed = raw >= 1d;
                return Math.Min(1d, raw);
            }

            completed = false;
            double phase = raw % 2d;
            return phase <= 1d ? phase : 2d - phase;
        }

        public static EnemyMovementSample Lerp(
            in EnemyMovementDefinition definition,
            double progress,
            bool completed,
            double yOffset = 0d)
        {
            double x = definition.StartXReferencePixels +
                       ((definition.EndXReferencePixels - definition.StartXReferencePixels) * progress);
            double y = definition.StartYReferencePixels +
                       ((definition.EndYReferencePixels - definition.StartYReferencePixels) * progress) +
                       yOffset;
            return new EnemyMovementSample(x, y, progress, completed);
        }

        public static double Oscillation(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            return definition.AmplitudeReferencePixels *
                   Math.Sin(elapsedSeconds * Math.PI * 2d * definition.Frequency);
        }
    }

    public sealed class LinearMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Linear;

        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            double progress = EnemyMovementMath.Progress(definition, elapsedSeconds, out bool completed);
            return EnemyMovementMath.Lerp(definition, progress, completed);
        }
    }

    public sealed class SineMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Sine;

        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            double progress = EnemyMovementMath.Progress(definition, elapsedSeconds, out bool completed);
            return EnemyMovementMath.Lerp(
                definition,
                progress,
                completed,
                EnemyMovementMath.Oscillation(definition, elapsedSeconds));
        }
    }

    public sealed class DiveMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Dive;

        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            double progress = EnemyMovementMath.Progress(definition, elapsedSeconds, out bool completed);
            return EnemyMovementMath.Lerp(definition, progress * progress, completed);
        }
    }

    public sealed class HoverMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Hover;

        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            double progress = EnemyMovementMath.Progress(definition, elapsedSeconds, out bool completed);
            return EnemyMovementMath.Lerp(
                definition,
                progress,
                completed,
                EnemyMovementMath.Oscillation(definition, elapsedSeconds));
        }
    }

    public sealed class BossMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Boss;

        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            double progress = EnemyMovementMath.Progress(definition, elapsedSeconds, out bool completed);
            return EnemyMovementMath.Lerp(definition, progress, completed);
        }
    }
}
