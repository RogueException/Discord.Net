using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Discord.WebSocket
{
    public partial class BaseSocketClient
    {
        //Channels
        /// <summary> Fired when a channel is created. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a generic channel has been created. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="SocketChannel"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         The newly created channel is passed into the event handler parameter. The given channel type may
        ///         include, but not limited to, Private Channels (DM, Group), Guild Channels (Text, Voice, Category);
        ///         see the derived classes of <see cref="SocketChannel"/> for more details.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code language="cs" region="ChannelCreated"
        ///           source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs"/>
        /// </example>
        public event Func<SocketChannel, Task> ChannelCreated
        {
            add { _channelCreatedEvent.Add(value); }
            remove { _channelCreatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketChannel, Task>> _channelCreatedEvent = new AsyncEvent<Func<SocketChannel, Task>>();
        /// <summary> Fired when a channel is destroyed. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a generic channel has been destroyed. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="SocketChannel"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         The destroyed channel is passed into the event handler parameter. The given channel type may
        ///         include, but not limited to, Private Channels (DM, Group), Guild Channels (Text, Voice, Category);
        ///         see the derived classes of <see cref="SocketChannel"/> for more details.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code language="cs" region="ChannelDestroyed"
        ///           source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs"/>
        /// </example>
        public event Func<SocketChannel, Task> ChannelDestroyed
        {
            add { _channelDestroyedEvent.Add(value); }
            remove { _channelDestroyedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketChannel, Task>> _channelDestroyedEvent = new AsyncEvent<Func<SocketChannel, Task>>();
        /// <summary> Fired when a channel is updated. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a generic channel has been destroyed. The event handler must return a
        ///         <see cref="Task"/> and accept 2 <see cref="SocketChannel"/> as its parameters.
        ///     </para>
        ///     <para>
        ///         The original (prior to update) channel is passed into the first <see cref="SocketChannel"/>, while
        ///         the updated channel is passed into the second. The given channel type may include, but not limited
        ///         to, Private Channels (DM, Group), Guild Channels (Text, Voice, Category); see the derived classes of
        ///         <see cref="SocketChannel"/> for more details.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code language="cs" region="ChannelUpdated"
        ///           source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs"/>
        /// </example>
        public event Func<SocketChannel, SocketChannel, Task> ChannelUpdated
        {
            add { _channelUpdatedEvent.Add(value); }
            remove { _channelUpdatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketChannel, SocketChannel, Task>> _channelUpdatedEvent = new AsyncEvent<Func<SocketChannel, SocketChannel, Task>>();

        //Messages
        /// <summary> Fired when a message is received. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a message is received. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="SocketMessage"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         The message that is sent to the client is passed into the event handler parameter as
        ///         <see cref="SocketMessage"/>. This message may be a system message (i.e.
        ///         <see cref="SocketSystemMessage"/>) or a user message (i.e. <see cref="SocketUserMessage"/>. See the
        ///         derived classes of <see cref="SocketMessage"/> for more details.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <para>The example below checks if the newly received message contains the target user.</para>
        ///     <code language="cs" region="MessageReceived"
        ///           source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs"/>
        /// </example>
        public event Func<SocketMessage, Task> MessageReceived
        {
            add { _messageReceivedEvent.Add(value); }
            remove { _messageReceivedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketMessage, Task>> _messageReceivedEvent = new AsyncEvent<Func<SocketMessage, Task>>();
        /// <summary> Fired when a message is deleted. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a message is deleted. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="Cacheable{TEntity,TId}"/> and 
        ///         <see cref="ISocketMessageChannel"/> as its parameters.
        ///     </para>
        ///     <para>
        ///         <note type="important">
        ///             It is not possible to retrieve the message via
        ///             <see cref="Cacheable{TEntity,TId}.DownloadAsync"/>; the message cannot be retrieved by Discord
        ///             after the message has been deleted.
        ///         </note>
        ///         If caching is enabled via <see cref="DiscordSocketConfig"/>, the
        ///         <see cref="Cacheable{TEntity,TId}"/> entity will contain the deleted message; otherwise, in event
        ///         that the message cannot be retrieved, the snowflake ID of the message is preserved in the 
        ///         <see cref="ulong"/>.
        ///     </para>
        ///     <para>
        ///         The source channel of the removed message will be passed into the 
        ///         <see cref="ISocketMessageChannel"/> parameter.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code language="cs" region="MessageDeleted"
        ///           source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs" />
        /// </example>
        public event Func<Cacheable<IMessage, ulong>, ISocketMessageChannel, Task> MessageDeleted
        {
            add { _messageDeletedEvent.Add(value); }
            remove { _messageDeletedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<Cacheable<IMessage, ulong>, ISocketMessageChannel, Task>> _messageDeletedEvent = new AsyncEvent<Func<Cacheable<IMessage, ulong>, ISocketMessageChannel, Task>>();
        /// <summary> Fired when multiple messages are bulk deleted. </summary>
        /// <remarks>
        ///     <note>
        ///         The <see cref="MessageDeleted"/> event will not be fired for individual messages contained in this event.
        ///     </note>
        ///     <para>
        ///         This event is fired when multiple messages are bulk deleted. The event handler must return a
        ///         <see cref="Task"/> and accept an <see cref="IReadOnlyCollection{Cacheable}"/> and 
        ///         <see cref="ISocketMessageChannel"/> as its parameters.
        ///     </para>
        ///     <para>
        ///         <note type="important">
        ///             It is not possible to retrieve the message via
        ///             <see cref="Cacheable{TEntity,TId}.DownloadAsync"/>; the message cannot be retrieved by Discord
        ///             after the message has been deleted.
        ///         </note>
        ///         If caching is enabled via <see cref="DiscordSocketConfig"/>, the
        ///         <see cref="Cacheable{TEntity,TId}"/> entity will contain the deleted message; otherwise, in event
        ///         that the message cannot be retrieved, the snowflake ID of the message is preserved in the 
        ///         <see cref="ulong"/>.
        ///     </para>
        ///     <para>
        ///         The source channel of the removed message will be passed into the 
        ///         <see cref="ISocketMessageChannel"/> parameter.
        ///     </para>
        /// </remarks>
        public event Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, ISocketMessageChannel, Task> MessagesBulkDeleted
        {
            add { _messagesBulkDeletedEvent.Add(value); }
            remove { _messagesBulkDeletedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, ISocketMessageChannel, Task>> _messagesBulkDeletedEvent = new AsyncEvent<Func<IReadOnlyCollection<Cacheable<IMessage, ulong>>, ISocketMessageChannel, Task>>();
        /// <summary> Fired when a message is updated. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a message is updated. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="Cacheable{TEntity,TId}"/>, <see cref="SocketMessage"/>,
        ///         and <see cref="ISocketMessageChannel"/> as its parameters.
        ///     </para>
        ///     <para>
        ///         If caching is enabled via <see cref="DiscordSocketConfig"/>, the
        ///         <see cref="Cacheable{TEntity,TId}"/> entity will contain the original message; otherwise, in event
        ///         that the message cannot be retrieved, the snowflake ID of the message is preserved in the 
        ///         <see cref="ulong"/>.
        ///     </para>
        ///     <para>
        ///         The updated message will be passed into the <see cref="SocketMessage"/> parameter.
        ///     </para>
        ///     <para>
        ///         The source channel of the updated message will be passed into the 
        ///         <see cref="ISocketMessageChannel"/> parameter.
        ///     </para>
        /// </remarks>
        public event Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> MessageUpdated
        {
            add { _messageUpdatedEvent.Add(value); }
            remove { _messageUpdatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task>> _messageUpdatedEvent = new AsyncEvent<Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task>>();
        /// <summary> Fired when a reaction is added to a message. </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when a reaction is added to a user message. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="Cacheable{TEntity,TId}"/>, an 
        ///         <see cref="ISocketMessageChannel"/>, and a <see cref="SocketReaction"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         If caching is enabled via <see cref="DiscordSocketConfig"/>, the
        ///         <see cref="Cacheable{TEntity,TId}"/> entity will contain the original message; otherwise, in event
        ///         that the message cannot be retrieved, the snowflake ID of the message is preserved in the 
        ///         <see cref="ulong"/>.
        ///     </para>
        ///     <para>
        ///         The source channel of the reaction addition will be passed into the 
        ///         <see cref="ISocketMessageChannel"/> parameter.
        ///     </para>
        ///     <para>
        ///         The reaction that was added will be passed into the <see cref="SocketReaction"/> parameter.
        ///     </para>
        ///     <note>
        ///         When fetching the reaction from this event, a user may not be provided under 
        ///         <see cref="SocketReaction.User"/>. Please see the documentation of the property for more
        ///         information.
        ///     </note>
        /// </remarks>
        /// <example>
        ///     <code language="cs" region="ReactionAdded"
        ///           source="..\Discord.Net.Examples\WebSocket\BaseSocketClient.Events.Examples.cs"/>
        /// </example>
        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionAdded
        {
            add { _reactionAddedEvent.Add(value); }
            remove { _reactionAddedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>> _reactionAddedEvent = new AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>();
        /// <summary> Fired when a reaction is removed from a message. </summary>
        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionRemoved
        {
            add { _reactionRemovedEvent.Add(value); }
            remove { _reactionRemovedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>> _reactionRemovedEvent = new AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>();
        /// <summary> Fired when all reactions to a message are cleared. </summary>
        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, Task> ReactionsCleared
        {
            add { _reactionsClearedEvent.Add(value); }
            remove { _reactionsClearedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, Task>> _reactionsClearedEvent = new AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, Task>>();
        /// <summary>
        ///     Fired when all reactions to a message with a specific emote are removed.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when all reactions to a message with a specific emote are removed.
        ///         The event handler must return a <see cref="Task"/> and accept a <see cref="ISocketMessageChannel"/> and
        ///         a <see cref="IEmote"/> as its parameters.
        ///     </para>
        ///     <para>
        ///         The channel where this message was sent will be passed into the <see cref="ISocketMessageChannel"/> parameter.
        ///     </para>
        ///     <para>
        ///         The emoji that all reactions had and were removed will be passed into the <see cref="IEmote"/> parameter.
        ///     </para>
        /// </remarks>
        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, IEmote, Task> ReactionsRemovedForEmote
        {
            add { _reactionsRemovedForEmoteEvent.Add(value); }
            remove { _reactionsRemovedForEmoteEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, IEmote, Task>> _reactionsRemovedForEmoteEvent = new AsyncEvent<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, IEmote, Task>>();

        //Roles
        /// <summary> Fired when a role is created. </summary>
        public event Func<SocketRole, Task> RoleCreated
        {
            add { _roleCreatedEvent.Add(value); }
            remove { _roleCreatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketRole, Task>> _roleCreatedEvent = new AsyncEvent<Func<SocketRole, Task>>();
        /// <summary> Fired when a role is deleted. </summary>
        public event Func<SocketRole, Task> RoleDeleted
        {
            add { _roleDeletedEvent.Add(value); }
            remove { _roleDeletedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketRole, Task>> _roleDeletedEvent = new AsyncEvent<Func<SocketRole, Task>>();
        /// <summary> Fired when a role is updated. </summary>
        public event Func<SocketRole, SocketRole, Task> RoleUpdated
        {
            add { _roleUpdatedEvent.Add(value); }
            remove { _roleUpdatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketRole, SocketRole, Task>> _roleUpdatedEvent = new AsyncEvent<Func<SocketRole, SocketRole, Task>>();

        //Guilds
        /// <summary> Fired when the connected account joins a guild. </summary>
        public event Func<SocketGuild, Task> JoinedGuild
        {
            add { _joinedGuildEvent.Add(value); }
            remove { _joinedGuildEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuild, Task>> _joinedGuildEvent = new AsyncEvent<Func<SocketGuild, Task>>();
        /// <summary> Fired when the connected account leaves a guild. </summary>
        public event Func<SocketGuild, Task> LeftGuild
        {
            add { _leftGuildEvent.Add(value); }
            remove { _leftGuildEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuild, Task>> _leftGuildEvent = new AsyncEvent<Func<SocketGuild, Task>>();
        /// <summary> Fired when a guild becomes available. </summary>
        public event Func<SocketGuild, Task> GuildAvailable
        {
            add { _guildAvailableEvent.Add(value); }
            remove { _guildAvailableEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuild, Task>> _guildAvailableEvent = new AsyncEvent<Func<SocketGuild, Task>>();
        /// <summary> Fired when a guild becomes unavailable. </summary>
        public event Func<SocketGuild, Task> GuildUnavailable
        {
            add { _guildUnavailableEvent.Add(value); }
            remove { _guildUnavailableEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuild, Task>> _guildUnavailableEvent = new AsyncEvent<Func<SocketGuild, Task>>();
        /// <summary> Fired when offline guild members are downloaded. </summary>
        public event Func<SocketGuild, Task> GuildMembersDownloaded
        {
            add { _guildMembersDownloadedEvent.Add(value); }
            remove { _guildMembersDownloadedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuild, Task>> _guildMembersDownloadedEvent = new AsyncEvent<Func<SocketGuild, Task>>();
        /// <summary> Fired when a guild is updated. </summary>
        public event Func<SocketGuild, SocketGuild, Task> GuildUpdated
        {
            add { _guildUpdatedEvent.Add(value); }
            remove { _guildUpdatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuild, SocketGuild, Task>> _guildUpdatedEvent = new AsyncEvent<Func<SocketGuild, SocketGuild, Task>>();

        //Users
        /// <summary> Fired when a user joins a guild. </summary>
        public event Func<SocketGuildUser, Task> UserJoined
        {
            add { _userJoinedEvent.Add(value); }
            remove { _userJoinedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuildUser, Task>> _userJoinedEvent = new AsyncEvent<Func<SocketGuildUser, Task>>();
        /// <summary> Fired when a user leaves a guild. </summary>
        public event Func<SocketGuildUser, Task> UserLeft
        {
            add { _userLeftEvent.Add(value); }
            remove { _userLeftEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuildUser, Task>> _userLeftEvent = new AsyncEvent<Func<SocketGuildUser, Task>>();
        /// <summary> Fired when a user is banned from a guild. </summary>
        public event Func<SocketUser, SocketGuild, Task> UserBanned
        {
            add { _userBannedEvent.Add(value); }
            remove { _userBannedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketUser, SocketGuild, Task>> _userBannedEvent = new AsyncEvent<Func<SocketUser, SocketGuild, Task>>();
        /// <summary> Fired when a user is unbanned from a guild. </summary>
        public event Func<SocketUser, SocketGuild, Task> UserUnbanned
        {
            add { _userUnbannedEvent.Add(value); }
            remove { _userUnbannedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketUser, SocketGuild, Task>> _userUnbannedEvent = new AsyncEvent<Func<SocketUser, SocketGuild, Task>>();
        /// <summary> Fired when a user is updated. </summary>
        public event Func<SocketUser, SocketUser, Task> UserUpdated
        {
            add { _userUpdatedEvent.Add(value); }
            remove { _userUpdatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketUser, SocketUser, Task>> _userUpdatedEvent = new AsyncEvent<Func<SocketUser, SocketUser, Task>>();
        /// <summary> Fired when a guild member is updated, or a member presence is updated. </summary>
        public event Func<SocketGuildUser, SocketGuildUser, Task> GuildMemberUpdated
        {
            add { _guildMemberUpdatedEvent.Add(value); }
            remove { _guildMemberUpdatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuildUser, SocketGuildUser, Task>> _guildMemberUpdatedEvent = new AsyncEvent<Func<SocketGuildUser, SocketGuildUser, Task>>();
        /// <summary> Fired when a user joins, leaves, or moves voice channels. </summary>
        public event Func<SocketUser, SocketVoiceState, SocketVoiceState, Task> UserVoiceStateUpdated
        {
            add { _userVoiceStateUpdatedEvent.Add(value); }
            remove { _userVoiceStateUpdatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketUser, SocketVoiceState, SocketVoiceState, Task>> _userVoiceStateUpdatedEvent = new AsyncEvent<Func<SocketUser, SocketVoiceState, SocketVoiceState, Task>>();
        /// <summary> Fired when the bot connects to a Discord voice server. </summary>
        public event Func<SocketVoiceServer, Task> VoiceServerUpdated
        {
            add { _voiceServerUpdatedEvent.Add(value); }
            remove { _voiceServerUpdatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketVoiceServer, Task>> _voiceServerUpdatedEvent = new AsyncEvent<Func<SocketVoiceServer, Task>>();
        /// <summary> Fired when the connected account is updated. </summary>
        public event Func<SocketSelfUser, SocketSelfUser, Task> CurrentUserUpdated
        {
            add { _selfUpdatedEvent.Add(value); }
            remove { _selfUpdatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketSelfUser, SocketSelfUser, Task>> _selfUpdatedEvent = new AsyncEvent<Func<SocketSelfUser, SocketSelfUser, Task>>();
        /// <summary> Fired when a user starts typing. </summary>
        public event Func<SocketUser, ISocketMessageChannel, Task> UserIsTyping
        {
            add { _userIsTypingEvent.Add(value); }
            remove { _userIsTypingEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketUser, ISocketMessageChannel, Task>> _userIsTypingEvent = new AsyncEvent<Func<SocketUser, ISocketMessageChannel, Task>>();
        /// <summary> Fired when a user joins a group channel. </summary>
        public event Func<SocketGroupUser, Task> RecipientAdded
        {
            add { _recipientAddedEvent.Add(value); }
            remove { _recipientAddedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGroupUser, Task>> _recipientAddedEvent = new AsyncEvent<Func<SocketGroupUser, Task>>();
        /// <summary> Fired when a user is removed from a group channel. </summary>
        public event Func<SocketGroupUser, Task> RecipientRemoved
        {
            add { _recipientRemovedEvent.Add(value); }
            remove { _recipientRemovedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGroupUser, Task>> _recipientRemovedEvent = new AsyncEvent<Func<SocketGroupUser, Task>>();

        //Invites
        /// <summary>
        ///     Fired when an invite is created.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when an invite is created. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="SocketInvite"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         The invite created will be passed into the <see cref="SocketInvite"/> parameter.
        ///     </para>
        /// </remarks>
        public event Func<SocketInvite, Task> InviteCreated
        {
            add { _inviteCreatedEvent.Add(value); }
            remove { _inviteCreatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketInvite, Task>> _inviteCreatedEvent = new AsyncEvent<Func<SocketInvite, Task>>();
        /// <summary>
        ///     Fired when an invite is deleted.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when an invite is deleted. The event handler must return
        ///         a <see cref="Task"/> and accept a <see cref="SocketGuildChannel"/> and
        ///         <see cref="string"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         The channel where this invite was created will be passed into the <see cref="SocketGuildChannel"/> parameter.
        ///     </para>
        ///     <para>
        ///         The code of the deleted invite will be passed into the <see cref="string"/> parameter.
        ///     </para>
        /// </remarks>
        public event Func<SocketGuildChannel, string, Task> InviteDeleted
        {
            add { _inviteDeletedEvent.Add(value); }
            remove { _inviteDeletedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketGuildChannel, string, Task>> _inviteDeletedEvent = new AsyncEvent<Func<SocketGuildChannel, string, Task>>();

        //Interactions
        /// <summary>
        ///     Fired when an Interaction is created.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when an interaction is created. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="SocketInteraction"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         The interaction created will be passed into the <see cref="SocketInteraction"/> parameter.
        ///     </para>
        /// </remarks>
        public event Func<SocketInteraction, Task> InteractionCreated
        {
            add { _interactionCreatedEvent.Add(value); }
            remove { _interactionCreatedEvent.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketInteraction, Task>> _interactionCreatedEvent = new AsyncEvent<Func<SocketInteraction, Task>>();

        /// <summary>
        ///     Fired when a guild application command is created.
        ///</summary>
        ///<remarks>
        ///     <para>
        ///         This event is fired when an application command is created. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="SocketApplicationCommand"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         The command that was deleted will be passed into the <see cref="SocketApplicationCommand"/> parameter.
        ///     </para>
        ///     <note>
        ///         <b>This event is an undocumented discord event and may break at any time, its not recommended to rely on this event</b>
        ///     </note>
        /// </remarks>
        public event Func<SocketApplicationCommand, Task> ApplicationCommandCreated
        {
            add { _applicationCommandCreated.Add(value); }
            remove { _applicationCommandCreated.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketApplicationCommand, Task>> _applicationCommandCreated = new AsyncEvent<Func<SocketApplicationCommand, Task>>();

        /// <summary>
        ///      Fired when a guild application command is updated.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when an application command is updated. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="SocketApplicationCommand"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         The command that was deleted will be passed into the <see cref="SocketApplicationCommand"/> parameter.
        ///     </para>
        ///     <note>
        ///         <b>This event is an undocumented discord event and may break at any time, its not recommended to rely on this event</b>
        ///     </note>
        /// </remarks>
        public event Func<SocketApplicationCommand, Task> ApplicationCommandUpdated
        {
            add { _applicationCommandUpdated.Add(value); }
            remove { _applicationCommandUpdated.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketApplicationCommand, Task>> _applicationCommandUpdated = new AsyncEvent<Func<SocketApplicationCommand, Task>>();

        /// <summary>
        ///      Fired when a guild application command is deleted.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This event is fired when an application command is deleted. The event handler must return a
        ///         <see cref="Task"/> and accept a <see cref="SocketApplicationCommand"/> as its parameter.
        ///     </para>
        ///     <para>
        ///         The command that was deleted will be passed into the <see cref="SocketApplicationCommand"/> parameter.
        ///     </para>
        ///     <note>
        ///         <b>This event is an undocumented discord event and may break at any time, its not recommended to rely on this event</b>
        ///     </note>
        /// </remarks>
        public event Func<SocketApplicationCommand, Task> ApplicationCommandDeleted
        {
            add { _applicationCommandDeleted.Add(value); }
            remove { _applicationCommandDeleted.Remove(value); }
        }
        internal readonly AsyncEvent<Func<SocketApplicationCommand, Task>> _applicationCommandDeleted = new AsyncEvent<Func<SocketApplicationCommand, Task>>();
    }
}
