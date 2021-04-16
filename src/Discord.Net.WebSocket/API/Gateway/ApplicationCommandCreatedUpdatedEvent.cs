using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API.Gateway
{
    internal class ApplicationCommandCreatedUpdatedEvent
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("application_id")]
        public ulong ApplicationId { get; set; }

        [JsonProperty("guild_id")]
        public ulong GuildId { get; set; }

        [JsonProperty("options")]
        public List<Discord.API.ApplicationCommandOption> Options { get; set; }
    }
}
