using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    ///     Represents data of an Interaction Command, see <see href="https://discord.com/developers/docs/interactions/slash-commands#interaction-applicationcommandinteractiondata"/>.
    /// </summary>
    public interface IApplicationCommandInteractionData
    {
        /// <summary>
        ///     The snowflake id of this command.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        ///     The name of this command.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     The params + values from the user.
        /// </summary>
        IReadOnlyCollection<IApplicationCommandInteractionDataOption> Options { get; }
    }
}
