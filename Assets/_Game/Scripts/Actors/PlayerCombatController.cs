using System;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using UnityEngine;

namespace OneStrokeDemon.Actors
{
    // 定义 PlayerCombatEventType 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum PlayerCombatEventType
    {
        None = 0,
        HpChanged = 1,
        EnergyChanged = 2,
        StanceChanged = 3,
        Died = 4
    }

    // 定义 PlayerCombatEvent 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct PlayerCombatEvent
    {
        // 初始化 PlayerCombatEvent，并建立角色运行时所需的初始状态。
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

    // 定义 SkillEnergySpendStatus 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public enum SkillEnergySpendStatus
    {
        None = 0,
        Spent = 1,
        WrongStance = 2,
        InsufficientEnergy = 3,
        PlayerDead = 4
    }

    // 定义 SkillEnergySpendResult 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct SkillEnergySpendResult
    {
        // 初始化 SkillEnergySpendResult，并建立角色运行时所需的初始状态。
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
    // 定义 PlayerCombatController 的角色领域数据与行为边界，供上层流程以明确契约使用。
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

        // 处理 Initialize 对应的角色逻辑，并返回或发布一致的状态结果。
        public void Initialize(IConfigProvider configuredProvider, string playerId)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 应用 ApplyDamage 对应的角色逻辑，并返回或发布一致的状态结果。
        public PlayerDamageResult ApplyDamage(
            long damage,
            double timestamp,
            string sourceId = "")
        {
            PlayerDamageResult result = Model.ApplyDamage(damage, timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 增加 GainEnergy 对应的角色逻辑，并返回或发布一致的状态结果。
        public PlayerEnergyResult GainEnergy(
            in DamageResult damageResult,
            double timestamp)
        {
            ValidateTimestamp(timestamp);
            PlayerEnergyResult result = Model.GainEnergy(damageResult);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 恢复 Heal 对应的角色逻辑，并返回或发布一致的状态结果。
        public PlayerHealResult Heal(
            long amount,
            double timestamp,
            string sourceId = "")
        {
            ValidateTimestamp(timestamp);
            PlayerHealResult result = Model.Heal(amount);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 增加 GainEnergy 对应的角色逻辑，并返回或发布一致的状态结果。
        public PlayerEnergyResult GainEnergy(
            long amount,
            double timestamp,
            string sourceId = "")
        {
            ValidateTimestamp(timestamp);
            PlayerEnergyResult result = Model.GainEnergy(amount);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 尝试执行 TrySpendSkillEnergy 对应的角色逻辑，并返回或发布一致的状态结果。
        public SkillEnergySpendResult TrySpendSkillEnergy(
            string skillId,
            double timestamp)
        {
            ValidateTimestamp(timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentException("Skill id must be non-empty.", nameof(skillId));
            }

            SkillConfig skill = configProvider.GetSkill(skillId);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (skill.EnergyCost < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(skillId),
                    skill.EnergyCost,
                    $"Skill '{skill.SkillId}' energy cost must be non-negative.");
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (Current.IsDead)
            {
                return new SkillEnergySpendResult(
                    SkillEnergySpendStatus.PlayerDead,
                    skill.SkillId,
                    skill.RequiredStanceId,
                    skill.EnergyCost,
                    default);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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
            // 按当前枚举或状态选择对应的角色行为分支。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 尝试执行 TrySpendUltimateEnergy 对应的角色逻辑，并返回或发布一致的状态结果。
        public SkillEnergySpendResult TrySpendUltimateEnergy(double timestamp)
        {
            return TrySpendSkillEnergy(Model.Settings.UltimateSkillId, timestamp);
        }

        // 尝试执行 TrySwitchStance 对应的角色逻辑，并返回或发布一致的状态结果。
        public StanceSwitchResult TrySwitchStance(string stanceId, double timestamp)
        {
            StanceSwitchResult result = Model.TrySwitchStance(stanceId, timestamp);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 创建 CreateDamageRules 对应的角色逻辑，并返回或发布一致的状态结果。
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

        // 处理 Publish 对应的角色逻辑，并返回或发布一致的状态结果。
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
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 校验 ValidateTimestamp 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void ValidateTimestamp(double timestamp)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timestamp),
                    "Player combat event timestamp must be finite and non-negative.");
            }
        }
    }
}
