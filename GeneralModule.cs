using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SlothuExtras;


namespace Slothu.Modules
{
    class GeneralModule : ModuleBase<SocketCommandContext>
    {
        [Command("helpme")]
        [Alias("commands")]
        public async Task Help()
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithColor(new Color(0, 170, 255));
            Embed.WithAuthor(Context.Client.GetApplicationInfoAsync().Result.Owner);
            Embed.WithFooter("32 commands total, 28 non-owner commands");
            Embed.AddField("Command List", "[Command Spreadsheet](https://docs.google.com/spreadsheets/d/18r7UHIkQS3Wb5GllrsFXgGTNmtcDRxDTf5_IojygrEs/edit?usp=sharing)");
            SocketTextChannel Channel = (SocketTextChannel)Context.Channel;
            await Channel.SendMessageAsync("", false, Embed);
        }

        [Command("status")]
        public async Task Status()
        {

        }

        [Command("updatelog")]
        public async Task GetUpdateLog()
        {

        }

        [Command("say")]
        public async Task Say([Remainder] String text)
        {

        }

        [Command("ping")]
        public async Task Ping()
        {

        }

        [RequireBotPermission(GuildPermission.ManageRoles)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [Command("setcustomrole")]
        public async Task SetCustomRole(SocketRole Role)
        {

        }

        [Command("setrole")]
        public async Task GiveRole(SocketRole Role)
        {

        }

        [Command("8ball")]
        public async Task EightBall([Remainder] String Question)
        {

        }

        [Command("isbsat")]
        public async Task Thot()
        {

        }

    }
}
