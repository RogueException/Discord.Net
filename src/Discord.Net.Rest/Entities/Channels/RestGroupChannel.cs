using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.API;
using Discord.Audio;
using Model = Discord.API.Channel;

namespace Discord.Rest
{
    [DebuggerDisplay(@"{" + nameof(DebuggerDisplay) + @",nq}")]
    public class RestGroupChannel : RestChannel, IGroupChannel, IRestPrivateChannel, IRestMessageChannel,
        IRestAudioChannel, IUpdateable
    {
        private string _iconId;
        private ImmutableDictionary<ulong, RestGroupUser> _users;

        internal RestGroupChannel(BaseDiscordClient discord, ulong id)
            : base(discord, id)
        {
        }

        public IReadOnlyCollection<RestGroupUser> Users => _users.ToReadOnlyCollection();

        public IReadOnlyCollection<RestGroupUser> Recipients
            => _users.Select(x => x.Value).Where(x => x.Id != Discord.CurrentUser.Id)
                .ToReadOnlyCollection(() => _users.Count - 1);

        private string DebuggerDisplay => $"{Name} ({Id}, Group)";

        public string Name { get; private set; }

        public Task LeaveAsync(RequestOptions options = null)
            => ChannelHelper.DeleteAsync(this, Discord, options);

        public Task DeleteMessageAsync(ulong messageId, RequestOptions options = null)
            => ChannelHelper.DeleteMessageAsync(this, messageId, Discord, options);

        public Task DeleteMessageAsync(IMessage message, RequestOptions options = null)
            => ChannelHelper.DeleteMessageAsync(this, message.Id, Discord, options);

        public Task TriggerTypingAsync(RequestOptions options = null)
            => ChannelHelper.TriggerTypingAsync(this, Discord, options);

        //IPrivateChannel
        IReadOnlyCollection<IUser> IPrivateChannel.Recipients => Recipients;

        //IMessageChannel
        async Task<IMessage> IMessageChannel.GetMessageAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            if (mode == CacheMode.AllowDownload)
                return await GetMessageAsync(id, options).ConfigureAwait(false);
            return null;
        }

        IAsyncEnumerable<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(int limit, CacheMode mode,
            RequestOptions options) => mode == CacheMode.AllowDownload
            ? GetMessagesAsync(limit, options)
            : AsyncEnumerable.Empty<IReadOnlyCollection<IMessage>>();

        IAsyncEnumerable<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(ulong fromMessageId,
            Direction dir, int limit, CacheMode mode, RequestOptions options) => mode == CacheMode.AllowDownload
            ? GetMessagesAsync(fromMessageId, dir, limit, options)
            : AsyncEnumerable.Empty<IReadOnlyCollection<IMessage>>();

        IAsyncEnumerable<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(IMessage fromMessage,
            Direction dir, int limit, CacheMode mode, RequestOptions options) => mode == CacheMode.AllowDownload
            ? GetMessagesAsync(fromMessage, dir, limit, options)
            : AsyncEnumerable.Empty<IReadOnlyCollection<IMessage>>();

        async Task<IReadOnlyCollection<IMessage>> IMessageChannel.GetPinnedMessagesAsync(RequestOptions options)
            => await GetPinnedMessagesAsync(options).ConfigureAwait(false);

        async Task<IUserMessage> IMessageChannel.SendFileAsync(string filePath, string text, bool isTTS, Embed embed,
            RequestOptions options)
            => await SendFileAsync(filePath, text, isTTS, embed, options).ConfigureAwait(false);

        async Task<IUserMessage> IMessageChannel.SendFileAsync(Stream stream, string filename, string text, bool isTTS,
            Embed embed, RequestOptions options)
            => await SendFileAsync(stream, filename, text, isTTS, embed, options).ConfigureAwait(false);

        async Task<IUserMessage> IMessageChannel.SendMessageAsync(string text, bool isTTS, Embed embed,
            RequestOptions options)
            => await SendMessageAsync(text, isTTS, embed, options).ConfigureAwait(false);

        IDisposable IMessageChannel.EnterTypingState(RequestOptions options)
            => EnterTypingState(options);

        //IAudioChannel
        Task<IAudioClient> IAudioChannel.ConnectAsync(bool selfDeaf, bool selfMute, bool external) =>
            throw new NotSupportedException();

        Task IAudioChannel.DisconnectAsync() => throw new NotSupportedException();

        //IChannel
        Task<IUser> IChannel.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IUser>(GetUser(id));

        IAsyncEnumerable<IReadOnlyCollection<IUser>> IChannel.GetUsersAsync(CacheMode mode, RequestOptions options)
            => ImmutableArray.Create<IReadOnlyCollection<IUser>>(Users).ToAsyncEnumerable();

        public Task<RestMessage> GetMessageAsync(ulong id, RequestOptions options = null)
            => ChannelHelper.GetMessageAsync(this, Discord, id, options);

        public IAsyncEnumerable<IReadOnlyCollection<RestMessage>> GetMessagesAsync(
            int limit = DiscordConfig.MaxMessagesPerBatch, RequestOptions options = null)
            => ChannelHelper.GetMessagesAsync(this, Discord, null, Direction.Before, limit, options);

        public IAsyncEnumerable<IReadOnlyCollection<RestMessage>> GetMessagesAsync(ulong fromMessageId, Direction dir,
            int limit = DiscordConfig.MaxMessagesPerBatch, RequestOptions options = null)
            => ChannelHelper.GetMessagesAsync(this, Discord, fromMessageId, dir, limit, options);

        public IAsyncEnumerable<IReadOnlyCollection<RestMessage>> GetMessagesAsync(IMessage fromMessage, Direction dir,
            int limit = DiscordConfig.MaxMessagesPerBatch, RequestOptions options = null)
            => ChannelHelper.GetMessagesAsync(this, Discord, fromMessage.Id, dir, limit, options);

        public Task<IReadOnlyCollection<RestMessage>> GetPinnedMessagesAsync(RequestOptions options = null)
            => ChannelHelper.GetPinnedMessagesAsync(this, Discord, options);

        public Task<RestUserMessage> SendMessageAsync(string text = null, bool isTTS = false, Embed embed = null,
            RequestOptions options = null)
            => ChannelHelper.SendMessageAsync(this, Discord, text, isTTS, embed, options);

        public Task<RestUserMessage> SendFileAsync(string filePath, string text, bool isTTS = false, Embed embed = null,
            RequestOptions options = null)
            => ChannelHelper.SendFileAsync(this, Discord, filePath, text, isTTS, embed, options);

        public Task<RestUserMessage> SendFileAsync(Stream stream, string filename, string text, bool isTTS = false,
            Embed embed = null, RequestOptions options = null)
            => ChannelHelper.SendFileAsync(this, Discord, stream, filename, text, isTTS, embed, options);

        //ISocketPrivateChannel
        IReadOnlyCollection<RestUser> IRestPrivateChannel.Recipients => Recipients;

        public override async Task UpdateAsync(RequestOptions options = null)
        {
            var model = await Discord.ApiClient.GetChannelAsync(Id, options).ConfigureAwait(false);
            Update(model);
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

        internal void UpdateUsers(User[] models)
        {
            var users = ImmutableDictionary.CreateBuilder<ulong, RestGroupUser>();
            for (var i = 0; i < models.Length; i++)
                users[models[i].Id] = RestGroupUser.Create(Discord, models[i]);
            _users = users.ToImmutable();
        }

        public RestUser GetUser(ulong id) => _users.TryGetValue(id, out var user) ? user : null;

        public IDisposable EnterTypingState(RequestOptions options = null)
            => ChannelHelper.EnterTypingState(this, Discord, options);

        public override string ToString() => Name;
    }
}
