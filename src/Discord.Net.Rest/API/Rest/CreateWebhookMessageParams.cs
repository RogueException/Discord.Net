#pragma warning disable CS1591
using Newtonsoft.Json;

namespace Discord.API.Rest
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class CreateWebhookMessageParams
    {
        [JsonProperty("content")]
        public string Content { get; }

        [JsonProperty("nonce")]
        public Optional<string> Nonce { get; set; }

        [JsonProperty("tts")]
        public Optional<bool> IsTTS { get; set; }

        [JsonProperty("embeds")]
        public Optional<Embed[]> Embeds { get; set; }

        [JsonProperty("username")]
        public Optional<string> Username { get; set; }

        [JsonProperty("avatar_url")]
        public Optional<string> AvatarUrl { get; set; }

        [JsonProperty("allowed_mentions")]
        public Optional<AllowedMentions> AllowedMentions { get; set; }

        [JsonProperty("flags")]
        public Optional<int> Flags { get; set; }

        public CreateWebhookMessageParams(string content)
        {
            Content = content;
        }
    }
}
