using Discord.API;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.API.Gateway
{
    internal class InteractionCreated
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("type")]
        public InteractionType Type { get; set; }

        [JsonProperty("data")]
        public Optional<ApplicationCommandInteractionData> Data { get; set; }

        [JsonProperty("guild_id")]
        public ulong GuildId { get; set; }

        [JsonProperty("channel_id")]
        public ulong ChannelId { get; set; }

        [JsonProperty("member")]
        public GuildMember Member { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }
    }
}
