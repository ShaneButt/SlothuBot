using System;
using System.Diagnostics;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Discord;
using System.Globalization;
using System.Linq;
using Discord.Rest;
using Discord.Commands;
using Slothu;
using Newtonsoft.Json;

namespace SlothuExtras
{
    class ExternalModule
    {
        private static readonly string local_dir = @"D:\DiscordBotFiles\";
        private static readonly string host_dir = "/home/ec2-user/BotFiles/";
        private static readonly string _rebootScript = local_dir + @"reboot.bat";
        private static readonly string host_reboot = host_dir + @"reboot.bat";
        DiscordSocketClient _client;

        public ExternalModule() { }

        public ExternalModule(DiscordSocketClient Client) => _client = Client;

        public void KillAllProcesses(string ProcessName)
        {
            foreach (Process p in Process.GetProcessesByName(ProcessName))
            {
                p.Kill();
            }
        }

        public void KillAllProcesses(string ProcessName, int PID_Exception)
        {
            foreach (Process p in Process.GetProcessesByName(ProcessName))
            {
                if (PID_Exception != p.Id) {
                    p.Kill();
                }
            }
        }

        public async Task Logout(DiscordSocketClient client)
        {
            Console.WriteLine("Logging out...");
            await client.LogoutAsync();
        }

        public async Task Shutdown(DiscordSocketClient client)
        {
            KillAllProcesses("bash");
            KillAllProcesses("dotnet");
            await Logout(client);
        }

        public async Task Reboot(DiscordSocketClient client)
        {
            ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/K " + _rebootScript);
            Process newApp = new Process();
            newApp = Process.Start(info);
            await Task.Delay(2000);
            KillAllProcesses("dotnet", newApp.Id);
            await Logout(client);
        }

        public string CreateFolder(string SourceDirectory, string FolderName) // "D:\DiscordBotGuilds\", "foldername"
        {
            string pathFile = Path.Combine(SourceDirectory, FolderName);
            if (Directory.Exists(pathFile)) return pathFile;
            else Directory.CreateDirectory(pathFile);
            return (Directory.Exists(pathFile)) ? pathFile : null;
        }

        public string CreateFile(string directory, string fileName) // "D:\DiscordBotGuilds\guildId" ; "guildId-info.txt"
        {
            string filePath = directory + @"\" + fileName; // "D:\DiscordGuildBots\foldername\guildId-info.txt
            if (File.Exists(filePath))
            {
                return filePath;
            }
            else
            {
                FileStream fs = File.Create(filePath);
                fs.Close();
            }
            return filePath;
        }

        public async Task<string> ReadFile(string filePath)
        {
            string fileContent;
            FileStream source = File.OpenRead(filePath);
            byte[] result = new byte[source.Length];
            await source.ReadAsync(result, 0, (int)source.Length);
            fileContent = Encoding.ASCII.GetString(result);
            source.Close();
            return fileContent;
        }

        public void WriteFile(string filePath, string text)
        {
            File.WriteAllText(filePath, text);
        }

        public void AppendFile(string filePath, string text)
        {
            File.AppendAllText(filePath, text);
        }

        public SocketTextChannel GetDefaultChannel(SocketGuild Guild)
        {
            SocketTextChannel DefaultOrOldest = Guild.DefaultChannel;
            foreach (SocketTextChannel Channel in Guild.TextChannels)
            {
                if (DefaultOrOldest == null)
                {
                    DefaultOrOldest = Channel;
                }
                else
                {
                    if (Channel.CreatedAt.Ticks < DefaultOrOldest.CreatedAt.Ticks)
                    {
                        DefaultOrOldest = Channel;
                    }
                }
            }
            return DefaultOrOldest;
        }

        public int OnlineMemberCount(IReadOnlyCollection<SocketUser> Users)
        {
            int OnlineCount = 0;
            foreach (SocketUser User in Users)
            {
                if (User.Status != UserStatus.Offline && User.Status != UserStatus.Invisible)
                {
                    OnlineCount++;
                }
            }
            return OnlineCount;
        }

        public int GetTotalHumans(IReadOnlyCollection<SocketUser> Users)
        {
            int TotalHumans = 0;
            foreach (SocketUser User in Users)
            {
                if (User.IsBot == false)
                {
                    TotalHumans++;
                }
            }
            return TotalHumans;
        }

        public string CreateGuildFolder(ulong guildId)
        {
            ExternalModule em = new ExternalModule();
            string Directory = @"D:\DiscordBotGuilds\";
            //string Directory = @"/home/ec2-user/GuildFolders/";
            string FolderName = guildId.ToString();
            string pathFile = em.CreateFolder(Directory, FolderName);
            return pathFile;
        }

        public void CreateInitialGuildFolders(string GuildDirectory)
        {
            ExternalModule EM = new ExternalModule();
            string[] InitialFolders =
            {
                "Bossing",
                "CustomResponses",
                "Reminders"
            };
            foreach (string folderName in InitialFolders)
            {
                EM.CreateFolder(GuildDirectory, folderName);
            }
        }

        public void CreateInitialFiles(string directory)
        {
            ExternalModule em = new ExternalModule();
            string[] InitialFiles = { "MorningMessage.txt", "EveningMessage.txt", "MorningTime.txt", "EveningTime.txt", "DefaultChannel.txt" };
            foreach (string fileName in InitialFiles)
            {
                em.CreateFile(directory, fileName);
            }
        }

        public async void TimedMessageAsync() // Morning and Night Messages
        {
            IReadOnlyCollection<SocketGuild> Guilds = _client.Guilds;
            ExternalModule EM = new ExternalModule();
            string MorningMessageFile;
            string EveningMessageFile;
            string MorningTimeFile;
            string EveningTimeFile;

            string MorningMessage = "";
            string EveningMessage = "";
            string MorningTime = "";
            string EveningTime = "";

            foreach (SocketGuild Guild in Guilds)
            {
                ulong GuildId = Guild.Id;
                string GuildName = Guild.Name;
                string FolderPath = @"D:\DiscordBotGuilds\" + Guild.Id.ToString();
                MorningMessageFile = FolderPath + @"\MorningMessage.txt";
                EveningMessageFile = FolderPath + @"\EveningMessage.txt";
                MorningTimeFile = FolderPath + @"\MorningTime.txt";
                EveningTimeFile = FolderPath + @"\EveningTime.txt";

                MorningMessage = await EM.ReadFile(MorningMessageFile);
                EveningMessage = await EM.ReadFile(EveningMessageFile);
                MorningTime = await EM.ReadFile(MorningTimeFile);
                EveningTime = await EM.ReadFile(EveningTimeFile);

                AwaitingCorrectTime(GuildId);
            }
        }

        public async void AwaitingCorrectTime(ulong GuildId)
        {
            ExternalModule EM = new ExternalModule();
            SocketGuild Guild = _client.GetGuild(GuildId);
            string GuildName = Guild.Name;
            string GuildDirectory = @"D:\DiscordBotGuilds\" + GuildId.ToString();


            while (_client.LoginState == LoginState.LoggedIn)
            {
                string MorningTime = await EM.ReadFile(GuildDirectory + @"\MorningTime.txt");
                string EveningTime = await EM.ReadFile(GuildDirectory + @"\EveningTime.txt");
                string MorningMessage = await EM.ReadFile(GuildDirectory + @"\MorningMessage.txt");
                string EveningMessage = await EM.ReadFile(GuildDirectory + @"\EveningMessage.txt");

                string ChannelIdString = await EM.ReadFile(GuildDirectory + @"\DefaultChannel.txt");
                ulong ChannelId = UInt64.TryParse(ChannelIdString, NumberStyles.Number, null, out ulong result) ? Convert.ToUInt64(ChannelIdString) : 0;
                SocketTextChannel Channel = Guild.GetTextChannel(ChannelId);


                string UtcNow = DateTime.UtcNow.ToShortTimeString();

                if (UtcNow.Equals(MorningTime))
                {
                    await CheckMessageSent(Channel, MorningMessage);
                }
                if (UtcNow.Equals(EveningTime))
                {
                    await CheckMessageSent(Channel, EveningMessage);
                }
                await Task.Delay(60000);
            }
        }

        public async Task CheckMessageSent(SocketTextChannel Channel, string Content)
        {
            IEnumerable<IMessage> ChannelMessages = Channel.GetMessagesAsync(10).Flatten().Result;
            DateTime Now = DateTime.UtcNow;
            bool MessageSent = false;
            foreach (IMessage Message in ChannelMessages)
            {
                DateTime SentAt = Message.Timestamp.DateTime;
                if (Message.Content.Equals(Content)
                    && Now.AddMinutes(-5) < SentAt)
                {
                    MessageSent = true;
                    break;
                }
            }

            if (!MessageSent)
            {
                await Channel.SendMessageAsync(Content);
            }
        }

        public async Task ModerationAction(IUser Target, IUser Sender, SocketTextChannel Channel, string Content, string Footer)
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithAuthor(Sender.Username);
            Embed.WithColor(new Color(0, 170, 255));
            Embed.WithCurrentTimestamp();
            Embed.AddField("Moderation Action!", Content, true);
            Embed.WithFooter(Footer);
            await Channel.SendMessageAsync("", false, Embed);
        }

        public async Task Unmute(int Seconds, SocketGuildUser User, SocketGuildUser Sender, IRole Role)
        {
            await Task.Delay(Seconds * 60 * 1000);
            if (!(User.Roles.Contains(Role)))
            {
                Console.WriteLine("User is already unmuted!");
                return;
            }
            else
            {
                await User.RemoveRoleAsync(Role);
                await ModerationAction(
                    User,
                    Sender,
                    GetDefaultChannel(User.Guild),
                    $"{User.Mention} has been unmuted",
                    "This action was performed automatically"
                );
                Console.WriteLine($"Unmuted {User.Username}#{User.DiscriminatorValue}");
            }

        }

        public async Task ModifyGuildChannels(SocketGuild Guild, IRole Role)
        {
            IReadOnlyCollection<SocketTextChannel> Channels = Guild.TextChannels;
            OverwritePermissions Perms = new OverwritePermissions(sendMessages: PermValue.Deny);
            foreach (SocketTextChannel Channel in Channels)
            {
                await Channel.AddPermissionOverwriteAsync(Role, Perms);
            }
        }

        public SocketRole RoleExists(SocketGuild Guild, string RoleName)
        {
            foreach (SocketRole Role in Guild.Roles)
            {
                if (Role.Name.Equals(RoleName))
                {
                    Console.WriteLine("Role found!");
                    return Role;
                }
            }
            Console.WriteLine("Role does not exist");
            return null;
        }

        public async Task<RestRole> CreateRole(SocketGuild Guild, string Name, ulong Value, Color Colour)
        {
            Console.WriteLine($"Creating role with name {Name}");
            RestRole Role = await Guild.CreateRoleAsync(Name, new GuildPermissions(Value), Colour);
            return Role;
        }

        public void SetPrefix(string Prefix)
        {
            Environment.SetEnvironmentVariable("prefix", Prefix);
        }

        public string GetPrefix()
        {
            return Environment.GetEnvironmentVariable("prefix");
        }

        public async Task Bossing(SocketCommandContext context, string boss, int time, List<SocketUser> team)
        {
            string GuildID = context.Guild.Id.ToString();
            string BossingFolder = $@"D:\DiscordBotGuilds\{GuildID}\Bossing";
            //string BossingFolder = $@"/home/ec2-user/GuildFolders/{GuildID}/Bossing";
            string SessionsFolder = CreateFolder(BossingFolder, "Sessions");
            int sessionCount = Directory.GetFiles(BossingFolder + @"\Sessions").Length + 1;
            Session Session = new Session(context, boss, team, sessionCount, time);
            string JSON = JsonConvert.SerializeObject(Session);
            CreateFile(SessionsFolder, "session" + sessionCount.ToString() + ".json");
            WriteFile(SessionsFolder + $@"\session{sessionCount}.json", JSON);
            await Session.ViewSession(context);
        }

        public async Task CreateSession(SocketCommandContext context)
        {
            string GuildID = context.Guild.Id.ToString();
            string BossingFolder = $@"D:\DiscordBotGuilds\{GuildID}\Bossing";
            //string BossingFolder = $@"/home/ec2-user/GuildFolders/{GuildID}/Bossing";
            string SessionsFolder = CreateFolder(BossingFolder, "Sessions");
            int sessionCount = Directory.GetFiles(BossingFolder + @"\Sessions").Length + 1;

        }

        public string GrabSessionJSON(SocketCommandContext Context, int SessionID)
        {
            string Folder = $@"D:\DiscordBotGuilds\{Context.Guild.Id}\Bossing\Sessions";
            //string Folder = $@"/home/ec2-user/GuildFolders/{Context.Guild.Id}/Bossing/Sessions";
            string FileName = $"session{SessionID}";
            string FilePath = Folder + $@"\{FileName}.json";
            bool doesExist = File.Exists(FilePath);

            if (!doesExist) return null;

            string JSON = ReadFile(FilePath).GetAwaiter().GetResult();
            return JSON;
        }

        public Session GetSession(SocketCommandContext Context, int SessionID)
        {
            string Folder = $@"D:\DiscordBotGuilds\{Context.Guild.Id}\Bossing\Sessions";
            //string Folder = $@"/home/ec2-user/GuildFolders/{Context.Guild.Id}/Bossing/Sessions";
            string FileName = $"session{SessionID}";
            string FilePath = Folder + $@"\{FileName}.json";

            bool doesExist = File.Exists(FilePath);

            if (!doesExist) return null;

            string JSON = ReadFile(FilePath).GetAwaiter().GetResult();
            Session session = new Session();
            session = JsonConvert.DeserializeObject<Session>(JSON);
            session.Deserialize(Context);
            return session;
        }

        public Session[] GetAllSessions(SocketCommandContext Context)
        {

            string Folder = $@"D:\DiscordBotGuilds\{Context.Guild.Id}\Bossing\Sessions";
            //string Folder = $@"/home/ec2-user/GuildFolders/{Context.Guild.Id}/Bossing/Sessions";
            int numberOfSessions = Directory.GetFiles(Folder).Count();
            if (numberOfSessions == 0)
                return new Session[0];

            Console.WriteLine("There are {0} sessions", numberOfSessions);
            List<Session> Sessions = new List<Session>();
            for (int i = 0; i < numberOfSessions; i++)
            {
                string FileName = $"session{i+1}";
                string FilePath = Folder + $@"\{FileName}.json";
                string JSON = ReadFile(FilePath).GetAwaiter().GetResult();
                Session session = new Session();
                session = JsonConvert.DeserializeObject<Session>(JSON);
                session.Deserialize(Context);
                Sessions.Add(session);
            }
            return Sessions.ToArray();
        }

        public bool IsInActiveSession(SocketUser caller, Session session)
        {
            List<SocketUser> Team = session.GetTeam();
            return (Team.Contains(caller) && session.Active==true);
        }
	}
}
