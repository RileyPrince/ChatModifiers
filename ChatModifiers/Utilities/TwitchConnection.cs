﻿using CatCore;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Twitch.Interfaces;
using ChatModifiers.API;
using System;
using System.Collections.Generic;

namespace ChatModifiers.Utilities
{
    internal class TwitchConnection
    {
        internal static Action<TwitchMessage, CustomModifier> OnMessage;
        private static Dictionary<string, DateTime> lastCommandExecuted = new Dictionary<string, DateTime>();

        internal static void Initialize()
        {
            CatCoreInstance instance = CatCoreInstance.Create();
            ITwitchService service = instance.RunTwitchServices();

            service.OnTextMessageReceived += Service_OnTextMessageReceived;
        }

        internal static bool IsAssignableType(Type targetType, string value)
        {
            try
            {
                Convert.ChangeType(value, targetType);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void Service_OnTextMessageReceived(ITwitchService service, TwitchMessage message)
        {
            string chatMessage = message.Message.ToLower();

            if (!chatMessage.StartsWith("!"))
                return;

            if(chatMessage.StartsWith("!cm about"))
            {
                message.Channel.SendMessage(HandleAboutCommand(message));
                return;
            }

            foreach (CustomModifier modifier in RegistrationManager._registeredModifiers)
            {
                if (chatMessage.StartsWith($"!{modifier.CommandKeyword.ToLower()}"))
                {
                    if (lastCommandExecuted.ContainsKey(modifier.CommandKeyword))
                    {
                        DateTime lastExecutedTime = lastCommandExecuted[modifier.CommandKeyword];
                        TimeSpan cooldownDuration = TimeSpan.FromSeconds(modifier.CoolDown);
                        if (DateTime.Now - lastExecutedTime < cooldownDuration)
                        {
                            Plugin.Log.Warn($"Cooldown not elapsed for {modifier.Name}. Remaining cooldown: {cooldownDuration - (DateTime.Now - lastExecutedTime)}");
                            message.Channel.SendMessage($"Cooldown not elapsed for {modifier.Name}. Remaining cooldown: {cooldownDuration - (DateTime.Now - lastExecutedTime)}");
                            return;
                        }
                    }

                    if (modifier.ActiveAreas == Areas.None) return;
                    if (modifier.ActiveAreas != Areas.Both)
                    {
                        if (modifier.ActiveAreas == Areas.Menu && GameState.IsInGame) return;
                        if (modifier.ActiveAreas == Areas.Game && !GameState.IsInGame) return;
                    }

                    Plugin.Log.Info($"Executing Modifier: {modifier.Name}");
                    if (modifier.DisableScoreSubmission)
                    {
                        BS_Utils.Gameplay.ScoreSubmission.DisableSubmission($"ChatModifier: {modifier.Name}");
                    }
                    lastCommandExecuted[modifier.CommandKeyword] = DateTime.Now;

                    string[] commandParts = chatMessage.Split(' ');
                    List<object> arguments = new List<object>();

                    for (int i = 1; i < commandParts.Length; i++)
                    {
                        if (i - 1 >= modifier.Arguments.Length)
                        {
                            Plugin.Log.Warn($"Too many arguments provided for {modifier.Name}");
                            message.Channel.SendMessage($"Too many arguments provided for {modifier.Name}");
                            return;
                        }

                        string argString = commandParts[i];
                        Type argType = modifier.Arguments[i - 1].Type;

                        object argValue = HandleArg(argString, argType);
                        if (argValue == null)
                        {
                            Plugin.Log.Warn($"Invalid argument type for {modifier.Name}. Expected: {argType.Name}");
                            message.Channel.SendMessage($"Invalid argument type for {modifier.Name}. Expected: {argType.Name}");
                            return;
                        }
                        arguments.Add(argValue);
                    }
                    if (arguments.Count != modifier.Arguments.Length)
                    {
                        Plugin.Log.Warn($"Not enough arguments provided for {modifier.Name}");
                        message.Channel.SendMessage($"Not enough arguments provided for {modifier.Name}");
                        return;
                    }

                    modifier.Function.Invoke(new MessageInfo(chatMessage, message.Sender.DisplayName, message.Channel.Name, DateTime.Now), arguments.ToArray());
                    OnMessage?.Invoke(message, modifier);
                    break;
                }
            }
        }

        internal static object HandleArg(string argString, Type argType)
        {
            if (argType.IsEnum)
            {
                Plugin.Log.Info($"Enum detected: {argType.Name}");
                string[] strings = argType.GetEnumNames();
                string matched = Array.Find(strings, name => string.Equals(name, argString, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    object enumValue = Enum.Parse(argType, matched);
                    return enumValue;
                }
                else
                {
                    return null;
                }
            }
            else if (IsAssignableType(argType, argString))
            {
                object argValue = Convert.ChangeType(argString, argType);
                return argValue;
            }
            else
            {
                return null;
            }
        }

        internal static string HandleAboutCommand(TwitchMessage message)
        {
            string message1 = "ChatModifers v0.0.1 ";
            message1 += "\n| Created by: Speecil and Nuggo ";
            message1 += "\n\n| Available Commands: ";
            message1 += "\n!about - Displays information about ChatModifiers ";
            foreach (CustomModifier modifier in RegistrationManager._registeredModifiers)
            {
                message1 += $"\n || !{modifier.CommandKeyword} - {modifier.Description}";
            }
            return message1;
        }
    }
}
