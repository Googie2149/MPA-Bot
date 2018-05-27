using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using RestSharp;
using Newtonsoft.Json;
using MPA_Bot.Modules.PSO2;
using Discord.Addons.Preconditions;
using MPA_Bot.Preconditions;

namespace MPA_Bot
{
    public class Standard : MPAModule
    {
        private EventStorage events;
        private Config config;
        private CommandService commands;
        private IServiceProvider services;
        
        public Standard(EventStorage _events, Config _config, CommandService _commands, IServiceProvider _services)
        {
            events = _events;
            config = _config;
            commands = _commands;
            services = _services;
        }

        [Command("help")]
        public async Task HelpCommand()
        {
            Context.IsHelp = true;

            StringBuilder output = new StringBuilder();
            StringBuilder module = new StringBuilder();
            var SeenModules = new List<string>();
            int i = 0;

            output.Append("These are the commands you can use:");

            foreach (var c in commands.Commands)
            {
                if (!SeenModules.Contains(c.Module.Name))
                {
                    if (i > 0)
                        output.Append(module.ToString());

                    module.Clear();

                    module.Append($"\n**{c.Module.Name}:**");
                    SeenModules.Add(c.Module.Name);
                    i = 0;
                }

                if ((await c.CheckPreconditionsAsync(Context, services)).IsSuccess)
                {
                    if (i == 0)
                        module.Append(" ");
                    else
                        module.Append(", ");

                    i++;

                    module.Append($"`{c.Name}`");
                }
            }

            if (i > 0)
                output.AppendLine(module.ToString());

            await ReplyAsync(output.ToString());
        }

        [Command("blah")]
        [Summary("Blah!")]
        [Priority(1000)]
        public async Task Blah()
        {
            await RespondAsync($"Blah to you too, {Context.User.Mention}.");
        }


        [Command("quit")]
        [Priority(1000)]
        [Hide]
        public async Task ShutDown()
        {
            if (Context.User.Id != 102528327251656704)
            {
                await RespondAsync(":no_good::skin-tone-3: You don't have permission to run this command!");
                return;
            }

            events.Save();
            config.Save();

            Task.Run(async () =>
            {
                await ReplyAsync("rip");
                //await Task.Delay(500);
                await Context.Client.LogoutAsync();
                Environment.Exit((int)ExitCodes.ExitCode.Success);
            });
        }

        [Command("update")]
        [Priority(1000)]
        [Hide]
        public async Task UpdateAndRestart()
        {
            Environment.SetEnvironmentVariable("UPDATE", Context.Channel.ToIDString());
            await ReplyAsync("if im not back in 15 minutes, you're legally allowed to delete the server");
            Context.Client.LogoutAsync();
            await Task.Delay(500);
            Environment.Exit((int)ExitCodes.ExitCode.RestartAndUpdate);
        }
    }
}
