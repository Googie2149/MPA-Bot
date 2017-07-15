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
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace MPA_Bot
{
    class Logger
    {
        private DiscordSocketClient client;
        private IServiceProvider services;
        private Config config;

        private Dictionary<ulong, Dictionary<ulong, DateTime>> cooldown = new Dictionary<ulong, Dictionary<ulong, DateTime>>();
        private Dictionary<string, Dictionary<ulong, string>> lastImage = new Dictionary<string, Dictionary<ulong, string>>();
        private Dictionary<ulong, StoredMessage> MessageLogs = new Dictionary<ulong, StoredMessage>();

        public async Task Install(IServiceProvider _services)
        {
            client = _services.GetService<DiscordSocketClient>();
            services = _services;

            client.MessageReceived += MessagesPLSWORK;
            client.MessageUpdated += Client_MessageUpdated;
            client.MessageDeleted += Client_MessageDeleted;

            client.UserUpdated += Client_UserUpdated;
            client.GuildMemberUpdated += Client_GuildMemberUpdated;

            client.UserJoined += Client_UserJoined;
            client.UserLeft += Client_UserLeft;

            client.UserBanned += Client_UserBanned;
            client.UserUnbanned += Client_UserUnbanned;

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var temp = new Dictionary<ulong, StoredMessage>(MessageLogs);

                        foreach (var kv in temp)
                        {
                            if (kv.Value.Timestamp.ToUniversalTime() > DateTime.UtcNow.AddSeconds(-46))
                                MessageLogs.Remove(kv.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        //_manager.Client.Log.Error("Message Logs", ex);
                    }

                    await Task.Delay(20 * 1000);
                }
            });
        }

        private async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            if (guild.Id == 132720341058453504)
            {
                await (client.GetGuild(132720341058453504).GetChannel(267377140859797515) as ISocketMessageChannel)
                    .SendMessageAsync($":shrug::skin-tone-3: " +
                    $"**User Unbanned** `{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}`\n" +
                    $"{user.Username}#{user.Discriminator} ({user.Id})");
            }
        }

        private async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            if (guild.Id == 132720341058453504)
            {
                await (client.GetGuild(132720341058453504).GetChannel(267377140859797515) as ISocketMessageChannel)
                    .SendMessageAsync($"<:banneDDD:270669936752328704> " +
                    $"**User Banned** `{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}`\n" +
                    $"{user.Username}#{user.Discriminator} ({user.Id})");
            }
        }

        private async Task Client_UserLeft(SocketGuildUser user)
        {
            if (user.Guild.Id == 132720341058453504)
            {
                await (client.GetGuild(132720341058453504).GetChannel(267377140859797515) as ISocketMessageChannel)
                    .SendMessageAsync($":door: " +
                    $"**User Left** `{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}`\n" +
                    $"{user.Username}#{user.Discriminator} ({user.Id})" +
                    ((user.JoinedAt.HasValue) ? $"\nOriginal Join Date `{user.JoinedAt.Value.ToLocalTime().ToString("d")} {user.JoinedAt.Value.ToLocalTime().ToString("T")}`" : ""));
            }
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            if (user.Guild.Id == 132720341058453504)
            {
                await (client.GetGuild(132720341058453504).GetChannel(267377140859797515) as ISocketMessageChannel)
                    .SendMessageAsync($":wave: " +
                    $"**User Joined** `{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}`\n" +
                    $"{user.Username}#{user.Discriminator} ({user.Id}) ({user.Mention})\n" +
                    $"**Account created** `{user.CreatedAt.ToLocalTime().ToString("d")} {user.CreatedAt.ToLocalTime().ToString("T")}`");
            }
        }

        private async Task Client_GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            if (before.Guild.Id == 132720341058453504)
            {
                if (before.Nickname != after.Nickname)
                    await (client.GetGuild(132720341058453504).GetChannel(267377140859797515) as ISocketMessageChannel)
                        .SendMessageAsync($":cartwheel: " +
                        $"**Nickname Changed** `{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}`\n" +
                        $"**User:** {before.Username}#{before.Discriminator} ({before.Id})\n" +
                        $"**Old:** {((before.Nickname == null) ? "`none`" : before.Nickname)}\n" +
                        $"**New:** {((after.Nickname == null) ? "`none`" : after.Nickname)}");
            }
        }

        private async Task Client_UserUpdated(SocketUser before, SocketUser after)
        {
            if (client.GetGuild(132720341058453504).Users.Contains(before))
            {
                if (before.Username != after.Username)
                    await (client.GetGuild(132720341058453504).GetChannel(267377140859797515) as ISocketMessageChannel)
                        .SendMessageAsync($":name_badge: " +
                        $"**Username Changed** `{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}`\n" +
                        $"**User:** {before.Username}#{before.Discriminator} ({before.Id})\n" +
                        $"**New:** {after.Username}#{after.Discriminator}");
            }
        }
        
        private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> ebore, SocketMessage after, ISocketMessageChannel mchannel)
        {
            if ((mchannel as IGuildChannel) == null) return;

            IGuildChannel channel = (mchannel as IGuildChannel);

            if (channel.GuildId == 132720341058453504)
            {
                if (MessageLogs.ContainsKey(after.Id) && MessageLogs[after.Id].RawText != after.Content)
                {
                    await ((await channel.Guild.GetChannelAsync(267377140859797515)) as ISocketMessageChannel)
                        .SendMessageAsync($":pencil: " +
                        $"**Message Edited** `{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}`\n" +
                        $"**User:** {after.Author.Username}#{after.Author.Discriminator} ({after.Author.Id})\n" +
                        $"**Channel:**<#{channel.Id}>\n" +
                        $"**Original send time:** `{MessageLogs[after.Id].Timestamp.ToLocalTime().ToString("d")} {MessageLogs[after.Id].Timestamp.ToLocalTime().ToString("T")}`\n" +
                        $"**Old:** {MessageLogs[after.Id].RawText}\n" +
                        $"**New:** {after.Content}");

                    MessageLogs[after.Id] = new StoredMessage((SocketUserMessage)after);
                }
            }
        }

        private async Task Client_MessageDeleted(Cacheable<IMessage, ulong> msg, ISocketMessageChannel mchannel)
        {
            if ((mchannel as IGuildChannel) == null) return;

            IGuildChannel channel = (mchannel as IGuildChannel);

            if (channel.GuildId == 132720341058453504)
            {
                if (MessageLogs.ContainsKey(msg.Id) && MessageLogs[msg.Id].Timestamp.ToUniversalTime() > DateTimeOffset.UtcNow.AddSeconds(-45))
                {
                    await ((await channel.Guild.GetChannelAsync(267377140859797515)) as ISocketMessageChannel)
                        .SendMessageAsync($":x: " +
                        $"**Message Deleted** `{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}`\n" +
                        $"**User:** {MessageLogs[msg.Id].User.Username}#{MessageLogs[msg.Id].User.Discriminator} ({MessageLogs[msg.Id].User.Id})\n" +
                        $"**Channel:**<#{channel.Id}>\n" +
                        $"**Original send time:** `{MessageLogs[msg.Id].Timestamp.ToLocalTime().ToString("d")} {MessageLogs[msg.Id].Timestamp.ToLocalTime().ToString("T")}`\n" +
                        $"{((MessageLogs[msg.Id].MentionedUsers.Count > 0) ? $"**Mentioned Users:** {MessageLogs[msg.Id].MentionedUsers.Count}\n" : "")}" +
                        $"{((MessageLogs[msg.Id].MentionedRoles.Count > 0) ? $"**Mentioned Roles:** {MessageLogs[msg.Id].MentionedRoles.Count}\n" : "")}" +
                        $"{((MessageLogs[msg.Id].Attachments.Count() > 0) ? $"**Attachments:** {MessageLogs[msg.Id].Attachments.Count()}\n{string.Join("\n", MessageLogs[msg.Id].Attachments.Select(x => $"<{x.Url}>"))}\n" : "")}" +
                        $"{((MessageLogs[msg.Id].RawText.Length > 0) ? $"**Message:** {MessageLogs[msg.Id].RawText}" : "")}");

                    MessageLogs.Remove(msg.Id);
                }
            }
        }

        public async Task MessagesPLSWORK(SocketMessage pMsg)
        {
            if (!(pMsg is SocketUserMessage message)) return;

            if (pMsg.Author.Id == client.CurrentUser.Id) return;
            if (message.Author.IsBot) return;

            if ((message.Channel as IGuildChannel) == null)
            {
                await message.Channel.SendMessageAsync("Why are you DMing me? I literally don't do anything in DMs. Go away.");
                return;
            }

            IGuildChannel channel = (message.Channel as IGuildChannel);

            if (channel.GuildId == 132720341058453504)
            {
                MessageLogs.Add(message.Id, new StoredMessage(message));

                if (message.MentionedRoles.Select(x => x.Id).ToList().Contains(132721372848848896))
                {
                    await ((await channel.Guild.GetChannelAsync(267377140859797515)) as ISocketMessageChannel)
                        .SendMessageAsync($":information_desk_person::skin-tone-3: " +
                        $"**Mod mention** `{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}`\n" +
                        $"**User:** {message.Author.Username}#{message.Author.Discriminator} ({message.Author.Id})\n" +
                        $"**Message:** {message.Content}");
                }
            }
        }
    }

    public class StoredMessage
    {
        public ulong Id;
        public SocketGuildChannel Channel;
        public SocketUser User;
        public string RawText;
        public DateTimeOffset Timestamp;
        public DateTimeOffset? EditedTimestamp;
        public List<Attachment> Attachments;
        public List<Embed> Embeds;
        public List<SocketUser> MentionedUsers;
        public List<SocketGuildChannel> MentionedChannels;
        public List<SocketRole> MentionedRoles;

        public StoredMessage(SocketUserMessage input)
        {
            Id = input.Id;
            Channel = (SocketGuildChannel)input.Channel;
            User = input.Author;
            RawText = input.Content;
            Timestamp = input.Timestamp;
            EditedTimestamp = input.EditedTimestamp;
            Attachments = input.Attachments.ToList();
            Embeds = input.Embeds.ToList();
            MentionedUsers = input.MentionedUsers.ToList();
            MentionedChannels = input.MentionedChannels.ToList();
            MentionedRoles = input.MentionedRoles.ToList();
        }
    }
}
