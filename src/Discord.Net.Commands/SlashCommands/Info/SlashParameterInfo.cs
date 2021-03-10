using Discord.Commands.Builders;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    public class SlashParameterInfo : SlashCommandOptionBuilder
    {
        public bool Nullable { get; internal set; }

        public object Parse(SocketInteractionDataOption dataOption)
        {
            switch (Type)
            {
                case ApplicationCommandOptionType.Boolean:
                    if (Nullable)
                        return (bool?)dataOption;
                    else
                        return (bool)dataOption;
                case ApplicationCommandOptionType.Integer:
                    if(Nullable)
                        return (int?)dataOption;
                    else
                        return (int)dataOption;
                case ApplicationCommandOptionType.String:
                    return (string)dataOption;
                case ApplicationCommandOptionType.Channel:
                    return (SocketGuildChannel)dataOption;
                case ApplicationCommandOptionType.Role:
                    return (SocketRole)dataOption;
                case ApplicationCommandOptionType.User:
                    return (SocketGuildUser)dataOption;
                case ApplicationCommandOptionType.SubCommandGroup:
                    throw new NotImplementedException();
                case ApplicationCommandOptionType.SubCommand:
                    throw new NotImplementedException();
            }
            throw new NotImplementedException($"There is no such type of data... unless we missed it. Please report this error on the Discord.Net github page! Type: {Type}");
        }
    }
}
