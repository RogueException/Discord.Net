using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Rest
{
    /// <summary>
    ///     Represents a type of Rest-based command.
    /// </summary>
    public enum RestApplicationCommandType
    {
        /// <summary>
        ///     Specifies that this command is a Global command.
        /// </summary>
        GlobalCommand,

        /// <summary>
        ///     Specifies that this command is a Guild specific command.
        /// </summary>
        GuildCommand
    }
}
