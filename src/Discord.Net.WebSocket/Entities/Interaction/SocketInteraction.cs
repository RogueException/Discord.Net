using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model = Discord.API.Gateway.InteractionCreated;

namespace Discord.WebSocket
{
    /// <summary>
    ///     Represents an Interaction recieved over the gateway.
    /// </summary>
    public class SocketInteraction : SocketEntity<ulong>, IDiscordInteraction
    {
        /// <summary>
        ///     The <see cref="SocketGuild"/> this interaction was used in.
        /// </summary>
        public SocketGuild Guild
            => Discord.GetGuild(GuildId);

        /// <summary>
        ///     The <see cref="SocketTextChannel"/> this interaction was used in.
        /// </summary>
        public SocketTextChannel Channel
            => Guild.GetTextChannel(ChannelId);

        /// <summary>
        ///     The <see cref="SocketGuildUser"/> who triggered this interaction.
        /// </summary>
        public SocketGuildUser Member
            => Guild.GetUser(MemberId);

        /// <summary>
        ///     The type of this interaction.
        /// </summary>
        public InteractionType Type { get; private set; }

        /// <summary>
        ///     The data associated with this interaction.
        /// </summary>
        public SocketInteractionData Data { get; private set; }

        /// <summary>
        ///     The token used to respond to this interaction.
        /// </summary>
        public string Token { get; private set; }

        /// <summary>
        ///     The version of this interaction.
        /// </summary>
        public int Version { get; private set; }

        public DateTimeOffset CreatedAt { get; }

        /// <summary>
        ///     <see langword="true"/> if the token is valid for replying to, otherwise <see langword="false"/>.
        /// </summary>
        public bool IsValidToken
            => CheckToken();

        private ulong GuildId { get; set; }
        private ulong ChannelId { get; set; }
        private ulong MemberId { get; set; }

        internal SocketInteraction(DiscordSocketClient client, ulong id)
            : base(client, id)
        {
        }

        internal static SocketInteraction Create(DiscordSocketClient client, Model model)
        {
            var entitiy = new SocketInteraction(client, model.Id);
            entitiy.Update(model);
            return entitiy;
        }

        internal void Update(Model model)
        {
            this.Data = model.Data.IsSpecified
                ? SocketInteractionData.Create(this.Discord, model.Data.Value, model.GuildId)
                : null;

            this.GuildId = model.GuildId;
            this.ChannelId = model.ChannelId;
            this.Token = model.Token;
            this.Version = model.Version;
            this.MemberId = model.Member.User.Id;
            this.Type = model.Type;
        }
        private bool CheckToken()
        {
            // Tokens last for 15 minutes according to https://discord.com/developers/docs/interactions/slash-commands#responding-to-an-interaction
            return (DateTime.UtcNow - this.CreatedAt.UtcDateTime).TotalMinutes >= 15d;
        }

        /// <summary>
        /// Responds to an Interaction.
        /// <para>
        ///     If you have <see cref="DiscordSocketConfig.AlwaysAcknowledgeInteractions"/> set to <see langword="true"/>, You should use
        ///     <see cref="FollowupAsync(string, bool, Embed, InteractionResponseType, AllowedMentions, RequestOptions)"/> instead.
        /// </para>
        /// </summary>
        /// <param name="text">The text of the message to be sent.</param>
        /// <param name="isTTS"><see langword="true"/> if the message should be read out by a text-to-speech reader, otherwise <see langword="false"/>.</param>
        /// <param name="embed">A <see cref="Embed"/> to send with this response.</param>
        /// <param name="Type">The type of response to this Interaction.</param>
        /// <param name="allowedMentions">The allowed mentions for this response.</param>
        /// <param name="options">The request options for this response.</param>
        /// <returns>
        ///     The <see cref="IMessage"/> sent as the response. If this is the first acknowledgement, it will return null.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        /// <exception cref="InvalidOperationException">The parameters provided were invalid or the token was invalid.</exception>

        public async Task<IMessage> RespondAsync(string text = null, bool isTTS = false, Embed embed = null, InteractionResponseType Type = InteractionResponseType.ChannelMessageWithSource, AllowedMentions allowedMentions = null, RequestOptions options = null)
        {
            if (Type == InteractionResponseType.Pong)
                throw new InvalidOperationException($"Cannot use {Type} on a send message function");

            if (!IsValidToken)
                throw new InvalidOperationException("Interaction token is no longer valid");

            if (Discord.AlwaysAcknowledgeInteractions)
                return await FollowupAsync();

            Preconditions.AtMost(allowedMentions?.RoleIds?.Count ?? 0, 100, nameof(allowedMentions.RoleIds), "A max of 100 role Ids are allowed.");
            Preconditions.AtMost(allowedMentions?.UserIds?.Count ?? 0, 100, nameof(allowedMentions.UserIds), "A max of 100 user Ids are allowed.");

            // check that user flag and user Id list are exclusive, same with role flag and role Id list
            if (allowedMentions != null && allowedMentions.AllowedTypes.HasValue)
            {
                if (allowedMentions.AllowedTypes.Value.HasFlag(AllowedMentionTypes.Users) &&
                    allowedMentions.UserIds != null && allowedMentions.UserIds.Count > 0)
                {
                    throw new ArgumentException("The Users flag is mutually exclusive with the list of User Ids.", nameof(allowedMentions));
                }

                if (allowedMentions.AllowedTypes.Value.HasFlag(AllowedMentionTypes.Roles) &&
                    allowedMentions.RoleIds != null && allowedMentions.RoleIds.Count > 0)
                {
                    throw new ArgumentException("The Roles flag is mutually exclusive with the list of Role Ids.", nameof(allowedMentions));
                }
            }


            var response = new API.InteractionResponse()
            {
                Type = Type,
                Data = new API.InteractionApplicationCommandCallbackData(text)
                {
                    AllowedMentions = allowedMentions?.ToModel(),
                    Embeds = embed != null
                        ? new API.Embed[] { embed.ToModel() }
                        : Optional<API.Embed[]>.Unspecified,
                    TTS = isTTS
                }
            };

            await Discord.Rest.ApiClient.CreateInteractionResponse(response, this.Id, Token, options);
            return null;
        }

        /// <summary>
        ///     Sends a followup message for this interaction.
        /// </summary>
        /// <param name="text">The text of the message to be sent</param>
        /// <param name="isTTS"><see langword="true"/> if the message should be read out by a text-to-speech reader, otherwise <see langword="false"/>.</param>
        /// <param name="embed">A <see cref="Embed"/> to send with this response.</param>
        /// <param name="Type">The type of response to this Interaction.</param>
        /// <param name="allowedMentions">The allowed mentions for this response.</param>
        /// <param name="options">The request options for this response.</param>
        /// <returns>
        ///     The sent message.
        /// </returns>
        public async Task<IMessage> FollowupAsync(string text = null, bool isTTS = false, Embed embed = null, InteractionResponseType Type = InteractionResponseType.ChannelMessageWithSource,
            AllowedMentions allowedMentions = null, RequestOptions options = null)
        {
            if (Type == InteractionResponseType.ACKWithSource || Type == InteractionResponseType.ACKWithSource || Type == InteractionResponseType.Pong)
                throw new InvalidOperationException($"Cannot use {Type} on a send message function");

            if (!IsValidToken)
                throw new InvalidOperationException("Interaction token is no longer valid");

            var args = new API.Rest.CreateWebhookMessageParams(text)
            {
                IsTTS = isTTS,
                Embeds = embed != null
                        ? new API.Embed[] { embed.ToModel() }
                        : Optional<API.Embed[]>.Unspecified,
            };
            
            return await InteractionHelper.SendFollowupAsync(Discord.Rest, args, Token, Channel, options);
            
        }

        /// <summary>
        ///     Acknowledges this interaction with the <see cref="InteractionResponseType.ACKWithSource"/>.
        /// </summary>
        /// <returns>
        ///     A task that represents the asynchronous operation of acknowledging the interaction.
        /// </returns>
        public async Task AcknowledgeAsync(RequestOptions options = null)
        {
            var response = new API.InteractionResponse()
            {
                Type = InteractionResponseType.ACKWithSource,
            };

            await Discord.Rest.ApiClient.CreateInteractionResponse(response, this.Id, Token, options).ConfigureAwait(false);
        }

        IApplicationCommandInteractionData IDiscordInteraction.Data => Data;
        IGuild IDiscordInteraction.Guild => Guild;
    }
}
