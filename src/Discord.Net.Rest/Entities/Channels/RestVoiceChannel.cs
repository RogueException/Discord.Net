using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.Channel;

namespace Discord.Rest
{
    /// <summary>
    ///     Represents a REST-based voice channel in a guild.
    /// </summary>
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class RestVoiceChannel : RestGuildChannel, IVoiceChannel, IRestAudioChannel
    {
        /// <inheritdoc />
        public int Bitrate { get; private set; }
        /// <inheritdoc />
        public int? UserLimit { get; private set; }
        /// <inheritdoc />
        public ulong? CategoryId { get; private set; }

        internal RestVoiceChannel(BaseDiscordClient discord, IGuild guild, ulong id)
            : base(discord, guild, id)
        {
        }
        internal new static RestVoiceChannel Create(BaseDiscordClient discord, IGuild guild, Model model)
        {
            var entity = new RestVoiceChannel(discord, guild, model.Id);
            entity.Update(model);
            return entity;
        }
        /// <inheritdoc />
        internal override void Update(Model model)
        {
            base.Update(model);
            CategoryId = model.CategoryId;
            Bitrate = model.Bitrate.Value;
            UserLimit = model.UserLimit.Value != 0 ? model.UserLimit.Value : (int?)null;
        }

        /// <inheritdoc />
        public async Task ModifyAsync(Action<VoiceChannelProperties> func, RequestOptions options = null)
        {
            var model = await ChannelHelper.ModifyAsync(this, Discord, func, options).ConfigureAwait(false);
            Update(model);
        }

        /// <summary>
        ///     Gets the parent (category) channel of this channel.
        /// </summary>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation. The task result contains the category channel
        ///     representing the parent of this channel; <c>null</c> if none is set.
        /// </returns>
        public Task<ICategoryChannel> GetCategoryAsync(RequestOptions options = null)
            => ChannelHelper.GetCategoryAsync(this, Discord, options);
        /// <inheritdoc />
        public Task SyncPermissionsAsync(RequestOptions options = null)
            => ChannelHelper.SyncPermissionsAsync(this, Discord, options);
         
        //Invites
        /// <inheritdoc />
        public async Task<IInviteMetadata> CreateInviteAsync(int? maxAge = 86400, int? maxUses = null, bool isTemporary = false, bool isUnique = false, RequestOptions options = null)
            => await ChannelHelper.CreateInviteAsync(this, Discord, maxAge, maxUses, isTemporary, isUnique, options).ConfigureAwait(false);
        /// <inheritdoc />
        public async Task<IReadOnlyCollection<IInviteMetadata>> GetInvitesAsync(RequestOptions options = null)
            => await ChannelHelper.GetInvitesAsync(this, Discord, options).ConfigureAwait(false);

        private string DebuggerDisplay => $"{Name} ({Id}, Voice)";

        //IAudioChannel
        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Connecting to a REST-based channel is not supported.</exception>
        Task<IAudioClient> IAudioChannel.ConnectAsync(bool selfDeaf, bool selfMute, bool external) { throw new NotSupportedException(); }
        Task IAudioChannel.DisconnectAsync() { throw new NotSupportedException(); }
        Task IAudioChannel.ModifyAsync(Action<AudioChannelProperties> func, RequestOptions options) { throw new NotSupportedException(); }

        //IGuildChannel
        /// <inheritdoc />
        Task<IGuildUser> IGuildChannel.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IGuildUser>(null);
        /// <inheritdoc />
        IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> IGuildChannel.GetUsersAsync(CacheMode mode, RequestOptions options)
            => AsyncEnumerable.Empty<IReadOnlyCollection<IGuildUser>>();

        // INestedChannel
        /// <inheritdoc />
        async Task<ICategoryChannel> INestedChannel.GetCategoryAsync(CacheMode mode, RequestOptions options)
        {
            if (CategoryId.HasValue && mode == CacheMode.AllowDownload)
                return (await Guild.GetChannelAsync(CategoryId.Value, mode, options).ConfigureAwait(false)) as ICategoryChannel;
            return null;
        }
    }
}
