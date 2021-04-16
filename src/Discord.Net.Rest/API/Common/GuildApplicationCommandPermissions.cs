using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API
{
    public class GuildApplicationCommandPermission
    {
        [JsonProperty("id")]
        public ulong Id { get; }

        [JsonProperty("application_id")]
        public ulong ApplicationId { get; }

        [JsonProperty("guild_id")]
        public ulong GuildId { get; }

        [JsonProperty("permissions")]
        public API.ApplicationCommandPermissions[] Permissions { get; set; }
    }
}
