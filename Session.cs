using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using SlothuExtras;

namespace Slothu
{
    class Session
    {
        public string Boss = "";
        public int SessionLength = 1;
        public int SessionID = 1;
        public string[] Team;
        public int Wealth = 0;
        public bool Active = false;
        public List<string> Loot = new List<string>(100);
        public DateTime LastActiveChange = DateTime.Now;

        private SocketGuild Guild;
        private SocketUser Leader;
        private List<SocketUser> Users = new List<SocketUser>();
        private static readonly Color EmbedColour = new Color(54, 57, 63);
        private SocketCommandContext Context = null;
        private String FilePath = @"D:\DiscordBotGuilds\";

        public Session()
        {

        }

        public Session(SocketCommandContext Context, SocketUser Member, int SessionID=1, int SessionLength=1)
        {
            this.Context = Context;
            this.SessionID = SessionID;
            this.SessionLength = SessionLength;
            Users.Add(Member);
            Team = new string[Users.Count];

        }

        public Session(SocketCommandContext Context, string Boss, List<SocketUser> Users, int SessionID, int SessionLength = 1)
        {
            Guild = Context.Guild;
            Leader = Context.Message.Author;
            this.Users = Users;
            this.Boss = Boss;
            this.SessionLength = Math.Clamp(SessionLength, 1, 4);
            Team = new string[Users.Count];
            Wealth = 0;
            Active = true;
            this.SessionID = SessionID;
            SetTeamMembers();
        }

        public async Task ViewSession(SocketCommandContext Context)
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.AddInlineField("Boss", $"{Boss.ToUpper()}"); // ([Wiki Page](https://runescape.wiki/w/Nex))");
            Embed.AddInlineField("Duration", SessionLength);
            Embed.AddInlineField("Team", MentionMembers());
            Embed.AddInlineField("SessionID", SessionID);
            Embed.AddInlineField("Loot", ConcatLoot());
            Embed.AddInlineField("Wealth", Wealth);
            Embed.AddField("Active", Active.ToString());
            Embed.WithColor(EmbedColour);
            Embed.WithThumbnailUrl(Leader.GetAvatarUrl());

            await Context.Channel.SendMessageAsync("", false, Embed);
        }

        public async Task ViewLoot()
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.AddInlineField("Total Loot", ConcatLoot());
            Embed.WithColor(EmbedColour);
            Embed.WithThumbnailUrl(Leader.GetAvatarUrl());
            await Context.Channel.SendMessageAsync("", false, Embed);
        }

        public async Task ViewWealth()
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.AddInlineField("Total Wealth", String.Format("{0:n0}gp", Wealth));
            Embed.WithColor(EmbedColour);
            Embed.WithThumbnailUrl(Leader.GetAvatarUrl());
            await Context.Channel.SendMessageAsync("", false, Embed);
        }

        public async Task ViewTeam()
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.AddInlineField("Team Members", MentionMembers());
            Embed.WithColor(EmbedColour);
            Embed.WithThumbnailUrl(Leader.GetAvatarUrl());
            await Context.Channel.SendMessageAsync("", false, Embed);
        }

        public async Task WealthSplit()
        {
            int TeamCount = GetTeam().Count;
            int Split = (Wealth / TeamCount);
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.AddField("Total Wealth", String.Format("{0:n0}gp", Wealth));
            Embed.AddField($"Wealth Split ({TeamCount} Members)", String.Format("{0:n0}gp each", Split));
            Embed.WithColor(EmbedColour);
            Embed.WithThumbnailUrl(Leader.GetAvatarUrl());
            await Context.Channel.SendMessageAsync("", false, Embed);
        }

        public async Task AddWealth(int Amount)
        {
            Wealth += Amount;
            Save();
            await ViewWealth();
        }

        public async Task AddLoot(string Item)
        {
            Loot.Add(Item);
            Save();
            await ViewLoot();
        }

        public async Task AddLoot(string[] Items)
        {
            foreach(string item in Items)
            {
                Loot.Add(item);
                Save();
            }
            await ViewLoot();
        }

        public void ChangeSessionTime(int Duration)
        {
            if (Duration == 0)
                Active = false;
            SessionLength = Duration;
        }

        public string MentionMembers()
        {
            string _team = "";

            foreach (SocketUser partner in Users)
            {
                _team += partner.Mention + "\n";
            }
            return _team;
        }

        public string ConcatLoot()
        {
            if (this.Loot.Count < 1) return "N/A";
            string Loot = "";
            foreach(string loot in this.Loot)
            {
                Loot += loot + "\n";
            }
            return Loot;
        }
        
        public void SetTeamMembers()
        {
            for(int i = 0; i < Users.Count; i++)
            {
                Team[i] = Users[i].Id.ToString();
            }
        }

        public void Deserialize(SocketCommandContext Context)
        {
            Guild = Context.Guild;
            FilePath += $@"{Guild.Id}\Bossing\Sessions\session{SessionID}.json";
            this.Context = Context;
            Users = new List<SocketUser>();
            for (int i = 0; i < Team.Length; i++)
            {
                ulong userId = UInt64.Parse(Team[i]);
                Users[i] = Guild.GetUser(userId);
            }
            Leader = Users[0];
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public void Save()
        {
            string JSON = Serialize();
            ExternalModule EM = new ExternalModule();
            EM.WriteFile(FilePath, JSON);
        }

        public int GetSessionID()
        {
            return SessionID;
        }

        public List<SocketUser> GetTeam()
        {
            return Users;
        }

        public override string ToString()
        {
            return "Session " + SessionID.ToString();
        }
    }
}
