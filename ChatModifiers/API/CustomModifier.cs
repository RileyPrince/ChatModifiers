﻿using System;

namespace ChatModifiers.API
{
    public class ArgumentInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }

        public ArgumentInfo(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }

    public class MessageInfo
    {
        public string Message { get; set; }
        public string Author { get; set; }
        public string Channel { get; set; }
        public DateTime TimeStamp { get; set; }

        public MessageInfo(string message, string author, string channel, DateTime timeStamp)
        {
            Message = message;
            Author = author;
            Channel = channel;
            TimeStamp = timeStamp;
        }
    }

    public class CustomModifier
    {
        public string Name { get; set; } = "Default";
        public string Description { get; set; } = "Default Description";
        public string Author { get; set; } = "Default Author";
        public string PathToIcon { get; set; } = "Default Path";
        public string CommandKeyword { get; set; } = "default";
        public Action<MessageInfo, object[]> Function { get; set; }
        public ArgumentInfo[] Arguments { get; set; }

        public CustomModifier(string name, string description, string author, string pathToIcon, string commandKeyword, Action<MessageInfo, object[]> function, ArgumentInfo[] arguments)
        {
            Name = name;
            Description = description;
            Author = author;
            PathToIcon = pathToIcon;
            CommandKeyword = commandKeyword;
            Function = function;
            Arguments = arguments;
        }
    }
}