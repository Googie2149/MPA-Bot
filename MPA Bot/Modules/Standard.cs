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
        [Command("blah")]
        [Summary("Blah!")]
        [Priority(1000)]
        public async Task Blah()
        {
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

            await ReplyAsync($"{Context.User.Mention} created event {Index.ToString("00")}\n" +
                $"{tempEvent.Description}\n" +
                $"Join: `>join {Index} [class]` | Leave: `>join {Index}`\n" +
                $"Set/Edit Class: `>class {Index} [class]` or set it as you join.\n" +
                $"Players in event {Index.ToString("00")}: `{tempEvent.Players.Count().ToString("00")}/12`");
        }

        [Command("close")]
        public async Task CloseEvent(int Index)
        {

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

            StringBuilder output = new StringBuilder();

            output.AppendLine($"**Event {Index.ToString("00")}**\n" +
                $"{EventStorage.ActiveEvents[Index].Description}");

            for (int i = 0; i < EventStorage.ActiveEvents[Index].Players.Count(); i++)
            {
                var user = await Context.Guild.GetUserAsync(EventStorage.ActiveEvents[Index].Players[i].UserId);

                output.Append($"`{(i + 1).ToString("00")} - ");
                if (EventStorage.ActiveEvents[Index].Players[i].Leader)
                    output.Append("Leader - ");
                if (user.Nickname != null)
                    output.Append(user.Nickname);
                else
                    output.Append(user.Username);
                output.Append("` ");
                output.AppendLine(EventStorage.ActiveEvents[Index].Players[i].Class);
            }

            await ReplyAsync(output.ToString());
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

            StringBuilder output = new StringBuilder();

            output.AppendLine($"**Event {Index.ToString("00")}**\n" +
                $"{EventStorage.ActiveEvents[Index].Description}");

            for (int i = 0; i < EventStorage.ActiveEvents[Index].Players.Count(); i++)
            {
                var user = await Context.Guild.GetUserAsync(EventStorage.ActiveEvents[Index].Players[i].UserId);

                output.Append($"`{(i + 1).ToString("00")}");
                if (EventStorage.ActiveEvents[Index].Players[i].Leader)
                    output.Append(" - Leader");
                output.Append("` ");
                output.Append(user.Mention + " ");
                output.AppendLine(EventStorage.ActiveEvents[Index].Players[i].Class);
            }

            await ReplyAsync(output.ToString());
        }

        [Command("join")]
        public async Task JoinEvent(int Index, [Remainder]string Class)
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
                await ReplyAsync($"{Context.User.Mention} joined event {Index.ToString("00")}\n" +
                    $"Players in event {Index.ToString("00")}: `{EventStorage.ActiveEvents[Index].Players.Count().ToString("00")}/12`" +
                    ((Class == "") ? $"\nRemember to use `>class {Index} [class]` to set your class!" : ""));
            }
            else
            {
                await ReplyAsync($"Sorry {Context.User.Mention}, event {Index.ToString("00")} is full. ~~try to type faster next time~~");
            }
        }

        [Command("class")]
        public async Task SetClass(int Index, [Remainder]string Class)
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

        public bool AddPlayer(IUser user, string className, bool leader = false)
        {
            if (Players.Count() >= 12 || ContainsPlayer(user))
                return false;

            Players.Add(new Player() { UserId = user.Id, Class = className, Leader = leader });

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

        public void SetClass(IUser user, string className)
        {
            var player = Players.FirstOrDefault(x => x.UserId == user.Id);
            player.SetClass(className);
        }
        
        public bool ContainsPlayer(IUser user)
        {
            return Players.Select(x => x.UserId).Contains(user.Id);
        }
    }

    public class Player
    {
        public ulong UserId;
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
