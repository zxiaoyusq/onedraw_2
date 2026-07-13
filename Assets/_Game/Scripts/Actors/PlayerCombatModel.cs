using System;
using OneStrokeDemon.Combat;

namespace OneStrokeDemon.Actors
{
    public readonly struct PlayerCombatSnapshot
    {
        internal PlayerCombatSnapshot(
            string playerId,
            long currentHp,
            long maximumHp,
            long currentEnergy,
            long maximumEnergy,
            in StanceSnapshot stance)
        {
            PlayerId = playerId;
            CurrentHp = currentHp;
            MaximumHp = maximumHp;
            CurrentEnergy = currentEnergy;
            MaximumEnergy = maximumEnergy;
            Stance = stance;
            IsInitialized = true;
        }

        public string PlayerId { get; }

        public long CurrentHp { get; }

        public long MaximumHp { get; }

        public long CurrentEnergy { get; }

        public long MaximumEnergy { get; }

        public StanceSnapshot Stance { get; }

        public string StanceId => Stance.StanceId;

        public bool IsDead => CurrentHp == 0;

        public bool IsInitialized { get; }
    }

    public enum PlayerDamageStatus
    {
        None = 0,
        Applied = 1,
        NoDamage = 2,
        Invulnerable = 3,
        AlreadyDead = 4
    }

    public readonly struct PlayerDamageResult
    {
        internal PlayerDamageResult(
            PlayerDamageStatus status,
            long requestedDamage,
            long appliedDamage,
            double timestamp,
            double invulnerableUntil,
            bool deathTriggered,
            in PlayerCombatSnapshot state)
        {
            Status = status;
            RequestedDamage = requestedDamage;
            AppliedDamage = appliedDamage;
            Timestamp = timestamp;
            InvulnerableUntil = invulnerableUntil;
            DeathTriggered = deathTriggered;
            State = state;
            IsValid = true;
        }

        public PlayerDamageStatus Status { get; }

        public long RequestedDamage { get; }

        public long AppliedDamage { get; }

        public double Timestamp { get; }

        public double InvulnerableUntil { get; }

        public bool DeathTriggered { get; }

        public PlayerCombatSnapshot State { get; }

        public bool IsValid { get; }

        public bool ChangedHp => AppliedDamage > 0;
    }

    public enum PlayerHealStatus
    {
        None = 0,
        Applied = 1,
        NoHealing = 2,
        AtMaximum = 3,
        AlreadyDead = 4
    }

    public readonly struct PlayerHealResult
    {
        internal PlayerHealResult(
            PlayerHealStatus status,
            long requestedHealing,
            long appliedHealing,
            in PlayerCombatSnapshot state)
        {
            Status = status;
            RequestedHealing = requestedHealing;
            AppliedHealing = appliedHealing;
            State = state;
            IsValid = true;
        }

        public PlayerHealStatus Status { get; }

        public long RequestedHealing { get; }

        public long AppliedHealing { get; }

        public PlayerCombatSnapshot State { get; }

        public bool IsValid { get; }

        public bool ChangedHp => AppliedHealing > 0;
    }

    public enum PlayerEnergyStatus
    {
        None = 0,
        Gained = 1,
        Spent = 2,
        NoChange = 3,
        AtCapacity = 4,
        InsufficientEnergy = 5,
        AlreadyDead = 6
    }

    public readonly struct PlayerEnergyResult
    {
        internal PlayerEnergyResult(
            PlayerEnergyStatus status,
            long requestedAmount,
            long appliedAmount,
            in PlayerCombatSnapshot state)
        {
            Status = status;
            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
            State = state;
            IsValid = true;
        }

        public PlayerEnergyStatus Status { get; }

        public long RequestedAmount { get; }

        public long AppliedAmount { get; }

        public PlayerCombatSnapshot State { get; }

        public bool IsValid { get; }

        public bool Succeeded =>
            Status == PlayerEnergyStatus.Gained ||
            Status == PlayerEnergyStatus.Spent ||
            Status == PlayerEnergyStatus.NoChange ||
            Status == PlayerEnergyStatus.AtCapacity;

        public bool ChangedEnergy =>
            Status == PlayerEnergyStatus.Gained ||
            Status == PlayerEnergyStatus.Spent;
    }

    public sealed class PlayerCombatModel
    {
        private readonly PlayerCombatSettings settings;
        private readonly StanceService stanceService;
        private long currentHp;
        private long currentEnergy;
        private double invulnerableUntil;
        private double lastDamageTimestamp;
        private bool hasDamageTimestamp;

        public PlayerCombatModel(
            in PlayerCombatSettings configuredSettings,
            StanceService configuredStanceService)
        {
            if (!configuredSettings.IsConfigured)
            {
                throw new ArgumentException(
                    "Player combat settings must be configured.",
                    nameof(configuredSettings));
            }

            stanceService = configuredStanceService ??
                throw new ArgumentNullException(nameof(configuredStanceService));
            if (!string.Equals(
                    configuredSettings.DefaultStanceId,
                    stanceService.Current.StanceId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Stance service must start in the configured player default stance.",
                    nameof(configuredStanceService));
            }

            settings = configuredSettings;
            currentHp = settings.MaximumHp;
            currentEnergy = 0L;
            invulnerableUntil = 0d;
        }

        public PlayerCombatSettings Settings => settings;

        public StanceService Stances => stanceService;

        public PlayerCombatSnapshot Current => new PlayerCombatSnapshot(
            settings.PlayerId,
            currentHp,
            settings.MaximumHp,
            currentEnergy,
            settings.MaximumEnergy,
            stanceService.Current);

        public PlayerDamageResult ApplyDamage(long damage, double timestamp)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damage),
                    "Player damage must be non-negative.");
            }

            ValidateDamageTimestamp(timestamp);
            ObserveDamageTimestamp(timestamp);
            if (currentHp == 0)
            {
                return DamageResult(
                    PlayerDamageStatus.AlreadyDead,
                    damage,
                    0L,
                    timestamp,
                    deathTriggered: false);
            }

            if (damage == 0)
            {
                return DamageResult(
                    PlayerDamageStatus.NoDamage,
                    damage,
                    0L,
                    timestamp,
                    deathTriggered: false);
            }

            if (timestamp < invulnerableUntil)
            {
                return DamageResult(
                    PlayerDamageStatus.Invulnerable,
                    damage,
                    0L,
                    timestamp,
                    deathTriggered: false);
            }

            long applied = Math.Min(damage, currentHp);
            long nextHp = currentHp - applied;
            double nextInvulnerableUntil = timestamp + settings.HitInvulnerabilitySeconds;
            if (double.IsInfinity(nextInvulnerableUntil) || double.IsNaN(nextInvulnerableUntil))
            {
                throw new OverflowException(
                    "Configured hit invulnerability exceeds timestamp capacity.");
            }

            bool deathTriggered = currentHp > 0 && nextHp == 0;
            currentHp = nextHp;
            invulnerableUntil = nextInvulnerableUntil;
            return DamageResult(
                PlayerDamageStatus.Applied,
                damage,
                applied,
                timestamp,
                deathTriggered);
        }

        public PlayerEnergyResult GainEnergy(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Player energy gain must be non-negative.");
            }

            if (currentHp == 0)
            {
                return EnergyResult(PlayerEnergyStatus.AlreadyDead, amount, 0L);
            }

            if (amount == 0)
            {
                return EnergyResult(PlayerEnergyStatus.NoChange, amount, 0L);
            }

            long remainingCapacity = settings.MaximumEnergy - currentEnergy;
            if (remainingCapacity == 0)
            {
                return EnergyResult(PlayerEnergyStatus.AtCapacity, amount, 0L);
            }

            long applied = Math.Min(amount, remainingCapacity);
            currentEnergy += applied;
            return EnergyResult(PlayerEnergyStatus.Gained, amount, applied);
        }

        public PlayerHealResult Heal(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Player healing must be non-negative.");
            }

            if (currentHp == 0)
            {
                return HealResult(PlayerHealStatus.AlreadyDead, amount, 0L);
            }

            if (amount == 0)
            {
                return HealResult(PlayerHealStatus.NoHealing, amount, 0L);
            }

            long missingHp = settings.MaximumHp - currentHp;
            if (missingHp == 0)
            {
                return HealResult(PlayerHealStatus.AtMaximum, amount, 0L);
            }

            long applied = Math.Min(amount, missingHp);
            currentHp += applied;
            return HealResult(PlayerHealStatus.Applied, amount, applied);
        }

        public PlayerEnergyResult GainEnergy(in DamageResult damageResult)
        {
            if (!damageResult.IsResolved)
            {
                throw new ArgumentException(
                    "Only resolved damage can grant player energy.",
                    nameof(damageResult));
            }

            return GainEnergy(damageResult.EnergyAward);
        }

        public PlayerEnergyResult TrySpendEnergy(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Player energy cost must be non-negative.");
            }

            if (currentHp == 0)
            {
                return EnergyResult(PlayerEnergyStatus.AlreadyDead, amount, 0L);
            }

            if (amount > currentEnergy)
            {
                return EnergyResult(
                    PlayerEnergyStatus.InsufficientEnergy,
                    amount,
                    0L);
            }

            if (amount == 0)
            {
                return EnergyResult(PlayerEnergyStatus.NoChange, amount, 0L);
            }

            currentEnergy -= amount;
            return EnergyResult(PlayerEnergyStatus.Spent, amount, amount);
        }

        public StanceSwitchResult TrySwitchStance(string stanceId, double timestamp)
        {
            if (currentHp == 0)
            {
                return stanceService.RejectBecausePlayerDead(stanceId, timestamp);
            }

            return stanceService.TrySwitch(stanceId, timestamp);
        }

        private PlayerDamageResult DamageResult(
            PlayerDamageStatus status,
            long requested,
            long applied,
            double timestamp,
            bool deathTriggered)
        {
            return new PlayerDamageResult(
                status,
                requested,
                applied,
                timestamp,
                invulnerableUntil,
                deathTriggered,
                Current);
        }

        private PlayerEnergyResult EnergyResult(
            PlayerEnergyStatus status,
            long requested,
            long applied)
        {
            return new PlayerEnergyResult(status, requested, applied, Current);
        }

        private PlayerHealResult HealResult(
            PlayerHealStatus status,
            long requested,
            long applied)
        {
            return new PlayerHealResult(status, requested, applied, Current);
        }

        private void ValidateDamageTimestamp(double timestamp)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    "Damage timestamp must be finite and non-negative.");
            }

            if (hasDamageTimestamp && timestamp < lastDamageTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    $"Damage timestamp cannot move backwards from {lastDamageTimestamp}.");
            }
        }

        private void ObserveDamageTimestamp(double timestamp)
        {
            lastDamageTimestamp = timestamp;
            hasDamageTimestamp = true;
        }
    }
}
