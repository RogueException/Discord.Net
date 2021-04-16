using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    ///     Returned when fetching the permissions for a command in a guild.
    /// </summary>
    public class GuildApplicationCommandPermissions
    {
        /// <summary>
        ///     The id of the command.
        /// </summary>
        public ulong Id { get; }

        /// <summary>
        ///     The id of the application the command belongs to.
        /// </summary>
        public ulong ApplicationId { get; }

        /// <summary>
        ///     The id of the guild.
        /// </summary>
        public ulong GuildId { get; }

        /// <summary>
        ///     The permissions for the command in the guild.
        /// </summary>
        public IReadOnlyCollection<ApplicationCommandPermission> Permissions { get; }

        internal GuildApplicationCommandPermissions(ulong id, ulong appId, ulong guildId, List<ApplicationCommandPermission> permissions)
        {
            this.Id = id;
            this.ApplicationId = appId;
            this.GuildId = guildId;
            this.Permissions = permissions.ToReadOnlyCollection();
        }
    }
}
