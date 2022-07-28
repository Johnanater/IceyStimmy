using System;
using IceyStimmy.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API.Ioc;
using OpenMod.Core.Helpers;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;
using SDG.Unturned;

namespace IceyStimmy.Helpers
{
    [Service]
    public interface IUtils
    {
        void RewardPlayer(Player player, RewardType rewardType);
        void PunishPlayer(Player player);
        decimal GetRewardAmount(RewardType rewardType);
        string GetRewardChatMessage(decimal amount, RewardType rewardType);
    }
    
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Scoped)]
    [UsedImplicitly]
    public class Utils : IUtils
    {
        private readonly IChatUtils m_ChatUtils;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IEconomyProvider m_EconomyProvider;

        public Utils(
            IChatUtils chatUtils,
            IStringLocalizer stringLocalizer,
            IEconomyProvider economyProvider)
        {
            m_ChatUtils = chatUtils;
            m_StringLocalizer = stringLocalizer;
            m_EconomyProvider = economyProvider;
        }
        public void RewardPlayer(Player player, RewardType rewardType)
        {
            var steam64 = player.channel.owner.playerID.steamID.ToString();
            var amount = GetRewardAmount(rewardType);
            var config = IceyStimmy.Config;
            var rewardSystem = config.RewardSystem;

            switch (rewardSystem)
            {
                case RewardSystem.Money:
                    m_EconomyProvider.UpdateBalanceAsync(steam64, KnownActorTypes.Player, amount, $"IceyStimmy {rewardType} Reward");
                    break;
                case RewardSystem.Experience:
                    player.skills.ServerSetExperience(player.skills.experience + (uint) amount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rewardSystem), rewardSystem, "Invalid RewardSystem");
            }

            var message = GetRewardChatMessage(amount, rewardType);

            AsyncHelper.Schedule("IceyStimmy reward chat message", () => m_ChatUtils.TellAsync(player.channel.owner, message, ChatUtils.ChatColor));
        }

        public void PunishPlayer(Player player)
        {
            var steam64 = player.channel.owner.playerID.steamID.ToString();
            var config = IceyStimmy.Config;
            var amount = -config.DeathAmount;
            var rewardSystem = config.RewardSystem;
            
            switch (rewardSystem)
            {
                case RewardSystem.Money:
                    m_EconomyProvider.UpdateBalanceAsync(steam64, KnownActorTypes.Player, amount, "IceyStimmy Punish");
                    break;
                case RewardSystem.Experience:
                    player.skills.ServerSetExperience(player.skills.experience + (uint) amount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rewardSystem), rewardSystem, "Invalid RewardSystem");
            }
            
            var message = m_StringLocalizer["Punishments:Death", new { Amount = -amount }]; // use negative amount to make chat appear correctly

            AsyncHelper.Schedule("IceyStimmy punish chat message", () => m_ChatUtils.TellAsync(player.channel.owner, message, ChatUtils.ChatColor));
        }

        public decimal GetRewardAmount(RewardType rewardType)
        {
            var config = IceyStimmy.Config;

            return rewardType switch
            {
                RewardType.PlayTime => config.PlayTime,
                RewardType.PlayerKill => config.PlayerKill,
                RewardType.ZombieKill => config.ZombieKill,
                RewardType.AnimalKill => config.AnimalKill,
                _ => throw new ArgumentOutOfRangeException(nameof(rewardType), rewardType, "Invalid RewardType")
            };
        }

        public string GetRewardChatMessage(decimal amount, RewardType rewardType)
        {
            var str = $"Rewards:{rewardType}";

            return rewardType switch
            {
                RewardType.PlayTime => m_StringLocalizer[str, new { Amount = amount, Time = IceyStimmy.Config.PlayTime}],
                RewardType.PlayerKill => m_StringLocalizer[str, new { Amount = amount}],
                RewardType.ZombieKill => m_StringLocalizer[str, new { Amount = amount}],
                RewardType.AnimalKill => m_StringLocalizer[str, new { Amount = amount}],
                _ => throw new ArgumentOutOfRangeException(nameof(rewardType), rewardType, null)
            };
        }
    }
}
