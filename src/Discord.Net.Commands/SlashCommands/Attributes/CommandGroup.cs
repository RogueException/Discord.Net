using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    /// <summary>
    ///     Defines the current as being a group of slash commands.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CommandGroup : Attribute
    {
        /// <summary>
        ///     The name of this slash command.
        /// </summary>
        public string groupName;

        /// <summary>
        ///     The description of this slash command.
        /// </summary>
        public string description;

        /// <summary>
        ///     Tells the <see cref="SlashCommandService"/> that this class/function is a slash command.
        /// </summary>
        /// <param name="commandName">The name of this slash command.</param>
        public CommandGroup(string groupName, string description = "No description.")
        {
            this.groupName = groupName.ToLower();
            this.description = description;
        }
    }
}
