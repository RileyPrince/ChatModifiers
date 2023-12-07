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

            foreach (CustomModifier modifier in RegistrationManager._registeredModifiers)
            {
                if (chatMessage.StartsWith($"!{modifier.CommandKeyword.ToLower()}"))
                {
                    if (modifier.ActiveAreas == Areas.Menu && GameCoreUtils.IsInGame) return;
                    if (modifier.ActiveAreas == Areas.Game && !GameCoreUtils.IsInGame) return;
                    if (modifier.ActiveAreas == Areas.None) return;
                    Plugin.Log.Info($"Executing Modifier: {modifier.Name}");

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

                        if (!IsAssignableType(argType, argString))
                        {
                            Plugin.Log.Warn($"Invalid argument type for {modifier.Name}. Expected: {argType.Name}");
                            message.Channel.SendMessage($"Invalid argument type for {modifier.Name}. Expected: {argType.Name}");
                            return;
                        }

                        object argValue = Convert.ChangeType(argString, argType);
                        arguments.Add(argValue);
                    }

                    if (arguments.Count != modifier.Arguments.Length)
                    {
                        Plugin.Log.Warn($"Not enough arguments provided for {modifier.Name}");
                        message.Channel.SendMessage($"Not enough arguments provided for {modifier.Name}");
                        return;
                    }

                    modifier.Function.Invoke(new MessageInfo(chatMessage, message.Sender.UserName, message.Channel.Name, DateTime.Now), arguments.ToArray());
                    OnMessage?.Invoke(message, modifier);
                    break;
                }
            }
        }

    }
}
