using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Model = Discord.API.User;

namespace Discord.Rest
{
    /// <summary>
    ///     Represents a REST-based user.
    /// </summary>
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class RestUser : RestEntity<ulong>, IUser, IUpdateable
    {
        /// <inheritdoc />
        public bool IsBot { get; private set; }
        /// <inheritdoc />
        public string Username { get; private set; }
        /// <inheritdoc />
        public ushort DiscriminatorValue { get; private set; }
        /// <inheritdoc />
        public string AvatarId { get; private set; }
        /// <inheritdoc />
        public UserProperties? PublicFlags { get; private set; }

        /// <inheritdoc />
        public DateTimeOffset CreatedAt => SnowflakeUtils.FromSnowflake(Id);
        /// <inheritdoc />
        public string Discriminator => DiscriminatorValue.ToString("D4");
        /// <inheritdoc />
        public string Mention => MentionUtils.MentionUser(Id);
        /// <inheritdoc />
        public virtual IActivity Activity => null;
        /// <inheritdoc />
        /// <remarks>
        /// Returns <see cref="UserStatus.Offline"/> always as REST API doesn't return Status.
        /// Use Gateway <c>GetUser</c> on the ID to get the status.
        /// </remarks>
        public virtual UserStatus Status => UserStatus.Offline;	
        /// <inheritdoc />
        public virtual IImmutableSet<ClientType> ActiveClients => ImmutableHashSet<ClientType>.Empty;
        /// <inheritdoc />
        public virtual IImmutableList<IActivity> Activities => ImmutableList<IActivity>.Empty;
        /// <inheritdoc />
        public virtual bool IsWebhook => false;

        internal RestUser(BaseDiscordClient discord, ulong id)
            : base(discord, id)
        {
        }
        internal static RestUser Create(BaseDiscordClient discord, Model model)
            => Create(discord, null, model, null);
        internal static RestUser Create(BaseDiscordClient discord, IGuild guild, Model model, ulong? webhookId)
        {
            RestUser entity;
            if (webhookId.HasValue)
                entity = new RestWebhookUser(discord, guild, model.Id, webhookId.Value);
            else
                entity = new RestUser(discord, model.Id);
            entity.Update(model);
            return entity;
        }
        internal virtual void Update(Model model)
        {
            if (model.Avatar.IsSpecified)
                AvatarId = model.Avatar.Value;
            if (model.Discriminator.IsSpecified)
                DiscriminatorValue = ushort.Parse(model.Discriminator.Value, NumberStyles.None, CultureInfo.InvariantCulture);
            if (model.Bot.IsSpecified)
                IsBot = model.Bot.Value;
            if (model.Username.IsSpecified)
                Username = model.Username.Value;
            if (model.PublicFlags.IsSpecified)
                PublicFlags = model.PublicFlags.Value;
        }

        /// <inheritdoc />
        public virtual async Task UpdateAsync(RequestOptions options = null)
        {
            var model = await Discord.ApiClient.GetUserAsync(Id, options).ConfigureAwait(false);
            Update(model);
        }

        /// <summary>
        ///     Creates a direct message channel to this user.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a rest DM channel where the user is the recipient.
        /// </returns>
        public Task<RestDMChannel> CreateDMChannelAsync(RequestOptions options = null)
            => UserHelper.CreateDMChannelAsync(this, Discord, options);

        /// <inheritdoc />
        public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
            => CDN.GetUserAvatarUrl(Id, AvatarId, size, format);

        /// <inheritdoc />
        public string GetDefaultAvatarUrl()
            => CDN.GetDefaultUserAvatarUrl(DiscriminatorValue);

        /// <summary>
        ///     Gets the Username#Discriminator of the user.
        /// </summary>
        /// <returns>
        ///     A string that resolves to Username#Discriminator of the user.
        /// </returns>
        public override string ToString() => $"{Username}#{Discriminator}";
        private string DebuggerDisplay => $"{Username}#{Discriminator} ({Id}{(IsBot ? ", Bot" : "")})";

        //IUser
        /// <inheritdoc />
        async Task<IDMChannel> IUser.CreateDMChannelAsync(RequestOptions options)
            => await CreateDMChannelAsync(options).ConfigureAwait(false);
    }
}
