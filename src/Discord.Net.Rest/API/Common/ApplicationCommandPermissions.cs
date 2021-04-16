using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API
{
    public class ApplicationCommandPermissions
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("type")]
        public PermissionTarget Type { get; set; }

        [JsonProperty("permission")]
        public bool Permission { get; set; }
    }
}
