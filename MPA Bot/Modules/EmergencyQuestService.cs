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
using Microsoft.Extensions.DependencyInjection;

namespace MPA_Bot.Modules.PSO2
{
    public class EmergencyQuestService
    {
        private Config config;
        private DiscordSocketClient client;

        public async Task Install(IServiceProvider _services)
        {
            client = _services.GetService<DiscordSocketClient>();
            config = _services.GetService<Config>();

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (config.ServerSettings.Count == 0)
                            continue;

                        Dictionary<ulong, EmergencyQuestConfig> check = new Dictionary<ulong, EmergencyQuestConfig>(config.ServerSettings);
                        List<ulong> remove = new List<ulong>();

                        foreach (var kv in check)
                        {
                            if (kv.Value.ChannelSettings.Values.All(x => x.Count() == 0))
                                remove.Add(kv.Key);
                        }

                        if (remove.Count() > 0)
                        {
                            foreach (var r in remove)
                                check.Remove(r);

                            config.ServerSettings = check;
                            config.Save();
                        }

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

                                    Console.WriteLine();


                                    Broadcast(data);
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }


                    await Task.Delay(1000 * 60 * 3);
                }
            });
        }

        public async Task Broadcast(List<EqList> data, bool force = false)
        {
            Dictionary<ulong, EmergencyQuestConfig> check = new Dictionary<ulong, EmergencyQuestConfig>(config.ServerSettings);

            if (!File.Exists("quests.json"))
            {
                JsonStorage.SerializeObjectToFile(data, "quests.json");
                return;
            }

            var cache = JsonStorage.DeserializeObjectFromFile<List<EqList>>("quests.json");

            if (data.First().Time != cache.First().Time || force)
            {
                foreach (var server in check.Select(x => x.Value.ChannelSettings))
                {
                    foreach (var setting in server)
                    {
                        var channel = (ISocketMessageChannel)client.GetChannel(setting.Key);
                        if (channel == null)
                            continue; // TODO: MARK CHANNEL FOR REMOVAL

                        var eqs = data.First().Quests.Where(x => setting.Value.Contains(x.Ship));

                        if (eqs.Count() == 0)
                            continue;

                        StringBuilder output = new StringBuilder();

                        output.AppendLine($"**Upcoming EQ in {(data.First().StartTime - DateTimeOffset.Now).Minutes} minutes!** ({data.First().StartTime.ToString("t")} JST)");

                        if (data.First().Quests.Count() == 10 && data.First().Quests.All(x => x.Name == data.First().Quests.First().Name))
                        {
                            output.AppendLine($"`ALL SHIPS:` {data.First().Quests.First().Name} ({data.First().Quests.First().JpName})");
                        }
                        else
                        {
                            foreach (var shipQuest in eqs)
                                output.AppendLine($"`Ship {shipQuest.Ship.ToString("00")}:` {shipQuest.Name} ({shipQuest.JpName})");
                        }

                        await channel.SendMessageAsync(output.ToString());
                    }
                }

                if (data.First().Time != cache.First().Time)
                    JsonStorage.SerializeObjectToFile(data, "quests.json");
            }
        }
    }

    public class Eq
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("jpName")]
        public string JpName { get; set; }
        [JsonProperty("ship")]
        public int Ship { get; set; }
    }

    public class EqList
    {
        [JsonProperty("time")]
        public DateTime UnmanagedTime { get; private set; }
        [JsonProperty("when")]
        public DateTime UnmanagedStartTime { get; private set; }

        [JsonProperty("eqs")]
        public List<Eq> Quests {
            get;
            set;
        }

        [JsonIgnore]
        public DateTimeOffset Time { get { return new DateTimeOffset(UnmanagedTime.Ticks, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time").BaseUtcOffset); } }

        [JsonIgnore]
        public DateTimeOffset StartTime { get { return new DateTimeOffset(UnmanagedStartTime.Ticks, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time").BaseUtcOffset); } }
    }
}
