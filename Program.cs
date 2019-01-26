using System;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace Slothu
{
    
    class Program
    {
		static void Main(string[] args)
		=> new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandHandler _handler;
        public DateTime _startTime = DateTime.Now;

        public async Task StartAsync()
        {
			string _token;
            FileStream token = File.Open(@"D:\DiscordBotFiles\token.txt", FileMode.Open, FileAccess.Read);
            byte[] result = new byte[token.Length];
            await token.ReadAsync(result, 0, (int)token.Length);
            _token = Encoding.ASCII.GetString(result);
            _client = new DiscordSocketClient();
            long ticks = DateTime.Now.Ticks;
            await _client.LoginAsync(
                tokenType: TokenType.Bot,
                token: _token
                );
            Console.WriteLine("-----------------------------------------------------" +
                "\nTime taken to login: {0:n3}s ({1}ms)" +
                "\n-----------------------------------------------------", 
                (DateTime.Now.Ticks - ticks) / 10 / 1000 / 1000, 
                (DateTime.Now.Ticks - ticks) / 10 / 1000);
            await _client.StartAsync();
            _handler = new CommandHandler(_client);
            await Task.Delay(-1);
        }

    }
}