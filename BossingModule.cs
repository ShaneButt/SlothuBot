using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using SlothuExtras;
using Newtonsoft.Json;

namespace Slothu.Modules
{
    public class Runescape : ModuleBase<SocketCommandContext>
    {
        [Group("boss")]
        public class BossingModule : ModuleBase<SocketCommandContext>
        {
            [Command("create")]
            public async Task Boss(string BossName) // Solo, default
            {
                List<SocketUser> Team = new List<SocketUser>
                {
                    Context.Message.Author
                };
                int Length = 1;
                ExternalModule EM = new ExternalModule();
                await EM.Bossing(Context, BossName, Length, Team);
            }

            [Command("create")]
            public async Task Boss(string BossName, string Length) // Solo, timed
            {
                bool success = Int32.TryParse(Length, out int SessionLength);
                if (!success) SessionLength = 1;
                List<SocketUser> Team = new List<SocketUser>
                {
                    Context.Message.Author
                };
                ExternalModule EM = new ExternalModule();
                await EM.Bossing(Context, BossName, SessionLength, Team);
            }

            [Command("create")]
            public async Task Boss(string BossName, int SessionLength, params SocketUser[] Partners) // Group timed
            {
                SocketUser Caller = Context.Message.Author;
                List<SocketUser> NewPartners = new List<SocketUser>() { };
                ExternalModule EM = new ExternalModule();
                bool CallerIsMember = false;
                foreach (SocketUser partner in Partners) if (partner.Equals(Caller)) CallerIsMember = true;
                if(!CallerIsMember)
                {
                    NewPartners.Add(Caller);
                }
                foreach (SocketUser user in Partners) NewPartners.Add(user);
                await EM.Bossing(Context, BossName, SessionLength, NewPartners);
            }

            [Command("session")]
            public async Task ViewSession(int SessionID)
            {
                ExternalModule EM = new ExternalModule();
                Session session = EM.GetSession(Context, SessionID);
                if (session == null)
                {
                    Context.Channel.SendMessageAsync("Session does not exist. Enter a valid session").GetAwaiter();
                    return;
                }
                await session.ViewSession(Context);
            }

            [Command("loot")]
            public async Task ViewLoot(int SessionID)
            {
                ExternalModule EM = new ExternalModule();
                Session session = EM.GetSession(Context, SessionID);
                if (session == null)
                {
                    Context.Channel.SendMessageAsync("Session does not exist. Enter a valid session").GetAwaiter();
                    return;
                }
                await session.ViewLoot();
            }

            [Command("wealth")]
            public async Task ViewWealth(int SessionID)
            {
                ExternalModule EM = new ExternalModule();
                Session session = EM.GetSession(Context, SessionID);
                if (session == null)
                {
                    Context.Channel.SendMessageAsync("Session does not exist. Enter a valid session").GetAwaiter();
                    return;
                }
                await session.ViewWealth();
            }

            [Command("wealthsplit")]
            public async Task WealthSplit(int SessionID)
            {
                ExternalModule EM = new ExternalModule();
                Session session = EM.GetSession(Context, SessionID);
                if (session == null)
                {
                    Context.Channel.SendMessageAsync("Session does not exist. Enter a valid session").GetAwaiter();
                    return;
                }
                await session.WealthSplit();
            }

            [Command("team")]
            public async Task ViewTeam(int SessionID)
            {
                ExternalModule EM = new ExternalModule();
                Session session = EM.GetSession(Context, SessionID);
                if (session == null)
                {
                    Context.Channel.SendMessageAsync("Session does not exist. Enter a valid session").GetAwaiter();
                    return;
                }
                await session.ViewTeam();
            }

            [Command("lootadd")]
            public async Task AddLoot(int SessionID, params string[] Loot)
            {
                SocketUser Caller = Context.Message.Author;
                ExternalModule EM = new ExternalModule();
                Session session = EM.GetSession(Context, SessionID);
                if (session == null)
                {
                    Context.Channel.SendMessageAsync("Session does not exist. Enter a valid session").GetAwaiter();
                    return;
                }

                List<SocketUser> Team = session.GetTeam();
                bool Member = false;
                foreach (SocketUser partner in Team)
                {
                    if (partner.Equals(Caller)) Member = true;
                }
                if (Member)
                {
                    await session.AddLoot(Loot);
                }
                else
                {
                    await Context.Channel.SendMessageAsync("You must be a member of this Session in order to add to the Loot table");
                }
            } // For a given session

            public async Task AddLoot(params string[] Loot) // Most recent active session
            {

            }

            [Command("wealthadd")]
            public async Task AddWealth(int SessionID, int Amount)
            {
                SocketUser Caller = Context.Message.Author;
                ExternalModule EM = new ExternalModule();
                Session session = EM.GetSession(Context, SessionID);
                if (session == null)
                {
                    Context.Channel.SendMessageAsync("Session does not exist. Enter a valid session").GetAwaiter();
                    return;
                }

                List<SocketUser> Team = session.GetTeam();
                bool Member = false;
                foreach (SocketUser partner in Team)
                {
                    if (partner.Equals(Caller)) Member = true;
                }
                if (Member)
                {
                    await session.AddWealth(Amount);
                }
                else
                {
                    await Context.Channel.SendMessageAsync("You must be a member of this Session in order to add to the Wealth");
                }
            }

            [Command("showactive")]
            public async Task ShowActive()
            {
                SocketUser caller = Context.Message.Author;
                ExternalModule EM = new ExternalModule();
                Session[] sessions = EM.GetAllSessions(Context);
                if (sessions.Length == 0)
                {
                    await Context.Channel.SendMessageAsync("You are currently not in an active session!");
                    return;
                }

                bool isInActiveSession = false;
                Session activeSession = null;
                foreach(Session session in sessions)
                {
                    if(EM.IsInActiveSession(caller, session))
                    {
                        isInActiveSession = true;
                        activeSession = session;
                        break;
                    }
                }
                if(isInActiveSession && activeSession!=null)
                {
                    await activeSession.ViewSession(Context);
                    return;
                }
                else
                {
                    await Context.Channel.SendMessageAsync("You are currently not in an active session!");
                    return;
                }
            }

            [Command("end")]
            public async Task EndSession(int SessionID)
            {
                SocketUser Caller = Context.Message.Author;
                ISocketMessageChannel Channel = Context.Channel;
                ExternalModule EM = new ExternalModule();
                Session session = EM.GetSession(Context, SessionID);
                if (session == null)
                {
                    Context.Channel.SendMessageAsync("Session does not exist. Enter a valid session").GetAwaiter();
                    return;
                }

                session.Active = false;
                await Channel.SendMessageAsync("You have concluded your session with " + session.Boss + ", here is your loot: ");
                await session.ViewLoot();
                await Channel.SendMessageAsync("Which comes to");
                await session.WealthSplit();
            }
        }

        [Group("portables")]
        public class Portables : ModuleBase<SocketCommandContext>
        {
            [Command()]
            public async Task GetPortables()
            {
                EmbedBuilder Embed = new EmbedBuilder();
                GoogleApi API = new GoogleApi();

                await Context.Channel.TriggerTypingAsync();

                long Ticks = DateTime.Now.Ticks;
                IList<IList<Object>> Locations = API.GetPortables();
                if (Locations != null && Locations.Count > 0)
                {
                    foreach (var column in Locations)
                    {
                        Embed.AddInlineField(column[0].ToString(), column[1].ToString().Replace("*", "\\*"));
                    }
                }
                Embed.AddInlineField("Portables Twitter", "[Twitter Bot](https://twitter.com/portablesrs)");
                Embed.AddInlineField("Portables Discord", "[Discord Bot](https://discord.gg/QhBCYYr)");
                string Author = API.GetAuthor().ToString();
                string AuthorTag = Author.Split('/')[0];
                AuthorTag = AuthorTag.Split('&')[0];
                AuthorTag = AuthorTag.Replace(" ", "%20");
                string AuthorImageUrl = "https://secure.runescape.com/m=avatar-rs/" + AuthorTag + "/chat.png";
                Embed.WithTitle("Portables Public Sheets");
                Embed.WithCurrentTimestamp();
                Embed.WithAuthor("Editor(s): " + Author);
                Embed.WithThumbnailUrl(AuthorImageUrl);
                Embed.WithFooter($"Executed in: { (DateTime.Now.Ticks - Ticks) / 10000 }ms");
                Embed.WithColor(new Color(54, 57, 63));
                Embed.WithUrl("https://docs.google.com/spreadsheets/d/16Yp-eLHQtgY05q6WBYA2MDyvQPmZ4Yr3RHYiBCBj2Hc");
                await Context.Channel.SendMessageAsync("", false, Embed);
            }

            [Command("info")]
            public async Task Sheets()
            {
                EmbedBuilder Embed = new EmbedBuilder();
                string AuthorImageUrl = "https://secure.runescape.com/m=avatar-rs/Electric/chat.png";
                Embed.WithThumbnailUrl(AuthorImageUrl);
                Embed.WithCurrentTimestamp();
                Embed.WithColor(new Color(54, 57, 63));
                Embed.WithTitle("Portables FC Info");
                Embed.AddField("Sheets", "[Sheets](https://docs.google.com/spreadsheets/d/16Yp-eLHQtgY05q6WBYA2MDyvQPmZ4Yr3RHYiBCBj2Hc)", true);
                Embed.AddField("Forums", "[RS Forum](http://services.runescape.com/m=forum/forums.ws?75,76,789,65988634)", true);
                Embed.AddField("Discord", "[Discord Server](https://discordapp.com/invite/QhBCYYr)", false);
                Embed.AddField("Twitter", "[Twitter Bot](https://twitter.com/PortablesRS)", true);
                await Context.Channel.SendMessageAsync("", false, Embed);
            }

            [Command("abbr")]
            [Alias("abbreviations")]
            public async Task Abbreviations()
            {
                EmbedBuilder Embed = new EmbedBuilder();
                string AuthorImageUrl = "https://secure.runescape.com/m=avatar-rs/Electric/chat.png";
                Embed.WithThumbnailUrl(AuthorImageUrl);
                Embed.WithCurrentTimestamp();
                Embed.WithColor(new Color(54, 57, 63));
                Embed.WithTitle("Portables FC Abbreviations");
                Embed.AddField("Locations", "CA = Combat Academy\nSP = Shantay Pass\nLC = Lumbridge Crater (Beach in Summer Event)" +
                    "\nBU = Burthorpe\nBA = Barbarian Assault\nCW = Castle Wars\nPrif = Prifddinas\nMG = Max Guild" +
                    "\nVIP = Menaphos VIP Area\nPOF = Player-Owned Farm", true);

                Embed.AddField("Portables", "F/FL = Fletcher\nCR = Crafter\nBR = Brazier\nM/Mill = Sawmill\nR = Range\nW = Well", true);
                await Context.Channel.SendMessageAsync("", false, Embed);
            }
        }

        [Group("session")]
        public class SessionModule : ModuleBase<SocketCommandContext>
        {
            [Command("create")]
            public async Task CreateSession()
            {
                ExternalModule Module = new ExternalModule();
                Session session = new Session();
                SocketUser Author = Context.Message.Author;
                
            }
        }
    }
}
