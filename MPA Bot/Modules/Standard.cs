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
    //public static class EventStorage
    //{
    //    public static Dictionary<int, Event> ActiveEvents = new Dictionary<int, Event>();
    //}

    public static class Randomizer
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class Standard : ModuleBase
    {
        private EventStorage events;

        public Standard(EventStorage _events)
        {
            events = _events;
        }

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
                $"Players in Event {Index.ToString("00")}: **{buildEvent.HeadCount()}**"
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
                x.Text = $"Join: >join {Index.ToString("00")} [class]   |   Leave: >leave {Index.ToString("00")}";
            });

            return embed.Build();
        }

        private Embed BuildParty(Event buildEvent, int Index)
        {
            var Builder = new EmbedBuilder();

            var embed = Builder
                .WithColor(Color.Green)
                .WithTitle($"Event {Index.ToString("00")} Teams");

            var shuffled = new List<Player>(buildEvent.Players);
            shuffled.Shuffle();

            var leaders = shuffled.Where(x => x.Leader).ToList();

            leaders.ForEach(x => shuffled.Remove(x));

            for(int i = 0; i < leaders.Count(); i++)
            {
                StringBuilder party = new StringBuilder();

                party.AppendLine(FormatSinglePlayer(leaders[i], 1));

                var players = shuffled.Skip(i * 3).Take(3).ToArray();

                for (int p = 0; p < players.Length; p++)
                {
                    party.AppendLine(FormatSinglePlayer(players[p], p + 2));
                }

                embed
                    .AddInlineField($"Party {(i + 1).ToString("00")}",
                    party.ToString());
            }

            if ((leaders.Count() * 3) < shuffled.Count())
            {
                StringBuilder party = new StringBuilder();
                var stragglers = shuffled.Skip(leaders.Count() * 3).ToArray();

                for (int i = 0; i < stragglers.Length; i++)
                {
                    party.AppendLine(FormatSinglePlayer(stragglers[i], i + 1));
                }

                party.Append("\nY'all get to fend for yourselves. Or you could just hang around the cafe. Either way.");
                embed.AddInlineField("Stragglers", party.ToString());
            }

            return embed.Build();
        }

        private string FormatPlayers(List<Player> players, int offset)
        {
            StringBuilder output = new StringBuilder();

            for (int i = 0; i < players.Count(); i++)
            {
                if (i > 0)
                    output.Append("\n");

                output.Append(FormatSinglePlayer(players[i], i + 1 + offset));
            }

            return output.ToString();
        }

        private string FormatSinglePlayer(Player player, int Index)
        {
            StringBuilder output = new StringBuilder();

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

            output.Append($"[`{Index.ToString("00")} –`]() ");

            output.Append(name);

            if (player.Leader)
                output.Append(" - Party Lead");

            output.Append("\n");

            output.Append("[`>  –`]() ");
            output.Append(player.Class);

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

            events.Save();

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

            if (events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There's already an event running in slot {Index.ToString("00")}!");
                return;
            }

            var tempEvent = new Event() { Description = Description };

            if (ownerJoining)
                tempEvent.AddPlayer(Context.User, "");

            events.ActiveEvents.Add(Index, tempEvent);

            //await ReplyAsync($"{Context.User.Mention} created event {Index.ToString("00")}\n" +
            //    $"{tempEvent.Description}\n" +
            //    $"Join: `>join {Index} [class]` | Leave: `>join {Index}`\n" +
            //    $"Set/Edit Class: `>class {Index} [class]` or set it as you join.\n" +
            //    $"Players in Event {Index.ToString("00")}: `{tempEvent.Players.Count().ToString("00")}/{EventStorage.ActiveEvents[Index].MaxPlayers.ToString("00")}`");

            await ReplyAsync($"{Context.User.Mention} created event {Index.ToString("00")}", embed: BuildEvent(events.ActiveEvents[Index], Index));
        }

        [Command("edit")]
        public async Task EditEvent(int Index, [Remainder]string Description)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            if (Description == "")
            {
                await ReplyAsync($"You can't set an empty description!");
                return;
            }

            events.ActiveEvents[Index].Description = Description;
            
            await ReplyAsync($"Description for Event {Index.ToString("00")} changed to {Description}");
        }

        [Command("block")]
        public async Task SetBlock(int Index, int Block = -8437)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            // magic numbers yayyyyy
            if (Block == -8437)
            {
                await ReplyAsync($"Event {Index.ToString("00")} will meet in Block-{events.ActiveEvents[Index].Block.ToString("000")}.");
                return;
            }

            // TODO: only allow actual blocks
            
            events.ActiveEvents[Index].Block = Block;

            await ReplyAsync($"The meeting block for Event {Index.ToString("00")} has been changed to Block-{events.ActiveEvents[Index].Block.ToString("000")}.");
        }

        [Command("size")]
        public async Task SetSize(int Index, int Size = -5797)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            // magic numbers yayyyyy
            if (Size == -5797)
            {
                await ReplyAsync($"Event {Index.ToString("00")} will meet in Block-{events.ActiveEvents[Index].Block.ToString("000")}.");
                return;
            }

            // TODO: only allow normal sizes
            // TODO: point out players that no longer fit the size

            if (Size > 12)
            {
                await ReplyAsync("You can't have more than 12 players in a MPA!");
                return;
            }

            if (Size == 1)
            {
                await ReplyAsync("Yeah let's make an empty MPA, that's it.");
                return;
            }

            if (Size < 0)
            {
                await ReplyAsync("Negative MPAs! That's how Sega has been hiding the 14*s!");
                return;
            }

            events.ActiveEvents[Index].MaxPlayers = Size;

            await ReplyAsync($"The player cap for Event {Index.ToString("00")} has been changed to `{Size}`.");
        }

        [Command("close")]
        public async Task CloseEvent(int Index)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            events.ActiveEvents.Remove(Index);

            await ReplyAsync($"Event {Index.ToString("00")} has been closed.");
        }

        [Command("list")]
        public async Task ListEvents()
        {
            if (events.ActiveEvents.Count() == 0)
            {
                await ReplyAsync($"There are no running events.");
                return;
            }

            StringBuilder output = new StringBuilder();

            output.AppendLine("```markdown");

            foreach (var kv in events.ActiveEvents)
            {
                output.AppendLine($"# Event {kv.Key.ToString("00")}");
                output.AppendLine($"> {Trim(kv.Value.Description)}");

                output.Append($"- {kv.Value.HeadCount()}");

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

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            await ReplyAsync("", embed: BuildEvent(events.ActiveEvents[Index], Index));
        }

        [Command("call")]
        public async Task EventPing(int Index)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            //await ReplyAsync($"Calling all registered members:\n<@" +
            //    $"{string.Join(">, <@", events.ActiveEvents[Index].Players.Select(x => x.UserId))}>", embed: BuildEvent(events.ActiveEvents[Index], Index));

            if (events.ActiveEvents[Index].Players.Count(x => x.Leader) > 0)
                await ReplyAsync("", embed: BuildParty(events.ActiveEvents[Index], Index));
            else
                await ReplyAsync("You don't have any party leads set!");
        }

        [Command("leader")]
        [Alias("leaders")]
        public async Task SetLeaders(int Index, [Remainder]string Mentions = "")
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            var mentions = ((SocketUserMessage)Context.Message).MentionedUsers.ToList();

            if (mentions.Count() == 0)
            {
                if (events.ActiveEvents[Index].Players.Count(x => x.Leader) == 0)
                {
                    await ReplyAsync($"There are no leaders set for event {Index.ToString("00")}");
                    return;
                }
                else
                {
                    await ReplyAsync($"Removed <@{string.Join(">, <@", events.ActiveEvents[Index].Players.Where(x => x.Leader).Select(x => x.UserId))}>" +
                        $"as leader{((events.ActiveEvents[Index].Players.Count(x => x.Leader) > 1) ? "s" : "")} for Event {Index.ToString("00")}");

                    events.ActiveEvents[Index].ClearLeaders();

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

            events.ActiveEvents[Index].SetLeaders(mentions);

            await ReplyAsync($"Set {string.Join(", ", mentions.Select(x => x.Mention))} as leader{((events.ActiveEvents[Index].Players.Count(x => x.Leader) > 1) ? "s" : "")} of Event {Index.ToString("00")}");
        }

        [Command("join")]
        public async Task JoinEvent(int Index, [Remainder]string Class  = "")
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            if (events.ActiveEvents[Index].ContainsPlayer(Context.User))
            {
                await ReplyAsync($"You're already in event {Index.ToString("00")}!\n" +
                    $"You can leave with `>leave {Index.ToString("00")}` if you want.");
                return;
            }

            if (events.ActiveEvents[Index].AddPlayer(Context.User, Class))
            {
                StringBuilder output = new StringBuilder();

                output.Append($"{Context.User.Mention} joined event {Index.ToString("00")}");
                if (Class != "")
                    output.Append($" as {Class}");
                output.AppendLine();

                output.Append($"Players in event {Index.ToString("00")}: " +
                    $"`{events.ActiveEvents[Index].HeadCount()}`");

                if (Class == "")
                    output.Append($"\nRemember to use `>class {Index} [class]` to set your class!");

                await ReplyAsync(output.ToString());
            }
            else
            {
                await ReplyAsync($"Sorry {Context.User.Mention}, event {Index.ToString("00")} is full. ~~try to type faster next time~~");
            }
        }

        //[Command("remove")]
        //public async Task RemovePlayer(int Index, [Remainder]string Player = "")
        //{

        //}

        //[Command("add")]
        //public async Task ForceAdd(int Index, [Remainder]string Name)
        //{
        //    if (Index < 0)
        //    {
        //        Index *= -1;
        //    }

        //    if (!events.ActiveEvents.ContainsKey(Index))
        //    {
        //        await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
        //        return;
        //    }

        //    if (events.ActiveEvents[Index].ContainsPlayer(Name) == true)
        //    {
        //        await ReplyAsync($"{Name} is already in event {Index.ToString("00")}!");
        //        return;
        //    }
        //    else if (events.ActiveEvents[Index].ContainsPlayer(Name) == null)
        //    {
        //        await ReplyAsync($"");
        //    }
            
        //}

        [Command("class")]
        public async Task SetClass(int Index, [Remainder]string Class = "")
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            if (!events.ActiveEvents[Index].ContainsPlayer(Context.User))
            {
                await ReplyAsync($"*throws a rifle at {Context.User.Mention}*");
                return;
            }

            if (Class == "")
            {
                var player = events.ActiveEvents[Index].GetPlayer(Context.User);

                if (player.Class == "")
                    await ReplyAsync($"You haven't set a class, {Context.User.Mention}!");
                else
                    await ReplyAsync($"{Context.User.Mention} your class in event {Index.ToString("00")} is\n{player.Class}");

                return;
            }

            events.ActiveEvents[Index].SetClass(Context.User, Class);

            await ReplyAsync($"{Context.User.Mention} your class in event {Index.ToString("00")} has been set to {Class}");
        }

        [Command("leave")]
        public async Task LeaveEvent(int Index)
        {
            if (Index < 0)
            {
                Index *= -1;
            }

            if (!events.ActiveEvents.ContainsKey(Index))
            {
                await ReplyAsync($"There is no event in slot {Index.ToString("00")}!");
                return;
            }

            if (events.ActiveEvents[Index].RemovePlayer(Context.User))
            {
                await ReplyAsync($"{Context.User.Mention} left event {Index.ToString("00")}.");
            }
            else
                await ReplyAsync($"*throws a rifle at {Context.User.Mention}*");
        }
    }
}
