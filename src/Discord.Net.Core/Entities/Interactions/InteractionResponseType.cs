using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    ///     The response type for an <see cref="IDiscordInteraction"/>.
    /// </summary>
    public enum InteractionResponseType : byte
    {
        /// <summary>
        ///     ACK a Ping.
        /// </summary>
        Pong = 1,

        /// <summary>
        ///     ACK a command without sending a message, eating the user's input.
        /// </summary>
        Acknowledge = 2,

        /// <summary>
        ///     Respond with a message, eating the user's input.
        /// </summary>
        ChannelMessage = 3,

        /// <summary>
        ///     Respond with a message, showing the user's input.
        /// </summary>
        ChannelMessageWithSource = 4,

        /// <summary>
        ///     ACK a command without sending a message, showing the user's input.
        /// </summary>
        ACKWithSource = 5
    }
}
