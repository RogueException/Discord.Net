using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API
{
    internal class ApplicationCommand
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }
        [JsonProperty("application_id")]
        public ulong ApplicationId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("options")]
        public Optional<ApplicationCommandOption[]> Options { get; set; }
    }
}
