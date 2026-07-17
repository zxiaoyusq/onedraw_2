using System;
using OneStrokeDemon.Combat;

namespace OneStrokeDemon.Actors
{
    // 定义 PlayerCombatSnapshot 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct PlayerCombatSnapshot
    {
        // 初始化 PlayerCombatSnapshot，并建立角色运行时所需的初始状态。
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

    // 定义 PlayerDamageStatus 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum PlayerDamageStatus
    {
        None = 0,
        Applied = 1,
        NoDamage = 2,
        Invulnerable = 3,
        AlreadyDead = 4
    }

    // 定义 PlayerDamageResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct PlayerDamageResult
    {
        // 初始化 PlayerDamageResult，并建立角色运行时所需的初始状态。
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

    // 定义 PlayerHealStatus 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum PlayerHealStatus
    {
        None = 0,
        Applied = 1,
        NoHealing = 2,
        AtMaximum = 3,
        AlreadyDead = 4
    }

    // 定义 PlayerHealResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct PlayerHealResult
    {
        // 初始化 PlayerHealResult，并建立角色运行时所需的初始状态。
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

    // 定义 PlayerEnergyStatus 的角色领域数据与行为边界，供上层流程以明确契约使用。
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

    // 定义 PlayerEnergyResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct PlayerEnergyResult
    {
        // 初始化 PlayerEnergyResult，并建立角色运行时所需的初始状态。
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

    // 定义 PlayerCombatModel 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public sealed class PlayerCombatModel
    {
        private readonly PlayerCombatSettings settings;
        private readonly StanceService stanceService;
        private long currentHp;
        private long currentEnergy;
        private double invulnerableUntil;
        private double lastDamageTimestamp;
        private bool hasDamageTimestamp;

        // 初始化 PlayerCombatModel，并建立角色运行时所需的初始状态。
        public PlayerCombatModel(
            in PlayerCombatSettings configuredSettings,
            StanceService configuredStanceService)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!configuredSettings.IsConfigured)
            {
                throw new ArgumentException(
                    "Player combat settings must be configured.",
                    nameof(configuredSettings));
            }

            stanceService = configuredStanceService ??
                throw new ArgumentNullException(nameof(configuredStanceService));
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 应用 ApplyDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        public PlayerDamageResult ApplyDamage(long damage, double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damage),
                    "Player damage must be non-negative.");
            }

            ValidateDamageTimestamp(timestamp);
            ObserveDamageTimestamp(timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentHp == 0)
            {
                return DamageResult(
                    PlayerDamageStatus.AlreadyDead,
                    damage,
                    0L,
                    timestamp,
                    deathTriggered: false);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (damage == 0)
            {
                return DamageResult(
                    PlayerDamageStatus.NoDamage,
                    damage,
                    0L,
                    timestamp,
                    deathTriggered: false);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 增加 GainEnergy 对应的角色逻辑，并返回或发布一致的状态结果。
        public PlayerEnergyResult GainEnergy(long amount)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Player energy gain must be non-negative.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentHp == 0)
            {
                return EnergyResult(PlayerEnergyStatus.AlreadyDead, amount, 0L);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount == 0)
            {
                return EnergyResult(PlayerEnergyStatus.NoChange, amount, 0L);
            }

            long remainingCapacity = settings.MaximumEnergy - currentEnergy;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (remainingCapacity == 0)
            {
                return EnergyResult(PlayerEnergyStatus.AtCapacity, amount, 0L);
            }

            long applied = Math.Min(amount, remainingCapacity);
            currentEnergy += applied;
            return EnergyResult(PlayerEnergyStatus.Gained, amount, applied);
        }

        // 恢复 Heal 对应的角色逻辑，并返回或发布一致的状态结果。
        public PlayerHealResult Heal(long amount)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Player healing must be non-negative.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentHp == 0)
            {
                return HealResult(PlayerHealStatus.AlreadyDead, amount, 0L);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount == 0)
            {
                return HealResult(PlayerHealStatus.NoHealing, amount, 0L);
            }

            long missingHp = settings.MaximumHp - currentHp;
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (missingHp == 0)
            {
                return HealResult(PlayerHealStatus.AtMaximum, amount, 0L);
            }

            long applied = Math.Min(amount, missingHp);
            currentHp += applied;
            return HealResult(PlayerHealStatus.Applied, amount, applied);
        }

        // 增加 GainEnergy 对应的角色逻辑，并返回或发布一致的状态结果。
        public PlayerEnergyResult GainEnergy(in DamageResult damageResult)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (!damageResult.IsResolved)
            {
                throw new ArgumentException(
                    "Only resolved damage can grant player energy.",
                    nameof(damageResult));
            }

            return GainEnergy(damageResult.EnergyAward);
        }

        // 尝试执行 TrySpendEnergy 对应的角色逻辑，并返回或发布一致的状态结果。
        public PlayerEnergyResult TrySpendEnergy(long amount)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Player energy cost must be non-negative.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentHp == 0)
            {
                return EnergyResult(PlayerEnergyStatus.AlreadyDead, amount, 0L);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount > currentEnergy)
            {
                return EnergyResult(
                    PlayerEnergyStatus.InsufficientEnergy,
                    amount,
                    0L);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (amount == 0)
            {
                return EnergyResult(PlayerEnergyStatus.NoChange, amount, 0L);
            }

            currentEnergy -= amount;
            return EnergyResult(PlayerEnergyStatus.Spent, amount, amount);
        }

        // 尝试执行 TrySwitchStance 对应的角色逻辑，并返回或发布一致的状态结果。
        public StanceSwitchResult TrySwitchStance(string stanceId, double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (currentHp == 0)
            {
                return stanceService.RejectBecausePlayerDead(stanceId, timestamp);
            }

            return stanceService.TrySwitch(stanceId, timestamp);
        }

        // 处理 DamageResult 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 处理 EnergyResult 对应的角色逻辑，并返回或发布一致的状态结果。
        private PlayerEnergyResult EnergyResult(
            PlayerEnergyStatus status,
            long requested,
            long applied)
        {
            return new PlayerEnergyResult(status, requested, applied, Current);
        }

        // 恢复 HealResult 对应的角色逻辑，并返回或发布一致的状态结果。
        private PlayerHealResult HealResult(
            PlayerHealStatus status,
            long requested,
            long applied)
        {
            return new PlayerHealResult(status, requested, applied, Current);
        }

        // 校验 ValidateDamageTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ValidateDamageTimestamp(double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    "Damage timestamp must be finite and non-negative.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (hasDamageTimestamp && timestamp < lastDamageTimestamp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    timestamp,
                    $"Damage timestamp cannot move backwards from {lastDamageTimestamp}.");
            }
        }

        // 处理 ObserveDamageTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private void ObserveDamageTimestamp(double timestamp)
        {
            lastDamageTimestamp = timestamp;
            hasDamageTimestamp = true;
        }
    }
}
