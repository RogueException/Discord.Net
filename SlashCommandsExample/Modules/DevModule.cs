using Discord.Commands;
using Discord.Commands.SlashCommands.Types;
using Discord.SlashCommands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SlashCommandsExample.Modules
{
    // You can make the whole module Global
    //[Global]
    public class DevModule : SlashCommandModule<SocketInteraction>
    {
        [SlashCommand("ping", "Ping the bot to see if it's alive!")]
        [Global]
        public async Task PingAsync()
        {
            await Reply(":white_check_mark: **Bot Online**");
        }

        [SlashCommand("echo", "I'll repeate everything you said to me, word for word.")]
        public async Task EchoAsync(
            [Description("The message you want repetead")]
            [Required]
            string message)
        {
            await Reply($"{Interaction.Member?.Nickname ?? Interaction.Member?.Username} told me to say this: \r\n{message}");
        }
        
        [SlashCommand("overload","Just hit me with every type of data you got, man!")]
        public async Task OverloadAsync(
            [ParameterName("var1")]
            bool? boolean,
            [ParameterName("var2")]
            int? integer,
            [ParameterName("var3")]
            string myString,
            SocketGuildChannel channel,
            SocketGuildUser user,
            SocketRole role
            )
        {
            await Reply($"You gave me:\r\n {boolean}, {integer}, {myString}, <#{channel?.Id}>, {user?.Mention}, {role?.Mention}");

        }

        [SlashCommand("stats","Get the stats from Game(tm) for players or teams.")]
        public async Task GetStatsAsync(
            [Required]
            [Choice("XBOX","xbox")]
            [Choice("PlayStation","ps")]
            [Choice("PC","pc")]
            string platform,
            [Choice("Player",1)]
            [Choice("Team",2)]
            int searchType
            )
        {
            await Reply($"Well I got this: {platform}, {searchType}");
        }

        [CommandGroup("root")]
        //[Global]
        public class DevModule_Root : SlashCommandModule<SocketInteraction>
        {
            [SlashCommand("rng", "Gives you a random number from this \"machine\"")]
            public async Task RNGAsync()
            {
                var rand = new Random();
                await Reply(rand.Next(0, 101).ToString());
            }

            [CommandGroup("usr")]
            public class DevModule_Root_Usr : SlashCommandModule<SocketInteraction>
            {
                [SlashCommand("zero", "Gives you a file from user zero from this \"machine\"")]
                public async Task ZeroAsync([Description("The file you want.")] string file)
                {
                    await Reply($"You don't have permissiont to access {file} from user \"zero\".");
                }
                [SlashCommand("johnny", "Gives you a file from user Johnny Test from this \"machine\"")]
                public async Task JohnnyAsync([Description("The file you want.")] string file)
                {
                    await Reply($"You don't have permissiont to access {file} from user \"johnny\".");
                }
            }
        }
    }
}
/*
The base way of defining a command using the regular command service:

public class PingModule : ModuleBase<SocketCommandContext>
{
    [Command("ping")]
    [Summary("Pong! Check if the bot is alive.")]
    public async Task PingAsync()
    {
        await ReplyAsync(":white_check_mark: **Bot Online**");
    }
}
*/
