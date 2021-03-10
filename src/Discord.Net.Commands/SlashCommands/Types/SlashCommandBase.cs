using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Commands.SlashCommands.Types
{
    /// <summary>
    ///     The base class to inherit for your slash command class's.
    /// </summary>
    /// <typeparam name="T">The type of the interaction.</typeparam>
    public abstract class SlashCommandBase<T> where T : IDiscordInteraction
    {
        public event Func<SlashCommandContext<T>, Task> CommandExecuted; 
        public SlashCommandBase()
        {

        }

        public void Register(SlashCommandCreationProperties command)
        {
            // TODO: register it, make sure that this command is what discord is expecting
        }

        internal Task ExecuteInternalAsync(IDiscordInteraction interaction)
        {
            // try catch? 
            return this.CommandExecuted?.Invoke(new SlashCommandContext<T>(interaction));
        }
    }

    /// <summary>
    ///     The base class to inherit for your slash command class's.
    /// </summary>
    public abstract class SlashCommandBase : SlashCommandBase<IDiscordInteraction> { }
}
