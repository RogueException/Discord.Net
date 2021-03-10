using Discord.Audio;
using Discord.Rest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChannelModel = Discord.API.Channel;
using EmojiUpdateModel = Discord.API.Gateway.GuildEmojiUpdateEvent;
using ExtendedModel = Discord.API.Gateway.ExtendedGuild;
using GuildSyncModel = Discord.API.Gateway.GuildSyncEvent;
using MemberModel = Discord.API.GuildMember;
using Model = Discord.API.Guild;
using PresenceModel = Discord.API.Presence;
using RoleModel = Discord.API.Role;
using UserModel = Discord.API.User;
using VoiceStateModel = Discord.API.VoiceState;

namespace Discord.WebSocket
{
    /// <summary>
    ///     Represents a WebSocket-based guild object.
    /// </summary>
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class SocketGuild : SocketEntity<ulong>, IGuild, IDisposable
    {
#pragma warning disable IDISP002, IDISP006
        private readonly SemaphoreSlim _audioLock;
        private TaskCompletionSource<bool> _syncPromise, _downloaderPromise;
        private TaskCompletionSource<AudioClient> _audioConnectPromise;
        private ConcurrentHashSet<ulong> _channels;
        private ConcurrentDictionary<ulong, SocketGuildUser> _members;
        private ConcurrentDictionary<ulong, SocketRole> _roles;
        private ConcurrentDictionary<ulong, SocketVoiceState> _voiceStates;
        private ImmutableArray<GuildEmote> _emotes;
        private ImmutableArray<string> _features;
        private AudioClient _audioClient;
#pragma warning restore IDISP002, IDISP006

        /// <inheritdoc />
        public string Name { get; private set; }
        /// <inheritdoc />
        public int AFKTimeout { get; private set; }
        /// <inheritdoc />
        public bool IsEmbeddable { get; private set; }
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
        /// <summary>
        ///     Gets the number of members.
        /// </summary>
        /// <remarks>
        ///     This property retrieves the number of members returned by Discord.
        ///     <note type="tip">
        ///     <para>
        ///         Due to how this property is returned by Discord instead of relying on the WebSocket cache, the
        ///         number here is the most accurate in terms of counting the number of users within this guild.
        ///     </para>
        ///     <para>
        ///         Use this instead of enumerating the count of the
        ///         <see cref="Discord.WebSocket.SocketGuild.Users" /> collection, as you may see discrepancy
        ///         between that and this property.
        ///     </para>
        ///     </note>
        /// </remarks>
        public int MemberCount { get; internal set; }
        /// <summary> Gets the number of members downloaded to the local guild cache. </summary>
        public int DownloadedMemberCount { get; private set; }
        internal bool IsAvailable { get; private set; }
        /// <summary> Indicates whether the client is connected to this guild. </summary>
        public bool IsConnected { get; internal set; }
        /// <inheritdoc />
        public ulong? ApplicationId { get; internal set; }

        internal ulong? AFKChannelId { get; private set; }
        internal ulong? EmbedChannelId { get; private set; }
        internal ulong? WidgetChannelId { get; private set; }
        internal ulong? SystemChannelId { get; private set; }
        internal ulong? RulesChannelId { get; private set; }
        internal ulong? PublicUpdatesChannelId { get; private set; }
        /// <inheritdoc />
        public ulong OwnerId { get; private set; }
        /// <summary> Gets the user that owns this guild. </summary>
        public SocketGuildUser Owner => GetUser(OwnerId);
        /// <inheritdoc />
        public string VoiceRegionId { get; private set; }
        /// <inheritdoc />
        public string IconId { get; private set; }
        /// <inheritdoc />
        public string SplashId { get; private set; }
        /// <inheritdoc />
        public string DiscoverySplashId { get; private set; }
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
        /// <summary> Indicates whether the client has all the members downloaded to the local guild cache. </summary>
        public bool HasAllMembers => MemberCount <= DownloadedMemberCount;// _downloaderPromise.Task.IsCompleted;
        /// <summary> Indicates whether the guild cache is synced to this guild. </summary>
        public bool IsSynced => _syncPromise.Task.IsCompleted;
        public Task SyncPromise => _syncPromise.Task;
        public Task DownloaderPromise => _downloaderPromise.Task;
        /// <summary>
        ///     Gets the <see cref="IAudioClient" /> associated with this guild.
        /// </summary>
        public IAudioClient AudioClient => _audioClient;
        /// <summary>
        ///     Gets the default channel in this guild.
        /// </summary>
        /// <remarks>
        ///     This property retrieves the first viewable text channel for this guild.
        ///     <note type="warning">
        ///         This channel does not guarantee the user can send message to it, as it only looks for the first viewable
        ///         text channel.
        ///     </note>
        /// </remarks>
        /// <returns>
        ///     A <see cref="SocketTextChannel"/> representing the first viewable channel that the user has access to.
        /// </returns>
        public SocketTextChannel DefaultChannel => TextChannels
            .Where(c => CurrentUser.GetPermissions(c).ViewChannel)
            .OrderBy(c => c.Position)
            .FirstOrDefault();
        /// <summary>
        ///     Gets the AFK voice channel in this guild.
        /// </summary>
        /// <returns>
        ///     A <see cref="SocketVoiceChannel" /> that the AFK users will be moved to after they have idled for too
        ///     long; <see langword="null"/> if none is set.
        /// </returns>
        public SocketVoiceChannel AFKChannel
        {
            get
            {
                var id = AFKChannelId;
                return id.HasValue ? GetVoiceChannel(id.Value) : null;
            }
        }
        /// <summary>
        ///     Gets the max bitrate for voice channels in this guild.
        /// </summary>
        /// <returns>
        ///     A <see cref="int"/> representing the maximum bitrate value allowed by Discord in this guild.
        /// </returns>
        public int MaxBitrate
        {
            get
            {
                var maxBitrate = PremiumTier switch
                {
                    PremiumTier.Tier1 => 128000,
                    PremiumTier.Tier2 => 256000,
                    PremiumTier.Tier3 => 384000,
                    _ => 96000,
                };
                return maxBitrate;
            }
        }
        /// <summary>
        ///     Gets the embed channel (i.e. the channel set in the guild's widget settings) in this guild.
        /// </summary>
        /// <returns>
        ///     A channel set within the server's widget settings; <see langword="null"/> if none is set.
        /// </returns>
        [Obsolete("This property is deprecated, use WidgetChannel instead.")]
        public SocketGuildChannel EmbedChannel
        {
            get
            {
                var id = EmbedChannelId;
                return id.HasValue ? GetChannel(id.Value) : null;
            }
        }
        /// <summary>
        ///     Gets the widget channel (i.e. the channel set in the guild's widget settings) in this guild.
        /// </summary>
        /// <returns>
        ///     A channel set within the server's widget settings; <see langword="null"/> if none is set.
        /// </returns>
        public SocketGuildChannel WidgetChannel
        {
            get
            {
                var id = WidgetChannelId;
                return id.HasValue ? GetChannel(id.Value) : null;
            }
        }
        /// <summary>
        ///     Gets the system channel where randomized welcome messages are sent in this guild.
        /// </summary>
        /// <returns>
        ///     A text channel where randomized welcome messages will be sent to; <see langword="null"/> if none is set.
        /// </returns>
        public SocketTextChannel SystemChannel
        {
            get
            {
                var id = SystemChannelId;
                return id.HasValue ? GetTextChannel(id.Value) : null;
            }
        }
        /// <summary>
        ///     Gets the channel with the guild rules.
        /// </summary>
        /// <returns>
        ///     A text channel with the guild rules; <see langword="null"/> if none is set.
        /// </returns>
        public SocketTextChannel RulesChannel
        {
            get
            {
                var id = RulesChannelId;
                return id.HasValue ? GetTextChannel(id.Value) : null;
            }
        }
        /// <summary>
        ///     Gets the channel where admins and moderators of Community guilds receive
        ///     notices from Discord.
        /// </summary>
        /// <returns>
        ///     A text channel where admins and moderators of Community guilds receive
        ///     notices from Discord; <see langword="null"/> if none is set.
        /// </returns>
        public SocketTextChannel PublicUpdatesChannel
        {
            get
            {
                var id = PublicUpdatesChannelId;
                return id.HasValue ? GetTextChannel(id.Value) : null;
            }
        }
        /// <summary>
        ///     Gets a collection of all text channels in this guild.
        /// </summary>
        /// <returns>
        ///     A read-only collection of message channels found within this guild.
        /// </returns>
        public IReadOnlyCollection<SocketTextChannel> TextChannels
            => Channels.OfType<SocketTextChannel>().ToImmutableArray();
        /// <summary>
        ///     Gets a collection of all voice channels in this guild.
        /// </summary>
        /// <returns>
        ///     A read-only collection of voice channels found within this guild.
        /// </returns>
        public IReadOnlyCollection<SocketVoiceChannel> VoiceChannels
            => Channels.OfType<SocketVoiceChannel>().ToImmutableArray();
        /// <summary>
        ///     Gets a collection of all category channels in this guild.
        /// </summary>
        /// <returns>
        ///     A read-only collection of category channels found within this guild.
        /// </returns>
        public IReadOnlyCollection<SocketCategoryChannel> CategoryChannels
            => Channels.OfType<SocketCategoryChannel>().ToImmutableArray();
        /// <summary>
        ///     Gets the current logged-in user.
        /// </summary>
        public SocketGuildUser CurrentUser => _members.TryGetValue(Discord.CurrentUser.Id, out SocketGuildUser member) ? member : null;
        /// <summary>
        ///     Gets the built-in role containing all users in this guild.
        /// </summary>
        /// <returns>
        ///     A role object that represents an <c>@everyone</c> role in this guild.
        /// </returns>
        public SocketRole EveryoneRole => GetRole(Id);
        /// <summary>
        ///     Gets a collection of all channels in this guild.
        /// </summary>
        /// <returns>
        ///     A read-only collection of generic channels found within this guild.
        /// </returns>
        public IReadOnlyCollection<SocketGuildChannel> Channels
        {
            get
            {
                var channels = _channels;
                var state = Discord.State;
                return channels.Select(x => state.GetChannel(x) as SocketGuildChannel).Where(x => x != null).ToReadOnlyCollection(channels);
            }
        }
        /// <inheritdoc />
        public IReadOnlyCollection<GuildEmote> Emotes => _emotes;
        /// <inheritdoc />
        public IReadOnlyCollection<string> Features => _features;
        /// <summary>
        ///     Gets a collection of users in this guild.
        /// </summary>
        /// <remarks>
        ///     This property retrieves all users found within this guild.
        ///     <note type="warning">
        ///         <para>
        ///             This property may not always return all the members for large guilds (i.e. guilds containing
        ///             100+ users). If you are simply looking to get the number of users present in this guild,
        ///             consider using <see cref="MemberCount"/> instead.
        ///         </para>
        ///         <para>
        ///             Otherwise, you may need to enable <see cref="DiscordSocketConfig.AlwaysDownloadUsers"/> to fetch
        ///             the full user list upon startup, or use <see cref="DownloadUsersAsync"/> to manually download
        ///             the users.
        ///         </para>
        ///     </note>
        /// </remarks>
        /// <returns>
        ///     A collection of guild users found within this guild.
        /// </returns>
        public IReadOnlyCollection<SocketGuildUser> Users => _members.ToReadOnlyCollection();
        /// <summary>
        ///     Gets a collection of all roles in this guild.
        /// </summary>
        /// <returns>
        ///     A read-only collection of roles found within this guild.
        /// </returns>
        public IReadOnlyCollection<SocketRole> Roles => _roles.ToReadOnlyCollection();

        internal SocketGuild(DiscordSocketClient client, ulong id)
            : base(client, id)
        {
            _audioLock = new SemaphoreSlim(1, 1);
            _emotes = ImmutableArray.Create<GuildEmote>();
            _features = ImmutableArray.Create<string>();
        }
        internal static SocketGuild Create(DiscordSocketClient discord, ClientState state, ExtendedModel model)
        {
            var entity = new SocketGuild(discord, model.Id);
            entity.Update(state, model);
            return entity;
        }
        internal void Update(ClientState state, ExtendedModel model)
        {
            IsAvailable = !(model.Unavailable ?? false);
            if (!IsAvailable)
            {
                if (_channels == null)
                    _channels = new ConcurrentHashSet<ulong>();
                if (_members == null)
                    _members = new ConcurrentDictionary<ulong, SocketGuildUser>();
                if (_roles == null)
                    _roles = new ConcurrentDictionary<ulong, SocketRole>();
                /*if (Emojis == null)
                    _emojis = ImmutableArray.Create<Emoji>();
                if (Features == null)
                    _features = ImmutableArray.Create<string>();*/
                _syncPromise = new TaskCompletionSource<bool>();
                _downloaderPromise = new TaskCompletionSource<bool>();
                return;
            }

            Update(state, model as Model);

            var channels = new ConcurrentHashSet<ulong>(ConcurrentHashSet.DefaultConcurrencyLevel, (int)(model.Channels.Length * 1.05));
            {
                for (int i = 0; i < model.Channels.Length; i++)
                {
                    var channel = SocketGuildChannel.Create(this, state, model.Channels[i]);
                    state.AddChannel(channel);
                    channels.TryAdd(channel.Id);
                }
            }
            _channels = channels;

            var members = new ConcurrentDictionary<ulong, SocketGuildUser>(ConcurrentHashSet.DefaultConcurrencyLevel, (int)(model.Members.Length * 1.05));
            {
                for (int i = 0; i < model.Members.Length; i++)
                {
                    var member = SocketGuildUser.Create(this, state, model.Members[i]);
                    members.TryAdd(member.Id, member);
                }
                DownloadedMemberCount = members.Count;

                for (int i = 0; i < model.Presences.Length; i++)
                {
                    if (members.TryGetValue(model.Presences[i].User.Id, out SocketGuildUser member))
                        member.Update(state, model.Presences[i], true);
                }
            }
            _members = members;
            MemberCount = model.MemberCount;

            var voiceStates = new ConcurrentDictionary<ulong, SocketVoiceState>(ConcurrentHashSet.DefaultConcurrencyLevel, (int)(model.VoiceStates.Length * 1.05));
            {
                for (int i = 0; i < model.VoiceStates.Length; i++)
                {
                    SocketVoiceChannel channel = null;
                    if (model.VoiceStates[i].ChannelId.HasValue)
                        channel = state.GetChannel(model.VoiceStates[i].ChannelId.Value) as SocketVoiceChannel;
                    var voiceState = SocketVoiceState.Create(channel, model.VoiceStates[i]);
                    voiceStates.TryAdd(model.VoiceStates[i].UserId, voiceState);
                }
            }
            _voiceStates = voiceStates;

            _syncPromise = new TaskCompletionSource<bool>();
            _downloaderPromise = new TaskCompletionSource<bool>();
            var _ = _syncPromise.TrySetResultAsync(true);
            /*if (!model.Large)
                _ = _downloaderPromise.TrySetResultAsync(true);*/
        }
        internal void Update(ClientState state, Model model)
        {
            AFKChannelId = model.AFKChannelId;
            if (model.EmbedChannelId.IsSpecified)
                EmbedChannelId = model.EmbedChannelId.Value;
            if (model.WidgetChannelId.IsSpecified)
                WidgetChannelId = model.WidgetChannelId.Value;
            SystemChannelId = model.SystemChannelId;
            RulesChannelId = model.RulesChannelId;
            PublicUpdatesChannelId = model.PublicUpdatesChannelId;
            AFKTimeout = model.AFKTimeout;
            if (model.EmbedEnabled.IsSpecified)
                IsEmbeddable = model.EmbedEnabled.Value;
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
            PreferredCulture = PreferredLocale == null ? null : new CultureInfo(PreferredLocale);

            if (model.Emojis != null)
            {
                var emojis = ImmutableArray.CreateBuilder<GuildEmote>(model.Emojis.Length);
                for (int i = 0; i < model.Emojis.Length; i++)
                    emojis.Add(model.Emojis[i].ToEntity());
                _emotes = emojis.ToImmutable();
            }
            else
                _emotes = ImmutableArray.Create<GuildEmote>();

            if (model.Features != null)
                _features = model.Features.ToImmutableArray();
            else
                _features = ImmutableArray.Create<string>();

            var roles = new ConcurrentDictionary<ulong, SocketRole>(ConcurrentHashSet.DefaultConcurrencyLevel, (int)(model.Roles.Length * 1.05));
            if (model.Roles != null)
            {
                for (int i = 0; i < model.Roles.Length; i++)
                {
                    var role = SocketRole.Create(this, state, model.Roles[i]);
                    roles.TryAdd(role.Id, role);
                }
            }
            _roles = roles;
        }
        internal void Update(ClientState state, GuildSyncModel model)
        {
            var members = new ConcurrentDictionary<ulong, SocketGuildUser>(ConcurrentHashSet.DefaultConcurrencyLevel, (int)(model.Members.Length * 1.05));
            {
                for (int i = 0; i < model.Members.Length; i++)
                {
                    var member = SocketGuildUser.Create(this, state, model.Members[i]);
                    members.TryAdd(member.Id, member);
                }
                DownloadedMemberCount = members.Count;

                for (int i = 0; i < model.Presences.Length; i++)
                {
                    if (members.TryGetValue(model.Presences[i].User.Id, out SocketGuildUser member))
                        member.Update(state, model.Presences[i], true);
                }
            }
            _members = members;

            var _ = _syncPromise.TrySetResultAsync(true);
            /*if (!model.Large)
                _ = _downloaderPromise.TrySetResultAsync(true);*/
        }

        internal void Update(ClientState state, EmojiUpdateModel model)
        {
            var emotes = ImmutableArray.CreateBuilder<GuildEmote>(model.Emojis.Length);
            for (int i = 0; i < model.Emojis.Length; i++)
                emotes.Add(model.Emojis[i].ToEntity());
            _emotes = emotes.ToImmutable();
        }

        //General
        /// <inheritdoc />
        public Task DeleteAsync(RequestOptions options = null)
            => GuildHelper.DeleteAsync(this, Discord, options);

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public Task ModifyAsync(Action<GuildProperties> func, RequestOptions options = null)
            => GuildHelper.ModifyAsync(this, Discord, func, options);

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        [Obsolete("This endpoint is deprecated, use ModifyWidgetAsync instead.")]
        public Task ModifyEmbedAsync(Action<GuildEmbedProperties> func, RequestOptions options = null)
            => GuildHelper.ModifyEmbedAsync(this, Discord, func, options);
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public Task ModifyWidgetAsync(Action<GuildWidgetProperties> func, RequestOptions options = null)
            => GuildHelper.ModifyWidgetAsync(this, Discord, func, options);
        /// <inheritdoc />
        public Task ReorderChannelsAsync(IEnumerable<ReorderChannelProperties> args, RequestOptions options = null)
            => GuildHelper.ReorderChannelsAsync(this, Discord, args, options);
        /// <inheritdoc />
        public Task ReorderRolesAsync(IEnumerable<ReorderRoleProperties> args, RequestOptions options = null)
            => GuildHelper.ReorderRolesAsync(this, Discord, args, options);

        /// <inheritdoc />
        public Task LeaveAsync(RequestOptions options = null)
            => GuildHelper.LeaveAsync(this, Discord, options);

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
        ///     Gets a channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the channel.</param>
        /// <returns>
        ///     A generic channel associated with the specified <paramref name="id" />; <see langword="null"/> if none is found.
        /// </returns>
        public SocketGuildChannel GetChannel(ulong id)
        {
            var channel = Discord.State.GetChannel(id) as SocketGuildChannel;
            if (channel?.Guild.Id == Id)
                return channel;
            return null;
        }
        /// <summary>
        ///     Gets a text channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the text channel.</param>
        /// <returns>
        ///     A text channel associated with the specified <paramref name="id" />; <see langword="null"/> if none is found.
        /// </returns>
        public SocketTextChannel GetTextChannel(ulong id)
            => GetChannel(id) as SocketTextChannel;
        /// <summary>
        ///     Gets a voice channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the voice channel.</param>
        /// <returns>
        ///     A voice channel associated with the specified <paramref name="id" />; <see langword="null"/> if none is found.
        /// </returns>
        public SocketVoiceChannel GetVoiceChannel(ulong id)
            => GetChannel(id) as SocketVoiceChannel;
        /// <summary>
        ///     Gets a category channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the category channel.</param>
        /// <returns>
        ///     A category channel associated with the specified <paramref name="id" />; <see langword="null"/> if none is found.
        /// </returns>
        public SocketCategoryChannel GetCategoryChannel(ulong id)
            => GetChannel(id) as SocketCategoryChannel;

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
        ///     Creates a new voice channel in this guild.
        /// </summary>
        /// <param name="name">The new name for the voice channel.</param>
        /// <param name="func">The delegate containing the properties to be applied to the channel upon its creation.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     voice channel.
        /// </returns>
        public Task<RestVoiceChannel> CreateVoiceChannelAsync(string name, Action<VoiceChannelProperties> func = null, RequestOptions options = null)
            => GuildHelper.CreateVoiceChannelAsync(this, Discord, name, options, func);
        /// <summary>
        ///     Creates a new channel category in this guild.
        /// </summary>
        /// <param name="name">The new name for the category.</param>
        /// <param name="func">The delegate containing the properties to be applied to the channel upon its creation.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     category channel.
        /// </returns>
        public Task<RestCategoryChannel> CreateCategoryChannelAsync(string name, Action<GuildChannelProperties> func = null, RequestOptions options = null)
            => GuildHelper.CreateCategoryChannelAsync(this, Discord, name, options, func);

        internal SocketGuildChannel AddChannel(ClientState state, ChannelModel model)
        {
            var channel = SocketGuildChannel.Create(this, state, model);
            _channels.TryAdd(model.Id);
            state.AddChannel(channel);
            return channel;
        }
        internal SocketGuildChannel RemoveChannel(ClientState state, ulong id)
        {
            if (_channels.TryRemove(id))
                return state.RemoveChannel(id) as SocketGuildChannel;
            return null;
        }
        internal void PurgeChannelCache(ClientState state)
        {
            foreach (var channelId in _channels)
                state.RemoveChannel(channelId);

            _channels.Clear();
        }

        //Voice Regions
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
        ///     A task that represents the asynchronous get operation. The task result contains the partial metadata of
        ///     the vanity invite found within this guild; <see langword="null"/> if none is found.
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
        public SocketRole GetRole(ulong id)
        {
            if (_roles.TryGetValue(id, out SocketRole value))
                return value;
            return null;
        }

        /// <inheritdoc />
        public Task<RestRole> CreateRoleAsync(string name, GuildPermissions? permissions = default(GuildPermissions?), Color? color = default(Color?),
            bool isHoisted = false, RequestOptions options = null)
            => GuildHelper.CreateRoleAsync(this, Discord, name, permissions, color, isHoisted, false, options);
        /// <summary>
        ///     Creates a new role with the provided name.
        /// </summary>
        /// <param name="name">The new name for the role.</param>
        /// <param name="permissions">The guild permission that the role should possess.</param>
        /// <param name="color">The color of the role.</param>
        /// <param name="isHoisted">Whether the role is separated from others on the sidebar.</param>
        /// <param name="isMentionable">Whether the role can be mentioned.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     role.
        /// </returns>
        public Task<RestRole> CreateRoleAsync(string name, GuildPermissions? permissions = default(GuildPermissions?), Color? color = default(Color?),
            bool isHoisted = false, bool isMentionable = false, RequestOptions options = null)
            => GuildHelper.CreateRoleAsync(this, Discord, name, permissions, color, isHoisted, isMentionable, options);
        internal SocketRole AddRole(RoleModel model)
        {
            var role = SocketRole.Create(this, Discord.State, model);
            _roles[model.Id] = role;
            return role;
        }
        internal SocketRole RemoveRole(ulong id)
        {
            if (_roles.TryRemove(id, out SocketRole role))
                return role;
            return null;
        }

        //Users
        /// <inheritdoc />
        public Task<RestGuildUser> AddGuildUserAsync(ulong id, string accessToken, Action<AddGuildUserProperties> func = null, RequestOptions options = null)
            => GuildHelper.AddGuildUserAsync(this, Discord, id, accessToken, func, options);

        /// <summary>
        ///     Gets a user from this guild.
        /// </summary>
        /// <remarks>
        ///     This method retrieves a user found within this guild.
        ///     <note>
        ///         This may return <see langword="null"/> in the WebSocket implementation due to incomplete user collection in
        ///         large guilds.
        ///     </note>
        /// </remarks>
        /// <param name="id">The snowflake identifier of the user.</param>
        /// <returns>
        ///     A guild user associated with the specified <paramref name="id"/>; <see langword="null"/> if none is found.
        /// </returns>
        public SocketGuildUser GetUser(ulong id)
        {
            if (_members.TryGetValue(id, out SocketGuildUser member))
                return member;
            return null;
        }
        /// <inheritdoc />
        public Task<int> PruneUsersAsync(int days = 30, bool simulate = false, RequestOptions options = null, IEnumerable<ulong> includeRoleIds = null)
            => GuildHelper.PruneUsersAsync(this, Discord, days, simulate, options, includeRoleIds);

        internal SocketGuildUser AddOrUpdateUser(UserModel model)
        {
            if (_members.TryGetValue(model.Id, out SocketGuildUser member))
                member.GlobalUser?.Update(Discord.State, model);
            else
            {
                member = SocketGuildUser.Create(this, Discord.State, model);
                member.GlobalUser.AddRef();
                _members[member.Id] = member;
                DownloadedMemberCount++;
            }
            return member;
        }
        internal SocketGuildUser AddOrUpdateUser(MemberModel model)
        {
            if (_members.TryGetValue(model.User.Id, out SocketGuildUser member))
                member.Update(Discord.State, model);
            else
            {
                member = SocketGuildUser.Create(this, Discord.State, model);
                if (member == null)
                    throw new InvalidOperationException("SocketGuildUser.Create failed to produce a member"); // TODO 2.2rel: delete this
                if (member.GlobalUser == null)
                    throw new InvalidOperationException("Member was created without global user"); // TODO 2.2rel: delete this
                member.GlobalUser.AddRef();
                _members[member.Id] = member;
                DownloadedMemberCount++;
            }
            if (member == null)
                throw new InvalidOperationException("AddOrUpdateUser failed to produce a user"); // TODO 2.2rel: delete this
            return member;
        }
        internal SocketGuildUser AddOrUpdateUser(PresenceModel model)
        {
            if (_members.TryGetValue(model.User.Id, out SocketGuildUser member))
                member.Update(Discord.State, model, false);
            else
            {
                member = SocketGuildUser.Create(this, Discord.State, model);
                member.GlobalUser.AddRef();
                _members[member.Id] = member;
                DownloadedMemberCount++;
            }
            return member;
        }
        internal SocketGuildUser RemoveUser(ulong id)
        {
            if (_members.TryRemove(id, out SocketGuildUser member))
            {
                DownloadedMemberCount--;
                member.GlobalUser.RemoveRef(Discord);
                return member;
            }
            return null;
        }
        internal void PurgeGuildUserCache()
        {
            var members = Users;
            var self = CurrentUser;
            _members.Clear();
            if (self != null)
                _members.TryAdd(self.Id, self);

            DownloadedMemberCount = _members.Count;

            foreach (var member in members)
            {
                if (member.Id != self?.Id)
                    member.GlobalUser.RemoveRef(Discord);
            }
        }

        /// <summary>
        ///     Gets a collection of all users in this guild.
        /// </summary>
        /// <remarks>
        ///     <para>This method retrieves all users found within this guild throught REST.</para>
        ///     <para>Users returned by this method are not cached.</para>
        /// </remarks>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a collection of guild
        ///     users found within this guild.
        /// </returns>
        public IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> GetUsersAsync(RequestOptions options = null)
        {
            if (HasAllMembers)
                return ImmutableArray.Create(Users).ToAsyncEnumerable<IReadOnlyCollection<IGuildUser>>();
            return GuildHelper.GetUsersAsync(this, Discord, null, null, options);
        }

        /// <inheritdoc />
        public async Task DownloadUsersAsync()
        {
            await Discord.DownloadUsersAsync(new[] { this }).ConfigureAwait(false);
        }
        internal void CompleteDownloadUsers()
        {
            _downloaderPromise.TrySetResultAsync(true);
        }

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
        /// <param name="beforeId">The audit log entry ID to filter entries before.</param>
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

        //Interactions
        /// <summary>
        ///     Gets this guilds slash commands commands
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection
        ///     of application commands found within the guild.
        /// </returns>
        public async Task<IReadOnlyCollection<RestApplicationCommand>> GetApplicationCommandsAsync(RequestOptions options = null)
            => await Discord.Rest.GetGuildApplicationCommands(this.Id, options);

        //Emotes
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

        //Voice States
        internal async Task<SocketVoiceState> AddOrUpdateVoiceStateAsync(ClientState state, VoiceStateModel model)
        {
            var voiceChannel = state.GetChannel(model.ChannelId.Value) as SocketVoiceChannel;
            var before = GetVoiceState(model.UserId) ?? SocketVoiceState.Default;
            var after = SocketVoiceState.Create(voiceChannel, model);
            _voiceStates[model.UserId] = after;

            if (_audioClient != null && before.VoiceChannel?.Id != after.VoiceChannel?.Id)
            {
                if (model.UserId == CurrentUser.Id)
                {
                    if (after.VoiceChannel != null && _audioClient.ChannelId != after.VoiceChannel?.Id)
                    {
                        _audioClient.ChannelId = after.VoiceChannel.Id;
                        await RepopulateAudioStreamsAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    await _audioClient.RemoveInputStreamAsync(model.UserId).ConfigureAwait(false); //User changed channels, end their stream
                    if (CurrentUser.VoiceChannel != null && after.VoiceChannel?.Id == CurrentUser.VoiceChannel?.Id)
                        await _audioClient.CreateInputStreamAsync(model.UserId).ConfigureAwait(false);
                }
            }

            return after;
        }
        internal SocketVoiceState? GetVoiceState(ulong id)
        {
            if (_voiceStates.TryGetValue(id, out SocketVoiceState voiceState))
                return voiceState;
            return null;
        }
        internal async Task<SocketVoiceState?> RemoveVoiceStateAsync(ulong id)
        {
            if (_voiceStates.TryRemove(id, out SocketVoiceState voiceState))
            {
                if (_audioClient != null)
                    await _audioClient.RemoveInputStreamAsync(id).ConfigureAwait(false); //User changed channels, end their stream
                return voiceState;
            }
            return null;
        }

        //Audio
        internal AudioInStream GetAudioStream(ulong userId)
        {
            return _audioClient?.GetInputStream(userId);
        }
        internal async Task<IAudioClient> ConnectAudioAsync(ulong channelId, bool selfDeaf, bool selfMute, bool external)
        {
            TaskCompletionSource<AudioClient> promise;

            await _audioLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await DisconnectAudioInternalAsync().ConfigureAwait(false);
                promise = new TaskCompletionSource<AudioClient>();
                _audioConnectPromise = promise;

                if (external)
                {
#pragma warning disable IDISP001
                    var _ = promise.TrySetResultAsync(null);
                    await Discord.ApiClient.SendVoiceStateUpdateAsync(Id, channelId, selfDeaf, selfMute).ConfigureAwait(false);
                    return null;
#pragma warning restore IDISP001
                }

                if (_audioClient == null)
                {
                    var audioClient = new AudioClient(this, Discord.GetAudioId(), channelId);
                    audioClient.Disconnected += async ex =>
                    {
                        if (!promise.Task.IsCompleted)
                        {
                            try
                            { audioClient.Dispose(); }
                            catch { }
                            _audioClient = null;
                            if (ex != null)
                                await promise.TrySetExceptionAsync(ex);
                            else
                                await promise.TrySetCanceledAsync();
                            return;
                        }
                    };
                    audioClient.Connected += () =>
                    {
#pragma warning disable IDISP001
                        var _ = promise.TrySetResultAsync(_audioClient);
#pragma warning restore IDISP001
                        return Task.Delay(0);
                    };
#pragma warning disable IDISP003
                    _audioClient = audioClient;
#pragma warning restore IDISP003
                }

                await Discord.ApiClient.SendVoiceStateUpdateAsync(Id, channelId, selfDeaf, selfMute).ConfigureAwait(false);
            }
            catch
            {
                await DisconnectAudioInternalAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                _audioLock.Release();
            }

            try
            {
                var timeoutTask = Task.Delay(15000);
                if (await Task.WhenAny(promise.Task, timeoutTask).ConfigureAwait(false) == timeoutTask)
                    throw new TimeoutException();
                return await promise.Task.ConfigureAwait(false);
            }
            catch
            {
                await DisconnectAudioAsync().ConfigureAwait(false);
                throw;
            }
        }

        internal async Task DisconnectAudioAsync()
        {
            await _audioLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await DisconnectAudioInternalAsync().ConfigureAwait(false);
            }
            finally
            {
                _audioLock.Release();
            }
        }
        private async Task DisconnectAudioInternalAsync()
        {
            _audioConnectPromise?.TrySetCanceledAsync(); //Cancel any previous audio connection
            _audioConnectPromise = null;
            if (_audioClient != null)
                await _audioClient.StopAsync().ConfigureAwait(false);
            await Discord.ApiClient.SendVoiceStateUpdateAsync(Id, null, false, false).ConfigureAwait(false);
            _audioClient?.Dispose();
            _audioClient = null;
        }
        internal async Task FinishConnectAudio(string url, string token)
        {
            //TODO: Mem Leak: Disconnected/Connected handlers arent cleaned up
            var voiceState = GetVoiceState(Discord.CurrentUser.Id).Value;

            await _audioLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_audioClient != null)
                {
                    await RepopulateAudioStreamsAsync().ConfigureAwait(false);
                    await _audioClient.StartAsync(url, Discord.CurrentUser.Id, voiceState.VoiceSessionId, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                await DisconnectAudioInternalAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await _audioConnectPromise.SetExceptionAsync(e).ConfigureAwait(false);
                await DisconnectAudioInternalAsync().ConfigureAwait(false);
            }
            finally
            {
                _audioLock.Release();
            }
        }

        internal async Task RepopulateAudioStreamsAsync()
        {
            await _audioClient.ClearInputStreamsAsync().ConfigureAwait(false); //We changed channels, end all current streams
            if (CurrentUser.VoiceChannel != null)
            {
                foreach (var pair in _voiceStates)
                {
                    if (pair.Value.VoiceChannel?.Id == CurrentUser.VoiceChannel?.Id && pair.Key != CurrentUser.Id)
                        await _audioClient.CreateInputStreamAsync(pair.Key).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        ///     Gets the name of the guild.
        /// </summary>
        /// <returns>
        ///     A string that resolves to <see cref="Discord.WebSocket.SocketGuild.Name"/>.
        /// </returns>
        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id})";
        internal SocketGuild Clone() => MemberwiseClone() as SocketGuild;

        //IGuild
        /// <inheritdoc />
        ulong? IGuild.AFKChannelId => AFKChannelId;
        /// <inheritdoc />
        IAudioClient IGuild.AudioClient => null;
        /// <inheritdoc />
        bool IGuild.Available => true;
        /// <inheritdoc />
        ulong IGuild.DefaultChannelId => DefaultChannel?.Id ?? 0;
        /// <inheritdoc />
        ulong? IGuild.EmbedChannelId => EmbedChannelId;
        /// <inheritdoc />
        ulong? IGuild.WidgetChannelId => WidgetChannelId;
        /// <inheritdoc />
        ulong? IGuild.SystemChannelId => SystemChannelId;
        /// <inheritdoc />
        ulong? IGuild.RulesChannelId => RulesChannelId;
        /// <inheritdoc />
        ulong? IGuild.PublicUpdatesChannelId => PublicUpdatesChannelId;
        /// <inheritdoc />
        IRole IGuild.EveryoneRole => EveryoneRole;
        /// <inheritdoc />
        IReadOnlyCollection<IRole> IGuild.Roles => Roles;
        /// <inheritdoc />
        int? IGuild.ApproximateMemberCount => null;
        /// <inheritdoc />
        int? IGuild.ApproximatePresenceCount => null;

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
        Task<IReadOnlyCollection<IGuildChannel>> IGuild.GetChannelsAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<IGuildChannel>>(Channels);
        /// <inheritdoc />
        Task<IGuildChannel> IGuild.GetChannelAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IGuildChannel>(GetChannel(id));
        /// <inheritdoc />
        Task<IReadOnlyCollection<ITextChannel>> IGuild.GetTextChannelsAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<ITextChannel>>(TextChannels);
        /// <inheritdoc />
        Task<ITextChannel> IGuild.GetTextChannelAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<ITextChannel>(GetTextChannel(id));
        /// <inheritdoc />
        Task<IReadOnlyCollection<IVoiceChannel>> IGuild.GetVoiceChannelsAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<IVoiceChannel>>(VoiceChannels);
        /// <inheritdoc />
        Task<IReadOnlyCollection<ICategoryChannel>> IGuild.GetCategoriesAsync(CacheMode mode , RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<ICategoryChannel>>(CategoryChannels);
        /// <inheritdoc />
        Task<IVoiceChannel> IGuild.GetVoiceChannelAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IVoiceChannel>(GetVoiceChannel(id));
        /// <inheritdoc />
        Task<IVoiceChannel> IGuild.GetAFKChannelAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IVoiceChannel>(AFKChannel);
        /// <inheritdoc />
        Task<ITextChannel> IGuild.GetDefaultChannelAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<ITextChannel>(DefaultChannel);
        /// <inheritdoc />
        [Obsolete("This method is deprecated, use GetWidgetChannelAsync instead.")]
        Task<IGuildChannel> IGuild.GetEmbedChannelAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IGuildChannel>(EmbedChannel);
        /// <inheritdoc />
        Task<IGuildChannel> IGuild.GetWidgetChannelAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IGuildChannel>(WidgetChannel);
        /// <inheritdoc />
        Task<ITextChannel> IGuild.GetSystemChannelAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<ITextChannel>(SystemChannel);
        /// <inheritdoc />
        Task<ITextChannel> IGuild.GetRulesChannelAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<ITextChannel>(RulesChannel);
        /// <inheritdoc />
        Task<ITextChannel> IGuild.GetPublicUpdatesChannelAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<ITextChannel>(PublicUpdatesChannel);
        /// <inheritdoc />
        async Task<ITextChannel> IGuild.CreateTextChannelAsync(string name, Action<TextChannelProperties> func, RequestOptions options)
            => await CreateTextChannelAsync(name, func, options).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<IVoiceChannel> IGuild.CreateVoiceChannelAsync(string name, Action<VoiceChannelProperties> func, RequestOptions options)
            => await CreateVoiceChannelAsync(name, func, options).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<ICategoryChannel> IGuild.CreateCategoryAsync(string name, Action<GuildChannelProperties> func, RequestOptions options)
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
        async Task<IReadOnlyCollection<IGuildUser>> IGuild.GetUsersAsync(CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload && !HasAllMembers)
                return (await GetUsersAsync(options).FlattenAsync().ConfigureAwait(false)).ToImmutableArray();
            else
                return Users;
        }

        /// <inheritdoc />
        async Task<IGuildUser> IGuild.AddGuildUserAsync(ulong userId, string accessToken, Action<AddGuildUserProperties> func, RequestOptions options)
            => await AddGuildUserAsync(userId, accessToken, func, options);
        /// <inheritdoc />
        Task<IGuildUser> IGuild.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IGuildUser>(GetUser(id));
        /// <inheritdoc />
        Task<IGuildUser> IGuild.GetCurrentUserAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IGuildUser>(CurrentUser);
        /// <inheritdoc />
        Task<IGuildUser> IGuild.GetOwnerAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IGuildUser>(Owner);
        /// <inheritdoc />
        async Task<IReadOnlyCollection<IGuildUser>> IGuild.SearchUsersAsync(string query, int limit, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await SearchUsersAsync(query, limit, options).ConfigureAwait(false);
            else
                return ImmutableArray.Create<IGuildUser>();
        }

        /// <inheritdoc />
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

        void IDisposable.Dispose()
        {
            DisconnectAudioAsync().GetAwaiter().GetResult();
            _audioLock?.Dispose();
            _audioClient?.Dispose();
        }
    }
}
