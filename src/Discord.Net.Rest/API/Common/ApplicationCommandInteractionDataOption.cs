using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API
{
    internal class ApplicationCommandInteractionDataOption
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public Optional<object> Value { get; set; }

        [JsonProperty("options")]
        public Optional<IEnumerable<ApplicationCommandInteractionDataOption>> Options { get; set; }
    }
}
