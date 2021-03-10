using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    /// <summary>
    /// An Attribute that gives the command parameter a description.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter , AllowMultiple = false)]
    public class Description : Attribute
    {
        public static string DefaultDescription = "No description.";
        /// <summary>
        ///     The description of this slash command parameter.
        /// </summary>
        public string description;

        /// <summary>
        ///     Tells the <see cref="SlashCommandService"/> that this parameter has a description.
        /// </summary>
        /// <param name="commandName">The name of this slash command.</param>
        public Description(string description)
        {
            this.description = description;
        }
    }

}
