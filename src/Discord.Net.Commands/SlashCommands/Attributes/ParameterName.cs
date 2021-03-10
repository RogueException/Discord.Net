using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    /// <summary>
    /// An Attribute that gives the command parameter a custom name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ParameterName : Attribute
    {
        /// <summary>
        ///     The name of this slash command parameter.
        /// </summary>
        public string name;

        /// <summary>
        ///     Tells the <see cref="SlashCommandService"/> that this parameter has a custom name.
        /// </summary>
        /// <param name="name">The name of this slash command.</param>
        public ParameterName(string name)
        {
            this.name = name;
        }
    }
}
