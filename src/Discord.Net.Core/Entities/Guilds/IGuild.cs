using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    ///     Represents a generic guild/server.
    /// </summary>
    public interface IGuild : IDeletable, ISnowflakeEntity
    {
        /// <summary>
        ///     Gets the name of this guild.
        /// </summary>
        /// <returns>
        ///     A string containing the name of this guild.
        /// </returns>
        string Name { get; }
        /// <summary>
        ///     Gets the amount of time (in seconds) a user must be inactive in a voice channel for until they are
        ///     automatically moved to the AFK voice channel.
        /// </summary>
        /// <returns>
        ///     An <see langword="int"/> representing the amount of time in seconds for a user to be marked as inactive
        ///     and moved into the AFK voice channel.
        /// </returns>
        int AFKTimeout { get; }
        /// <summary>
        ///     Gets a value that indicates whether this guild has the widget enabled.
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if this guild has a widget enabled; otherwise <see langword="false" />.
        /// </returns>
        bool IsWidgetEnabled { get; }
        /// <summary>
        ///     Gets the default message notifications for users who haven't explicitly set their notification settings.
        /// </summary>
        DefaultMessageNotifications DefaultMessageNotifications { get; }
        /// <summary>
        ///     Gets the level of Multi-Factor Authentication requirements a user must fulfill before being allowed to
        ///     perform administrative actions in this guild.
        /// </summary>
        /// <returns>
        ///     The level of MFA requirement.
        /// </returns>
        MfaLevel MfaLevel { get; }
        /// <summary>
        ///     Gets the level of requirements a user must fulfill before being allowed to post messages in this guild.
        /// </summary>
        /// <returns>
        ///     The level of requirements.
        /// </returns>
        VerificationLevel VerificationLevel { get; }
        /// <summary>
        ///     Gets the level of content filtering applied to user's content in a Guild.
        /// </summary>
        /// <returns>
        ///     The level of explicit content filtering.
        /// </returns>
        ExplicitContentFilterLevel ExplicitContentFilter { get; }
        /// <summary>
        ///     Gets the ID of this guild's icon.
        /// </summary>
        /// <returns>
        ///     An identifier for the splash image; <see langword="null" /> if none is set.
        /// </returns>
        string IconId { get; }
        /// <summary>
        ///     Gets the URL of this guild's icon.
        /// </summary>
        /// <returns>
        ///     A URL pointing to the guild's icon; <see langword="null" /> if none is set.
        /// </returns>
        string IconUrl { get; }
        /// <summary>
        ///     Gets the ID of this guild's splash image.
        /// </summary>
        /// <returns>
        ///     An identifier for the splash image; <see langword="null" /> if none is set.
        /// </returns>
        string SplashId { get; }
        /// <summary>
        ///     Gets the URL of this guild's splash image.
        /// </summary>
        /// <returns>
        ///     A URL pointing to the guild's splash image; <see langword="null" /> if none is set.
        /// </returns>
        string SplashUrl { get; }
        /// <summary>
        ///     Gets the ID of this guild's discovery splash image.
        /// </summary>
        /// <returns>
        ///     An identifier for the discovery splash image; <see langword="null" /> if none is set.
        /// </returns>
        string DiscoverySplashId { get; }
        /// <summary>
        ///     Gets the URL of this guild's discovery splash image.
        /// </summary>
        /// <returns>
        ///     A URL pointing to the guild's discovery splash image; <see langword="null" /> if none is set.
        /// </returns>
        string DiscoverySplashUrl { get; }
        /// <summary>
        ///     Determines if this guild is currently connected and ready to be used.
        /// </summary>
        /// <remarks>
        ///     <note>
        ///         This property only applies to a WebSocket-based client.
        ///     </note>
        ///     This boolean is used to determine if the guild is currently connected to the WebSocket and is ready to be used/accessed.
        /// </remarks>
        /// <returns>
        ///     <c>true</c> if this guild is currently connected and ready to be used; otherwise <see langword="false"/>.
        /// </returns>
        bool Available { get; }

        /// <summary>
        ///     Gets the ID of the AFK voice channel for this guild.
        /// </summary>
        /// <returns>
        ///     A <see langword="ulong"/> representing the snowflake identifier of the AFK voice channel; <see langword="null" /> if
        ///     none is set.
        /// </returns>
        ulong? AFKChannelId { get; }
        /// <summary>
        ///     Gets the ID of the channel assigned to the widget of this guild.
        /// </summary>
        /// <returns>
        ///     A <see langword="ulong"/> representing the snowflake identifier of the channel assigned to the widget found
        ///     within the widget settings of this guild; <see langword="null" /> if none is set.
        /// </returns>
        ulong? WidgetChannelId { get; }
        /// <summary>
        ///     Gets the ID of the channel where randomized welcome messages are sent.
        /// </summary>
        /// <returns>
        ///     A <see langword="ulong"/> representing the snowflake identifier of the system channel where randomized
        ///     welcome messages are sent; <see langword="null" /> if none is set.
        /// </returns>
        ulong? SystemChannelId { get; }
        /// <summary>
        ///     Gets the ID of the channel with the rules.
        /// </summary>
        /// <returns>
        ///     A <see langword="ulong"/> representing the snowflake identifier of the channel that contains the rules;
        ///     <see langword="null" /> if none is set.
        /// </returns>
        ulong? RulesChannelId { get; }
        /// <summary>
        ///     Gets the ID of the channel where admins and moderators of Community guilds receive notices from Discord.
        /// </summary>
        /// <returns>
        ///     A <see langword="ulong"/> representing the snowflake identifier of the channel where admins and moderators
        ///     of Community guilds  receive notices from Discord; <see langword="null" /> if none is set.
        /// </returns>
        ulong? PublicUpdatesChannelId { get; }
        /// <summary>
        ///     Gets the ID of the user that owns this guild.
        /// </summary>
        /// <returns>
        ///     A <see langword="ulong"/> representing the snowflake identifier of the user that owns this guild.
        /// </returns>
        ulong OwnerId { get; }
        /// <summary>
        ///     Gets the application ID of the guild creator if it is bot-created.
        /// </summary>
        /// <returns>
        ///     A <see langword="ulong"/> representing the snowflake identifier of the application ID that created this guild, or <see langword="null" /> if it was not bot-created.
        /// </returns>
        ulong? ApplicationId { get; }
        /// <summary>
        ///     Gets the ID of the region hosting this guild's voice channels.
        /// </summary>
        /// <returns>
        ///     A string containing the identifier for the voice region that this guild uses (e.g. <c>eu-central</c>).
        /// </returns>
        string VoiceRegionId { get; }
        /// <summary>
        ///     Gets the <see cref="IAudioClient"/> currently associated with this guild.
        /// </summary>
        /// <returns>
        ///     An <see cref="IAudioClient"/> currently associated with this guild.
        /// </returns>
        IAudioClient AudioClient { get; }
        /// <summary>
        ///     Gets the built-in role containing all users in this guild.
        /// </summary>
        /// <returns>
        ///     A role object that represents an <c>@everyone</c> role in this guild.
        /// </returns>
        IRole EveryoneRole { get; }
        /// <summary>
        ///     Gets a collection of all custom emotes for this guild.
        /// </summary>
        /// <returns>
        ///     A read-only collection of all custom emotes for this guild.
        /// </returns>
        IReadOnlyCollection<GuildEmote> Emotes { get; }
        /// <summary>
        ///     Gets a collection of all extra features added to this guild.
        /// </summary>
        /// <returns>
        ///     A read-only collection of enabled features in this guild.
        /// </returns>
        IReadOnlyCollection<string> Features { get; }
        /// <summary>
        ///     Gets a collection of all roles in this guild.
        /// </summary>
        /// <returns>
        ///     A read-only collection of roles found within this guild.
        /// </returns>
        IReadOnlyCollection<IRole> Roles { get; }
        /// <summary>
        ///     Gets the tier of guild boosting in this guild.
        /// </summary>
        /// <returns>
        ///     The tier of guild boosting in this guild.
        /// </returns>
        PremiumTier PremiumTier { get; }
        /// <summary>
        ///     Gets the identifier for this guilds banner image.
        /// </summary>
        /// <returns>
        ///     An identifier for the banner image; <see langword="null" /> if none is set.
        /// </returns>
        string BannerId { get; }
        /// <summary>
        ///     Gets the URL of this guild's banner image.
        /// </summary>
        /// <returns>
        ///     A URL pointing to the guild's banner image; <see langword="null" /> if none is set.
        /// </returns>
        string BannerUrl { get; }
        /// <summary>
        ///     Gets the code for this guild's vanity invite URL.
        /// </summary>
        /// <returns>
        ///     A string containing the vanity invite code for this guild; <see langword="null" /> if none is set.
        /// </returns>
        string VanityURLCode { get; }
        /// <summary>
        ///     Gets the flags for the types of system channel messages that are disabled.
        /// </summary>
        /// <returns>
        ///     The flags for the types of system channel messages that are disabled.
        /// </returns>
        SystemChannelMessageDeny SystemChannelFlags { get; }
        /// <summary>
        ///     Gets the description for the guild.
        /// </summary>
        /// <returns>
        ///     The description for the guild; <see langword="null" /> if none is set.
        /// </returns>
        string Description { get; }
        /// <summary>
        ///     Gets the number of premium subscribers of this guild.
        /// </summary>
        /// <remarks>
        ///     This is the number of users who have boosted this guild.
        /// </remarks>
        /// <returns>
        ///     The number of premium subscribers of this guild; <see langword="null" /> if not available.
        /// </returns>
        int PremiumSubscriptionCount { get; }
        /// <summary>
        ///     Gets the maximum number of presences for the guild.
        /// </summary>
        /// <returns>
        ///     The maximum number of presences for the guild.
        /// </returns>
        int? MaxPresences { get; }
        /// <summary>
        ///     Gets the maximum number of members for the guild.
        /// </summary>
        /// <returns>
        ///     The maximum number of members for the guild.
        /// </returns>
        int? MaxMembers { get; }
        /// <summary>
        ///     Gets the maximum amount of users in a video channel.
        /// </summary>
        /// <returns>
        ///     The maximum amount of users in a video channel.
        /// </returns>
        int? MaxVideoChannelUsers { get; }
        /// <summary>
        ///     Gets the approximate number of members in this guild.
        /// </summary>
        /// <remarks>
        ///     Only available when getting a guild via REST when `with_counts` is true.
        /// </remarks>
        /// <returns>
        ///     The approximate number of members in this guild.
        /// </returns>
        int? ApproximateMemberCount { get; }
        /// <summary>
        ///     Gets the approximate number of non-offline members in this guild.
        /// </summary>
        /// <remarks>
        ///     Only available when getting a guild via REST when `with_counts` is true.
        /// </remarks>
        /// <returns>
        ///     The approximate number of non-offline members in this guild.
        /// </returns>
        int? ApproximatePresenceCount { get; }

        /// <summary>
        ///     Gets the preferred locale of this guild in IETF BCP 47
        ///     language tag format.
        /// </summary>
        /// <returns>
        ///     The preferred locale of the guild in IETF BCP 47
        ///     language tag format.
        /// </returns>
        string PreferredLocale { get; }

        /// <summary>
        ///     Gets the preferred culture of this guild.
        /// </summary>
        /// <returns>
        ///     The preferred culture information of this guild.
        /// </returns>
        CultureInfo PreferredCulture { get; }

        /// <summary>
        ///     Modifies this guild.
        /// </summary>
        /// <param name="func">The delegate containing the properties to modify the guild with.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous modification operation.
        /// </returns>
        Task ModifyAsync(Action<GuildProperties> func, RequestOptions options = null);
        /// <summary>
        ///     Modifies this guild's widget.
        /// </summary>
        /// <param name="func">The delegate containing the properties to modify the guild widget with.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous modification operation.
        /// </returns>
        Task ModifyWidgetAsync(Action<GuildWidgetProperties> func, RequestOptions options = null);
        /// <summary>
        ///     Bulk-modifies the order of channels in this guild.
        /// </summary>
        /// <param name="args">The properties used to modify the channel positions with.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous reorder operation.
        /// </returns>
        Task ReorderChannelsAsync(IEnumerable<ReorderChannelProperties> args, RequestOptions options = null);
        /// <summary>
        ///     Bulk-modifies the order of roles in this guild.
        /// </summary>
        /// <param name="args">The properties used to modify the role positions with.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous reorder operation.
        /// </returns>
        Task ReorderRolesAsync(IEnumerable<ReorderRoleProperties> args, RequestOptions options = null);
        /// <summary>
        ///     Leaves this guild.
        /// </summary>
        /// <remarks>
        ///     This method will make the currently logged-in user leave the guild.
        ///     <note>
        ///         If the user is the owner of this guild, use <see cref="IDeletable.DeleteAsync"/> instead.
        ///     </note>
        /// </remarks>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous leave operation.
        /// </returns>
        Task LeaveAsync(RequestOptions options = null);

        /// <summary>
        ///     Gets a collection of all users banned in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     ban objects that this guild currently possesses, with each object containing the user banned and reason
        ///     behind the ban.
        /// </returns>
        Task<IReadOnlyCollection<IBan>> GetBansAsync(RequestOptions options = null);
        /// <summary>
        ///     Gets a ban object for a banned user.
        /// </summary>
        /// <param name="user">The banned user.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a ban object, which
        ///     contains the user information and the reason for the ban; <see langword="null" /> if the ban entry cannot be found.
        /// </returns>
        Task<IBan> GetBanAsync(IUser user, RequestOptions options = null);
        /// <summary>
        ///     Gets a ban object for a banned user.
        /// </summary>
        /// <param name="userId">The snowflake identifier for the banned user.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a ban object, which
        ///     contains the user information and the reason for the ban; <see langword="null" /> if the ban entry cannot be found.
        /// </returns>
        Task<IBan> GetBanAsync(ulong userId, RequestOptions options = null);
        /// <summary>
        ///     Bans the user from this guild and optionally prunes their recent messages.
        /// </summary>
        /// <param name="user">The user to ban.</param>
        /// <param name="pruneDays">The number of days to remove messages from this user for, and this number must be between [0, 7].</param>
        /// <param name="reason">The reason of the ban to be written in the audit log.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <exception cref="ArgumentException"><paramref name="pruneDays"/> is not between 0 to 7.</exception>
        /// <returns>
        ///     A task that represents the asynchronous add operation for the ban.
        /// </returns>
        Task AddBanAsync(IUser user, int pruneDays = 0, string reason = null, RequestOptions options = null);
        /// <summary>
        ///     Bans the user from this guild and optionally prunes their recent messages.
        /// </summary>
        /// <param name="userId">The snowflake ID of the user to ban.</param>
        /// <param name="pruneDays">The number of days to remove messages from this user for, and this number must be between [0, 7].</param>
        /// <param name="reason">The reason of the ban to be written in the audit log.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <exception cref="ArgumentException"><paramref name="pruneDays"/> is not between 0 to 7.</exception>
        /// <returns>
        ///     A task that represents the asynchronous add operation for the ban.
        /// </returns>
        Task AddBanAsync(ulong userId, int pruneDays = 0, string reason = null, RequestOptions options = null);
        /// <summary>
        ///     Unbans the user if they are currently banned.
        /// </summary>
        /// <param name="user">The user to be unbanned.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous removal operation for the ban.
        /// </returns>
        Task RemoveBanAsync(IUser user, RequestOptions options = null);
        /// <summary>
        ///     Unbans the user if they are currently banned.
        /// </summary>
        /// <param name="userId">The snowflake identifier of the user to be unbanned.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous removal operation for the ban.
        /// </returns>
        Task RemoveBanAsync(ulong userId, RequestOptions options = null);

        /// <summary>
        ///     Gets a collection of all channels in this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode" /> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     generic channels found within this guild.
        /// </returns>
        Task<IReadOnlyCollection<IGuildChannel>> GetChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets a channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the channel.</param>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the generic channel
        ///     associated with the specified <paramref name="id"/>; <see langword="null" /> if none is found.
        /// </returns>
        Task<IGuildChannel> GetChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets a collection of all text channels in this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     message channels found within this guild.
        /// </returns>
        Task<IReadOnlyCollection<ITextChannel>> GetTextChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets a text channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the text channel.</param>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the text channel
        ///     associated with the specified <paramref name="id"/>; <see langword="null" /> if none is found.
        /// </returns>
        Task<ITextChannel> GetTextChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets a collection of all voice channels in this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     voice channels found within this guild.
        /// </returns>
        Task<IReadOnlyCollection<IVoiceChannel>> GetVoiceChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets a collection of all category channels in this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode" /> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     category channels found within this guild.
        /// </returns>
        Task<IReadOnlyCollection<ICategoryChannel>> GetCategoriesAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets a voice channel in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the voice channel.</param>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the voice channel associated
        ///     with the specified <paramref name="id"/>; <see langword="null" /> if none is found.
        /// </returns>
        Task<IVoiceChannel> GetVoiceChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets the AFK voice channel in this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the voice channel that the
        ///     AFK users will be moved to after they have idled for too long; <see langword="null" /> if none is set.
        /// </returns>
        Task<IVoiceChannel> GetAFKChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets the system channel where randomized welcome messages are sent in this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the text channel where
        ///     randomized welcome messages will be sent to; <see langword="null" /> if none is set.
        /// </returns>
        Task<ITextChannel> GetSystemChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets the first viewable text channel in this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode" /> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the first viewable text
        ///     channel in this guild; <see langword="null" /> if none is found.
        /// </returns>
        Task<ITextChannel> GetDefaultChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets the widget channel (i.e. the channel set in the guild's widget settings) in this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode" /> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the widget channel set
        ///     within the server's widget settings; <see langword="null" /> if none is set.
        /// </returns>
        Task<IGuildChannel> GetWidgetChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets the text channel where Community guilds can display rules and/or guidelines.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the text channel
        ///     where Community guilds can display rules and/or guidelines; <see langword="null" /> if none is set.
        /// </returns>
        Task<ITextChannel> GetRulesChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets the text channel channel where admins and moderators of Community guilds receive notices from Discord.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the text channel channel where
        ///     admins and moderators of Community guilds receive notices from Discord; <see langword="null" /> if none is set.
        /// </returns>
        Task<ITextChannel> GetPublicUpdatesChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);

        /// <summary>
        ///     Creates a new text channel in this guild.
        /// </summary>
        /// <example>
        ///     <para>The following example creates a new text channel under an existing category named <c>Wumpus</c> with a set topic.</para>
        ///     <code language="cs" region="CreateTextChannelAsync"
        ///           source="..\..\..\Discord.Net.Examples\Core\Entities\Guilds\IGuild.Examples.cs"/>
        /// </example>
        /// <param name="name">The new name for the text channel.</param>
        /// <param name="func">The delegate containing the properties to be applied to the channel upon its creation.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     text channel.
        /// </returns>
        Task<ITextChannel> CreateTextChannelAsync(string name, Action<TextChannelProperties> func = null, RequestOptions options = null);
        /// <summary>
        ///     Creates a new voice channel in this guild.
        /// </summary>
        /// <param name="name">The new name for the voice channel.</param>
        /// <param name="func">The delegate containing the properties to be applied to the channel upon its creation.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     voice channel.
        /// </returns>
        Task<IVoiceChannel> CreateVoiceChannelAsync(string name, Action<VoiceChannelProperties> func = null, RequestOptions options = null);
        /// <summary>
        ///     Creates a new channel category in this guild.
        /// </summary>
        /// <param name="name">The new name for the category.</param>
        /// <param name="func">The delegate containing the properties to be applied to the channel upon its creation.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     category channel.
        /// </returns>
        Task<ICategoryChannel> CreateCategoryChannelAsync(string name, Action<GuildChannelProperties> func = null, RequestOptions options = null);

        /// <summary>
        ///     Gets a collection of all the voice regions this guild can access.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     voice regions the guild can access.
        /// </returns>
        Task<IReadOnlyCollection<IVoiceRegion>> GetVoiceRegionsAsync(RequestOptions options = null);

        Task<IReadOnlyCollection<IGuildIntegration>> GetIntegrationsAsync(RequestOptions options = null);
        Task<IGuildIntegration> CreateIntegrationAsync(ulong id, string type, RequestOptions options = null);

        /// <summary>
        ///     Gets a collection of all invites in this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection of
        ///     invite metadata, each representing information for an invite found within this guild.
        /// </returns>
        Task<IReadOnlyCollection<IInviteMetadata>> GetInvitesAsync(RequestOptions options = null);
        /// <summary>
        ///     Gets the vanity invite URL of this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the partial metadata of
        ///     the vanity invite found within this guild; <see langword="null" /> if none is found.
        /// </returns>
        Task<IInviteMetadata> GetVanityInviteAsync(RequestOptions options = null);

        /// <summary>
        ///     Gets a role in this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the role.</param>
        /// <returns>
        ///     A role that is associated with the specified <paramref name="id"/>; <see langword="null" /> if none is found.
        /// </returns>
        IRole GetRole(ulong id);
        /// <summary>
        ///     Creates a new role with the provided name.
        /// </summary>
        /// <param name="name">The new name for the role.</param>
        /// <param name="permissions">The guild permission that the role should possess.</param>
        /// <param name="color">The color of the role.</param>
        /// <param name="isHoisted">Whether the role is separated from others on the sidebar.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     role.
        /// </returns>
        Task<IRole> CreateRoleAsync(string name, GuildPermissions? permissions = null, Color? color = null, bool isHoisted = false, RequestOptions options = null);
        // TODO remove CreateRoleAsync overload that does not have isMentionable when breaking change is acceptable
        /// <summary>
        ///     Creates a new role with the provided name.
        /// </summary>
        /// <param name="name">The new name for the role.</param>
        /// <param name="permissions">The guild permission that the role should possess.</param>
        /// <param name="color">The color of the role.</param>
        /// <param name="isHoisted">Whether the role is separated from others on the sidebar.</param>
        /// <param name="isMentionable">Whether the role can be mentioned.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the newly created
        ///     role.
        /// </returns>
        Task<IRole> CreateRoleAsync(string name, GuildPermissions? permissions = null, Color? color = null, bool isHoisted = false, bool isMentionable = false, RequestOptions options = null);

        /// <summary>
        ///     Adds a user to this guild.
        /// </summary>
        /// <remarks>
        ///     This method requires you have an OAuth2 access token for the user, requested with the guilds.join scope, and that the bot have the MANAGE_INVITES permission in the guild.
        /// </remarks>
        /// <param name="userId">The snowflake identifier of the user.</param>
        /// <param name="accessToken">The OAuth2 access token for the user, requested with the guilds.join scope.</param>
        /// <param name="func">The delegate containing the properties to be applied to the user upon being added to the guild.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>A guild user associated with the specified <paramref name="userId" />; <see langword="null" /> if the user is already in the guild.</returns>
        Task<IGuildUser> AddGuildUserAsync(ulong userId, string accessToken, Action<AddGuildUserProperties> func = null, RequestOptions options = null);
        /// <summary>
        ///     Gets a collection of all users in this guild.
        /// </summary>
        /// <remarks>
        ///     This method retrieves all users found within this guild.
        ///     <note>
        ///         This may return an incomplete collection in the WebSocket implementation due to how Discord does not
        ///         send a complete user list for large guilds.
        ///     </note>
        /// </remarks>
        /// <param name="mode">The <see cref="CacheMode" /> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a collection of guild
        ///     users found within this guild.
        /// </returns>
        Task<IReadOnlyCollection<IGuildUser>> GetUsersAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets a user from this guild.
        /// </summary>
        /// <remarks>
        ///     This method retrieves a user found within this guild.
        ///     <note>
        ///         This may return <see langword="null" /> in the WebSocket implementation due to incomplete user collection in
        ///         large guilds.
        ///     </note>
        /// </remarks>
        /// <param name="id">The snowflake identifier of the user.</param>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the guild user
        ///     associated with the specified <paramref name="id"/>; <see langword="null" /> if none is found.
        /// </returns>
        Task<IGuildUser> GetUserAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets the current user for this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the currently logged-in
        ///     user within this guild.
        /// </returns>
        Task<IGuildUser> GetCurrentUserAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Gets the owner of this guild.
        /// </summary>
        /// <param name="mode">The <see cref="CacheMode"/> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the owner of this guild.
        /// </returns>
        Task<IGuildUser> GetOwnerAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);
        /// <summary>
        ///     Downloads all users for this guild if the current list is incomplete.
        /// </summary>
        /// <remarks>
        ///     This method downloads all users found within this guild throught the Gateway and caches them.
        /// </remarks>
        /// <returns>
        ///     A task that represents the asynchronous download operation.
        /// </returns>
        Task DownloadUsersAsync();
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
        /// <param name="includeRoleIds">An array of role IDs to be included in the prune of users who do not have any additional roles.</param>
        /// <returns>
        ///     A task that represents the asynchronous prune operation. The task result contains the number of users to
        ///     be or has been removed from this guild.
        /// </returns>
        Task<int> PruneUsersAsync(int days = 30, bool simulate = false, RequestOptions options = null, IEnumerable<ulong> includeRoleIds = null);
        /// <summary>
        ///     Gets a collection of users in this guild that the name or nickname starts with the
        ///     provided <see cref="string"/> at <paramref name="query"/>.
        /// </summary>
        /// <remarks>
        ///     The <paramref name="limit"/> can not be higher than <see cref="DiscordConfig.MaxUsersPerBatch"/>.
        /// </remarks>
        /// <param name="query">The partial name or nickname to search.</param>
        /// <param name="limit">The maximum number of users to be gotten.</param>
        /// <param name="mode">The <see cref="CacheMode" /> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a collection of guild
        ///     users that the name or nickname starts with the provided <see cref="string"/> at <paramref name="query"/>.
        /// </returns>
        Task<IReadOnlyCollection<IGuildUser>> SearchUsersAsync(string query, int limit = DiscordConfig.MaxUsersPerBatch, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null);

        /// <summary>
        ///     Gets the specified number of audit log entries for this guild.
        /// </summary>
        /// <param name="limit">The number of audit log entries to fetch.</param>
        /// <param name="mode">The <see cref="CacheMode" /> that determines whether the object should be fetched from cache.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <param name="beforeId">The audit log entry ID to get entries before.</param>
        /// <param name="actionType">The type of actions to filter.</param>
        /// <param name="userId">The user ID to filter entries for.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection
        ///     of the requested audit log entries.
        /// </returns>
        Task<IReadOnlyCollection<IAuditLogEntry>> GetAuditLogsAsync(int limit = DiscordConfig.MaxAuditLogEntriesPerBatch,
            CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null, ulong? beforeId = null, ulong? userId = null,
            ActionType? actionType = null);

        /// <summary>
        ///     Gets a webhook found within this guild.
        /// </summary>
        /// <param name="id">The identifier for the webhook.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the webhook with the
        ///     specified <paramref name="id"/>; <see langword="null" /> if none is found.
        /// </returns>
        Task<IWebhook> GetWebhookAsync(ulong id, RequestOptions options = null);
        /// <summary>
        ///     Gets a collection of all webhook from this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection
        ///     of webhooks found within the guild.
        /// </returns>
        Task<IReadOnlyCollection<IWebhook>> GetWebhooksAsync(RequestOptions options = null);

        /// <summary>
        ///     Gets a collection of emotes from this guild.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains a read-only collection
        ///     of emotes found within the guild.
        /// </returns>
        Task<IReadOnlyCollection<GuildEmote>> GetEmotesAsync(RequestOptions options = null);
        /// <summary>
        ///     Gets a specific emote from this guild.
        /// </summary>
        /// <param name="id">The snowflake identifier for the guild emote.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the emote found with the
        ///     specified <paramref name="id"/>; <see langword="null" /> if none is found.
        /// </returns>
        Task<GuildEmote> GetEmoteAsync(ulong id, RequestOptions options = null);
        /// <summary>
        ///     Creates a new <see cref="GuildEmote"/> in this guild.
        /// </summary>
        /// <param name="name">The name of the guild emote.</param>
        /// <param name="image">The image of the new emote.</param>
        /// <param name="roles">The roles to limit the emote usage to.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous creation operation. The task result contains the created emote.
        /// </returns>
        Task<GuildEmote> CreateEmoteAsync(string name, Image image, Optional<IEnumerable<IRole>> roles = default(Optional<IEnumerable<IRole>>), RequestOptions options = null);

        /// <summary>
        ///     Modifies an existing <see cref="GuildEmote"/> in this guild.
        /// </summary>
        /// <param name="emote">The emote to be modified.</param>
        /// <param name="func">The delegate containing the properties to modify the emote with.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous modification operation. The task result contains the modified
        ///     emote.
        /// </returns>
        Task<GuildEmote> ModifyEmoteAsync(GuildEmote emote, Action<EmoteProperties> func, RequestOptions options = null);
        /// <summary>
        ///     Deletes an existing <see cref="GuildEmote"/> from this guild.
        /// </summary>
        /// <param name="emote">The emote to delete.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous removal operation.
        /// </returns>
        Task DeleteEmoteAsync(GuildEmote emote, RequestOptions options = null);
    }
}
