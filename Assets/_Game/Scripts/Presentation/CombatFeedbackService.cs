using System;
using System.Collections.Generic;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Presentation
{
    public enum CombatFeedbackType
    {
        EnemyHit = 0,
        WeakpointHit = 1,
        ArmorBreak = 2,
        ProjectileReflect = 3,
        PlayerHit = 4,
    }

    public enum FeedbackVibrationPattern
    {
        Off = 0,
        Light = 1,
        Medium = 2,
        Heavy = 3,
    }

    public sealed class CombatFeedbackProfile
    {
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

        private void Validate()
        {
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

        private static FeedbackVibrationPattern ParseVibration(string value, string feedbackId)
        {
            if (!Enum.TryParse(value, false, out FeedbackVibrationPattern parsed) ||
                !Enum.IsDefined(typeof(FeedbackVibrationPattern), parsed))
            {
                throw new ArgumentException(
                    $"FeedbackCues row '{feedbackId}' has unsupported vibration '{value}'.",
                    "configProvider");
            }

            return parsed;
        }

        private static int CheckedPositiveInt(long value, string feedbackId, string field)
        {
            if (value < 1L || value > int.MaxValue)
            {
                throw new ArgumentException(
                    $"FeedbackCues row '{feedbackId}' field '{field}' must fit a positive runtime integer.",
                    "configProvider");
            }

            return (int)value;
        }

        private static bool IsFiniteInRange(float value, float minimum, float maximum) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum;

        private static bool IsFinitePositive(float value) => IsFiniteInRange(value, float.Epsilon, float.MaxValue);

        private static bool IsFiniteNonNegative(float value) => IsFiniteInRange(value, 0f, float.MaxValue);

        private static bool IsColorHex(string value)
        {
            if (value == null || value.Length != 9 || value[0] != '#')
            {
                return false;
            }

            for (int index = 1; index < value.Length; index += 1)
            {
                char character = value[index];
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

    public sealed class CombatFeedbackSettings
    {
        private readonly IReadOnlyDictionary<CombatFeedbackType, CombatFeedbackProfile> profiles;

        private CombatFeedbackSettings(
            IReadOnlyDictionary<CombatFeedbackType, CombatFeedbackProfile> configuredProfiles)
        {
            profiles = configuredProfiles;
        }

        public static CombatFeedbackSettings Create(IConfigProvider configProvider)
        {
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

        public CombatFeedbackProfile Get(CombatFeedbackType type)
        {
            if (!profiles.TryGetValue(type, out CombatFeedbackProfile profile))
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Feedback type is not configured.");
            }

            return profile;
        }

        private static CombatFeedbackProfile CreateProfile(
            IConfigProvider configProvider,
            CombatFeedbackType type,
            string feedbackId) =>
            new CombatFeedbackProfile(type, configProvider.GetFeedbackCue(feedbackId));
    }

    public readonly struct CombatFeedbackEvent
    {
        public CombatFeedbackEvent(
            CombatFeedbackType type,
            int targetId,
            string sourceId,
            long signedAmount,
            double timestamp)
        {
            if (!Enum.IsDefined(typeof(CombatFeedbackType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (targetId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetId));
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Feedback source id must be non-empty.", nameof(sourceId));
            }

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

    public readonly struct CombatFeedbackCommand
    {
        internal CombatFeedbackCommand(in CombatFeedbackEvent feedbackEvent, CombatFeedbackProfile profile)
        {
            Event = feedbackEvent;
            Profile = profile;
        }

        public CombatFeedbackEvent Event { get; }
        public CombatFeedbackProfile Profile { get; }
    }

    public interface ICombatFeedbackOutput
    {
        void Emit(in CombatFeedbackCommand command);
    }

    public interface ICombatFeedbackVibration
    {
        void Request(FeedbackVibrationPattern pattern);
    }

    public sealed class CombatFeedbackService
    {
        private readonly CombatFeedbackSettings settings;
        private readonly ICombatFeedbackOutput output;
        private readonly ICombatFeedbackVibration vibration;

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

        public CombatFeedbackCommand Publish(in CombatFeedbackEvent feedbackEvent)
        {
            if (!feedbackEvent.IsValid)
            {
                throw new ArgumentException("Feedback event must be initialized.", nameof(feedbackEvent));
            }

            CombatFeedbackProfile profile = settings.Get(feedbackEvent.Type);
            var command = new CombatFeedbackCommand(feedbackEvent, profile);
            output.Emit(command);
            if (VibrationEnabled &&
                vibration != null &&
                profile.VibrationPattern != FeedbackVibrationPattern.Off)
            {
                vibration.Request(profile.VibrationPattern);
            }

            return command;
        }

        public bool HandleEnemyHit(
            in DamageResult resolvedDamage,
            in EnemyHitResolution appliedHit,
            string sourceId,
            double timestamp)
        {
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

        public bool HandleProjectileStroke(in ProjectileStrokeResult result, double timestamp)
        {
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

        public bool HandlePlayerEvent(in PlayerCombatEvent playerEvent, int targetId)
        {
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
