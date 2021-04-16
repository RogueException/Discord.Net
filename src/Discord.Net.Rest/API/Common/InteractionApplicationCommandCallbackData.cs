using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API
{
    internal class InteractionApplicationCommandCallbackData
    {
        [JsonProperty("tts")]
        public Optional<bool> TTS { get; set; }

        [JsonProperty("content")]
        public Optional<string> Content { get; set; }

        [JsonProperty("embeds")]
        public Optional<Embed[]> Embeds { get; set; }

        [JsonProperty("allowed_mentions")]
        public Optional<AllowedMentions> AllowedMentions { get; set; }

        // New flags prop. this make the response "ephemeral". see https://discord.com/developers/docs/interactions/slash-commands#interaction-response-interactionapplicationcommandcallbackdata
        [JsonProperty("flags")]
        public Optional<int> Flags { get; set; }

        public InteractionApplicationCommandCallbackData() { }
        public InteractionApplicationCommandCallbackData(string text)
        {
            this.Content = text;
        }
        
    }
}
