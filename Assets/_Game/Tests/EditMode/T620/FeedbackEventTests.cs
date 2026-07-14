using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;
using OneStrokeDemon.Presentation;
using OneStrokeDemon.Tests.EditMode.T230;

namespace OneStrokeDemon.Tests.EditMode.T620
{
    [Category("T620")]
    public sealed class FeedbackEventTests
    {
        private GameplayConfigService config;

        [SetUp]
        public void SetUp()
        {
            config = Load(RuntimeConfigTestFixture.LoadJson(), "test:T620:feedback-events");
        }

        [Test]
        public void FiveSemanticEventsSelectConfiguredProfilesWithoutChangingEventTruth()
        {
            var output = new RecordingOutput();
            var vibration = new RecordingVibration();
            var service = new CombatFeedbackService(
                CombatFeedbackSettings.Create(config),
                output,
                vibration);
            var events = new[]
            {
                new CombatFeedbackEvent(CombatFeedbackType.EnemyHit, 11, "slash", -10L, 1d),
                new CombatFeedbackEvent(CombatFeedbackType.WeakpointHit, 12, "talisman", -24L, 2d),
                new CombatFeedbackEvent(CombatFeedbackType.ArmorBreak, 13, "break", -30L, 3d),
                new CombatFeedbackEvent(CombatFeedbackType.ProjectileReflect, 14, "bolt", 0L, 4d),
                new CombatFeedbackEvent(CombatFeedbackType.PlayerHit, -1, "enemy_attack", -18L, 5d),
            };

            for (int index = 0; index < events.Length; index += 1)
            {
                CombatFeedbackCommand command = service.Publish(events[index]);
                Assert.That(command.Event.TargetId, Is.EqualTo(events[index].TargetId));
                Assert.That(command.Event.SignedAmount, Is.EqualTo(events[index].SignedAmount));
                Assert.That(command.Event.SourceId, Is.EqualTo(events[index].SourceId));
            }

            Assert.That(output.Commands, Has.Count.EqualTo(5));
            Assert.That(output.Commands[0].Profile.FeedbackId, Is.EqualTo(ConfigIds.FeedbackCues.FeedbackEnemyHit));
            Assert.That(output.Commands[1].Profile.FeedbackId, Is.EqualTo(ConfigIds.FeedbackCues.FeedbackWeakpointHit));
            Assert.That(output.Commands[2].Profile.FeedbackId, Is.EqualTo(ConfigIds.FeedbackCues.FeedbackArmorBreak));
            Assert.That(output.Commands[3].Profile.FeedbackId, Is.EqualTo(ConfigIds.FeedbackCues.FeedbackProjectileReflect));
            Assert.That(output.Commands[4].Profile.FeedbackId, Is.EqualTo(ConfigIds.FeedbackCues.FeedbackPlayerHit));
            Assert.That(vibration.Patterns, Is.EqualTo(new[]
            {
                FeedbackVibrationPattern.Light,
                FeedbackVibrationPattern.Medium,
                FeedbackVibrationPattern.Heavy,
                FeedbackVibrationPattern.Medium,
                FeedbackVibrationPattern.Heavy,
            }));
        }

        [Test]
        public void VibrationMasterSwitchSuppressesPlatformRequestsButKeepsVisualCommand()
        {
            var output = new RecordingOutput();
            var vibration = new RecordingVibration();
            var service = new CombatFeedbackService(
                CombatFeedbackSettings.Create(config),
                output,
                vibration)
            {
                VibrationEnabled = false,
            };

            service.Publish(new CombatFeedbackEvent(
                CombatFeedbackType.ArmorBreak,
                17,
                "armor",
                -40L,
                1d));

            Assert.That(output.Commands, Has.Count.EqualTo(1));
            Assert.That(output.Commands[0].Profile.VibrationPattern, Is.EqualTo(FeedbackVibrationPattern.Heavy));
            Assert.That(vibration.Patterns, Is.Empty);
        }

        [Test]
        public void StrengthAndCueChangesFlowFromReloadedConfigWithoutCodeChanges()
        {
            string mutatedJson = RuntimeConfigTestFixture.MutateAndRehash(root =>
            {
                JObject row = FindFeedback(root, ConfigIds.FeedbackCues.FeedbackEnemyHit);
                row["shakeStrengthRefPx"] = 33d;
                row["timeScale"] = 0.45d;
                row["vfxScaleRefPx"] = 55d;
                row["audioKey"] = ConfigIds.AudioCues.SfxBreak;
            });
            GameplayConfigService changed = Load(mutatedJson, "test:T620:mutated");
            var output = new RecordingOutput();
            var service = new CombatFeedbackService(CombatFeedbackSettings.Create(changed), output);

            service.Publish(new CombatFeedbackEvent(
                CombatFeedbackType.EnemyHit,
                21,
                "changed",
                -5L,
                1d));

            CombatFeedbackProfile profile = output.Commands[0].Profile;
            Assert.That(profile.ShakeStrengthReferencePixels, Is.EqualTo(33f));
            Assert.That(profile.TimeScale, Is.EqualTo(0.45f));
            Assert.That(profile.VfxScaleReferencePixels, Is.EqualTo(55f));
            Assert.That(profile.AudioKey, Is.EqualTo(ConfigIds.AudioCues.SfxBreak));
        }

        [Test]
        public void DefaultEventIsRejectedBeforeAnyOutput()
        {
            var output = new RecordingOutput();
            var service = new CombatFeedbackService(CombatFeedbackSettings.Create(config), output);

            Assert.That(() => service.Publish(default), Throws.ArgumentException);
            Assert.That(output.Commands, Is.Empty);
        }

        private static JObject FindFeedback(JObject root, string feedbackId)
        {
            foreach (JObject row in ((JArray)root["feedbackCues"]).Children<JObject>())
            {
                if (row["feedbackId"]?.Value<string>() == feedbackId)
                {
                    return row;
                }
            }

            throw new AssertionException($"Feedback row '{feedbackId}' was not found.");
        }

        private static GameplayConfigService Load(string json, string source)
        {
            var service = new GameplayConfigService();
            service.Load(json, source);
            return service;
        }

        private sealed class RecordingOutput : ICombatFeedbackOutput
        {
            internal List<CombatFeedbackCommand> Commands { get; } = new List<CombatFeedbackCommand>();

            public void Emit(in CombatFeedbackCommand command)
            {
                Commands.Add(command);
            }
        }

        private sealed class RecordingVibration : ICombatFeedbackVibration
        {
            internal List<FeedbackVibrationPattern> Patterns { get; } =
                new List<FeedbackVibrationPattern>();

            public void Request(FeedbackVibrationPattern pattern)
            {
                Patterns.Add(pattern);
            }
        }
    }
}
