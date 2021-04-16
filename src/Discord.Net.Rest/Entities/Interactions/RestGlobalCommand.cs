using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model = Discord.API.ApplicationCommand;

namespace Discord.Rest
{
    /// <summary>
    ///     Represents a global Slash command.
    /// </summary>
    public class RestGlobalCommand : RestApplicationCommand
    {
        internal RestGlobalCommand(BaseDiscordClient client, ulong id)
            : base(client, id)
        {
            this.CommandType = RestApplicationCommandType.GlobalCommand;
        }

        internal static RestGlobalCommand Create(BaseDiscordClient client, Model model)
        {
            var entity = new RestGlobalCommand(client, model.Id);
            entity.Update(model);
            return entity;
        }

        /// <inheritdoc/>
        public override async Task DeleteAsync(RequestOptions options = null)
            => await InteractionHelper.DeleteGlobalCommand(Discord, this).ConfigureAwait(false);

        /// <summary>
        ///     Modifies this <see cref="RestApplicationCommand"/>.
        /// </summary>
        /// <param name="func">The delegate containing the properties to modify the command with.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     The modified command.
        /// </returns>
        public async Task<RestGlobalCommand> ModifyAsync(Action<ApplicationCommandProperties> func, RequestOptions options = null)
            => await InteractionHelper.ModifyGlobalCommand(Discord, this, func, options).ConfigureAwait(false);
    }
}
