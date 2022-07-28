using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Cysharp.Threading.Tasks;
using IceyStimmy.Helpers;
using IceyStimmy.Models;
using JetBrains.Annotations;
using OpenMod.Unturned.Plugins;
using OpenMod.API.Plugins;
using OpenMod.Core.Helpers;
using SDG.Unturned;
using Steamworks;

[assembly: PluginMetadata("IceyStimmy",
    DisplayName = "IceyStimmy",
    Author = "Johnanater",
    Website = "https://johnanater.com")]
namespace IceyStimmy
{
    [UsedImplicitly]
    public class IceyStimmy : OpenModUnturnedPlugin
    {
        private readonly IUtils m_Utils;
        private readonly IConfiguration m_Configuration;
        private readonly ILogger<IceyStimmy> m_Logger;

        private bool _pluginRunning;
        private readonly List<CSteamID> _payPlayers = new();

        public static Config Config;

        public IceyStimmy(
            IUtils utils,
            IConfiguration configuration,
            ILogger<IceyStimmy> logger,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Utils = utils;
            m_Configuration = configuration;
            m_Logger = logger;
        }

        protected override async UniTask OnLoadAsync()
        {
            Config = m_Configuration.Get<Config>();

            _pluginRunning = true;

            await UniTask.SwitchToMainThread();
            
            Provider.onEnemyConnected += OnEnemyConnected;
            Provider.onEnemyDisconnected += OnEnemyDisconnected;
            PlayerLife.onPlayerDied += OnPlayerDied;
            DamageTool.damageAnimalRequested += DamageAnimalRequested;
            DamageTool.damageZombieRequested += OnDamageZombieRequested;

            m_Logger.LogInformation($"Successfully loaded {GetType().Name} by Johnanater, version {Version}");
        }

        protected override async UniTask OnUnloadAsync()
        {
            _pluginRunning = false;
            
            await UniTask.SwitchToMainThread();
            
            Provider.onEnemyConnected -= OnEnemyConnected;
            Provider.onEnemyDisconnected -= OnEnemyDisconnected;
            PlayerLife.onPlayerDied -= OnPlayerDied;
            DamageTool.damageAnimalRequested -= DamageAnimalRequested;
            DamageTool.damageZombieRequested -= OnDamageZombieRequested;

            m_Logger.LogInformation($"Successfully unloaded {GetType().Name} by Johnanater, version {Version}");
        }

        private async Task StartPlayTimer(SteamPlayer steamPlayer)
        {
            while (_pluginRunning && _payPlayers.Contains(steamPlayer.playerID.steamID))
            {
                await Task.Delay(Config.TimeMinutes * 1000 * 60);

                if (!_pluginRunning || !_payPlayers.Contains(steamPlayer.playerID.steamID))
                    return;
                
                m_Utils.RewardPlayer(steamPlayer.player, RewardType.PlayTime);
            }
        }
        
        private void OnEnemyConnected(SteamPlayer steamPlayer)
        {
            var playerId = steamPlayer.playerID;
            
            _payPlayers.Add(playerId.steamID);
            AsyncHelper.Schedule($"IceyStimmy StartPlayTimer for {playerId.playerName} ({playerId.steamID.ToString()})", () => StartPlayTimer(steamPlayer));
        }
        
        private void OnEnemyDisconnected(SteamPlayer steamPlayer)
        {
            _payPlayers.RemoveAll(s => s.Equals(steamPlayer.playerID.steamID));
        }
        
        private void OnPlayerDied(PlayerLife playerLife, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            if (instigator == CSteamID.Nil)
                return;

            var killer = PlayerTool.getPlayer(instigator);

            if (killer == null)
                return;

            // if the victim is the killer, no reward
            if (!killer.channel.owner.playerID.steamID.ToString().Equals(playerLife.player.channel.owner.playerID.steamID.ToString()))
                m_Utils.RewardPlayer(killer, RewardType.PlayerKill);

            if (Config.PunishOnDeath) 
                m_Utils.PunishPlayer(playerLife.player);
        }
        
        private void DamageAnimalRequested(ref DamageAnimalParameters parameters, ref bool shouldAllow)
        {
            try
            {
                if (parameters.instigator is not Player killer)
                    return; 
                
                var animal = parameters.animal;
                var health = ushort.Parse(ReflectionUtils.GetInstanceField(animal, "health").ToString());
                var newHealth = health - parameters.damage * parameters.times;
                
                if (newHealth >= 0 )
                    return;
                
                m_Utils.RewardPlayer(killer, RewardType.AnimalKill);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error on DamageAnimalRequested!");
            }
        }
        
        private void OnDamageZombieRequested(ref DamageZombieParameters parameters, ref bool shouldAllow)
        {
            try
            {
                if (parameters.instigator is not Player player)
                    return;
            
                var zombie = parameters.zombie;
                var health = float.Parse(ReflectionUtils.GetInstanceField(zombie, "health").ToString());
                var newHealth = health - parameters.damage * parameters.times;
            
                if (newHealth >= 0 )
                    return;
            
                m_Utils.RewardPlayer(player, RewardType.ZombieKill);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error on OnDamageZombieRequested!");
            }
        }
    }
}
