using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System;
using Discord;
using SlothuExtras;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using Discord.Rest;

namespace Slothu
{
    class CommandHandler
    {
        private readonly string _prefix = "~";
        private DiscordSocketClient _client;
        private CommandService _service;
        private DateTime _start;

        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;
			new ExternalModule(_client).SetPrefix(_prefix);
			CommandServiceConfig Config = new CommandServiceConfig
			{
				CaseSensitiveCommands = false
			};
			_service = new CommandService(Config);
			_service.AddModulesAsync(Assembly.GetEntryAssembly());
            _client.MessageReceived += HandleCommandAsync;
            _client.LoggedIn += HandleLoginAsync;
            _client.Ready += HandleReadyAsync;
            _client.Log += HandleLog;
			_client.JoinedGuild += HandleGuildJoin;
			//_client.MessageReceived += MessageRecieved;
			//_client.MessageDeleted += MessageDeleted;
			_client.MessageUpdated += OnMessageEdit;

        }

		private async Task HandleGuildJoin(SocketGuild Guild)
		{
			ExternalModule EM = new ExternalModule(_client);
			await Guild.CreateRoleAsync("Slothu", GuildPermissions.All, new Color(75, 190, 255));
			string GuildDirectory = EM.CreateGuildFolder(Guild.Id);
			EM.CreateInitialFiles(GuildDirectory);
			EM.CreateInitialGuildFolders(GuildDirectory);
			EM.WriteFile(GuildDirectory + @"\DefaultChannel", EM.GetDefaultChannel(Guild).Id.ToString());
		}

        private async Task HandleCommandAsync(SocketMessage s)
        {
            SocketUserMessage msg = s as SocketUserMessage;
            if (msg == null){ return;  }
            if (msg.Author.IsBot) { return; }
            int pos = 0;
            SocketCommandContext context = new SocketCommandContext(_client, msg);
   
            if (msg.HasMentionPrefix(_client.CurrentUser, ref pos))
            {
                Console.WriteLine("pinged!");
                IResult res = await _service.ExecuteAsync(context, pos);
                Console.WriteLine(res);
                if (!res.IsSuccess && res.Error != CommandError.UnknownCommand)
                {
                    Console.WriteLine(res.Error);
                }
            }
            int argPos = 0;
            if (msg.HasStringPrefix(_prefix, ref argPos))
            {
                IResult result = await _service.ExecuteAsync(context, argPos);

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    Console.WriteLine(result.ErrorReason);
                }
            }
		}

        private async Task HandleLoginAsync()
        {
            new ExternalModule().KillAllProcesses("dotnet", Process.GetCurrentProcess().Id);
            new ExternalModule().KillAllProcesses("bash", Process.GetCurrentProcess().Id);
            _start = DateTime.Now;
            SocketUser _owner = _client.GetUser("ShaneSloth", "6205");
            IDMChannel ownerChat = await _owner.GetOrCreateDMChannelAsync();
            IUserMessage message = await ownerChat.SendMessageAsync("ShaneBot has booted up!", false, null);
            await ownerChat.CloseAsync();
            Console.WriteLine("Successfully logged in!");
        }

        private async Task HandleReadyAsync()
        {

            RestApplication info = await _client.GetApplicationInfoAsync();
            ExternalModule EM = new ExternalModule(_client);
            foreach(SocketGuild guild in _client.Guilds)
            {
				Environment.SetEnvironmentVariable("startTime", DateTime.Now.ToString());
                string FolderDirectory = EM.CreateGuildFolder(guild.Id);
				EM.CreateInitialFiles(FolderDirectory);
                EM.CreateInitialGuildFolders(FolderDirectory);
            }
            await _client.GetUser(170874510218625024).SendMessageAsync("Good day, Daddy");
            await _client.SetGameAsync("Slothu on " + _client.Guilds.Count + " servers | Prefix: " + _prefix);

            Thread thread = new Thread(() => EM.TimedMessageAsync());
            thread.Start();
        }

        private async Task HandleLog(LogMessage msg) => Console.WriteLine(msg.ToString());

		private async Task OnMessageEdit(Cacheable<IMessage, ulong> List, SocketMessage Message, ISocketMessageChannel Channel)
		{

		}
    }
}
