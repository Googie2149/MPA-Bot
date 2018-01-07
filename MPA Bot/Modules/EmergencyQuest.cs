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
using System.Net;

namespace MPA_Bot.Modules.PSO2
{
    [Group("alerts")]
    [RequireContext(ContextType.Guild)]
    public class EmergencyQuest : ModuleBase
    {
        private Config config;
        private EmergencyQuestService service;
        
        public EmergencyQuest(EmergencyQuestService _service, Config _config)
        {
            config = _config;
            service = _service;
        }
        
        [Command("add")]
        [Priority(1000)]
        public async Task SetEQ(params int[] numbers)
        {
            if (numbers.Length == 0)
            {
                await ReplyAsync("You have to tell me which ships you want alerts for!");
                return;
            }

            List<int> ships = numbers.Distinct().OrderBy(x => x).ToList();

            foreach (var s in ships)
            {
                if (s < 1 || s > 10)
                {
                    await ReplyAsync($"There is no Ship {s}!");
                    return;
                }
            }

            AddShips((IGuildChannel)Context.Channel, ships);

            StringBuilder output = new StringBuilder();
            output.Append("You will receive Emergency Quest alerts for ");

            if (ships.Count() == 1)
                output.Append($"Ship {ships.First()}");
            else if (ships.Count() > 1)
            {
                output.Append(string.Join(", ", ships.Take(ships.Count() - 1).Select(x => $"Ship {x}")));

                if (ships.Count() > 2)
                    output.Append(",");

                output.Append($" and Ship {ships.Last()}");
            }

            output.Append($" in <#{Context.Channel.Id}>");

            await ReplyAsync(output.ToString());
        }

        [Command("remove")]
        [Priority(1000)]
        public async Task RemoveEQ(params int[] numbers)
        {
            if (numbers.Length == 0)
            {
                await ReplyAsync("You have to tell me which ships you want alerts for!");
                return;
            }

            List<int> ships = numbers.Distinct().OrderBy(x => x).ToList();

            foreach (var s in ships)
            {
                if (s < 1 || s > 10)
                {
                    await ReplyAsync($"There is no Ship {s}!");
                    return;
                }
            }

            if (CheckSettings((IGuildChannel)Context.Channel) == false)
            {
                await ReplyAsync("You weren't listening to any of those ships to begin with!");
                return;
            }

            List<int> skipped = new List<int>();
            List<int> removed = new List<int>();

            foreach (var s in ships)
            {
                if (config.ServerSettings[Context.Guild.Id].ChannelSettings[Context.Channel.Id].Remove(s))
                    removed.Add(s);
                else
                    skipped.Add(s);
            }

            if (removed.Count() == 0)
            {
                await ReplyAsync("You weren't listening to any of those ships to being with!");
                return;
            }
            else
                config.Save();

            StringBuilder output = new StringBuilder();
            output.Append("You will stop receive EQ alerts for ");

            if (removed.Count() == 1)
                output.Append($"Ship {removed.First()}");
            else if (removed.Count() > 1)
            {
                output.Append(string.Join(", ", removed.Take(removed.Count() - 1).Select(x => $"Ship {x}")));

                if (removed.Count() > 2)
                    output.Append(",");

                output.Append($" or Ship {removed.Last()}");
            }

            output.Append($" in <#{Context.Channel.Id}>");

            if (skipped.Count() > 0)
            {
                output.Append("\nYou were never receiving alerts for ");

                if (skipped.Count() == 1)
                    output.Append($"Ship {skipped.First()}");
                else if (skipped.Count() > 1)
                {
                    output.Append(string.Join(", ", skipped.Take(skipped.Count() - 1).Select(x => $"Ship {x}")));

                    if (skipped.Count() > 2)
                        output.Append(",");

                    output.Append($" and Ship {skipped.Last()}");
                }

                output.Append(" to begin with.");
            }

            await ReplyAsync(output.ToString());
        }

        [Command("clear")]
        [Priority(1001)]
        public async Task ClearEQ()
        {
            if (config.ServerSettings[Context.Guild.Id].ChannelSettings[Context.Channel.Id] != null)
            {
                config.ServerSettings[Context.Guild.Id].ChannelSettings[Context.Channel.Id].Clear();
                config.Save();
            }

            await ReplyAsync($"You will stop receiving EQ alerts for any ship in <#{Context.Channel.Id}>");
        }

        [Command("broadcast")]
        public async Task ForceBroadcast()
        {
            Console.WriteLine("Starting forced download");
            var request = (HttpWebRequest)WebRequest.Create("http://pso2.kaze.rip/eq/");
            request.Method = "GET";
            request.AllowReadStreamBuffering = false;

            using (var response = await request.GetResponseAsync())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        var data = JsonConvert.DeserializeObject<List<EqList>>(await reader.ReadToEndAsync());

                        Console.WriteLine("Data deserialized");
                        
                        for (int i = 0; i < data.Count(); i++)
                        {
                            if (data[i].Quests.All(x => x.Ship == 0))
                                Console.WriteLine($"All ships are 0 in event {i}");
                        }

                        if (data.Count() == 0)
                            Console.WriteLine("Data is empty");

                        Console.WriteLine("Forcing broadcast");

                        service.Broadcast(data, true);
                    }

                }
            }
        }

        private bool CheckSettings(IGuildChannel channel)
        {
            if (!config.ServerSettings.ContainsKey(channel.GuildId))
                return false;

            if (config.ServerSettings[channel.GuildId].ChannelSettings[channel.Id] == null)
                return false;

            return true;
        }

        private void AddShips(IGuildChannel channel, List<int> ships)
        {
            if (!config.ServerSettings.ContainsKey(channel.GuildId))
                config.ServerSettings[channel.GuildId] = new EmergencyQuestConfig();

            if (config.ServerSettings[channel.GuildId].ChannelSettings.ContainsKey(channel.Id) && 
                config.ServerSettings[channel.GuildId].ChannelSettings[channel.Id] != null)
            {
                ships = config.ServerSettings[channel.GuildId].ChannelSettings[channel.Id].Concat(ships).Distinct().ToList();
            }

            config.ServerSettings[channel.GuildId].ChannelSettings[channel.Id] = ships;

            config.Save();
        }
    }
}
