using System;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using UnityEngine;

namespace OneStrokeDemon.Actors
{
    public enum PlayerCombatEventType
    {
        None = 0,
        HpChanged = 1,
        EnergyChanged = 2,
        StanceChanged = 3,
        Died = 4
    }

    public readonly struct PlayerCombatEvent
    {
        internal PlayerCombatEvent(
            ulong sequence,
            PlayerCombatEventType eventType,
            in PlayerCombatSnapshot state,
            string sourceId,
            long signedAmount,
            string previousStanceId,
            string currentStanceId,
            string effectGroupId,
            double timestamp)
        {
            Sequence = sequence;
            EventType = eventType;
            State = state;
            SourceId = sourceId;
            SignedAmount = signedAmount;
            PreviousStanceId = previousStanceId;
            CurrentStanceId = currentStanceId;
            EffectGroupId = effectGroupId;
            Timestamp = timestamp;
            IsValid = true;
        }

        public ulong Sequence { get; }

        public PlayerCombatEventType EventType { get; }

        public PlayerCombatSnapshot State { get; }

        public string SourceId { get; }

        public long SignedAmount { get; }

        public string PreviousStanceId { get; }

        public string CurrentStanceId { get; }

        public string EffectGroupId { get; }

        public double Timestamp { get; }

        public bool IsValid { get; }
    }

    public enum SkillEnergySpendStatus
    {
        None = 0,
        Spent = 1,
        WrongStance = 2,
        InsufficientEnergy = 3,
        PlayerDead = 4
    }

    public readonly struct SkillEnergySpendResult
    {
        internal SkillEnergySpendResult(
            SkillEnergySpendStatus status,
            string skillId,
            string requiredStanceId,
            long configuredEnergyCost,
            in PlayerEnergyResult energyResult)
        {
            Status = status;
            SkillId = skillId;
            RequiredStanceId = requiredStanceId;
            ConfiguredEnergyCost = configuredEnergyCost;
            EnergyResult = energyResult;
            IsValid = true;
        }

        public SkillEnergySpendStatus Status { get; }

        public string SkillId { get; }

        public string RequiredStanceId { get; }

        public long ConfiguredEnergyCost { get; }

        public PlayerEnergyResult EnergyResult { get; }

        public bool IsValid { get; }

        public bool Succeeded => Status == SkillEnergySpendStatus.Spent;
    }

    [DisallowMultipleComponent]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        private IConfigProvider configProvider;
        private PlayerCombatModel model;
        private ulong nextEventSequence = 1;

        public event Action<PlayerCombatEvent> CombatEventPublished;

        public bool IsInitialized => model != null;

        public PlayerCombatModel Model => model ??
            throw new InvalidOperationException("Player combat controller is not initialized.");

        public PlayerCombatSnapshot Current => Model.Current;

        public void Initialize(IConfigProvider configuredProvider, string playerId)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException(
                    "Player combat controller cannot be initialized more than once.");
            }

            configProvider = configuredProvider ??
                throw new ArgumentNullException(nameof(configuredProvider));
            PlayerCombatSettings settings = PlayerCombatSettingsFactory.Create(
                configProvider,
                playerId);
            var stances = new StanceService(configProvider, settings.DefaultStanceId);
            model = new PlayerCombatModel(settings, stances);
        }

        public PlayerDamageResult ApplyDamage(
            long damage,
            double timestamp,
            string sourceId = "")
        {
            PlayerDamageResult result = Model.ApplyDamage(damage, timestamp);
            if (!result.ChangedHp)
            {
                return result;
            }

            Publish(
                PlayerCombatEventType.HpChanged,
                result.State,
                sourceId,
                -result.AppliedDamage,
                string.Empty,
                result.State.StanceId,
                string.Empty,
                timestamp);
            if (result.DeathTriggered)
            {
                Publish(
                    PlayerCombatEventType.Died,
                    result.State,
                    sourceId,
                    0L,
                    string.Empty,
                    result.State.StanceId,
                    string.Empty,
                    timestamp);
            }

            return result;
        }

        public PlayerEnergyResult GainEnergy(
            in DamageResult damageResult,
            double timestamp)
        {
            ValidateTimestamp(timestamp);
            PlayerEnergyResult result = Model.GainEnergy(damageResult);
            if (result.ChangedEnergy)
            {
                Publish(
                    PlayerCombatEventType.EnergyChanged,
                    result.State,
                    damageResult.FormulaId,
                    result.AppliedAmount,
                    string.Empty,
                    result.State.StanceId,
                    string.Empty,
                    timestamp);
            }

            return result;
        }

        public PlayerHealResult Heal(
            long amount,
            double timestamp,
            string sourceId = "")
        {
            ValidateTimestamp(timestamp);
            PlayerHealResult result = Model.Heal(amount);
            if (result.ChangedHp)
            {
                Publish(
                    PlayerCombatEventType.HpChanged,
                    result.State,
                    sourceId,
                    result.AppliedHealing,
                    string.Empty,
                    result.State.StanceId,
                    string.Empty,
                    timestamp);
            }

            return result;
        }

        public PlayerEnergyResult GainEnergy(
            long amount,
            double timestamp,
            string sourceId = "")
        {
            ValidateTimestamp(timestamp);
            PlayerEnergyResult result = Model.GainEnergy(amount);
            if (result.ChangedEnergy)
            {
                Publish(
                    PlayerCombatEventType.EnergyChanged,
                    result.State,
                    sourceId,
                    result.AppliedAmount,
                    string.Empty,
                    result.State.StanceId,
                    string.Empty,
                    timestamp);
            }

            return result;
        }

        public SkillEnergySpendResult TrySpendSkillEnergy(
            string skillId,
            double timestamp)
        {
            ValidateTimestamp(timestamp);
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentException("Skill id must be non-empty.", nameof(skillId));
            }

            SkillConfig skill = configProvider.GetSkill(skillId);
            if (skill.EnergyCost < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(skillId),
                    skill.EnergyCost,
                    $"Skill '{skill.SkillId}' energy cost must be non-negative.");
            }

            if (Current.IsDead)
            {
                return new SkillEnergySpendResult(
                    SkillEnergySpendStatus.PlayerDead,
                    skill.SkillId,
                    skill.RequiredStanceId,
                    skill.EnergyCost,
                    default);
            }

            if (!string.IsNullOrEmpty(skill.RequiredStanceId) &&
                !string.Equals(
                    skill.RequiredStanceId,
                    Current.StanceId,
                    StringComparison.Ordinal))
            {
                return new SkillEnergySpendResult(
                    SkillEnergySpendStatus.WrongStance,
                    skill.SkillId,
                    skill.RequiredStanceId,
                    skill.EnergyCost,
                    default);
            }

            PlayerEnergyResult energyResult = Model.TrySpendEnergy(skill.EnergyCost);
            SkillEnergySpendStatus status;
            switch (energyResult.Status)
            {
                case PlayerEnergyStatus.Spent:
                case PlayerEnergyStatus.NoChange:
                    status = SkillEnergySpendStatus.Spent;
                    break;
                case PlayerEnergyStatus.InsufficientEnergy:
                    status = SkillEnergySpendStatus.InsufficientEnergy;
                    break;
                case PlayerEnergyStatus.AlreadyDead:
                    status = SkillEnergySpendStatus.PlayerDead;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unexpected player energy spend status '{energyResult.Status}'.");
            }

            var result = new SkillEnergySpendResult(
                status,
                skill.SkillId,
                skill.RequiredStanceId,
                skill.EnergyCost,
                energyResult);
            if (result.Succeeded && energyResult.ChangedEnergy)
            {
                Publish(
                    PlayerCombatEventType.EnergyChanged,
                    energyResult.State,
                    skill.SkillId,
                    -energyResult.AppliedAmount,
                    string.Empty,
                    energyResult.State.StanceId,
                    string.Empty,
                    timestamp);
            }

            return result;
        }

        public SkillEnergySpendResult TrySpendUltimateEnergy(double timestamp)
        {
            return TrySpendSkillEnergy(Model.Settings.UltimateSkillId, timestamp);
        }

        public StanceSwitchResult TrySwitchStance(string stanceId, double timestamp)
        {
            StanceSwitchResult result = Model.TrySwitchStance(stanceId, timestamp);
            if (result.DidSwitch)
            {
                Publish(
                    PlayerCombatEventType.StanceChanged,
                    Current,
                    result.Current.StanceId,
                    0L,
                    result.Previous.StanceId,
                    result.Current.StanceId,
                    result.OnSwitchEffectGroupId,
                    timestamp);
            }

            return result;
        }

        public DamageRuleSet CreateDamageRules(
            string defenseRuleId,
            string weakpointRuleId)
        {
            return DamageRuleSetFactory.Create(
                configProvider,
                Current.StanceId,
                defenseRuleId,
                weakpointRuleId);
        }

        private void Publish(
            PlayerCombatEventType eventType,
            in PlayerCombatSnapshot state,
            string sourceId,
            long signedAmount,
            string previousStanceId,
            string currentStanceId,
            string effectGroupId,
            double timestamp)
        {
            ulong sequence = nextEventSequence;
            if (sequence == 0 || sequence == ulong.MaxValue)
            {
                throw new OverflowException("Player combat event sequence is exhausted.");
            }

            nextEventSequence = sequence + 1;
            CombatEventPublished?.Invoke(new PlayerCombatEvent(
                sequence,
                eventType,
                state,
                sourceId ?? string.Empty,
                signedAmount,
                previousStanceId ?? string.Empty,
                currentStanceId ?? string.Empty,
                effectGroupId ?? string.Empty,
                timestamp));
        }

        private static void ValidateTimestamp(double timestamp)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    "Player combat event timestamp must be finite and non-negative.");
            }
        }
    }
}
