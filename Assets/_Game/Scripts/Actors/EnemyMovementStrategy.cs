using System;
using System.Collections.Generic;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    // 定义 EnemyMovementPatternTypes 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public static class EnemyMovementPatternTypes
    {
        public const string Linear = "Linear";
        public const string Sine = "Sine";
        public const string Dive = "Dive";
        public const string Hover = "Hover";
        public const string Boss = "Boss";
    }

    // 定义 EnemyMovementDefinition 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyMovementDefinition
    {
        // 初始化 EnemyMovementDefinition，并建立角色运行时所需的初始状态。
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

    // 定义 EnemyMovementSample 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct EnemyMovementSample
    {
        // 初始化 EnemyMovementSample，并建立角色运行时所需的初始状态。
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

    // 定义 IEnemyMovementStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public interface IEnemyMovementStrategy
    {
        string PatternType { get; }

        EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds);
    }

    // 定义 MovementStrategyRegistry 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class MovementStrategyRegistry
    {
        private readonly Dictionary<string, IEnemyMovementStrategy> strategies =
            new Dictionary<string, IEnemyMovementStrategy>(StringComparer.Ordinal);

        // 初始化 MovementStrategyRegistry，并建立角色运行时所需的初始状态。
        public MovementStrategyRegistry(IEnumerable<IEnemyMovementStrategy> configuredStrategies)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configuredStrategies == null)
            {
                throw new ArgumentNullException(nameof(configuredStrategies));
            }

            // 逐项推进本组角色数据，确保每个元素都遵循同一规则。
            foreach (IEnemyMovementStrategy strategy in configuredStrategies)
            {
                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (strategy == null || string.IsNullOrWhiteSpace(strategy.PatternType))
                {
                    throw new ArgumentException(
                        "Movement strategies and their pattern types must be non-null.",
                        nameof(configuredStrategies));
                }

                // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
                if (!strategies.TryAdd(strategy.PatternType, strategy))
                {
                    throw new ArgumentException(
                        $"Movement strategy '{strategy.PatternType}' is registered more than once.",
                        nameof(configuredStrategies));
                }
            }
        }

        // 创建 CreateDefault 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 获取 Get 对应的角色逻辑，并返回或发布一致的状态结果。
        public IEnemyMovementStrategy Get(string patternType)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (patternType != null && strategies.TryGetValue(patternType, out IEnemyMovementStrategy strategy))
            {
                return strategy;
            }

            throw new KeyNotFoundException(
                $"No enemy movement strategy is registered for pattern type '{patternType}'.");
        }

        // 处理 Sample 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!definition.IsConfigured)
            {
                throw new ArgumentException(
                    "Enemy movement definition must be configured.",
                    nameof(definition));
            }

            return Get(definition.PatternType).Sample(definition, elapsedSeconds);
        }
    }

    // 定义 EnemyMovementDefinitionFactory 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public static class EnemyMovementDefinitionFactory
    {
        // 创建 Create 对应的角色逻辑，并返回或发布一致的状态结果。
        public static EnemyMovementDefinition Create(
            IConfigProvider configProvider,
            string enemyId,
            MovementStrategyRegistry registry = null)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            EnemyDefinition enemy = EnemyDefinitionFactory.Create(configProvider, enemyId);
            return Create(configProvider, enemy, registry);
        }

        // 创建 Create 对应的角色逻辑，并返回或发布一致的状态结果。
        public static EnemyMovementDefinition Create(
            IConfigProvider configProvider,
            in EnemyDefinition enemy,
            MovementStrategyRegistry registry = null)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 处理 RequirePositiveGlobalInt 对应的角色逻辑，并返回或发布一致的状态结果。
        private static long RequirePositiveGlobalInt(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 处理 RequireNormalized 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireNormalized(string rowId, string field, double value)
        {
            RequireFiniteRange(rowId, field, value, 0d, 1d);
        }

        // 处理 RequireFiniteRange 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireFiniteRange(
            string rowId,
            string field,
            double value,
            double minimum,
            double maximum)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

    // 定义 EnemyMovementMath 的角色领域数据与行为边界，供上层流程以明确契约使用。
    internal static class EnemyMovementMath
    {
        // 校验 ValidateElapsed 对应的角色逻辑，并返回或发布一致的状态结果。
        public static void ValidateElapsed(double elapsedSeconds)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 处理 Progress 对应的角色逻辑，并返回或发布一致的状态结果。
        public static double Progress(
            in EnemyMovementDefinition definition,
            double elapsedSeconds,
            out bool completed)
        {
            ValidateElapsed(elapsedSeconds);
            double distance = definition.DirectDistanceReferencePixels;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (distance <= 0d || definition.SpeedReferencePixelsPerSecond <= 0d)
            {
                completed = !definition.Loop;
                return 0d;
            }

            double raw = elapsedSeconds * definition.SpeedReferencePixelsPerSecond / distance;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!definition.Loop)
            {
                completed = raw >= 1d;
                return Math.Min(1d, raw);
            }

            completed = false;
            double phase = raw % 2d;
            return phase <= 1d ? phase : 2d - phase;
        }

        // 处理 Lerp 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 处理 Oscillation 对应的角色逻辑，并返回或发布一致的状态结果。
        public static double Oscillation(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            return definition.AmplitudeReferencePixels *
                   Math.Sin(elapsedSeconds * Math.PI * 2d * definition.Frequency);
        }
    }

    // 定义 LinearMovementStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class LinearMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Linear;

        // 处理 Sample 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            double progress = EnemyMovementMath.Progress(definition, elapsedSeconds, out bool completed);
            return EnemyMovementMath.Lerp(definition, progress, completed);
        }
    }

    // 定义 SineMovementStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class SineMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Sine;

        // 处理 Sample 对应的角色逻辑，并返回或发布一致的状态结果。
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

    // 定义 DiveMovementStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class DiveMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Dive;

        // 处理 Sample 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            double progress = EnemyMovementMath.Progress(definition, elapsedSeconds, out bool completed);
            return EnemyMovementMath.Lerp(definition, progress * progress, completed);
        }
    }

    // 定义 HoverMovementStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class HoverMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Hover;

        // 处理 Sample 对应的角色逻辑，并返回或发布一致的状态结果。
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

    // 定义 BossMovementStrategy 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class BossMovementStrategy : IEnemyMovementStrategy
    {
        public string PatternType => EnemyMovementPatternTypes.Boss;

        // 处理 Sample 对应的角色逻辑，并返回或发布一致的状态结果。
        public EnemyMovementSample Sample(
            in EnemyMovementDefinition definition,
            double elapsedSeconds)
        {
            double progress = EnemyMovementMath.Progress(definition, elapsedSeconds, out bool completed);
            return EnemyMovementMath.Lerp(definition, progress, completed);
        }
    }
}
