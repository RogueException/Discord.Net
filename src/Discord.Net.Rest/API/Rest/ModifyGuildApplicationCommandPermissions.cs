using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API.Rest
{
    internal class ModifyGuildApplicationCommandPermissions
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("permissions")]
        public ApplicationCommandPermission[] Permissions { get; set; }
    }
}
