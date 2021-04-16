using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    ///     Represents a type of Interaction from discord.
    /// </summary>
    public enum InteractionType : byte
    {
        /// <summary>
        ///     A ping from discord.
        /// </summary>
        Ping = 1,

        /// <summary>
        ///     An <see cref="IApplicationCommand"/> sent from discord.
        /// </summary>
        ApplicationCommand = 2
    }
}
