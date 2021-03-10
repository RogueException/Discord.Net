using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API
{
    internal class InteractionResponse
    {
        [JsonProperty("type")]
        public InteractionResponseType Type { get; set; }

        [JsonProperty("data")]
        public Optional<InteractionApplicationCommandCallbackData> Data { get; set; }
    }
}
