using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    ///     Represents a discord interaction
    ///     <para>
    ///         An interaction is the base "thing" that is sent when a user invokes a command, and is the same for Slash Commands
    ///         and other future interaction types. see <see href="https://discord.com/developers/docs/interactions/slash-commands#interaction"/>.
    ///     </para>
    /// </summary>
    public interface IDiscordInteraction : ISnowflakeEntity
    {
        /// <summary>
        ///     The id of the interaction.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// 
        /// </summary>
        IGuild Guild { get; }

        /// <summary>
        ///     The type of this <see cref="IDiscordInteraction"/>.
        /// </summary>
        InteractionType Type { get; }

        /// <summary>
        ///     The command data payload.
        /// </summary>
        IApplicationCommandInteractionData? Data { get; }

        /// <summary>
        ///     A continuation token for responding to the interaction.
        /// </summary>
        string Token { get; }

        /// <summary>
        ///     read-only property, always 1.
        /// </summary>
        int Version { get; }
    }
}
