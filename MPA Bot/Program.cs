﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using MPA_Bot.Modules.PSO2;

namespace MPA_Bot
{
    class Program
    {
        static void Main(string[] args) =>
            new Program().RunAsync().GetAwaiter().GetResult();

        private DiscordSocketClient client;
        private Config config;
        private EventStorage events;
        private CommandHandler handler;
        private EmergencyQuestService eqService; 

        private async Task RunAsync()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                //WebSocketProvider = WS4NetProvider.Instance,
                LogLevel = LogSeverity.Verbose
            });
            client.Log += Log;

            config = Config.Load();
            events = EventStorage.Load();

            var map = new ServiceCollection().AddSingleton(client).AddSingleton(config).AddSingleton(events).BuildServiceProvider();

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            eqService = new EmergencyQuestService();
            await eqService.Install(map);

            map = new ServiceCollection().AddSingleton(client).AddSingleton(config).AddSingleton(events).AddSingleton(eqService).BuildServiceProvider();

            SuccessfulConnectionTimer();
            client.Disconnected += SocketClient_Disconnected;


            handler = new CommandHandler();
            await handler.Install(map);

            //await Task.Delay(3000);

            //var avatar = new Image(File.OpenRead(".\\TaranzaSOUL.png"));
            //await client.CurrentUser.ModifyAsync(x => x.Avatar = avatar);

            await Task.Delay(-1);
        }

        private async Task SocketClient_Disconnected(Exception ex)
        {
            // If we disconnect, wait 3 minutes and see if we regained the connection.
            // If we did, great, exit out and continue. If not, check again 3 minutes later
            // just to be safe, and restart to exit a deadlock.
            var task = Task.Run(async () =>
            {
                for (int i = 0; i < 2; i++)
                {
                    await Task.Delay(1000 * 60 * 3);

                    if (client.ConnectionState == ConnectionState.Connected)
                        break;
                    else if (i == 1)
                    {
                        Environment.Exit((int)ExitCodes.ExitCode.DeadlockEscape);
                    }
                }
            });
        }

        private async Task SuccessfulConnectionTimer()
        {
            // Wait 20 minutes, and if we're still running reset the restart counter
            await Task.Delay(1000 * 60 * 20);
            Environment.SetEnvironmentVariable("RESTARTS", "0");
        }

        private Task Log(LogMessage msg)
        {
            //Console.WriteLine(msg.ToString());

            //Color
            ConsoleColor color;
            switch (msg.Severity)
            {
                case LogSeverity.Error: color = ConsoleColor.Red; break;
                case LogSeverity.Warning: color = ConsoleColor.Yellow; break;
                case LogSeverity.Info: color = ConsoleColor.White; break;
                case LogSeverity.Verbose: color = ConsoleColor.Gray; break;
                case LogSeverity.Debug: default: color = ConsoleColor.DarkGray; break;
            }

            //Exception
            string exMessage;
            Exception ex = msg.Exception;
            if (ex != null)
            {
                while (ex is AggregateException && ex.InnerException != null)
                    ex = ex.InnerException;
                exMessage = $"{ex.Message}";
                if (exMessage != "Reconnect failed: HTTP/1.1 503 Service Unavailable")
                    exMessage += $"\n{ex.StackTrace}";
            }
            else
                exMessage = null;

            //Source
            string sourceName = msg.Source?.ToString();

            //Text
            string text;
            if (msg.Message == null)
            {
                text = exMessage ?? "";
                exMessage = null;
            }
            else
                text = msg.Message;

            //if (text.Contains("GUILD_UPDATE: ") && text.Contains("UTC"))
            //    return Task.CompletedTask;
            //else if (text.StartsWith("CHANNEL_UPDATE: "))
            //    return Task.CompletedTask;

            if (sourceName == "Command")
                color = ConsoleColor.Cyan;
            else if (sourceName == "<<Message")
                color = ConsoleColor.Green;
            else if (sourceName == ">>Message")
                return Task.CompletedTask;

            //Build message
            StringBuilder builder = new StringBuilder(text.Length + (sourceName?.Length ?? 0) + (exMessage?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }
            builder.Append($"[{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}] ");
            for (int i = 0; i < text.Length; i++)
            {
                //Strip control chars
                char c = text[i];
                if (c == '\n' || !char.IsControl(c) || c != (char)8226)
                    builder.Append(c);
            }
            if (exMessage != null)
            {
                builder.Append(": ");
                builder.Append(exMessage);
            }

            text = builder.ToString();
            //if (msg.Severity <= LogSeverity.Info)
            //{
                Console.ForegroundColor = color;
                Console.WriteLine(text);
            //}
#if DEBUG
            System.Diagnostics.Debug.WriteLine(text);
#endif



            return Task.CompletedTask;
        }
    }
}
