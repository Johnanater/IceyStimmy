extern alias JetBrainsAnnotations;
using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrainsAnnotations::JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Commands;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.Core.Eventing;
using OpenMod.Core.Events;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace IceyStimmy.Helpers
{
    [Service]
    public interface IChatUtils
    {
        void Announce(string message, Color color, bool shouldLog = true);
        void Tell(SteamPlayer steamPlayer, string message, Color color);
        void Tell(SteamPlayer steamPlayer, string message);
        void Tell(CSteamID steamId, string message, Color color);
        void Tell(ICommandActor actor, string message, Color color);
        Task TellAsync(SteamPlayer steamPlayer, string message, Color color);
        Task TellAsync(SteamPlayer steamPlayer, string message);
        Task TellAsync(ICommandActor actor, string message, Color color);
        Color ParseColor(string str);
    }
    
    [ServiceImplementation(Lifetime = ServiceLifetime.Scoped)]
    [UsedImplicitly]
    public class ChatUtils : IChatUtils, IEventListener<OpenModInitializedEvent>
    {
        private readonly ILogger<ChatUtils> m_Logger;

        public static Color ChatColor { get; private set; }
        public static Color ErrorChatColor { get; private set; }

        public ChatUtils(ILogger<ChatUtils> logger)
        {
            m_Logger = logger;
        }

        public void Announce(string message, Color color, bool shouldLog = true)
        {
            var config = IceyStimmy.Config;
            
            if (string.IsNullOrEmpty(message))
                return;
                    
            if (shouldLog)
                m_Logger.LogInformation(message);
            
            ChatManager.serverSendMessage(message, color, iconURL: config?.ChatConfig.ChatMessageIconUrl, useRichTextFormatting: config?.ChatConfig.ChatUseRichText ?? false);
        }
        
        public void Tell(SteamPlayer steamPlayer, string message, Color color)
        {
            var config = IceyStimmy.Config;
            
            if (string.IsNullOrEmpty(message))
                return;
            
            ChatManager.serverSendMessage(message, color, iconURL: config?.ChatConfig.ChatMessageIconUrl, toPlayer: steamPlayer, useRichTextFormatting: config?.ChatConfig.ChatUseRichText ?? false);
        }

        public void Tell(SteamPlayer steamPlayer, string message)
        {
            Tell(steamPlayer, message, ChatColor);
        }

        public void Tell(CSteamID steamId, string message, Color color)
        {
            var steamPlayer = PlayerTool.getSteamPlayer(steamId);

            if (steamPlayer == null)
                throw new Exception($"Failed to send message ({message}) to player ({steamId})! Failed to get player from CSteamID!");

            Tell(steamPlayer, message, color);
        }

        public void Tell(ICommandActor actor, string message, Color color)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (actor is UnturnedUser untUser)
                Tell(untUser.Player.SteamPlayer, message, color);
            else
                actor.PrintMessageAsync(message, System.Drawing.Color.FromArgb((int) color.a, (int) color.r, (int) color.g, (int) color.b));
        }

        public async Task TellAsync(SteamPlayer steamPlayer, string message, Color color)
        {
            if (!Thread.CurrentThread.IsGameThread())
                await UniTask.SwitchToMainThread();
                
            Tell(steamPlayer, message, color);
        }

        public async Task TellAsync(SteamPlayer steamPlayer, string message)
        {
            await TellAsync(steamPlayer, message, ChatColor);
        }
        
        public async Task TellAsync(ICommandActor actor, string message, Color color)
        {
            if (!Thread.CurrentThread.IsGameThread())
                await UniTask.SwitchToMainThread();
                
            Tell(actor, message, color);
        }
        
        // Parse color from as an html color
        public Color ParseColor(string str)
        {
            if (ColorUtility.TryParseHtmlString(str, out var color))
                return color;

            m_Logger.LogInformation($"ERROR: Cannot parse color '{str}', please fix this in your config!");
            m_Logger.LogInformation("You can find out how to specify colors here: https://docs.unity3d.com/ScriptReference/ColorUtility.TryParseHtmlString.html");
            return Color.green;
        }

        [EventListener(IgnoreCancelled = true, Priority = EventListenerPriority.Low)]
        public Task HandleEventAsync(object sender, OpenModInitializedEvent @event)
        {
            var config = IceyStimmy.Config;

            ChatColor = ParseColor(config?.ChatConfig.ChatMessageColor);
            ErrorChatColor = ParseColor(config?.ChatConfig.ErrorChatMessageColor);

            return Task.CompletedTask;
        }
    }
}
