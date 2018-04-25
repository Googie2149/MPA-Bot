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

namespace MPA_Bot
{
    public class Standard : MPAModule
    {
        private EventStorage events;
        private Config config;

        public Standard(EventStorage _events, Config _config)
        {
            events = _events;
            config = _config;
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
                Environment.Exit(0);
            });
        }
    }
}
