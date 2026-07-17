using System;
using System.Collections.Generic;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Presentation
{
    // 定义 CombatFeedbackType 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public enum CombatFeedbackType
    {
        EnemyHit = 0,
        WeakpointHit = 1,
        ArmorBreak = 2,
        ProjectileReflect = 3,
        PlayerHit = 4,
    }

    // 定义 FeedbackVibrationPattern 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public enum FeedbackVibrationPattern
    {
        Off = 0,
        Light = 1,
        Medium = 2,
        Heavy = 3,
    }

    // 定义 CombatFeedbackProfile 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class CombatFeedbackProfile
    {
        // 初始化 CombatFeedbackProfile，并建立表现层所需的引用与初始显示状态。
        internal CombatFeedbackProfile(CombatFeedbackType type, FeedbackCueConfig row)
        {
            Type = type;
            FeedbackId = row.FeedbackId;
            VfxKey = row.VfxKey;
            AudioKey = row.AudioKey;
            TimeScale = row.TimeScale;
            TimeScaleSeconds = row.TimeScaleSec;
            FlashSeconds = row.FlashSec;
            ShakeStrengthReferencePixels = row.ShakeStrengthRefPx;
            ShakeSeconds = row.ShakeSec;
            VibrationPattern = ParseVibration(row.VibrationPattern, row.FeedbackId);
            DamageNumberColorHex = row.DamageNumberColorHex;
            DamageNumberFontSizeReferencePixels = CheckedPositiveInt(
                row.DamageNumberFontSizeRefPx,
                row.FeedbackId,
                nameof(row.DamageNumberFontSizeRefPx));
            DamageNumberLifeSeconds = row.DamageNumberLifeSec;
            DamageNumberRiseReferencePixels = row.DamageNumberRiseRefPx;
            VfxTintColorHex = row.VfxTintColorHex;
            VfxScaleReferencePixels = row.VfxScaleRefPx;
            Validate();
        }

        public CombatFeedbackType Type { get; }
        public string FeedbackId { get; }
        public string VfxKey { get; }
        public string AudioKey { get; }
        public float TimeScale { get; }
        public float TimeScaleSeconds { get; }
        public float FlashSeconds { get; }
        public float ShakeStrengthReferencePixels { get; }
        public float ShakeSeconds { get; }
        public FeedbackVibrationPattern VibrationPattern { get; }
        public string DamageNumberColorHex { get; }
        public int DamageNumberFontSizeReferencePixels { get; }
        public float DamageNumberLifeSeconds { get; }
        public float DamageNumberRiseReferencePixels { get; }
        public string VfxTintColorHex { get; }
        public float VfxScaleReferencePixels { get; }

        // 校验 Validate 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private void Validate()
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(FeedbackId) ||
                string.IsNullOrWhiteSpace(VfxKey) ||
                string.IsNullOrWhiteSpace(AudioKey) ||
                !IsFiniteInRange(TimeScale, 0f, 1f) ||
                !IsFinitePositive(TimeScaleSeconds) ||
                !IsFiniteNonNegative(FlashSeconds) ||
                !IsFiniteNonNegative(ShakeStrengthReferencePixels) ||
                !IsFiniteNonNegative(ShakeSeconds) ||
                !IsFinitePositive(DamageNumberLifeSeconds) ||
                !IsFiniteNonNegative(DamageNumberRiseReferencePixels) ||
                !IsFinitePositive(VfxScaleReferencePixels) ||
                !IsColorHex(DamageNumberColorHex) ||
                !IsColorHex(VfxTintColorHex))
            {
                throw new ArgumentException(
                    $"FeedbackCues row '{FeedbackId}' is not valid for runtime feedback.",
                    "configProvider");
            }
        }

        // 处理 ParseVibration 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static FeedbackVibrationPattern ParseVibration(string value, string feedbackId)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!Enum.TryParse(value, false, out FeedbackVibrationPattern parsed) ||
                !Enum.IsDefined(typeof(FeedbackVibrationPattern), parsed))
            {
                throw new ArgumentException(
                    $"FeedbackCues row '{feedbackId}' has unsupported vibration '{value}'.",
                    "configProvider");
            }

            return parsed;
        }

        // 处理 CheckedPositiveInt 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static int CheckedPositiveInt(long value, string feedbackId, string field)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (value < 1L || value > int.MaxValue)
            {
                throw new ArgumentException(
                    $"FeedbackCues row '{feedbackId}' field '{field}' must fit a positive runtime integer.",
                    "configProvider");
            }

            return (int)value;
        }

        // 判断是否 IsFiniteInRange 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static bool IsFiniteInRange(float value, float minimum, float maximum) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum;

        // 判断是否 IsFinitePositive 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static bool IsFinitePositive(float value) => IsFiniteInRange(value, float.Epsilon, float.MaxValue);

        // 判断是否 IsFiniteNonNegative 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static bool IsFiniteNonNegative(float value) => IsFiniteInRange(value, 0f, float.MaxValue);

        // 判断是否 IsColorHex 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static bool IsColorHex(string value)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (value == null || value.Length != 9 || value[0] != '#')
            {
                return false;
            }

            // 逐项更新视图或池对象，保持显示顺序和回收行为一致。
            for (int index = 1; index < value.Length; index += 1)
            {
                char character = value[index];
                // 检查视图状态、资源或生命周期边界，避免产生无效表现。
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f') ||
                      (character >= 'A' && character <= 'F')))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // 定义 CombatFeedbackSettings 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class CombatFeedbackSettings
    {
        private readonly IReadOnlyDictionary<CombatFeedbackType, CombatFeedbackProfile> profiles;

        // 初始化 CombatFeedbackSettings，并建立表现层所需的引用与初始显示状态。
        private CombatFeedbackSettings(
            IReadOnlyDictionary<CombatFeedbackType, CombatFeedbackProfile> configuredProfiles)
        {
            profiles = configuredProfiles;
        }

        // 创建 Create 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public static CombatFeedbackSettings Create(IConfigProvider configProvider)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            return new CombatFeedbackSettings(
                new Dictionary<CombatFeedbackType, CombatFeedbackProfile>
                {
                    [CombatFeedbackType.EnemyHit] = CreateProfile(
                        configProvider,
                        CombatFeedbackType.EnemyHit,
                        ConfigIds.FeedbackCues.FeedbackEnemyHit),
                    [CombatFeedbackType.WeakpointHit] = CreateProfile(
                        configProvider,
                        CombatFeedbackType.WeakpointHit,
                        ConfigIds.FeedbackCues.FeedbackWeakpointHit),
                    [CombatFeedbackType.ArmorBreak] = CreateProfile(
                        configProvider,
                        CombatFeedbackType.ArmorBreak,
                        ConfigIds.FeedbackCues.FeedbackArmorBreak),
                    [CombatFeedbackType.ProjectileReflect] = CreateProfile(
                        configProvider,
                        CombatFeedbackType.ProjectileReflect,
                        ConfigIds.FeedbackCues.FeedbackProjectileReflect),
                    [CombatFeedbackType.PlayerHit] = CreateProfile(
                        configProvider,
                        CombatFeedbackType.PlayerHit,
                        ConfigIds.FeedbackCues.FeedbackPlayerHit),
                });
        }

        // 获取 Get 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public CombatFeedbackProfile Get(CombatFeedbackType type)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!profiles.TryGetValue(type, out CombatFeedbackProfile profile))
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Feedback type is not configured.");
            }

            return profile;
        }

        // 创建 CreateProfile 对应的表现逻辑，使视图与只读战斗状态保持同步。
        private static CombatFeedbackProfile CreateProfile(
            IConfigProvider configProvider,
            CombatFeedbackType type,
            string feedbackId) =>
            new CombatFeedbackProfile(type, configProvider.GetFeedbackCue(feedbackId));
    }

    // 定义 CombatFeedbackEvent 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public readonly struct CombatFeedbackEvent
    {
        // 初始化 CombatFeedbackEvent，并建立表现层所需的引用与初始显示状态。
        public CombatFeedbackEvent(
            CombatFeedbackType type,
            int targetId,
            string sourceId,
            long signedAmount,
            double timestamp)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!Enum.IsDefined(typeof(CombatFeedbackType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (targetId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetId));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Feedback source id must be non-empty.", nameof(sourceId));
            }

            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp));
            }

            Type = type;
            TargetId = targetId;
            SourceId = sourceId;
            SignedAmount = signedAmount;
            Timestamp = timestamp;
            IsValid = true;
        }

        public CombatFeedbackType Type { get; }
        public int TargetId { get; }
        public string SourceId { get; }
        public long SignedAmount { get; }
        public double Timestamp { get; }
        public bool IsValid { get; }
    }

    // 定义 CombatFeedbackCommand 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public readonly struct CombatFeedbackCommand
    {
        // 初始化 CombatFeedbackCommand，并建立表现层所需的引用与初始显示状态。
        internal CombatFeedbackCommand(in CombatFeedbackEvent feedbackEvent, CombatFeedbackProfile profile)
        {
            Event = feedbackEvent;
            Profile = profile;
        }

        public CombatFeedbackEvent Event { get; }
        public CombatFeedbackProfile Profile { get; }
    }

    // 定义 ICombatFeedbackOutput 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public interface ICombatFeedbackOutput
    {
        void Emit(in CombatFeedbackCommand command);
    }

    // 定义 ICombatFeedbackVibration 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public interface ICombatFeedbackVibration
    {
        void Request(FeedbackVibrationPattern pattern);
    }

    // 定义 CombatFeedbackService 的表现层契约，隔离战斗状态与具体Unity视图实现。
    public sealed class CombatFeedbackService
    {
        private readonly CombatFeedbackSettings settings;
        private readonly ICombatFeedbackOutput output;
        private readonly ICombatFeedbackVibration vibration;

        // 初始化 CombatFeedbackService，并建立表现层所需的引用与初始显示状态。
        public CombatFeedbackService(
            CombatFeedbackSettings configuredSettings,
            ICombatFeedbackOutput configuredOutput,
            ICombatFeedbackVibration configuredVibration = null)
        {
            settings = configuredSettings ?? throw new ArgumentNullException(nameof(configuredSettings));
            output = configuredOutput ?? throw new ArgumentNullException(nameof(configuredOutput));
            vibration = configuredVibration;
        }

        public bool VibrationEnabled { get; set; } = true;

        // 发布 Publish 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public CombatFeedbackCommand Publish(in CombatFeedbackEvent feedbackEvent)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!feedbackEvent.IsValid)
            {
                throw new ArgumentException("Feedback event must be initialized.", nameof(feedbackEvent));
            }

            CombatFeedbackProfile profile = settings.Get(feedbackEvent.Type);
            var command = new CombatFeedbackCommand(feedbackEvent, profile);
            output.Emit(command);
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (VibrationEnabled &&
                vibration != null &&
                profile.VibrationPattern != FeedbackVibrationPattern.Off)
            {
                vibration.Request(profile.VibrationPattern);
            }

            return command;
        }

        // 处理 HandleEnemyHit 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool HandleEnemyHit(
            in DamageResult resolvedDamage,
            in EnemyHitResolution appliedHit,
            string sourceId,
            double timestamp)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!resolvedDamage.IsResolved || !appliedHit.IsValid || !appliedHit.Damage.Changed)
            {
                return false;
            }

            CombatFeedbackType type = appliedHit.Damage.ArmorBroken
                ? CombatFeedbackType.ArmorBreak
                : resolvedDamage.IsWeakpoint
                    ? CombatFeedbackType.WeakpointHit
                    : CombatFeedbackType.EnemyHit;
            Publish(new CombatFeedbackEvent(
                type,
                resolvedDamage.TargetId,
                sourceId,
                -appliedHit.Damage.AppliedTotalDamage,
                timestamp));
            return true;
        }

        // 处理 HandleProjectileStroke 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool HandleProjectileStroke(in ProjectileStrokeResult result, double timestamp)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!result.IsValid || result.Outcome != ProjectileStrokeOutcome.Reflected)
            {
                return false;
            }

            Publish(new CombatFeedbackEvent(
                CombatFeedbackType.ProjectileReflect,
                result.HitTargetId,
                result.ProjectileId,
                0L,
                timestamp));
            return true;
        }

        // 处理 HandlePlayerEvent 对应的表现逻辑，使视图与只读战斗状态保持同步。
        public bool HandlePlayerEvent(in PlayerCombatEvent playerEvent, int targetId)
        {
            // 检查视图状态、资源或生命周期边界，避免产生无效表现。
            if (!playerEvent.IsValid ||
                playerEvent.EventType != PlayerCombatEventType.HpChanged ||
                playerEvent.SignedAmount >= 0L)
            {
                return false;
            }

            Publish(new CombatFeedbackEvent(
                CombatFeedbackType.PlayerHit,
                targetId,
                playerEvent.SourceId,
                playerEvent.SignedAmount,
                playerEvent.Timestamp));
            return true;
        }
    }
}
