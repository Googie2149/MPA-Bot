using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using Discord.Addons.EmojiTools;
using Microsoft.Extensions.DependencyInjection;
using MPA_Bot.Modules.PSO2;
//using Minitori.Modules.HelpModule;

namespace MPA_Bot
{
    public class CommandHandler
    {
        private CommandService commands;
        private DiscordSocketClient client;
        //private IDependencyMap map;
        private IServiceProvider services;
        private Config config;
        private EventStorage events;
        private EmergencyQuestService service;

        public async Task Install(IServiceProvider _services)
        {
            // Create Command Service, inject it into Dependency Map
            client = _services.GetService<DiscordSocketClient>();
            commands = new CommandService();
            //_map.Add(commands);
            services = _services;
            config = _services.GetService<Config>();
            events = _services.GetService<EventStorage>();
            service = _services.GetService<EmergencyQuestService>();

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            //await HelpModule.Install(commands);

            client.MessageReceived += HandleCommand;
        }

        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;

            // Mark where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            //if (!(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos))) return;
            if (!ParseTriggers(message, ref argPos)) return;

            // Create a Command Context
            var context = new MPAContext(client, message);
            // Execute the Command, store the result
            var result = await commands.ExecuteAsync(context, argPos, services);
            
            // If the command failed, notify the user
            if (!result.IsSuccess)
            {
                if (result.Error.HasValue)
                {
                    switch (result.Error.Value)
                    {
                        case CommandError.ParseFailed:
                            await message.Channel.SendMessageAsync($"**Something went wrong:** That isn't an actual number!");
                            break;
                        case CommandError.BadArgCount:
                            await message.Channel.SendMessageAsync($"**Something went wrong:** You're missing some parts of that command!");
                            break;
                        case CommandError.UnknownCommand:
                            break;
                        case CommandError.UnmetPrecondition:
                            break;
                        default:
                            await message.Channel.SendMessageAsync($"**Something went wrong:** `{result.ErrorReason}`\n" +
                                $"Poke Googie2149#1368 about it if the reason doesn't make sense.");
                            Console.WriteLine($"{result.ErrorReason}\n{result.Error.Value.ToString()}");
                            break;
                    }
                }

                
            }
        }

        //private readonly IDependencyMap _map;
        //private readonly CommandService _commands;
        //private readonly DiscordSocketClient _client;
        //private readonly Config _config;

        //public CommandHandler(DependencyMap map)
        //{
        //    _map = map;
        //    _client = _map.Get<DiscordSocketClient>();
        //    _client.MessageReceived += ProcessCommandAsync;
        //    _commands = _map.Get<CommandService>();
        //    _config = _map.Get<Config>();
        //}

        //public async Task ConfigureAsync()
        //{
        //    await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        //}

        //private async Task ProcessCommandAsync(SocketMessage pMsg)
        //{
        //    var message = pMsg as SocketUserMessage;
        //    if (message == null) return;
        //    if (message.Content.StartsWith("##")) return;

        //    int argPos = 0;
        //    if (!ParseTriggers(message, ref argPos)) return;

        //    var context = new SocketCommandContext(_client, message);
        //    var result = await _commands.ExecuteAsync(context, argPos, _map);
        //    if (result is SearchResult search && !search.IsSuccess)
        //    {
        //        await message.AddReactionAsync(UnicodeEmoji.FromText(":mag_right:"));
        //    }
        //    else if (result is PreconditionResult precondition && !precondition.IsSuccess)
        //        await message.AddReactionAsync(UnicodeEmoji.FromText(":no_entry:"));
        //    else if (result is ParseResult parse && !parse.IsSuccess)
        //        await message.Channel.SendMessageAsync($"**Parse Error:** {parse.ErrorReason}");
        //    else if (result is TypeReaderResult reader && !reader.IsSuccess)
        //        await message.Channel.SendMessageAsync($"**Read Error:** {reader.ErrorReason}");
        //    else if (result is ExecuteResult execute && !execute.IsSuccess)
        //    {
        //        await message.AddReactionAsync(UnicodeEmoji.FromText(":loudspeaker:"));
        //        await message.Channel.SendMessageAsync($"**Error:** {execute.ErrorReason}");
        //    }
        //    else if (!result.IsSuccess)
        //        await message.AddReactionAsync(UnicodeEmoji.FromText(":rage:"));
        //}

        private bool ParseTriggers(SocketUserMessage message, ref int argPos)
        {
            bool flag = false;
            if (message.HasMentionPrefix(client.CurrentUser, ref argPos)) flag = true;
            else
            {
                foreach (var prefix in config.PrefixList)
                {
                    if (message.HasStringPrefix(prefix, ref argPos))
                    {
                        flag = true;
                        break;
                    }
                }
            };

            return flag;
        }
    }
}
