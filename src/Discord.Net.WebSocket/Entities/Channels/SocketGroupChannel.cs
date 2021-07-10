using Discord.Audio;
using Discord.Rest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.Channel;
using UserModel = Discord.API.User;
using VoiceStateModel = Discord.API.VoiceState;

namespace Discord.WebSocket
{
    /// <summary>
    ///     Represents a WebSocket-based private group channel.
    /// </summary>
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class SocketGroupChannel : SocketChannel, IGroupChannel, ISocketPrivateChannel, ISocketMessageChannel, ISocketAudioChannel
    {
        private readonly MessageCache _messages;
        private readonly ConcurrentDictionary<ulong, SocketVoiceState> _voiceStates;

        private string _iconId;
        private ConcurrentDictionary<ulong, SocketGroupUser> _users;

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public IReadOnlyCollection<SocketMessage> CachedMessages => _messages?.Messages ?? ImmutableArray.Create<SocketMessage>();
        public new IReadOnlyCollection<SocketGroupUser> Users => _users.ToReadOnlyCollection();
        public IReadOnlyCollection<SocketGroupUser> Recipients
            => _users.Select(x => x.Value).Where(x => x.Id != Discord.CurrentUser.Id).ToReadOnlyCollection(() => _users.Count - 1);

        internal SocketGroupChannel(DiscordSocketClient discord, ulong id)
            : base(discord, id)
        {
            if (Discord.MessageCacheSize > 0)
                _messages = new MessageCache(Discord);
            _voiceStates = new ConcurrentDictionary<ulong, SocketVoiceState>(ConcurrentHashSet.DefaultConcurrencyLevel, 5);
            _users = new ConcurrentDictionary<ulong, SocketGroupUser>(ConcurrentHashSet.DefaultConcurrencyLevel, 5);
        }
        internal static SocketGroupChannel Create(DiscordSocketClient discord, ClientState state, Model model)
        {
            var entity = new SocketGroupChannel(discord, model.Id);
            entity.Update(state, model);
            return entity;
        }
        internal override void Update(ClientState state, Model model)
        {
            if (model.Name.IsSpecified)
                Name = model.Name.Value;
            if (model.Icon.IsSpecified)
                _iconId = model.Icon.Value;

            if (model.Recipients.IsSpecified)
                UpdateUsers(state, model.Recipients.Value);
        }
        private void UpdateUsers(ClientState state, UserModel[] models)
        {
            var users = new ConcurrentDictionary<ulong, SocketGroupUser>(ConcurrentHashSet.DefaultConcurrencyLevel, (int)(models.Length * 1.05));
            for (int i = 0; i < models.Length; i++)
                users[models[i].Id] = SocketGroupUser.Create(this, state, models[i]);
            _users = users;
        }

        /// <inheritdoc />
        public Task LeaveAsync(RequestOptions options = null)
            => ChannelHelper.DeleteAsync(this, Discord, options);

        /// <exception cref="NotSupportedException">Voice is not yet supported for group channels.</exception>
        public Task<IAudioClient> ConnectAsync()
        {
            throw new NotSupportedException("Voice is not yet supported for group channels.");
        }

        //Messages
        /// <inheritdoc />
        public SocketMessage GetCachedMessage(ulong id)
            => _messages?.Get(id);
        /// <summary>
        ///     Gets a message from this message channel.
        /// </summary>
        /// <remarks>
        ///     This method follows the same behavior as described in <see cref="IMessageChannel.GetMessageAsync"/>.
        ///     Please visit its documentation for more details on this method.
        /// </remarks>
        /// <param name="id">The snowflake identifier of the message.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents an asynchronous get operation for retrieving the message. The task result contains
        ///     the retrieved message; <c>null</c> if no message is found with the specified identifier.
        /// </returns>
        public async Task<IMessage> GetMessageAsync(ulong id, RequestOptions options = null)
        {
            IMessage msg = _messages?.Get(id);
            if (msg == null)
                msg = await ChannelHelper.GetMessageAsync(this, Discord, id, options).ConfigureAwait(false);
            return msg;
        }

        /// <summary>
        ///     Gets the last N messages from this message channel.
        /// </summary>
        /// <remarks>
        ///     This method follows the same behavior as described in <see cref="IMessageChannel.GetMessagesAsync(int, CacheMode, RequestOptions)"/>.
        ///     Please visit its documentation for more details on this method.
        /// </remarks>
        /// <param name="limit">The numbers of message to be gotten from.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     Paged collection of messages.
        /// </returns>
        public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(int limit = DiscordConfig.MaxMessagesPerBatch, RequestOptions options = null)
            => SocketChannelHelper.GetMessagesAsync(this, Discord, _messages, null, Direction.Before, limit, CacheMode.AllowDownload, options);
        /// <summary>
        ///     Gets a collection of messages in this channel.
        /// </summary>
        /// <remarks>
        ///     This method follows the same behavior as described in <see cref="IMessageChannel.GetMessagesAsync(ulong, Direction, int, CacheMode, RequestOptions)"/>.
        ///     Please visit its documentation for more details on this method.
        /// </remarks>
        /// <param name="fromMessageId">The ID of the starting message to get the messages from.</param>
        /// <param name="dir">The direction of the messages to be gotten from.</param>
        /// <param name="limit">The numbers of message to be gotten from.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     Paged collection of messages.
        /// </returns>
        public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(ulong fromMessageId, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch, RequestOptions options = null)
            => SocketChannelHelper.GetMessagesAsync(this, Discord, _messages, fromMessageId, dir, limit, CacheMode.AllowDownload, options);
        /// <summary>
        ///     Gets a collection of messages in this channel.
        /// </summary>
        /// <remarks>
        ///     This method follows the same behavior as described in <see cref="IMessageChannel.GetMessagesAsync(IMessage, Direction, int, CacheMode, RequestOptions)"/>.
        ///     Please visit its documentation for more details on this method.
        /// </remarks>
        /// <param name="fromMessage">The starting message to get the messages from.</param>
        /// <param name="dir">The direction of the messages to be gotten from.</param>
        /// <param name="limit">The numbers of message to be gotten from.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     Paged collection of messages.
        /// </returns>
        public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(IMessage fromMessage, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch, RequestOptions options = null)
            => SocketChannelHelper.GetMessagesAsync(this, Discord, _messages, fromMessage.Id, dir, limit, CacheMode.AllowDownload, options);
        /// <inheritdoc />
        public IReadOnlyCollection<SocketMessage> GetCachedMessages(int limit = DiscordConfig.MaxMessagesPerBatch)
            => SocketChannelHelper.GetCachedMessages(this, Discord, _messages, null, Direction.Before, limit);
        /// <inheritdoc />
        public IReadOnlyCollection<SocketMessage> GetCachedMessages(ulong fromMessageId, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch)
            => SocketChannelHelper.GetCachedMessages(this, Discord, _messages, fromMessageId, dir, limit);
        /// <inheritdoc />
        public IReadOnlyCollection<SocketMessage> GetCachedMessages(IMessage fromMessage, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch)
            => SocketChannelHelper.GetCachedMessages(this, Discord, _messages, fromMessage.Id, dir, limit);
        /// <inheritdoc />
        public Task<IReadOnlyCollection<RestMessage>> GetPinnedMessagesAsync(RequestOptions options = null)
            => ChannelHelper.GetPinnedMessagesAsync(this, Discord, options);

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        public Task<RestUserMessage> SendMessageAsync(string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
            => ChannelHelper.SendMessageAsync(this, Discord, text, isTTS, embed, allowedMentions, messageReference, options);

        /// <inheritdoc />
        public Task<RestUserMessage> SendFileAsync(string filePath, string text, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
            => ChannelHelper.SendFileAsync(this, Discord, filePath, text, isTTS, embed, allowedMentions, messageReference, options, isSpoiler);
        /// <inheritdoc />
        public Task<RestUserMessage> SendFileAsync(Stream stream, string filename, string text, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
            => ChannelHelper.SendFileAsync(this, Discord, stream, filename, text, isTTS, embed, allowedMentions, messageReference, options, isSpoiler);

        /// <inheritdoc />
        public Task DeleteMessageAsync(ulong messageId, RequestOptions options = null)
            => ChannelHelper.DeleteMessageAsync(this, messageId, Discord, options);
        /// <inheritdoc />
        public Task DeleteMessageAsync(IMessage message, RequestOptions options = null)
            => ChannelHelper.DeleteMessageAsync(this, message.Id, Discord, options);

        /// <inheritdoc />
        public async Task<IUserMessage> ModifyMessageAsync(ulong messageId, Action<MessageProperties> func, RequestOptions options = null)
            => await ChannelHelper.ModifyMessageAsync(this, messageId, func, Discord, options).ConfigureAwait(false);

        /// <inheritdoc />
        public Task TriggerTypingAsync(RequestOptions options = null)
            => ChannelHelper.TriggerTypingAsync(this, Discord, options);
        /// <inheritdoc />
        public IDisposable EnterTypingState(RequestOptions options = null)
            => ChannelHelper.EnterTypingState(this, Discord, options);

        internal void AddMessage(SocketMessage msg)
            => _messages?.Add(msg);
        internal SocketMessage RemoveMessage(ulong id)
            => _messages?.Remove(id);

        //Users
        /// <summary>
        ///     Gets a user from this group.
        /// </summary>
        /// <param name="id">The snowflake identifier of the user.</param>
        /// <returns>
        ///     A WebSocket-based group user associated with the snowflake identifier.
        /// </returns>
        public new SocketGroupUser GetUser(ulong id)
        {
            if (_users.TryGetValue(id, out SocketGroupUser user))
                return user;
            return null;
        }
        internal SocketGroupUser GetOrAddUser(UserModel model)
        {
            if (_users.TryGetValue(model.Id, out SocketGroupUser user))
                return user;
            else
            {
                var privateUser = SocketGroupUser.Create(this, Discord.State, model);
                privateUser.GlobalUser.AddRef();
                _users[privateUser.Id] = privateUser;
                return privateUser;
            }
        }
        internal SocketGroupUser RemoveUser(ulong id)
        {
            if (_users.TryRemove(id, out SocketGroupUser user))
            {
                user.GlobalUser.RemoveRef(Discord);
                return user;
            }
            return null;
        }

        //Voice States
        internal SocketVoiceState AddOrUpdateVoiceState(ClientState state, VoiceStateModel model)
        {
            var voiceChannel = state.GetChannel(model.ChannelId.Value) as SocketVoiceChannel;
            var voiceState = SocketVoiceState.Create(voiceChannel, model);
            _voiceStates[model.UserId] = voiceState;
            return voiceState;
        }
        internal SocketVoiceState? GetVoiceState(ulong id)
        {
            if (_voiceStates.TryGetValue(id, out SocketVoiceState voiceState))
                return voiceState;
            return null;
        }
        internal SocketVoiceState? RemoveVoiceState(ulong id)
        {
            if (_voiceStates.TryRemove(id, out SocketVoiceState voiceState))
                return voiceState;
            return null;
        }

        /// <summary>
        ///     Returns the name of the group.
        /// </summary>
        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id}, Group)";
        internal new SocketGroupChannel Clone() => MemberwiseClone() as SocketGroupChannel;

        //SocketChannel
        /// <inheritdoc />
        internal override IReadOnlyCollection<SocketUser> GetUsersInternal() => Users;
        /// <inheritdoc />
        internal override SocketUser GetUserInternal(ulong id) => GetUser(id);

        //ISocketPrivateChannel
        /// <inheritdoc />
        IReadOnlyCollection<SocketUser> ISocketPrivateChannel.Recipients => Recipients;

        //IPrivateChannel
        /// <inheritdoc />
        IReadOnlyCollection<IUser> IPrivateChannel.Recipients => Recipients;

        //IMessageChannel
        /// <inheritdoc />
        async Task<IMessage> IMessageChannel.GetMessageAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetMessageAsync(id, options).ConfigureAwait(false);
            else
                return GetCachedMessage(id);
        }
        /// <inheritdoc />
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(int limit, CacheMode mode, RequestOptions options)
            => SocketChannelHelper.GetMessagesAsync(this, Discord, _messages, null, Direction.Before, limit, mode, options);
        /// <inheritdoc />
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(ulong fromMessageId, Direction dir, int limit, CacheMode mode, RequestOptions options)
            => SocketChannelHelper.GetMessagesAsync(this, Discord, _messages, fromMessageId, dir, limit, mode, options);
        /// <inheritdoc />
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(IMessage fromMessage, Direction dir, int limit, CacheMode mode, RequestOptions options)
            => SocketChannelHelper.GetMessagesAsync(this, Discord, _messages, fromMessage.Id, dir, limit, mode, options);
        /// <inheritdoc />
        async Task<IReadOnlyCollection<IMessage>> IMessageChannel.GetPinnedMessagesAsync(RequestOptions options)
            => await GetPinnedMessagesAsync(options).ConfigureAwait(false);

        /// <inheritdoc />
        async Task<IUserMessage> IMessageChannel.SendFileAsync(string filePath, string text, bool isTTS, Embed embed, RequestOptions options, bool isSpoiler, AllowedMentions allowedMentions, MessageReference messageReference)
            => await SendFileAsync(filePath, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<IUserMessage> IMessageChannel.SendFileAsync(Stream stream, string filename, string text, bool isTTS, Embed embed, RequestOptions options, bool isSpoiler, AllowedMentions allowedMentions, MessageReference messageReference)
            => await SendFileAsync(stream, filename, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference).ConfigureAwait(false);
        /// <inheritdoc />
        async Task<IUserMessage> IMessageChannel.SendMessageAsync(string text, bool isTTS, Embed embed, RequestOptions options, AllowedMentions allowedMentions, MessageReference messageReference)
            => await SendMessageAsync(text, isTTS, embed, options, allowedMentions, messageReference).ConfigureAwait(false);

        //IAudioChannel
        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Connecting to a group channel is not supported.</exception>
        Task<IAudioClient> IAudioChannel.ConnectAsync(bool selfDeaf, bool selfMute, bool external) { throw new NotSupportedException(); }
        Task IAudioChannel.DisconnectAsync() { throw new NotSupportedException(); }
        Task IAudioChannel.ModifyAsync(Action<AudioChannelProperties> func, RequestOptions options) { throw new NotSupportedException(); }

        //IChannel        
        /// <inheritdoc />
        Task<IUser> IChannel.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IUser>(GetUser(id));
        /// <inheritdoc />
        IAsyncEnumerable<IReadOnlyCollection<IUser>> IChannel.GetUsersAsync(CacheMode mode, RequestOptions options)
            => ImmutableArray.Create<IReadOnlyCollection<IUser>>(Users).ToAsyncEnumerable();
    }
}
