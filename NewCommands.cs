using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SlothuExtras;
using System.Collections.Generic;
using Discord.Audio;
using Discord.Rest;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Net;

namespace Slothu.Modules
{
    public class BotModule : ModuleBase<SocketCommandContext>
    {
        [Group("help")]
        [Alias("commands")]
        public class HelpModule : ModuleBase<SocketCommandContext>
        {
            [Command]
            public async Task Help()
            {
                EmbedBuilder Embed = new EmbedBuilder();
                Embed.WithColor(new Color(0, 170, 255));
                Embed.WithAuthor(Context.Client.GetApplicationInfoAsync().Result.Owner);
                Embed.WithFooter("33 total non-owner [functional] commands");
                Embed.AddField("Command List", "[Command Spreadsheet](https://docs.google.com/spreadsheets/d/18r7UHIkQS3Wb5GllrsFXgGTNmtcDRxDTf5_IojygrEs/edit?usp=sharing)");
                SocketTextChannel Channel = (SocketTextChannel)Context.Channel;
                await Channel.SendMessageAsync("", false, Embed);
            }

            public async Task Help(string group)
            {

            }

            public async Task Help(string group, string command)
            {

            }
        }

        [Command("prefix")]
        public async Task GetPrefix()
        {
            string prefix = Environment.GetEnvironmentVariable("prefix");
            prefix = (prefix == null) ? "~" : Environment.GetEnvironmentVariable("prefix");

        }

        [Command("stats")]
        [Alias("info")]
        public async Task GetInfo()
        {
            string latency = Context.Client.Latency.ToString() + "ms";
            string serverCount = Context.Client.Guilds.Count.ToString();
            string connectionState = Context.Client.ConnectionState.ToString();
            DateTime now = DateTime.Now;
            DateTime start = DateTime.Parse(Environment.GetEnvironmentVariable("startTime"));
            TimeSpan _span = now - start;
            int _days = _span.Days;
            int _hours = _span.Hours;
            int _minutes = _span.Minutes;
            int _seconds = _span.Seconds;
            string _uptime =
                (_days > 0 ? _days.ToString() + " days, " : "")
                + (_hours > 0 ? _hours.ToString() + " hours, " : "")
                + (_minutes > 0 ? _minutes.ToString() + " minutes, " : "")
                + (_seconds > 0 ? _seconds.ToString() + " seconds." : "");

            EmbedBuilder a = new EmbedBuilder();
            a.WithTitle("**Status**");
            a.AddField("Latency", latency, true);
            a.AddField("Uptime", _uptime, true);
            a.WithThumbnailUrl(Context.Guild.IconUrl);
            a.AddField("Connection State", connectionState, true);
            a.AddField("Connected to", serverCount + " server" + (Convert.ToInt32(serverCount) > 1 ? "s" : ""), true);
            a.WithColor(new Color(0, 170, 255));
            ISocketMessageChannel gencom = Context.Channel;
            await gencom.SendMessageAsync("", false, a);
        }

        [Command("updates")]
        public async Task GetUpdates()
        {
            SocketUser Owner = Context.Client.GetUser(170874510218625024);
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithColor(new Color(0, 170, 255));
            Embed.WithAuthor(Owner.Mention);
            Embed.AddField(
                "Most Recent Update (12/10/2018)",
                "https://docs.google.com/document/d/1aW2cGOGATi6RDDXqDdhX5xqgKvfl6TZU0EL4bN_Ip2Q/edit?usp=sharing",
                true
            );
            Embed.WithFooter("Managed by " + Owner.Mention);
        }

        [Command("say")]
        [RequireOwner]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task Echo([Remainder] string Message)
        {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync(Message, false, null);
        }

        public async Task Echo(ISocketMessageChannel Channel, [Remainder] string Message)
        {
            await Context.Message.DeleteAsync();
            await Channel.SendMessageAsync(Message, false, null);
        }
    }

    [Group("moderate")]
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        [Command("mute")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task MuteUser(IUser User, int Length, [Remainder] string Reason)
        {
            SocketGuild Guild = Context.Guild;
            SocketGuildUser Sender = (SocketGuildUser)Context.Message.Author;
            SocketGuildUser GuildUser = (SocketGuildUser)User;
            IReadOnlyCollection<SocketRole> Roles = Guild.Roles;
            ExternalModule Module = new ExternalModule();
            SocketTextChannel Default = Module.GetDefaultChannel(Guild);
            int TargetPosition = Roles.ToList().Find(role => role.Name == "Slothu").Position - 1;
            Length = Length * 60;

            IRole MuteRole = null;
            if ((Module.RoleExists(Guild, "Mute")) == null)
            {
                MuteRole = (IRole)Module.CreateRole(Guild, "Mute", 67175424, new Color(20, 20, 20));
            }
            else MuteRole = Module.RoleExists(Guild, "Mute");

            await MuteRole.ModifyAsync(rp => rp.Position = TargetPosition, null);
            await GuildUser.AddRoleAsync(MuteRole);
            await Module.ModifyGuildChannels(Guild, MuteRole);
            await Module.ModerationAction(
                User,
                Sender,
                Default,
                $"{Sender.Mention} has muted {User.Mention} for {(Reason.Trim().Equals("") ? "no given reason" : Reason)}",
                "This can be undone at any time by a user with the KickMembers and ManageRoles permissions"
            );
            new Thread(async () => await Module.Unmute(Length, GuildUser, Sender, MuteRole)).Start();
        }

        [Command("unmute")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task UnmuteUser(IUser User)
        {
            SocketGuild Guild = Context.Guild;
            SocketGuildUser Sender = (SocketGuildUser)Context.Message.Author;
            SocketGuildUser Target = (SocketGuildUser)User;
            ExternalModule Module = new ExternalModule();
            SocketTextChannel Default = Module.GetDefaultChannel(Guild);
            IRole MuteRole = Module.RoleExists(Guild, "Mute");
            await Target.RemoveRoleAsync(MuteRole);
            await Module.ModerationAction(
                Target,
                Sender,
                Default,
                $"{Sender.Mention} has unmuted {Target.Mention}!",
                "This action can be undone by a user with the KickMembers and ManaRoles permissions!"
            );
        }

        [Command("kick")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickUser(IUser User, [Remainder] string Reason)
        {
            SocketGuildUser GuildUser = (SocketGuildUser)User;
            await GuildUser.KickAsync(Reason);
            ExternalModule Module = new ExternalModule();
            SocketGuild Guild = Context.Guild;
            SocketGuildUser Sender = (SocketGuildUser)Context.Message.Author;
            SocketTextChannel Default = Module.GetDefaultChannel(Guild);
            await Module.ModerationAction(
                GuildUser,
                Sender,
                Default,
                $"{Sender.Mention} has kicked {User.Mention} for {(Reason.Equals("") ? "no given reason" : Reason)}",
                "This action cannot be reversed and the user must rejoin the guild manually"
            );
        }
        public async Task KickUser(IUser User)
        {
            string Reason = "";
            SocketGuildUser GuildUser = (SocketGuildUser)User;
            await GuildUser.KickAsync(Reason);
            ExternalModule Module = new ExternalModule();
            SocketGuild Guild = Context.Guild;
            SocketGuildUser Sender = (SocketGuildUser)Context.Message.Author;
            SocketTextChannel Default = Module.GetDefaultChannel(Guild);
            await Module.ModerationAction(
                GuildUser,
                Sender,
                Default,
                $"{Sender.Mention} has kicked {User.Mention} for {(Reason.Equals("") ? "no given reason" : Reason)}",
                "This action cannot be reversed and the user must rejoin the guild manually"
            );
        }

        [Command("ban")]
        [Alias("addban")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUser(IUser User, [Remainder] string Reason)
        {
            ExternalModule Module = new ExternalModule();
            SocketGuild Guild = Context.Guild;
            SocketGuildUser Sender = (SocketGuildUser)Context.Message.Author;
            SocketTextChannel Default = Module.GetDefaultChannel(Guild);
            await Guild.AddBanAsync(User as IUser, 0, Reason); // Change back to 1 after testing
            await Module.ModerationAction(
                User,
                Sender,
                Default,
                $"{Sender.Mention} has banned {User.Mention} for {(Reason.Equals("") ? "no given reason" : Reason)}",
                "This action can be reversed at any time by an Administrator or user with the Ban Members permission"
            );
        }

        [Command("unban")]
        [Alias("removeban")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task UnbanUser(string FullUser)
        {
            ExternalModule Module = new ExternalModule();
            SocketGuild Guild = Context.Guild;
            SocketTextChannel Default = Module.GetDefaultChannel(Guild);
            SocketGuildUser Sender = (SocketGuildUser)Context.Message.Author;
            IReadOnlyCollection<RestBan> Bans = await Guild.GetBansAsync();
            foreach (RestBan Ban in Bans)
            {
                RestUser User = Ban.User;
                if (FullUser.Equals($"{User.Username}#{User.DiscriminatorValue}"))
                {
                    await Guild.RemoveBanAsync(User as IUser);
                    await Module.ModerationAction(
                        User,
                        Sender,
                        Default,
                        $"{Sender.Mention} has banned {User.Mention}",
                        "This action cannot be undone unless the user rejoins the guild"
                    );
                }
            }
        }

        [Command("prunemessages")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task PruneMessages(int Amount)
        {
            ExternalModule Module = new ExternalModule();
            SocketGuild Guild = Context.Guild;
            SocketGuildUser Sender = (SocketGuildUser)Context.Message.Author;
            SocketTextChannel Default = Module.GetDefaultChannel(Guild);

            await Context.Message.DeleteAsync();
            ISocketMessageChannel Channel = Context.Channel;
            int Remainder = Amount % 100; // 120 == 20 | 230 == 30
            int RepeatedAmount = Amount / 100; // 120 == 1 | 270 == 2
            long NowTicks = DateTime.Now.Ticks;

            for (int i = 1; i <= RepeatedAmount; i++)
            {
                IEnumerable<IMessage> Messages = await Channel.GetMessagesAsync(100).Flatten();
                await Channel.DeleteMessagesAsync(Messages);
            }
            IEnumerable<IMessage> RemainderMessages = await Channel.GetMessagesAsync(Remainder).Flatten();
            await Channel.DeleteMessagesAsync(RemainderMessages);

            long EndTicks = DateTime.Now.Ticks;
            await Module.ModerationAction(
                null,
                Sender,
                Default,
                $"{Sender.Mention} has removed {Amount} message(s) in {Channel}!",
                "This action cannot be reversed."
            );
        }

    }

    [Group("messages")]
    public class MessagesModule : ModuleBase<SocketCommandContext>
    {
        [Group("morning")]
        public class MorningMessages : ModuleBase<SocketCommandContext>
        {
            [Command("get")]
            public async Task GetMorningMessage()
            {
                SocketGuild Guild = Context.Guild;
                ExternalModule Module = new ExternalModule();
                string GuildFolder = @"D:\DiscordBotGuilds\" + Guild.Id;
                string MessageFilePath = GuildFolder + @"\MorningMessage.txt";
                string TimeFilePath = GuildFolder + @"\MorningTime.txt";

                string Message = await Module.ReadFile(MessageFilePath);
                string Time = await Module.ReadFile(TimeFilePath);

                await Context.Channel.SendMessageAsync("The current morning message " +
                    $"is set to: { ((Message != "") ? "*" + Message + "*" : "") }" +
                    $"\nAnd will send at: { ((Time != "") ? "*" + Time + "*" : "") }");
            }

            [Command("set")]
            public async Task SetMorningMessage(string Time, [Remainder] string Content)
            {
                SocketGuild Guild = Context.Guild;
                ExternalModule Module = new ExternalModule();
                string GuildFolder = @"D:\DiscordBotGuilds\" + Guild.Id;
                string MessageFilePath = GuildFolder + @"\MorningMessage.txt";
                string TimeFilePath = GuildFolder + @"\MorningTime.txt";

                Module.WriteFile(MessageFilePath, Content);
                Module.WriteFile(TimeFilePath, Time);

                await Context.Channel.SendMessageAsync("The morning message" +
                    $" has been set to: { ((Content != "") ? "*" + Content + "*" : "") }" +
                    $"\nAnd will send at: { ((Time != "") ? "*" + Time + "*" : "") }");
            }

            [Command("clear")]
            public async Task ClearMorningMessage()
            {
                SocketGuild Guild = Context.Guild;
                ExternalModule Module = new ExternalModule();
                string GuildFolder = @"D:\DiscordBotGuilds\" + Guild.Id;
                string MessageFilePath = GuildFolder + @"\MorningMessage.txt";
                string TimeFilePath = GuildFolder + @"\MorningTime.txt";

                Module.WriteFile(MessageFilePath, "");
                Module.WriteFile(TimeFilePath, "");

                await Context.Channel.SendMessageAsync("The current morning message" +
                    " has been cleared!");
            }

        }

        [Group("evening")]
        public class EveningMessages : ModuleBase<SocketCommandContext>
        {
            [Command("get")]
            public async Task GetMorningMessage()
            {
                SocketGuild Guild = Context.Guild;
                ExternalModule Module = new ExternalModule();
                string GuildFolder = @"D:\DiscordBotGuilds\" + Guild.Id;
                string MessageFilePath = GuildFolder + @"\EveningMessage.txt";
                string TimeFilePath = GuildFolder + @"\EveningTime.txt";

                string Message = await Module.ReadFile(MessageFilePath);
                string Time = await Module.ReadFile(TimeFilePath);

                await Context.Channel.SendMessageAsync("The current evening message " +
                    $"is set to: { ((Message != "") ? "*" + Message + "*" : "") }" +
                    $"\nAnd will send at: { ((Time != "") ? "*" + Time + "*" : "") }");
            }

            [Command("set")]
            public async Task SetMorningMessage(string Time, [Remainder] string Content)
            {
                SocketGuild Guild = Context.Guild;
                ExternalModule Module = new ExternalModule();
                string GuildFolder = @"D:\DiscordBotGuilds\" + Guild.Id;
                string MessageFilePath = GuildFolder + @"\EveningMessage.txt";
                string TimeFilePath = GuildFolder + @"\EveningTime.txt";

                Module.WriteFile(MessageFilePath, Content);
                Module.WriteFile(TimeFilePath, Time);

                await Context.Channel.SendMessageAsync("The evening message" +
                    $" has been set to: { ((Content != "") ? "*" + Content + "*" : "") }" +
                    $"\nAnd will send at: { ((Time != "") ? "*" + Time + "*" : "") }");
            }

            [Command("clear")]
            public async Task ClearMorningMessage()
            {
                SocketGuild Guild = Context.Guild;
                ExternalModule Module = new ExternalModule();
                string GuildFolder = @"D:\DiscordBotGuilds\" + Guild.Id;
                string MessageFilePath = GuildFolder + @"\EveningMessage.txt";
                string TimeFilePath = GuildFolder + @"\EveningTime.txt";

                Module.WriteFile(MessageFilePath, "");
                Module.WriteFile(TimeFilePath, "");

                await Context.Channel.SendMessageAsync("The current evening message" +
                    " has been cleared!");
            }
        }

    }

    [Group("server")]
    public class ServerModule : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public async Task GetServerInfo()
        {
            SocketGuild Guild = Context.Guild;
            EmbedBuilder Embed = new EmbedBuilder();
            ExternalModule EM = new ExternalModule();
            IReadOnlyCollection<SocketUser> Users = Guild.Users;
            int OnlineMemberCount = EM.OnlineMemberCount(Users);
            int HumanCount = EM.GetTotalHumans(Users);
            int BotCount = Guild.MemberCount - HumanCount;
            DateTimeOffset CreatedAt = Guild.CreatedAt;
            Embed.WithTitle($"**{Guild.Name}** Server Info");
            Embed.AddField("Owner",
                Guild.Owner.Username + "#" + Guild.Owner.DiscriminatorValue,
                true
            );
            Embed.AddField("Server Created",
                "Created on " + $"{CreatedAt.Day}/{CreatedAt.Month}/{CreatedAt.Year}",
                true
            );
            Embed.AddField("Member Info", Guild.MemberCount + " members\n"
                + OnlineMemberCount + " online\n"
                + HumanCount + " humans, " + BotCount + " bots",
                true
            );
            Embed.AddField("Channel Info",
                Guild.Channels.Count + " total channels:\n"
                + Guild.TextChannels.Count + " text, " + Guild.VoiceChannels.Count + " voice\n",
                true
            );
            Embed.AddField("Roles",
                Guild.Roles.Count.ToString(),
                true
            );
            Embed.AddField("Voice Region", Guild.VoiceRegionId, true);
            Embed.WithFooter($"ServerID: {Guild.Id}");
            Embed.WithThumbnailUrl(Guild.IconUrl);
            Embed.WithColor(new Color(0, 170, 255));
            Embed.WithCurrentTimestamp();
            await Context.Channel.SendMessageAsync("", false, Embed);
        }

        [Command("users")]
        public async Task GetServerUsers()
        {
            SocketGuild Guild = Context.Guild;
            IReadOnlyCollection<SocketGuildUser> GuildUsers = Guild.Users;
        }

        public async Task GetServerUsers(SocketRole RoleFilter)
        {
            SocketGuild Guild = Context.Guild;
            IReadOnlyCollection<SocketGuildUser> GuildUsers = Guild.Users;
        }

        public async Task GetServerUsers(string NameFilter)
        {
            SocketGuild Guild = Context.Guild;
            IReadOnlyCollection<SocketGuildUser> GuildUsers = Guild.Users;
        }

        [Command("bans")]
        public async Task GetServerBans()
        {
            SocketGuild Guild = Context.Guild;
            IReadOnlyCollection<RestBan> Bans = await Guild.GetBansAsync();
        }

        [Group("defaultchannel")]
        public class DefaulChannel : ServerModule
        {
            [Command("set")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task SetDefaultChannel(SocketTextChannel Channel)
            {
                SocketGuild Guild = Context.Guild;
                ExternalModule Module = new ExternalModule();
                string GuildFolder = @"D:\DiscordBotGuilds\" + Guild.Id;
                Module.WriteFile(GuildFolder + @"\DefaultChannel.txt", Channel.Id.ToString());
                await Context.Channel.SendMessageAsync("The new default channel is: " + Channel.Mention);
                // Construct Embed detailing the channel to replace the text send
            }

            [Command("get")]
            public async Task GetDefaultChannel()
            {
                SocketGuild Guild = Context.Guild;
                ExternalModule Module = new ExternalModule();
                string GuildFolder = @"D:\DiscordBotGuilds\" + Guild.Id;
                string DefaultChannelId = await Module.ReadFile(GuildFolder + @"\DefaultChannel.txt");
                SocketTextChannel Channel = Guild.GetTextChannel(Convert.ToUInt64(DefaultChannelId));
                await Context.Channel.SendMessageAsync("The current default channel is: " + Channel.Mention);
                // Construct Embed detailing the channel to replace the text send
            }
        }

    }

    [Group("user")]
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("roles")]
        public async Task GetUserRoles()
        {

        }
        public async Task GetUserRoles(SocketGuildUser User)
        {

        }

        [Command("colour")]
        public async Task SetRoleColour()
        {

        }
    }

    [Group("voice")]
    public class VoiceModule : ModuleBase<SocketCommandContext>
    {
        [Command("connect")]
        public async Task Connect()
        {

        }
    }

    [Group("fun")]
    public class FunModule : ModuleBase<SocketCommandContext>
    {
        [Command("8ball")]
        public async Task EightBall([Remainder] string Question)
        {
            EightBall ball8 = new EightBall();
            int ind = ball8.RandomNumber();
            string answer = ball8.Choose(ind);

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithAuthor(Context.Message.Author);
            eb.WithDescription("**Question:**\n"
                + "*" + Question + "*\n"
                + "**8 Ball says:**\n"
                + "*" + answer + "*"
                + "\n :bangbang:"
                );
            eb.WithColor(new Color(0, 170, 255));

            await Context.Channel.SendMessageAsync("", false, eb);
        }

        [Command("isbsat")]
        public async Task Thot()
        {
            SocketTextChannel chan = (SocketTextChannel)Context.Channel;
            SocketUserMessage msg = Context.Message;
            await msg.DeleteAsync();
            await chan.SendMessageAsync("IF SHE BREATHES; SHES A THOT");
        }
    }

    [Group("runescape")]
    [Alias("rs")]
    public class RunescapeModule : ModuleBase<SocketCommandContext>
    {
        [Command("dailies")]
        public async Task DnD()
        {
            SocketUser User = Context.Message.Author;
            await Context.Message.DeleteAsync();
            IDMChannel DMChannel = User.GetOrCreateDMChannelAsync().Result;
            EmbedBuilder EB = new EmbedBuilder();
            EB.WithDescription(
                "- Warbands (Daily) 11m XP/month, 10 minutes/day"
                + "\n- Book of Char(Daily) 4m XP / month, 2 min / day"
                + "\n- Divine Yews(Daily) 4.5m XP / month, 10 min / day"
                + "\n- Supply runs(Aka Goebiebands)(Daily) 1.5m / month, 3min/ day"
                + "\n- Tree runs(Daily) 7m XP / month, 10 min / day"
                + "\n- Guthix Caches(Daily) 4.5m XP / month, 10min/ day"
                + "\n- Jack of Trades T4(Daily) 1.5m XP / month, 2min/ day"
                + "\n- Daily Challenge(DG > Smith > RC) 12m DG XP / month, 5 min / day"
                + "\n- Sinkholes(Daily) 9m XP / month, 20 min / day"
                + "\n- Divination Arc contracts w / Energy - gathering Scrimshaw(Daily) 8m XP / month, 25min/ day"
                + "\n- Meg(Weekly) Varied XP lamps, 1 min per week"
                + "\n- Troll Invasion(Monthly) 70k XP in 5 min"
                + "\n- God Statues(Monthly) 260k XP in 5 min"
                + "\n- Giant Oyster(Monthly) 280k XP in 5 min");
            await DMChannel.SendMessageAsync("", false, EB);
        }
    }


    [Group("owner")]
	[RequireOwner]
	public class OwnerModule : ModuleBase<SocketCommandContext>
	{
		private async Task Shutdown([Remainder] string Reason)
		{
			ExternalModule EM = new ExternalModule();
			SocketUser _caller = Context.Message.Author;
			EmbedBuilder a = new EmbedBuilder();
			a.WithTitle("**Status Update**");
			a.WithDescription("Shutting down...\n" + Reason);
			a.WithColor(new Color(255, 0, 0));

			DiscordSocketClient Self = Context.Client;
			IReadOnlyCollection<SocketGuild> Guilds = Self.Guilds;
			foreach (SocketGuild Guild in Guilds)
			{
				await EM.GetDefaultChannel(Guild).SendMessageAsync("", false, a);
			}
		}
		[Command("shutdown")]
		public async Task ShutdownBot()
		{
			await Context.Message.DeleteAsync();
			await new ExternalModule().Shutdown(Context.Client);
		}

		[Command("reboot")]
		public async Task RebootBot()
		{
            if(!Context.IsPrivate)
            {
                await Context.Message.DeleteAsync();
            }
			EmbedBuilder a = new EmbedBuilder();
			a.WithTitle("**Status Update**");
			a.WithDescription("Updating Client - Rebooting...");
			a.WithColor(new Color(170, 50, 50));
			await Context.Channel.SendMessageAsync("", false, a);
			await new ExternalModule().Reboot(Context.Client);
		}

		[Command("leave")]
		public async Task LeaveServer(ulong GuildId)
		{
			await Context.Client.GetGuild(GuildId).LeaveAsync();
		}

		[Command("announce")]
		public async Task Announce([Remainder] string Content)
		{
			IReadOnlyCollection<SocketGuild> Guilds = Context.Client.Guilds;
			ExternalModule EM = new ExternalModule();
			foreach (SocketGuild Guild in Guilds)
			{
				await EM.GetDefaultChannel(Guild).SendMessageAsync(Content, false, null);
			}
		}
	}
}
