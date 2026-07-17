using System;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Actors
{
    // 定义 PlayerCombatSettings 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public readonly struct PlayerCombatSettings
    {
        // 初始化 PlayerCombatSettings，并建立角色运行时所需的初始状态。
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

    // 定义 PlayerCombatSettingsFactory 的角色领域数据与行为边界，供上层流程以明确契约使用。
    public static class PlayerCombatSettingsFactory
    {
        // 创建 Create 对应的角色逻辑，并返回或发布一致的状态结果。
        public static PlayerCombatSettings Create(
            IConfigProvider configProvider,
            string playerId)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException("Player id must be non-empty.", nameof(playerId));
            }

            PlayerConfig player = configProvider.GetPlayer(playerId);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (player.MaxHp <= 0)
            {
                throw Invalid(player.PlayerId, nameof(player.MaxHp), player.MaxHp);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (player.MaxEnergy < 0)
            {
                throw Invalid(player.PlayerId, nameof(player.MaxEnergy), player.MaxEnergy);
            }

            RequireFiniteNonNegative(
                player.PlayerId,
                nameof(player.HitInvulnSec),
                player.HitInvulnSec);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(player.DefaultStanceId))
            {
                throw new ArgumentException(
                    $"Player '{player.PlayerId}' must configure a default stance.",
                    nameof(playerId));
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (string.IsNullOrWhiteSpace(player.UltimateSkillId))
            {
                throw new ArgumentException(
                    $"Player '{player.PlayerId}' must configure an ultimate skill.",
                    nameof(playerId));
            }

            StanceSnapshot defaultStance = StanceSnapshot.FromConfig(
                configProvider.GetStance(player.DefaultStanceId));
            SkillConfig ultimate = configProvider.GetSkill(player.UltimateSkillId);
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (ultimate.EnergyCost < 0)
            {
                throw Invalid(ultimate.SkillId, nameof(ultimate.EnergyCost), ultimate.EnergyCost);
            }

            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
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

        // 处理 RequireFiniteNonNegative 对应的角色逻辑，并返回或发布一致的状态结果。
        private static void RequireFiniteNonNegative(
            string rowId,
            string field,
            double value)
        {
            // 检查当前条件并处理对应边界，避免角色状态沿错误路径继续推进。
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw Invalid(rowId, field, value);
            }
        }

        // 处理 Invalid 对应的角色逻辑，并返回或发布一致的状态结果。
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
