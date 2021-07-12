using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WidgetModel = Discord.API.GuildWidget;
using Model = Discord.API.Guild;

namespace Discord.Rest
{
    /// <summary>
    ///     Represents a REST-based guild/server.
    /// </summary>
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class RestGuild : RestEntity<ulong>, IGuild, IUpdateable
    {
        private ImmutableDictionary<ulong, RestRole> _roles;
        private ImmutableArray<GuildEmote> _emotes;
        private ImmutableArray<string> _features;

        /// <inheritdoc />
        public string Name { get; private set; }
        /// <inheritdoc />
        public int AFKTimeout { get; private set; }
        /// <inheritdoc />
        public bool IsWidgetEnabled { get; private set; }
        /// <inheritdoc />
        public VerificationLevel VerificationLevel { get; private set; }
        /// <inheritdoc />
        public MfaLevel MfaLevel { get; private set; }
        /// <inheritdoc />
        public DefaultMessageNotifications DefaultMessageNotifications { get; private set; }
        /// <inheritdoc />
        public ExplicitContentFilterLevel ExplicitContentFilter { get; private set; }

        /// <inheritdoc />
        public ulong? AFKChannelId { get; private set; }
        /// <inheritdoc />
        public ulong? WidgetChannelId { get; private set; }
        /// <inheritdoc />
        public ulong? SystemChannelId { get; private set; }
        /// <inheritdoc />
        public ulong? RulesChannelId { get; private set; }
        /// <inheritdoc />
        public ulong? PublicUpdatesChannelId { get; private set; }
        /// <inheritdoc />
        public ulong OwnerId { get; private set; }
        /// <inheritdoc />
        public string VoiceRegionId { get; private set; }
        /// <inheritdoc />
        public string IconId { get; private set; }
        /// <inheritdoc />
        public string SplashId { get; private set; }
        /// <inheritdoc />
        public string DiscoverySplashId { get; private set; }
        internal bool Available { get; private set; }
        /// <inheritdoc />
        public ulong? ApplicationId { get; private set; }
        /// <inheritdoc />
        public PremiumTier PremiumTier { get; private set; }
        /// <inheritdoc />
        public string BannerId { get; private set; }
        /// <inheritdoc />
        public string VanityURLCode { get; private set; }
        /// <inheritdoc />
        public SystemChannelMessageDeny SystemChannelFlags { get; private set; }
        /// <inheritdoc />
        public string Description { get; private set; }
        /// <inheritdoc />
        public int PremiumSubscriptionCount { get; private set; }
        /// <inheritdoc />
        public string PreferredLocale { get; private set; }
        /// <inheritdoc />
        public int? MaxPresences { get; private set; }
        /// <inheritdoc />
        public int? MaxMembers { get; private set; }
        /// <inheritdoc />
        public int? MaxVideoChannelUsers { get; private set; }
        /// <inheritdoc />
        public int? ApproximateMemberCount { get; private set; }
        /// <inheritdoc />
        public int? ApproximatePresenceCount { get; private set; }

        /// <inheritdoc />
        public CultureInfo PreferredCulture { get; private set; }

        /// <inheritdoc />
        public DateTimeOffset CreatedAt => SnowflakeUtils.FromSnowflake(Id);

        /// <inheritdoc />
        public string IconUrl => CDN.GetGuildIconUrl(Id, IconId);
        /// <inheritdoc />
        public string SplashUrl => CDN.GetGuildSplashUrl(Id, SplashId);
        /// <inheritdoc />
        public string DiscoverySplashUrl => CDN.GetGuildDiscoverySplashUrl(Id, DiscoverySplashId);
        /// <inheritdoc />
        public string BannerUrl => CDN.GetGuildBannerUrl(Id, BannerId);

        /// <summary>
        ///     Gets the built-in role containing all users in this guild.
        /// </summary>
        public RestRole EveryoneRole => GetRole(Id);

        /// <summary>
        ///     Gets a collection of all roles in this guild.
        /// </summary>
        public IReadOnlyCollection<RestRole> Roles => _roles.ToReadOnlyCollection();
        /// <inheritdoc />
        public IReadOnlyCollection<GuildEmote> Emotes => _emotes;
        /// <inheritdoc />
        public IReadOnlyCollection<string> Features => _features;

        internal RestGuild(BaseDiscordClient client, ulong id)
            : base(client, id)
        {
        }
        internal static RestGuild Create(BaseDiscordClient discord, Model model)
        {
            var entity = new RestGuild(discord, model.Id);
            entity.Update(model);
            return entity;
        }
        internal void Update(Model model)
        {
            AFKChannelId = model.AFKChannelId;
            if (model.WidgetChannelId.IsSpecified)
                WidgetChannelId = model.WidgetChannelId.Value;
            SystemChannelId = model.SystemChannelId;
            RulesChannelId = model.RulesChannelId;
            PublicUpdatesChannelId = model.PublicUpdatesChannelId;
            AFKTimeout = model.AFKTimeout;
            if (model.WidgetEnabled.IsSpecified)
                IsWidgetEnabled = model.WidgetEnabled.Value;
            IconId = model.Icon;
            Name = model.Name;
            OwnerId = model.OwnerId;
            VoiceRegionId = model.Region;
            SplashId = model.Splash;
            DiscoverySplashId = model.DiscoverySplash;
            VerificationLevel = model.VerificationLevel;
            MfaLevel = model.MfaLevel;
            DefaultMessageNotifications = model.DefaultMessageNotifications;
            ExplicitContentFilter = model.ExplicitContentFilter;
            ApplicationId = model.ApplicationId;
            PremiumTier = model.PremiumTier;
            VanityURLCode = model.VanityURLCode;
            BannerId = model.Banner;
            SystemChannelFlags = model.SystemChannelFlags;
            Description = model.Description;
            PremiumSubscriptionCount = model.PremiumSubscriptionCount.GetValueOrDefault();
            if (model.MaxPresences.IsSpecified)
                MaxPresences = model.MaxPresences.Value ?? 25000;
            if (model.MaxMembers.IsSpecified)
                MaxMembers = model.MaxMembers.Value;
            if (model.MaxVideoChannelUsers.IsSpecified)
                MaxVideoChannelUsers = model.MaxVideoChannelUsers.Value;
            PreferredLocale = model.PreferredLocale;
            PreferredCulture = new CultureInfo(PreferredLocale);
            if (model.ApproximateMemberCount.IsSpecified)
                ApproximateMemberCount = model.ApproximateMemberCount.Value;
            if (model.ApproximatePresenceCount.IsSpecified)
                ApproximatePresenceCount = model.ApproximatePresenceCount.Value;

            if (model.Emojis != null)
            {
                var emotes = ImmutableArray.CreateBuilder<GuildEmote>(model.Emojis.Length);
                for (int i = 0; i < model.Emojis.Length; i++)
                    emotes.Add(model.Emojis[i].ToEntity());
                _emotes = emotes.ToImmutableArray();
            }
            else
                _emotes = ImmutableArray.Create<GuildEmote>();

            if (model.Features != null)
                _features = model.Features.ToImmutableArray();
            else
                _features = ImmutableArray.Create<string>();

            var roles = ImmutableDictionary.CreateBuilder<ulong, RestRole>();
            if (model.Roles != null)
            {
                for (int i = 0; i < model.Roles.Length; i++)
                    roles[model.Roles[i].Id] = RestRole.Create(Discord, this, model.Roles[i]);
            }
            _roles = roles.ToImmutable();

            Available = true;
        }
        internal void Update(WidgetModel model)
        {
            WidgetChannelId = model.ChannelId;
            IsWidgetEnabled = model.Enabled;
        }

        //General
        /// <inheritdoc />
        public async Task UpdateAsync(RequestOptions options = null)
            => Update(await Discord.ApiClient.GetGuildAsync(Id, false, options).ConfigureAwait(false));
        /// <summary>
        ///     Updates this object's properties with its current state.
        /// </summary>
        /// <param name="withCounts">
        ///     If true, <see cref="ApproximateMemberCount"/> and <see cref="ApproximatePresenceCount"/>
        ///     will be updated as well.
        /// </param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <remarks>
        ///     If <paramref name="withCounts"/> is true, <see cref="ApproximateMemberCount"/> and
        ///     <see cref="ApproximatePresenceCount"/> will be updated as well.
        /// </remarks>
        public async Task UpdateAsync(bool withCounts, RequestOptions options = null)
            => Update(await Discord.ApiClient.GetGuildAsync(Id, withCounts, options).ConfigureAwait(false));
        /// <inheritdoc />
        public Task DeleteAsync(RequestOptions options = null)
            => GuildHelper.DeleteAsync(this, Discord, options);

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public async Task ModifyAsync(Action<GuildProperties> func, RequestOptions options = null)
        {
            var model = await GuildHelper.ModifyAsync(this, Discord, func, options).ConfigureAwait(false);
            Update(model);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public async Task ModifyWidgetAsync(Action<GuildWidgetProperties> func, RequestOptions options = null)
        {
            var model = await GuildHelper.ModifyWidgetAsync(this, Discord, func, options).ConfigureAwait(false);
            Update(model);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="args" /> is <see langword="null"/>.</exception>
        public async Task ReorderChannelsAsync(IEnumerable<ReorderChannelProperties> args, RequestOptions options = null)
        {
            var arr = args.ToArray();
            await GuildHelper.ReorderChannelsAsync(this, Discord, arr, options).ConfigureAwait(false);
        }
        /// <inheritdoc />
        public async Task ReorderRolesAsync(IEnumerable<ReorderRoleProperties> args, RequestOptions options = null)
        {
            var models = await GuildHelper.ReorderRolesAsync(this, Discord, args, options).ConfigureAwait(false);
            foreach (var model in models)
            {
                var role = GetRole(model.Id);
                role?.Update(model);
            }
        }

        /// <inheritdoc />
        public Task LeaveAsync(RequestOptions options = null)
            => GuildHelper.LeaveAsync(this, Discord, options);

        //Bans
        //Bans
        /// <summary>
        ///     Gets a collection of all users banned in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     ban objects that this guild currently possesses, with each object containing the user banned and reason
        ///     behind the ban.
        /// </returns>
        public Task<IReadOnlyCollection<RestBan>> GetBansAsync(RequestOptions options = null)
            => GuildHelper.GetBansAsync(this, Discord, options);
        /// <summary>
        ///     Gets a ban object for a banned user.
        /// </summary>
        /// <param name="user">The banned user.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a ban object, which
        ///     contains the user information and the reason for the ban; <see langword="null"/> if the ban entry cannot be found.
        /// </returns>
        public Task<RestBan> GetBanAsync(IUser user, RequestOptions options = null)
            => GuildHelper.GetBanAsync(this, Discord, user.Id, options);
        /// <summary>
        ///     Gets a ban object for a banned user.
        /// </summary>
        /// <param name="userId">The snowflake identifier for the banned user.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a ban object, which
        ///     contains the user information and the reason for the ban; <see langword="null"/> if the ban entry cannot be found.
        /// </returns>
        public Task<RestBan> GetBanAsync(ulong userId, RequestOptions options = null)
            => GuildHelper.GetBanAsync(this, Discord, userId, options);

        /// <inheritdoc />
        public Task AddBanAsync(IUser user, int pruneDays = 0, string reason = null, RequestOptions options = null)
            => GuildHelper.AddBanAsync(this, Discord, user.Id, pruneDays, reason, options);
        /// <inheritdoc />
        public Task AddBanAsync(ulong userId, int pruneDays = 0, string reason = null, RequestOptions options = null)
            => GuildHelper.AddBanAsync(this, Discord, userId, pruneDays, reason, options);

        /// <inheritdoc />
        public Task RemoveBanAsync(IUser user, RequestOptions options = null)
            => GuildHelper.RemoveBanAsync(this, Discord, user.Id, options);
        /// <inheritdoc />
        public Task RemoveBanAsync(ulong userId, RequestOptions options = null)
            => GuildHelper.RemoveBanAsync(this, Discord, userId, options);

        //Channels
        /// <summary>
        ///     Gets a collection of all channels in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     generic channels found within this guild.
        /// </returns>
        public Task<IReadOnlyCollection<RestGuildChannel>> GetChannelsAsync(RequestOptions options = null)
            => GuildHelper.GetChannelsAsync(this, Discord, options);

        /// <summary>
        ///     Gets a channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the channel.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the generic channel
        ///     associated with the specified <paramref name="id"/>; <see langword="null"/> if none is found.
        /// </returns>
        public Task<RestGuildChannel> GetChannelAsync(ulong id, RequestOptions options = null)
            => GuildHelper.GetChannelAsync(this, Discord, id, options);

        /// <summary>
        ///     Gets a text channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the text channel.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the text channel
        ///     associated with the specified <paramref name="id"/>; <see langword="null"/> if none is found.
        /// </returns>
        public async Task<RestTextChannel> GetTextChannelAsync(ulong id, RequestOptions options = null)
        {
            var channel = await GuildHelper.GetChannelAsync(this, Discord, id, options).ConfigureAwait(false);
            return channel as RestTextChannel;
        }

        /// <summary>
        ///     Gets a collection of all text channels in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     message channels found within this guild.
        /// </returns>
        public async Task<IReadOnlyCollection<RestTextChannel>> GetTextChannelsAsync(RequestOptions options = null)
        {
            var channels = await GuildHelper.GetChannelsAsync(this, Discord, options).ConfigureAwait(false);
            return channels.OfType<RestTextChannel>().ToImmutableArray();
        }

        /// <summary>
        ///     Gets a voice channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the voice channel.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the voice channel associated
        ///     with the specified <paramref name="id"/>; <see langword="null"/> if none is found.
        /// </returns>
        public async Task<RestVoiceChannel> GetVoiceChannelAsync(ulong id, RequestOptions options = null)
        {
            var channel = await GuildHelper.GetChannelAsync(this, Discord, id, options).ConfigureAwait(false);
            return channel as RestVoiceChannel;
        }

        /// <summary>
        ///     Gets a collection of all voice channels in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     voice channels found within this guild.
        /// </returns>
        public async Task<IReadOnlyCollection<RestVoiceChannel>> GetVoiceChannelsAsync(RequestOptions options = null)
        {
            var channels = await GuildHelper.GetChannelsAsync(this, Discord, options).ConfigureAwait(false);
            return channels.OfType<RestVoiceChannel>().ToImmutableArray();
        }

        /// <summary>
        ///     Gets a collection of all category channels in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     category channels found within this guild.
        /// </returns>
        public async Task<IReadOnlyCollection<RestCategoryChannel>> GetCategoryChannelsAsync(RequestOptions options = null)
        {
            var channels = await GuildHelper.GetChannelsAsync(this, Discord, options).ConfigureAwait(false);
            return channels.OfType<RestCategoryChannel>().ToImmutableArray();
        }

        /// <summary>
        ///     Gets the AFK voice channel in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the voice channel that the
        ///     AFK users will be moved to after they have idled for too long; <see langword="null"/> if none is set.
        /// </returns>
        public async Task<RestVoiceChannel> GetAFKChannelAsync(RequestOptions options = null)
        {
            var afkId = AFKChannelId;
            if (afkId.HasValue)
            {
                var channel = await GuildHelper.GetChannelAsync(this, Discord, afkId.Value, options).ConfigureAwait(false);
                return channel as RestVoiceChannel;
            }
            return null;
        }

        /// <summary>
        ///     Gets the first viewable text channel in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the first viewable text
        ///     channel in this guild; <see langword="null"/> if none is found.
        /// </returns>
        public async Task<RestTextChannel> GetDefaultChannelAsync(RequestOptions options = null)
        {
            var channels = await GetTextChannelsAsync(options).ConfigureAwait(false);
            var user = await GetCurrentUserAsync(options).ConfigureAwait(false);
            return channels
                .Where(c => user.GetPermissions(c).ViewChannel)
                .OrderBy(c => c.Position)
                .FirstOrDefault();
        }

        /// <summary>
        ///     Gets the widget channel (i.e. the channel set in the guild's widget settings) in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the widget channel set
        ///     within the server's widget settings; <see langword="null"/> if none is set.
        /// </returns>
        public async Task<RestGuildChannel> GetWidgetChannelAsync(RequestOptions options = null)
        {
            var widgetChannelId = WidgetChannelId;
            if (widgetChannelId.HasValue)
                return await GuildHelper.GetChannelAsync(this, Discord, widgetChannelId.Value, options).ConfigureAwait(false);
            return null;
        }

        /// <summary>
        ///     Gets the text channel where guild notices such as welcome messages and boost events are posted.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the text channel
        ///     where guild notices such as welcome messages and boost events are poste; <see langword="null"/> if none is found.
        /// </returns>
        public async Task<RestTextChannel> GetSystemChannelAsync(RequestOptions options = null)
        {
            var systemId = SystemChannelId;
            if (systemId.HasValue)
            {
                var channel = await GuildHelper.GetChannelAsync(this, Discord, systemId.Value, options).ConfigureAwait(false);
                return channel as RestTextChannel;
            }
            return null;
        }

        /// <summary>
        ///     Gets the text channel where Community guilds can display rules and/or guidelines.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the text channel
        ///     where Community guilds can display rules and/or guidelines; <see langword="null"/> if none is set.
        /// </returns>
        public async Task<RestTextChannel> GetRulesChannelAsync(RequestOptions options = null)
        {
            var rulesChannelId = RulesChannelId;
            if (rulesChannelId.HasValue)
            {
                var channel = await GuildHelper.GetChannelAsync(this, Discord, rulesChannelId.Value, options).ConfigureAwait(false);
                return channel as RestTextChannel;
            }
            return null;
        }

        /// <summary>
        ///     Gets the text channel channel where admins and moderators of Community guilds receive notices from Discord.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the text channel channel where
        ///     admins and moderators of Community guilds receive notices from Discord; <see langword="null"/> if none is set.
        /// </returns>
        public async Task<RestTextChannel> GetPublicUpdatesChannelAsync(RequestOptions options = null)
        {
            var publicUpdatesChannelId = PublicUpdatesChannelId;
            if (publicUpdatesChannelId.HasValue)
            {
                var channel = await GuildHelper.GetChannelAsync(this, Discord, publicUpdatesChannelId.Value, options).ConfigureAwait(false);
                return channel as RestTextChannel;
            }
            return null;
        }

        /// <summary>
        ///     Creates a new text channel in this guild.
        /// </summary>
        /// <example>
        ///     The following example creates a new text channel under an existing category named <c>Wumpus</c> with a set topic.
        ///     <code language="cs">
        ///     var categories = await guild.GetCategoriesAsync();
        ///     var targetCategory = categories.FirstOrDefault(x => x.Name == "wumpus");
        ///     if (targetCategory == null) return;
        ///     await Context.Guild.CreateTextChannelAsync(name, x =>
        ///     {
        ///         x.CategoryId = targetCategory.Id;
        ///         x.Topic = $"This channel was created at {DateTimeOffset.UtcNow} by {user}.";
        ///     });
        ///     </code>
        /// </example>
        /// <param name="name">The new name for the text channel.</param>
        /// <param name="func">The delegate containing the properties to be applied to the channel upon its creation.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     text channel.
        /// </returns>
        public Task<RestTextChannel> CreateTextChannelAsync(string name, Action<TextChannelProperties> func = null, RequestOptions options = null)
            => GuildHelper.CreateTextChannelAsync(this, Discord, name, options, func);
        /// <summary>
        ///     Creates a voice channel with the provided name.
        /// </summary>
        /// <param name="name">The name of the new channel.</param>
        /// <param name="func">The delegate containing the properties to be applied to the channel upon its creation.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null"/>.</exception>
        /// <returns>
        ///     The created voice channel.
        /// </returns>
        public Task<RestVoiceChannel> CreateVoiceChannelAsync(string name, Action<VoiceChannelProperties> func = null, RequestOptions options = null)
            => GuildHelper.CreateVoiceChannelAsync(this, Discord, name, options, func);
        /// <summary>
        ///     Creates a category channel with the provided name.
        /// </summary>
        /// <param name="name">The name of the new channel.</param>
        /// <param name="func">The delegate containing the properties to be applied to the channel upon its creation.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null"/>.</exception>
        /// <returns>
        ///     The created category channel.
        /// </returns>
        public Task<RestCategoryChannel> CreateCategoryChannelAsync(string name, Action<GuildChannelProperties> func = null, RequestOptions options = null)
            => GuildHelper.CreateCategoryChannelAsync(this, Discord, name, options, func);

        /// <summary>
        ///     Gets a collection of all the voice regions this guild can access.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     voice regions the guild can access.
        /// </returns>
        public Task<IReadOnlyCollection<RestVoiceRegion>> GetVoiceRegionsAsync(RequestOptions options = null)
            => GuildHelper.GetVoiceRegionsAsync(this, Discord, options);

        //Integrations
        public Task<IReadOnlyCollection<RestGuildIntegration>> GetIntegrationsAsync(RequestOptions options = null)
            => GuildHelper.GetIntegrationsAsync(this, Discord, options);
        public Task<RestGuildIntegration> CreateIntegrationAsync(ulong id, string type, RequestOptions options = null)
            => GuildHelper.CreateIntegrationAsync(this, Discord, id, type, options);

        //Invites
        /// <summary>
        ///     Gets a collection of all invites in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     invite metadata, each representing information for an invite found within this guild.
        /// </returns>
        public Task<IReadOnlyCollection<RestInviteMetadata>> GetInvitesAsync(RequestOptions options = null)
            => GuildHelper.GetInvitesAsync(this, Discord, options);
        /// <summary>
        ///     Gets the vanity invite URL of this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A partial metadata of the vanity invite found within this guild.
        /// </returns>
        public Task<RestInviteMetadata> GetVanityInviteAsync(RequestOptions options = null)
            => GuildHelper.GetVanityInviteAsync(this, Discord, options);

        //Roles
        /// <summary>
        ///     Gets a role in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the role.</param>
        /// <returns>
        ///     A role that is associated with the specified <paramref name="id"/>; <see langword="null"/> if none is found.
        /// </returns>
        public RestRole GetRole(ulong id)
        {
            if (_roles.TryGetValue(id, out RestRole value))
                return value;
            return null;
        }

        /// <inheritdoc />
        public Task<RestRole> CreateRoleAsync(string name, GuildPermissions? permissions = default(GuildPermissions?), Color? color = default(Color?),
            bool isHoisted = false, RequestOptions options = null)
            => CreateRoleAsync(name, permissions, color, isHoisted, false, options);

        /// <summary>
        ///     Creates a new role with the provided name.
        /// </summary>
        /// <param name="name">The new name for the role.</param>
        /// <param name="permissions">The guild permission that the role should possess.</param>
        /// <param name="color">The color of the role.</param>
        /// <param name="isHoisted">Whether the role is separated from others on the sidebar.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <param name="isMentionable">Whether the role can be mentioned.</param>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     role.
        /// </returns>
        public async Task<RestRole> CreateRoleAsync(string name, GuildPermissions? permissions = default(GuildPermissions?), Color? color = default(Color?),
            bool isHoisted = false, bool isMentionable = false, RequestOptions options = null)
        {
            var role = await GuildHelper.CreateRoleAsync(this, Discord, name, permissions, color, isHoisted, isMentionable, options).ConfigureAwait(false);
            _roles = _roles.Add(role.Id, role);
            return role;
        }

        //Users
        /// <summary>
        ///     Gets a collection of all users in this guild.
        /// </summary>
        /// <remarks>
        ///     This method retrieves all users found within this guild.
        /// </remarks>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a collection of guild
        ///     users found within this guild.
        /// </returns>
        public IAsyncEnumerable<IReadOnlyCollection<RestGuildUser>> GetUsersAsync(RequestOptions options = null)
            => GuildHelper.GetUsersAsync(this, Discord, null, null, options);

        /// <inheritdoc />
        public Task<RestGuildUser> AddGuildUserAsync(ulong id, string accessToken, Action<AddGuildUserProperties> func = null, RequestOptions options = null)
            => GuildHelper.AddGuildUserAsync(this, Discord, id, accessToken, func, options);

        /// <summary>
        ///     Gets a user from this guild.
        /// </summary>
        /// <remarks>
        ///     This method retrieves a user found within this guild.
        /// </remarks>
        /// <param name="id">The snowflake identifier of the user.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the guild user
        ///     associated with the specified <paramref name="id"/>; <see langword="null"/> if none is found.
        /// </returns>
        public Task<RestGuildUser> GetUserAsync(ulong id, RequestOptions options = null)
            => GuildHelper.GetUserAsync(this, Discord, id, options);

        /// <summary>
        ///     Gets the current user for this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the currently logged-in
        ///     user within this guild.
        /// </returns>
        public Task<RestGuildUser> GetCurrentUserAsync(RequestOptions options = null)
            => GuildHelper.GetUserAsync(this, Discord, Discord.CurrentUser.Id, options);

        /// <summary>
        ///     Gets the owner of this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the owner of this guild.
        /// </returns>
        public Task<RestGuildUser> GetOwnerAsync(RequestOptions options = null)
            => GuildHelper.GetUserAsync(this, Discord, OwnerId, options);

        /// <inheritdoc />
        /// <summary>
        ///     Prunes inactive users.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method removes all users that have not logged on in the provided number of <paramref name="days"/>.
        ///     </para>
        ///     <para>
        ///         If <paramref name="simulate" /> is <c>true</c>, this method will only return the number of users that
        ///         would be removed without kicking the users.
        ///     </para>
        /// </remarks>
        /// <param name="days">The number of days required for the users to be kicked.</param>
        /// <param name="simulate">Whether this prune action is a simulation.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous prune operation. The task result contains the number of users to
        ///     be or has been removed from this guild.
        /// </returns>
        public Task<int> PruneUsersAsync(int days = 30, bool simulate = false, RequestOptions options = null, IEnumerable<ulong> includeRoleIds = null)
            => GuildHelper.PruneUsersAsync(this, Discord, days, simulate, options, includeRoleIds);

        /// <summary>
        ///     Gets a collection of users in this guild that the name or nickname starts with the
        ///     provided <see cref="string"/> at <paramref name="query"/>.
        /// </summary>
        /// <remarks>
        ///     The <paramref name="limit"/> can not be higher than <see cref="DiscordConfig.MaxUsersPerBatch"/>.
        /// </remarks>
        /// <param name="query">The partial name or nickname to search.</param>
        /// <param name="limit">The maximum number of users to be gotten.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a collection of guild
        ///     users that the name or nickname starts with the provided <see cref="string"/> at <paramref name="query"/>.
        /// </returns>
        public Task<IReadOnlyCollection<RestGuildUser>> SearchUsersAsync(string query, int limit = DiscordConfig.MaxUsersPerBatch, RequestOptions options = null)
            => GuildHelper.SearchUsersAsync(this, Discord, query, limit, options);

        //Audit logs
        /// <summary>
        ///     Gets the specified number of audit log entries for this guild.
        /// </summary>
        /// <param name="limit">The number of audit log entries to fetch.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <param name="beforeId">The audit log entry ID to get entries before.</param>
        /// <param name="actionType">The type of actions to filter.</param>
        /// <param name="userId">The user ID to filter entries for.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection
        ///     of the requested audit log entries.
        /// </returns>
        public IAsyncEnumerable<IReadOnlyCollection<RestAuditLogEntry>> GetAuditLogsAsync(int limit, RequestOptions options = null, ulong? beforeId = null, ulong? userId = null, ActionType? actionType = null)
            => GuildHelper.GetAuditLogsAsync(this, Discord, beforeId, limit, options, userId: userId, actionType: actionType);

        //Webhooks
        /// <summary>
        ///     Gets a webhook found within this guild.
        /// </summary>
        /// <param name="id">The identifier for the webhook.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the webhook with the
        ///     specified <paramref name="id"/>; <see langword="null"/> if none is found.
        /// </returns>
        public Task<RestWebhook> GetWebhookAsync(ulong id, RequestOptions options = null)
            => GuildHelper.GetWebhookAsync(this, Discord, id, options);

        /// <summary>
        ///     Gets a collection of all webhook from this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection
        ///     of webhooks found within the guild.
        /// </returns>
        public Task<IReadOnlyCollection<RestWebhook>> GetWebhooksAsync(RequestOptions options = null)
            => GuildHelper.GetWebhooksAsync(this, Discord, options);

        /// <summary>
        ///     Returns the name of the guild.
        /// </summary>
        /// <returns>
        ///     The name of the guild.
        /// </returns>
        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id})";

        //Emotes
        /// <inheritdoc />
        public Task<IReadOnlyCollection<GuildEmote>> GetEmotesAsync(RequestOptions options = null)
            => GuildHelper.GetEmotesAsync(this, Discord, options);
        /// <inheritdoc />
        public Task<GuildEmote> GetEmoteAsync(ulong id, RequestOptions options = null)
            => GuildHelper.GetEmoteAsync(this, Discord, id, options);
        /// <inheritdoc />
        public Task<GuildEmote> CreateEmoteAsync(string name, Image image, Optional<IEnumerable<IRole>> roles = default(Optional<IEnumerable<IRole>>), RequestOptions options = null)
            => GuildHelper.CreateEmoteAsync(this, Discord, name, image, roles, options);
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public Task<GuildEmote> ModifyEmoteAsync(GuildEmote emote, Action<EmoteProperties> func, RequestOptions options = null)
            => GuildHelper.ModifyEmoteAsync(this, Discord, emote.Id, func, options);
        /// <inheritdoc />
        public Task DeleteEmoteAsync(GuildEmote emote, RequestOptions options = null)
            => GuildHelper.DeleteEmoteAsync(this, Discord, emote.Id, options);

        //IGuild
        /// <inheritdoc />
        bool IGuild.Available => Available;
        /// <inheritdoc />
        IAudioClient IGuild.AudioClient => null;
        /// <inheritdoc />
        IRole IGuild.EveryoneRole => EveryoneRole;
        /// <inheritdoc />
        IReadOnlyCollection<IRole> IGuild.Roles => Roles;

        /// <inheritdoc />
        async Task<IReadOnlyCollection<IBan>> IGuild.GetBansAsync(RequestOptions options)
            => await GetBansAsync(options).ConfigureAwait(false);
        /// <inheritdoc/>
        async Task<IBan> IGuild.GetBanAsync(IUser user, RequestOptions options)
            => await GetBanAsync(user, options).ConfigureAwait(false);
        /// <inheritdoc/>
        async Task<IBan> IGuild.GetBanAsync(ulong userId, RequestOptions options)
            => await GetBanAsync(userId, options).ConfigureAwait(false);

        /// <inheritdoc />
        async Task<IReadOnlyCollection<IGuildChannel>> IGuild.GetChannelsAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetChannelsAsync(options).ConfigureAwait(false);
            else
                return ImmutableArray.Create<IGuildChannel>();
        }
        /// <inheritdoc />
        async Task<IGuildChannel> IGuild.GetChannelAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetChannelAsync(id, options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<IReadOnlyCollection<ITextChannel>> IGuild.GetTextChannelsAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetTextChannelsAsync(options).ConfigureAwait(false);
            else
                return ImmutableArray.Create<ITextChannel>();
        }
        /// <inheritdoc />
        async Task<ITextChannel> IGuild.GetTextChannelAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetTextChannelAsync(id, options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<IReadOnlyCollection<IVoiceChannel>> IGuild.GetVoiceChannelsAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetVoiceChannelsAsync(options).ConfigureAwait(false);
            else
                return ImmutableArray.Create<IVoiceChannel>();
        }
        /// <inheritdoc />
        async Task<IReadOnlyCollection<ICategoryChannel>> IGuild.GetCategoriesAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetCategoryChannelsAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<IVoiceChannel> IGuild.GetVoiceChannelAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetVoiceChannelAsync(id, options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<IVoiceChannel> IGuild.GetAFKChannelAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetAFKChannelAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<ITextChannel> IGuild.GetDefaultChannelAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetDefaultChannelAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<IGuildChannel> IGuild.GetWidgetChannelAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetWidgetChannelAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<ITextChannel> IGuild.GetSystemChannelAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetSystemChannelAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<ITextChannel> IGuild.GetRulesChannelAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetRulesChannelAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<ITextChannel> IGuild.GetPublicUpdatesChannelAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetPublicUpdatesChannelAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<ITextChannel> IGuild.CreateTextChannelAsync(string name, Action<TextChannelProperties> func, RequestOptions options)
            => await CreateTextChannelAsync(name, func, options).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<IVoiceChannel> IGuild.CreateVoiceChannelAsync(string name, Action<VoiceChannelProperties> func, RequestOptions options)
            => await CreateVoiceChannelAsync(name, func, options).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<ICategoryChannel> IGuild.CreateCategoryChannelAsync(string name, Action<GuildChannelProperties> func, RequestOptions options)
            => await CreateCategoryChannelAsync(name, func, options).ConfigureAwait(false);

        /// <inheritdoc />
        async Task<IReadOnlyCollection<IVoiceRegion>> IGuild.GetVoiceRegionsAsync(RequestOptions options)
            => await GetVoiceRegionsAsync(options).ConfigureAwait(false);

        /// <inheritdoc />
        async Task<IReadOnlyCollection<IGuildIntegration>> IGuild.GetIntegrationsAsync(RequestOptions options)
            => await GetIntegrationsAsync(options).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<IGuildIntegration> IGuild.CreateIntegrationAsync(ulong id, string type, RequestOptions options)
            => await CreateIntegrationAsync(id, type, options).ConfigureAwait(false);

        /// <inheritdoc />
        async Task<IReadOnlyCollection<IInviteMetadata>> IGuild.GetInvitesAsync(RequestOptions options)
            => await GetInvitesAsync(options).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<IInviteMetadata> IGuild.GetVanityInviteAsync(RequestOptions options)
            => await GetVanityInviteAsync(options).ConfigureAwait(false);

        /// <inheritdoc />
        IRole IGuild.GetRole(ulong id)
            => GetRole(id);
        /// <inheritdoc />
        async Task<IRole> IGuild.CreateRoleAsync(string name, GuildPermissions? permissions, Color? color, bool isHoisted, RequestOptions options)
            => await CreateRoleAsync(name, permissions, color, isHoisted, false, options).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<IRole> IGuild.CreateRoleAsync(string name, GuildPermissions? permissions, Color? color, bool isHoisted, bool isMentionable, RequestOptions options)
            => await CreateRoleAsync(name, permissions, color, isHoisted, isMentionable, options).ConfigureAwait(false);

        /// <inheritdoc />
        async Task<IGuildUser> IGuild.AddGuildUserAsync(ulong userId, string accessToken, Action<AddGuildUserProperties> func, RequestOptions options)
            => await AddGuildUserAsync(userId, accessToken, func, options);

        /// <inheritdoc />
        async Task<IGuildUser> IGuild.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetUserAsync(id, options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<IGuildUser> IGuild.GetCurrentUserAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetCurrentUserAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<IGuildUser> IGuild.GetOwnerAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetOwnerAsync(options).ConfigureAwait(false);
            else
                return null;
        }
        /// <inheritdoc />
        async Task<IReadOnlyCollection<IGuildUser>> IGuild.GetUsersAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return (await GetUsersAsync(options).FlattenAsync().ConfigureAwait(false)).ToImmutableArray();
            else
                return ImmutableArray.Create<IGuildUser>();
        }
        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Downloading users is not supported for a REST-based guild.</exception>
        Task IGuild.DownloadUsersAsync() =>
            throw new NotSupportedException();
        /// <inheritdoc />
        async Task<IReadOnlyCollection<IGuildUser>> IGuild.SearchUsersAsync(string query, int limit, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await SearchUsersAsync(query, limit, options).ConfigureAwait(false);
            else
                return ImmutableArray.Create<IGuildUser>();
        }

        async Task<IReadOnlyCollection<IAuditLogEntry>> IGuild.GetAuditLogsAsync(int limit, CacheMode cacheMode, RequestOptions options,
            ulong? beforeId, ulong? userId, ActionType? actionType)
        {
            if (cacheMode == CacheMode.AllowDownload)
                return (await GetAuditLogsAsync(limit, options, beforeId: beforeId, userId: userId, actionType: actionType).FlattenAsync().ConfigureAwait(false)).ToImmutableArray();
            else
                return ImmutableArray.Create<IAuditLogEntry>();
        }

        /// <inheritdoc />
        async Task<IWebhook> IGuild.GetWebhookAsync(ulong id, RequestOptions options)
            => await GetWebhookAsync(id, options).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<IReadOnlyCollection<IWebhook>> IGuild.GetWebhooksAsync(RequestOptions options)
            => await GetWebhooksAsync(options).ConfigureAwait(false);
    }
}
