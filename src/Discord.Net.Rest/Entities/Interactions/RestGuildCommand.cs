using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model = Discord.API.ApplicationCommand;

namespace Discord.Rest
{
    /// <summary>
    ///     Represents a Rest-based guild command.
    /// </summary>
    public class RestGuildCommand : RestApplicationCommand
    {
        /// <summary>
        ///     The guild Id where this command originates.
        /// </summary>
        public ulong GuildId { get; set; }

        internal RestGuildCommand(BaseDiscordClient client, ulong id, ulong guildId)
            : base(client, id)
        {
            this.CommandType = RestApplicationCommandType.GuildCommand;
            this.GuildId = guildId;
        }

        internal static RestGuildCommand Create(BaseDiscordClient client, Model model, ulong guildId)
        {
            var entity = new RestGuildCommand(client, model.Id, guildId);
            entity.Update(model);
            return entity;
        }

        /// <inheritdoc/>
        public override async Task DeleteAsync(RequestOptions options = null)
            => await InteractionHelper.DeleteGuildCommand(Discord, this).ConfigureAwait(false);

        /// <summary>
        ///     Modifies this <see cref="RestApplicationCommand"/>.
        /// </summary>
        /// <param name="func">The delegate containing the properties to modify the command with.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     The modified command
        /// </returns>
        public async Task<RestGuildCommand> ModifyAsync(Action<ApplicationCommandProperties> func, RequestOptions options = null)
            => await InteractionHelper.ModifyGuildCommand(Discord, this, func, options).ConfigureAwait(false);
    }
}
