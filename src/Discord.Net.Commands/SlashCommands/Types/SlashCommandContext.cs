using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Commands.SlashCommands.Types
{
    public class SlashCommandContext<T> where T : IDiscordInteraction
    {
        public T Interaction;

        public IGuild Guild
            => Interaction.Guild;

        internal SlashCommandContext(IDiscordInteraction interaction)
        {
            this.Interaction = (T)interaction;
        }
    }
}
