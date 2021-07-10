using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.Channel;

namespace Discord.Rest
{
    /// <summary>
    ///     Represents a REST-based group-message channel.
    /// </summary>
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class RestGroupChannel : RestChannel, IGroupChannel, IRestPrivateChannel, IRestMessageChannel, IRestAudioChannel
    {
        private string _iconId;
        private ImmutableDictionary<ulong, RestGroupUser> _users;

        /// <inheritdoc />
        public string Name { get; private set; }

        public IReadOnlyCollection<RestGroupUser> Users => _users.ToReadOnlyCollection();
        public IReadOnlyCollection<RestGroupUser> Recipients
            => _users.Select(x => x.Value).Where(x => x.Id != Discord.CurrentUser.Id).ToReadOnlyCollection(() => _users.Count - 1);

        internal RestGroupChannel(BaseDiscordClient discord, ulong id)
            : base(discord, id)
        {
        }
        internal new static RestGroupChannel Create(BaseDiscordClient discord, Model model)
        {
            var entity = new RestGroupChannel(discord, model.Id);
            entity.Update(model);
            return entity;
        }
        internal override void Update(Model model)
        {
            if (model.Name.IsSpecified)
                Name = model.Name.Value;
            if (model.Icon.IsSpecified)
                _iconId = model.Icon.Value;

            if (model.Recipients.IsSpecified)
                UpdateUsers(model.Recipients.Value);
        }
        internal void UpdateUsers(API.User[] models)
        {
            var users = ImmutableDictionary.CreateBuilder<ulong, RestGroupUser>();
            for (int i = 0; i < models.Length; i++)
                users[models[i].Id] = RestGroupUser.Create(Discord, models[i]);
            _users = users.ToImmutable();
        }
        /// <inheritdoc />
        public override async Task UpdateAsync(RequestOptions options = null)
        {
            var model = await Discord.ApiClient.GetChannelAsync(Id, options).ConfigureAwait(false);
            Update(model);
        }
        /// <inheritdoc />
        public Task LeaveAsync(RequestOptions options = null)
            => ChannelHelper.DeleteAsync(this, Discord, options);

        public RestUser GetUser(ulong id)
        {
            if (_users.TryGetValue(id, out RestGroupUser user))
                return user;
            return null;
        }

        /// <inheritdoc />
        public Task<RestMessage> GetMessageAsync(ulong id, RequestOptions options = null)
            => ChannelHelper.GetMessageAsync(this, Discord, id, options);
        /// <inheritdoc />
        public IAsyncEnumerable<IReadOnlyCollection<RestMessage>> GetMessagesAsync(int limit = DiscordConfig.MaxMessagesPerBatch, RequestOptions options = null)
            => ChannelHelper.GetMessagesAsync(this, Discord, null, Direction.Before, limit, options);
        /// <inheritdoc />
        public IAsyncEnumerable<IReadOnlyCollection<RestMessage>> GetMessagesAsync(ulong fromMessageId, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch, RequestOptions options = null)
            => ChannelHelper.GetMessagesAsync(this, Discord, fromMessageId, dir, limit, options);
        /// <inheritdoc />
        public IAsyncEnumerable<IReadOnlyCollection<RestMessage>> GetMessagesAsync(IMessage fromMessage, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch, RequestOptions options = null)
            => ChannelHelper.GetMessagesAsync(this, Discord, fromMessage.Id, dir, limit, options);
        /// <inheritdoc />
        public Task<IReadOnlyCollection<RestMessage>> GetPinnedMessagesAsync(RequestOptions options = null)
            => ChannelHelper.GetPinnedMessagesAsync(this, Discord, options);

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
        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        public Task<RestUserMessage> SendMessageAsync(string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
            => ChannelHelper.SendMessageAsync(this, Discord, text, isTTS, embed, allowedMentions, messageReference, options);

        /// <inheritdoc />
        /// <exception cref="ArgumentException">
        /// <paramref name="filePath" /> is a zero-length string, contains only white space, or contains one or more
        /// invalid characters as defined by <see cref="System.IO.Path.GetInvalidPathChars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filePath" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// The specified path, file name, or both exceed the system-defined maximum length. For example, on
        /// Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260
        /// characters.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// The specified path is invalid, (for example, it is on an unmapped drive).
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="filePath" /> specified a directory.-or- The caller does not have the required permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The file specified in <paramref name="filePath" /> was not found.
        /// </exception>
        /// <exception cref="NotSupportedException"><paramref name="filePath" /> is in an invalid format.</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        public Task<RestUserMessage> SendFileAsync(string filePath, string text, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
            => ChannelHelper.SendFileAsync(this, Discord, filePath, text, isTTS, embed, allowedMentions, messageReference, options, isSpoiler);
        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        public Task<RestUserMessage> SendFileAsync(Stream stream, string filename, string text, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
            => ChannelHelper.SendFileAsync(this, Discord, stream, filename, text, isTTS, embed, allowedMentions, messageReference, options, isSpoiler);

        /// <inheritdoc />
        public Task TriggerTypingAsync(RequestOptions options = null)
            => ChannelHelper.TriggerTypingAsync(this, Discord, options);
        /// <inheritdoc />
        public IDisposable EnterTypingState(RequestOptions options = null)
            => ChannelHelper.EnterTypingState(this, Discord, options);

        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id}, Group)";

        //ISocketPrivateChannel
        IReadOnlyCollection<RestUser> IRestPrivateChannel.Recipients => Recipients;

        //IPrivateChannel
        IReadOnlyCollection<IUser> IPrivateChannel.Recipients => Recipients;

        //IMessageChannel
        async Task<IMessage> IMessageChannel.GetMessageAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetMessageAsync(id, options).ConfigureAwait(false);
            else
                return null;
        }
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(int limit, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return GetMessagesAsync(limit, options);
            else
                return AsyncEnumerable.Empty<IReadOnlyCollection<IMessage>>();
        }
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(ulong fromMessageId, Direction dir, int limit, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return GetMessagesAsync(fromMessageId, dir, limit, options);
            else
                return AsyncEnumerable.Empty<IReadOnlyCollection<IMessage>>();
        }
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(IMessage fromMessage, Direction dir, int limit, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return GetMessagesAsync(fromMessage, dir, limit, options);
            else
                return AsyncEnumerable.Empty<IReadOnlyCollection<IMessage>>();
        }
        async Task<IReadOnlyCollection<IMessage>> IMessageChannel.GetPinnedMessagesAsync(RequestOptions options)
            => await GetPinnedMessagesAsync(options).ConfigureAwait(false);

        async Task<IUserMessage> IMessageChannel.SendFileAsync(string filePath, string text, bool isTTS, Embed embed, RequestOptions options, bool isSpoiler, AllowedMentions allowedMentions, MessageReference messageReference)
            => await SendFileAsync(filePath, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference).ConfigureAwait(false);

        async Task<IUserMessage> IMessageChannel.SendFileAsync(Stream stream, string filename, string text, bool isTTS, Embed embed, RequestOptions options, bool isSpoiler, AllowedMentions allowedMentions, MessageReference messageReference)
            => await SendFileAsync(stream, filename, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference).ConfigureAwait(false);

        async Task<IUserMessage> IMessageChannel.SendMessageAsync(string text, bool isTTS, Embed embed, RequestOptions options, AllowedMentions allowedMentions, MessageReference messageReference)
            => await SendMessageAsync(text, isTTS, embed, options, allowedMentions, messageReference).ConfigureAwait(false);

        //IAudioChannel
        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Connecting to a group channel is not supported.</exception>
        Task<IAudioClient> IAudioChannel.ConnectAsync(bool selfDeaf, bool selfMute, bool external) { throw new NotSupportedException(); }
        Task IAudioChannel.DisconnectAsync() { throw new NotSupportedException(); }
        Task IAudioChannel.ModifyAsync(Action<AudioChannelProperties> func, RequestOptions options) { throw new NotSupportedException(); }

        //IChannel        
        Task<IUser> IChannel.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IUser>(GetUser(id));
        IAsyncEnumerable<IReadOnlyCollection<IUser>> IChannel.GetUsersAsync(CacheMode mode, RequestOptions options)
            => ImmutableArray.Create<IReadOnlyCollection<IUser>>(Users).ToAsyncEnumerable();
    }
}
