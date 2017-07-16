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

// If you were curious enough to look for this, cool. This idea isn't mine, it's based on BeeBot's quick event stuff with some tweaks.
// Whoever made BeeBot, you're awesome, please don't kill me

namespace MPA_Bot.Modules.Standard
{
    public static class EventStorage
    {
        public static Dictionary<int, Event> ActiveEvents = new Dictionary<int, Event>();
    }

    public class Standard : ModuleBase
    {
        private string Trim(string input)
        {
            int TrimLength = 100;

            string noLineBreaks = "";

            if (input.Contains('\n'))
            {
                int firstLineBreak = input.IndexOf('\n');
                noLineBreaks = input.Substring(0, firstLineBreak);
            }
            else
                noLineBreaks = input;

            if (noLineBreaks.Length <= TrimLength)
                return noLineBreaks;

            string temp = noLineBreaks.Substring(0, TrimLength - 3);

            if (!noLineBreaks.Contains(' '))
                return $"{temp}...";

            int lastSpace = temp.LastIndexOf(' ');

            temp = temp.Substring(0, lastSpace);

            temp = $"{temp}...";

            return temp;
        }
        
        private Embed BuildEvent(Event buildEvent, int Index)
        {
            var Builder = new EmbedBuilder();

            var embed = Builder
                .WithColor(Color.DarkPurple)
                .WithTitle($"Event {Index.ToString("00")} Details")
                .WithDescription(
                "```markdown\n" +
                $"# {buildEvent.Description}\n" +
                "```\n" +
                $"**Please try to be at the café {((buildEvent.Block != 0) ? $"in Block–{buildEvent.Block.ToString("000")} " : "")}10-15 minutes before the event starts.**\n" +
                $"Players in Event {Index.ToString("00")}: **{buildEvent.Players.Count().ToString("00")}/{buildEvent.MaxPlayers.ToString("00")}**" +
                $"{((buildEvent.Players.Count() >= buildEvent.MaxPlayers) ? "**[FULL]**" : "")}"
                );

            if (buildEvent.Players.Count() > 0)
            {
                bool secondField = false;

                int firstEnd = buildEvent.Players.Count();
                int secondStart = 0;
                int secondEnd = 0;

                int secondOffset = 0;

                if (buildEvent.Players.Count() > 1)
                {
                    secondField = true;

                    if (buildEvent.Players.Count() > 1 && (buildEvent.Players.Count() % 2) > 0)
                        secondOffset = 1;

                    firstEnd = (firstEnd / 2) + secondOffset;
                    secondStart = firstEnd + 1;
                    secondEnd = buildEvent.Players.Count();
                }

                Builder.AddInlineField($"Players 01–{firstEnd.ToString("00")}",
                    FormatPlayers(buildEvent.Players.Take(firstEnd).ToList(), 0));
                
                if (secondField)
                    Builder.AddInlineField($"Players {secondStart.ToString("00")}–{secondEnd.ToString("00")}",
                    FormatPlayers(buildEvent.Players.Skip(firstEnd).ToList(), firstEnd));
            }

            embed.WithFooter(x =>
            {
                x.Text = "Join: >join 1 [class]   |   Leave: >leave 1";
            });

            return embed.Build();
        }

        private string FormatPlayers(List<Player> players, int offset)
        {
            StringBuilder output = new StringBuilder();

            for (int i = 0; i < players.Count(); i++)
            {
                if (i > 0)
                    output.Append("\n");

                var player = players[i];
                string name = "";

                if (player.PSOName == "")
                {
                    var user = ((SocketGuild)Context.Guild).GetUser(player.UserId);
                    if (user.Nickname != null)
                        name = user.Nickname;
                    else
                        name = user.Username;
                }
                else
                    name = player.PSOName;

                output.Append($"[`{(i + 1 + offset).ToString("00")} –`]() ");

                if (player.Leader)
                    output.Append("**");

                output.Append(name);

                if (player.Leader)
                    output.Append("**");

                output.Append("\n");

                output.Append("[`>  –`]() ");
                output.Append(player.Class);
            }

            return output.ToString();
        }

        [Command("blah")]
        [Summary("Blah!")]
        [Priority(1000)]
        public async Task Blah()
        {
            //var test = new EmbedBuilder();

            //var blah = test
            //    .WithTitle("Event 01")
            //    .WithDescription("```markdown\n" +
            //    "# Some event details could go here or something. I wonder how big this text box will get before it wraps around.```\n" +
            //    "**Please try to be at the café in Block–201 10-15 minutes before the event starts.**\n" +
            //    "Players in Event 01: **09/12**\n")
            //    .WithColor(Color.DarkPurple)
            //    .AddField(x =>
            //    {
            //        x.Name = "Players 6/12";
            //        x.Value =
            //        "[`01 –`]() **Googie2149**    \n" +
            //        "__[`>  –`]() Ra/Hu__\n" +
            //        "[`02 –`]() Someone\n" +
            //        "[`>  –`]()\n" +
            //        "[`03 –`]() So & So\n" +
            //        "__[`>  –`]__() __Hu/Br__\n" +
            //        "[`04 –`]() Someone\n" +
            //        "[`>  –`]()\n" +
            //        "[`05 –`]() Why do I have a long nickname???\n" +
            //        "[__`>  –`__]() __Ra/Hu__\n" +
            //        "[`06 –`]() Someone\n" +
            //        "[`>  –`]()";
            //        x.IsInline = true;
            //    })
            //    .AddField(x =>
            //    {
            //        x.Name = "Players 12/12";
            //        x.Value =
            //        "[`07 –`]() Something somewhere\n" +
            //        "[`>  –`]() Ra/Hu\n" +
            //        "[`08 –`]() Someone\n" +
            //        "[`>  –`]()\n" +
            //        "[`09 –`]() person\n" +
            //        "[`>  –`]() Hu / Br\n" +
            //        "[`10 –`]()\n" +
            //        "[`>  –`]()\n" +
            //        "[`11 –`]()\n" +
            //        "[`>  –`]()\n" +
            //        "[`12 –`]()\n" +
            //        "[`>  –`]()";
            //        x.IsInline = true;
            //    })
            //    .WithFooter(x =>
            //    {
            //        x.Text = "Join: >join 1 [class]   |   Leave: >leave 1";
            //    })
            //    .Build();

            //await ReplyAsync("", embed: blah);

            await ReplyAsync($"Blah to you too, {Context.User.Mention}.");
        }

        [Command("quit")]
        public async Task ShutDown()
        {
            if (Context.User.Id != 102528327251656704)
            {
                await ReplyAsync(":no_good::skin-tone-3: You don't have permission to run this command!");
                return;
            }

            Task.Run(async () =>
            {
                await ReplyAsync("rip");
                //await Task.Delay(500);
                await ((DiscordSocketClient)Context.Client).LogoutAsync();
                Environment.Exit(0);
            });
        }

        

        [Command("create")]
        public async Task CreateEvent(int Index, [Remainder]string Description)
        {
            bool ownerJoining = true;
            if (Index < 0)
            {
                ownerJoining = false;
                Index *= -1;
            }

            if (Index > 99)
            {
                await ReplyAsync($"You can't go higher than 99. Why? I dunno ¯\\\\\\_(ツ)_/¯");
                return;
            }

            if (EventStorage.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There's already an event running in slot {Index.ToString("00")}!");
                return;
            }

            var tempEvent = new Event() { Description = Description };

            if (ownerJoining)
                tempEvent.AddPlayer(Context.User, "");

            EventStorage.ActiveEvents.Add(Index, tempEvent);

            //await ReplyAsync($"{Context.User.Mention} created event {Index.ToString("00")}\n" +
            //    $"{tempEvent.Description}\n" +
            //    $"Join: `>join {Index} [class]` | Leave: `>join {Index}`\n" +
            //    $"Set/Edit Class: `>class {Index} [class]` or set it as you join.\n" +
            //    $"Players in Event {Index.ToString("00")}: `{tempEvent.Players.Count().ToString("00")}/{EventStorage.ActiveEvents[Index].MaxPlayers.ToString("00")}`");

            await ReplyAsync($"{Context.User.Mention} created event {Index.ToString("00")}", embed: BuildEvent(EventStorage.ActiveEvents[Index], Index));
        }

        [Command("close")]
        public async Task CloseEvent(int Index)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!EventStorage.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            EventStorage.ActiveEvents.Remove(Index);

            await ReplyAsync($"Event {Index.ToString("00")} has been closed.");
        }

        [Command("list")]
        public async Task ListEvents()
        {
            if (EventStorage.ActiveEvents.Count() == 0)
            {
                await ReplyAsync($"There are no running events.");
                return;
            }

            StringBuilder output = new StringBuilder();

            output.AppendLine("```markdown");

            foreach (var kv in EventStorage.ActiveEvents)
            {
                output.AppendLine($"# Event {kv.Key.ToString("00")}");
                output.AppendLine($"> {Trim(kv.Value.Description)}");

                output.Append($"- {kv.Value.Players.Count().ToString("00")}/{kv.Value.MaxPlayers.ToString("00")}");
                if (kv.Value.Players.Count() >= kv.Value.MaxPlayers)
                    output.Append(" **[FULL]**");

                output.Append("\n\n");
            }

            output.Append("```");

            await ReplyAsync(output.ToString());
        }

        [Command("details")]
        public async Task EventDetails(int Index)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!EventStorage.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            //StringBuilder output = new StringBuilder();

            //output.AppendLine($"**Event {Index.ToString("00")}**\n" +
            //    $"{EventStorage.ActiveEvents[Index].Description}\n\n" +
            //    $"Join: `>join {Index} [class]` | Leave: `>join {Index}`\n" +
            //    $"Set/Edit Class: `>class {Index} [class]` or set it as you join.\n\n" +
            //    $"Players in Event {Index.ToString("00")}: `{EventStorage.ActiveEvents[Index].Players.Count().ToString("00")}/{EventStorage.ActiveEvents[Index].MaxPlayers.ToString("00")}`");

            //for (int i = 0; i < EventStorage.ActiveEvents[Index].Players.Count(); i++)
            //{
            //    var user = await Context.Guild.GetUserAsync(EventStorage.ActiveEvents[Index].Players[i].UserId);

            //    output.Append($"`{(i + 1).ToString("00")}`");
            //    if (EventStorage.ActiveEvents[Index].Players[i].Leader)
            //        output.Append(" **- Leader**");
            //    output.Append(" ");
            //    if (user.Nickname != null)
            //        output.Append(user.Nickname);
            //    else
            //        output.Append(user.Username);

            //    if (EventStorage.ActiveEvents[Index].Players[i].Class != "")
            //    {
            //        output.Append(" | ");
            //        output.AppendLine(EventStorage.ActiveEvents[Index].Players[i].Class);
            //    }
            //}

            //await ReplyAsync(output.ToString());

            await ReplyAsync("", embed: BuildEvent(EventStorage.ActiveEvents[Index], Index));
        }

        [Command("call")]
        public async Task EventPing(int Index)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!EventStorage.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            //StringBuilder output = new StringBuilder();

            //output.AppendLine($"**Event {Index.ToString("00")}**\n" +
            //    $"{EventStorage.ActiveEvents[Index].Description}\n");

            //for (int i = 0; i < EventStorage.ActiveEvents[Index].Players.Count(); i++)
            //{
            //    var user = await Context.Guild.GetUserAsync(EventStorage.ActiveEvents[Index].Players[i].UserId);

            //    output.Append($"`{(i + 1).ToString("00")}`");
            //    if (EventStorage.ActiveEvents[Index].Players[i].Leader)
            //        output.Append(" **- Leader**");
            //    output.Append(" ");
            //    output.Append(user.Mention);

            //    if (EventStorage.ActiveEvents[Index].Players[i].Class != "")
            //    {
            //        output.Append(" | ");
            //        output.AppendLine(EventStorage.ActiveEvents[Index].Players[i].Class);
            //    }
            //}

            //await ReplyAsync(output.ToString());

            await ReplyAsync($"Calling all registered members:\n<@{string.Join(">, <@", EventStorage.ActiveEvents[Index].Players.Select(x => x.UserId))}>", embed: BuildEvent(EventStorage.ActiveEvents[Index], Index));
        }

        [Command("leader")]
        [Alias("leaders")]
        public async Task SetLeaders(int Index, [Remainder]string Mentions = "")
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!EventStorage.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            var mentions = ((SocketUserMessage)Context.Message).MentionedUsers.ToList();

            if (mentions.Count() == 0)
            {
                if (EventStorage.ActiveEvents[Index].Players.Count(x => x.Leader) == 0)
                {
                    await ReplyAsync($"There are no leaders set for event {Index.ToString("00")}");
                    return;
                }
                else
                {
                    await ReplyAsync($"Removed <@{string.Join(">, <@", EventStorage.ActiveEvents[Index].Players.Where(x => x.Leader).Select(x => x.UserId))}>" +
                        $"as leader{((EventStorage.ActiveEvents[Index].Players.Count(x => x.Leader) > 1) ? "s" : "")} for Event {Index.ToString("00")}");

                    EventStorage.ActiveEvents[Index].ClearLeaders();

                    return;
                }
            }

            // IF I ever want to list out which leaders were removed

            //List<Player> oldLeaders = new List<Player>();

            //if (EventStorage.ActiveEvents[Index].Players.Count(x => x.Leader) > 0)
            //{
            //    //oldLeaders = EventStorage.ActiveEvents[Index].Players.Where(x => x.Leader).ToList();

            //    //foreach (var m in mentions.Select(x => x.Id))
            //    //{
            //    //    var old = oldLeaders.FirstOrDefault(x => x.UserId == m);

            //    //    if (old != null)
            //    //        oldLeaders.Remove(old);
            //    //}

            //    EventStorage.ActiveEvents[Index].Players.ForEach(x => x.RemoveLeader());
            //}

            EventStorage.ActiveEvents[Index].SetLeaders(mentions);

            await ReplyAsync($"Set {string.Join(", ", mentions.Select(x => x.Mention))} as leader{((EventStorage.ActiveEvents[Index].Players.Count(x => x.Leader) > 1) ? "s" : "")} of Event {Index.ToString("00")}");
        }

        [Command("join")]
        public async Task JoinEvent(int Index, [Remainder]string Class  = "")
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!EventStorage.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            if (EventStorage.ActiveEvents[Index].ContainsPlayer(Context.User))
            {
                await ReplyAsync($"You're already in event {Index.ToString("00")}!\n" +
                    $"You can leave with `>leave {Index.ToString("00")}` if you want.");
                return;
            }

            if (EventStorage.ActiveEvents[Index].AddPlayer(Context.User, Class))
            {
                StringBuilder output = new StringBuilder();

                output.Append($"{Context.User.Mention} joined event {Index.ToString("00")}");
                if (Class != "")
                    output.Append($" as {Class}");
                output.AppendLine();

                output.Append($"Players in event {Index.ToString("00")}: " +
                    $"`{EventStorage.ActiveEvents[Index].Players.Count().ToString("00")}/" +
                    $"{EventStorage.ActiveEvents[Index].MaxPlayers.ToString("00")}");
                if (EventStorage.ActiveEvents[Index].Players.Count() >= EventStorage.ActiveEvents[Index].MaxPlayers)
                    output.Append(" [Full]`");
                else
                    output.Append("`");

                if (Class == "")
                    output.Append($"\nRemember to use `>class {Index} [class]` to set your class!");

                await ReplyAsync(output.ToString());
            }
            else
            {
                await ReplyAsync($"Sorry {Context.User.Mention}, event {Index.ToString("00")} is full. ~~try to type faster next time~~");
            }
        }

        //[Command("add")]
        public async Task ForceAdd(int Index, [Remainder]string Name)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!EventStorage.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            if (EventStorage.ActiveEvents[Index].ContainsPlayer(Name))
            {
                await ReplyAsync($"They're already in event {Index.ToString("00")}!");
                return;
            }

            if (EventStorage.ActiveEvents[Index].AddPlayer(Name))
            {
                StringBuilder output = new StringBuilder();

                output.Append($"{Name} joined event {Index.ToString("00")}");
                output.AppendLine();

                output.Append($"Players in event {Index.ToString("00")}: " +
                    $"`{EventStorage.ActiveEvents[Index].Players.Count().ToString("00")}/" +
                    $"{EventStorage.ActiveEvents[Index].MaxPlayers.ToString("00")}");
                if (EventStorage.ActiveEvents[Index].Players.Count() >= EventStorage.ActiveEvents[Index].MaxPlayers)
                    output.Append(" [Full]`");
                else
                    output.Append("`");
                
                await ReplyAsync(output.ToString());
            }
            else
            {
                await ReplyAsync($"Sorry {Name}, Event {Index.ToString("00")} is full. ~~try to type faster next time~~");
            }
        }

        [Command("class")]
        public async Task SetClass(int Index, [Remainder]string Class = "")
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!EventStorage.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            if (!EventStorage.ActiveEvents[Index].ContainsPlayer(Context.User))
            {
                await ReplyAsync($"*throws a rifle at {Context.User.Mention}*");
                return;
            }

            if (Class == "")
            {
                var player = EventStorage.ActiveEvents[Index].GetPlayer(Context.User);

                if (player.Class == "")
                    await ReplyAsync($"You haven't set a class, {Context.User.Mention}!");
                else
                    await ReplyAsync($"{Context.User.Mention} your class in event {Index.ToString("00")} is\n{player.Class}");

                return;
            }

            EventStorage.ActiveEvents[Index].SetClass(Context.User, Class);

            await ReplyAsync($"{Context.User.Mention} your class in event {Index.ToString("00")} has been set to:\n{Class}");
        }

        [Command("leave")]
        public async Task LeaveEvent(int Index)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!EventStorage.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            if (EventStorage.ActiveEvents[Index].RemovePlayer(Context.User))
            {
                await ReplyAsync($"{Context.User.Mention} left event {Index.ToString("00")}.");
            }
            else
                await ReplyAsync($"*throws a rifle at {Context.User.Mention}*");
        }
    }

    public class Event
    {
        public string Description;
        public List<Player> Players = new List<Player>();
        public int MaxPlayers = 12;
        public int Block = 0;

        public bool AddPlayer(IUser user, string className = "", bool leader = false)
        {
            if (Players.Count() >= MaxPlayers || ContainsPlayer(user))
                return false;

            Players.Add(new Player() { UserId = user.Id, Class = className, Leader = leader });

            return true;
        }

        public bool AddPlayer(string name, string className = "", bool leader = false)
        {
            if (Players.Count() >= MaxPlayers || ContainsPlayer(name))
                return false;

            Players.Add(new Player() { PSOName = name, Class = className, Leader = leader });

            return true;
        }

        public bool RemovePlayer(IUser user)
        {
            if (!ContainsPlayer(user))
                return false;

            var player = Players.FirstOrDefault(x => x.UserId == user.Id);
            Players.Remove(player);
            return true;
        }

        public bool RemovePlayer(string name)
        {
            if (!ContainsPlayer(name))
                return false;

            var player = Players.FirstOrDefault(x => x.PSOName.ToLower() == name.ToLower());
            Players.Remove(player);
            return true;
        }

        public Player GetPlayer(IUser user)
        {
            if (!ContainsPlayer(user))
                return null;

            var player = Players.FirstOrDefault(x => x.UserId == user.Id);
            return player;
        }

        public Player GetPlayer(string name)
        {
            if (ContainsPlayer(name))
                return null;

            var player = Players.FirstOrDefault(x => x.PSOName.ToLower() == name.ToLower());
            return player;
        }

        public void SetClass(IUser user, string className)
        {
            var player = Players.FirstOrDefault(x => x.UserId == user.Id);
            player.SetClass(className);
        }

        public void SetClass(string name, string className)
        {
            var player = Players.FirstOrDefault(x => x.PSOName.ToLower() == name.ToLower());
            player.SetClass(className);
        }
        
        public bool ContainsPlayer(IUser user)
        {
            return Players.Select(x => x.UserId).Contains(user.Id);
        }

        public bool ContainsPlayer(string name)
        {
            return Players.Select(x => x.PSOName.ToLower()).Contains(name.ToLower());
        }

        public void SetLeaders(IEnumerable<IUser> users)
        {
            ClearLeaders();

            foreach (var u in users)
            {
                GetPlayer(u).SetLeader();
            }
        }

        public void ClearLeaders()
        {
            Players.ForEach(x => x.RemoveLeader());
        }
    }

    public class Player
    {
        public ulong UserId;
        public string PSOName = "";
        public bool Discord = true;
        public string Class;
        public bool Leader = false;

        public void SetClass(string className)
        {
            Class = className;
        }

        public void SetLeader()
        {
            Leader = true;
        }

        public void RemoveLeader()
        {
            Leader = false;
        }
    }
}
