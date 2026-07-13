using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    public readonly struct PlayerCombatSettings
    {
        internal PlayerCombatSettings(
            string playerId,
            long maximumHp,
            long maximumEnergy,
            string defaultStanceId,
            string ultimateSkillId,
            long ultimateEnergyCost,
            double hitInvulnerabilitySeconds)
        {
            PlayerId = playerId;
            MaximumHp = maximumHp;
            MaximumEnergy = maximumEnergy;
            DefaultStanceId = defaultStanceId;
            UltimateSkillId = ultimateSkillId;
            UltimateEnergyCost = ultimateEnergyCost;
            HitInvulnerabilitySeconds = hitInvulnerabilitySeconds;
            IsConfigured = true;
        }

        public string PlayerId { get; }

        public long MaximumHp { get; }

        public long MaximumEnergy { get; }

        public string DefaultStanceId { get; }

        public string UltimateSkillId { get; }

        public long UltimateEnergyCost { get; }

        public double HitInvulnerabilitySeconds { get; }

        public bool IsConfigured { get; }
    }

    public static class PlayerCombatSettingsFactory
    {
        public static PlayerCombatSettings Create(
            IConfigProvider configProvider,
            string playerId)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException("Player id must be non-empty.", nameof(playerId));
            }

            PlayerConfig player = configProvider.GetPlayer(playerId);
            if (player.MaxHp <= 0)
            {
                throw Invalid(player.PlayerId, nameof(player.MaxHp), player.MaxHp);
            }

            if (player.MaxEnergy < 0)
            {
                throw Invalid(player.PlayerId, nameof(player.MaxEnergy), player.MaxEnergy);
            }

            RequireFiniteNonNegative(
                player.PlayerId,
                nameof(player.HitInvulnSec),
                player.HitInvulnSec);
            if (string.IsNullOrWhiteSpace(player.DefaultStanceId))
            {
                throw new ArgumentException(
                    $"Player '{player.PlayerId}' must configure a default stance.",
                    nameof(playerId));
            }

            if (string.IsNullOrWhiteSpace(player.UltimateSkillId))
            {
                throw new ArgumentException(
                    $"Player '{player.PlayerId}' must configure an ultimate skill.",
                    nameof(playerId));
            }

            StanceSnapshot defaultStance = StanceSnapshot.FromConfig(
                configProvider.GetStance(player.DefaultStanceId));
            SkillConfig ultimate = configProvider.GetSkill(player.UltimateSkillId);
            if (ultimate.EnergyCost < 0)
            {
                throw Invalid(ultimate.SkillId, nameof(ultimate.EnergyCost), ultimate.EnergyCost);
            }

            if (ultimate.EnergyCost > player.MaxEnergy)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerId),
                    ultimate.EnergyCost,
                    $"Ultimate skill '{ultimate.SkillId}' costs more energy than player '{player.PlayerId}' can hold.");
            }

            return new PlayerCombatSettings(
                player.PlayerId,
                player.MaxHp,
                player.MaxEnergy,
                defaultStance.StanceId,
                ultimate.SkillId,
                ultimate.EnergyCost,
                player.HitInvulnSec);
        }

        private static void RequireFiniteNonNegative(
            string rowId,
            string field,
            double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw Invalid(rowId, field, value);
            }
        }

        private static ArgumentOutOfRangeException Invalid(
            string rowId,
            string field,
            object value)
        {
            return new ArgumentOutOfRangeException(
                field,
                value,
                $"Configured value for '{rowId}.{field}' is outside the supported player combat range.");
        }
    }
}
