using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    /// <summary>
    ///     Defines the current class or function as a slash command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SlashCommand : Attribute
    {
        /// <summary>
        ///     The name of this slash command.
        /// </summary>
        public string commandName;

        /// <summary>
        ///     The description of this slash command.
        /// </summary>
        public string description;

        /// <summary>
        ///     Tells the <see cref="SlashCommandService"/> that this class/function is a slash command.
        /// </summary>
        /// <param name="commandName">The name of this slash command.</param>
        public SlashCommand(string commandName, string description = "No description.")
        {
            this.commandName = commandName.ToLower();
            this.description = description;
        }
    }
}
