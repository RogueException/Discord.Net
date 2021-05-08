using Discord.Net.Rest;

namespace Discord.Rest
{
    /// <summary>
    ///     Represents a configuration class for <see cref="DiscordRestClient"/>.
    /// </summary>
    public class DiscordRestConfig : DiscordConfig
    {
        /// <summary> Gets or sets the provider used to generate new REST connections. </summary>
        public RestClientProvider RestClientProvider { get; set; } = DefaultRestClientProvider.Instance;

        /// <summary>
        /// The client Id provided by Discord. Provide this if you want to use the refresh token features. Else they will throw errors if you atempt to use them!!
        /// </summary>
        public string ClientId { get; set; } = null;
        /// <summary>
        /// The client secret provided by Discord. Provide this if you want to use the refresh token features. Else they will throw errors if you atempt to use them!!
        /// </summary>
        public string ClientSecret { get; set; } = null;
    }
    
}
