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
    /// <remarks>
    ///     After receiving an interaction, you must respond to acknowledge it. You can choose to respond with a message immediately using <see cref="ChannelMessageWithSource"/>
    ///     or you can choose to send a deferred response with <see cref="DeferredChannelMessageWithSource"/>. If choosing a deferred response, the user will see a loading state for the interaction,
    ///     and you'll have up to 15 minutes to edit the original deferred response using Edit Original Interaction Response.
    ///     You can read more about Response types <see href="https://discord.com/developers/docs/interactions/slash-commands#interaction-response">Here</see>
    /// </remarks>
    public enum InteractionResponseType : byte
    {
        /// <summary>
        ///     ACK a Ping.
        /// </summary>
        Pong = 1,

        [Obsolete("This response type has been depricated by discord. Either use ChannelMessageWithSource or DeferredChannelMessageWithSource", true)]
        /// <summary>
        ///     ACK a command without sending a message, eating the user's input.
        /// </summary>
        Acknowledge = 2,

        [Obsolete("This response type has been depricated by discord. Either use ChannelMessageWithSource or DeferredChannelMessageWithSource", true)]
        /// <summary>
        ///     Respond with a message, showing the user's input.
        /// </summary>
        ChannelMessage = 3,

        /// <summary>
        ///     Respond to an interaction with a message.
        /// </summary>
        ChannelMessageWithSource = 4,

        /// <summary>
        ///     ACK an interaction and edit a response later, the user sees a loading state.
        /// </summary>
        DeferredChannelMessageWithSource = 5
    }
}
