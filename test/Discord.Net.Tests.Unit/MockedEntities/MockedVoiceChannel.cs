using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Audio;

namespace Discord
{
    internal sealed class MockedVoiceChannel : IVoiceChannel
    {
        public int Bitrate => throw new NotImplementedException();

        public int? UserLimit => throw new NotImplementedException();

        public ulong? CategoryId => throw new NotImplementedException();

        public int Position => throw new NotImplementedException();

        public IGuild Guild => throw new NotImplementedException();

        public ulong GuildId => throw new NotImplementedException();

        public IReadOnlyCollection<Overwrite> PermissionOverwrites => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public DateTimeOffset CreatedAt => throw new NotImplementedException();

        public ulong Id => throw new NotImplementedException();

        public Task AddPermissionOverwriteAsync(IRole role, OverwritePermissions permissions, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task AddPermissionOverwriteAsync(IUser user, OverwritePermissions permissions, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IAudioClient> ConnectAsync(bool selfDeaf = false, bool selfMute = false, bool external = false)
        {
            throw new NotImplementedException();
        }

        public Task<IInviteMetadata> CreateInviteAsync(int? maxAge = 86400, int? maxUses = null, bool isTemporary = false, bool isUnique = false, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task DisconnectAsync()
        {
            throw new NotImplementedException();
        }

        public Task ModifyAsync(Action<AudioChannelProperties> func, RequestOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<ICategoryChannel> GetCategoryAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IInviteMetadata>> GetInvitesAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public OverwritePermissions? GetPermissionOverwrite(IRole role)
        {
            throw new NotImplementedException();
        }

        public OverwritePermissions? GetPermissionOverwrite(IUser user)
        {
            throw new NotImplementedException();
        }

        public Task<IGuildUser> GetUserAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> GetUsersAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task ModifyAsync(Action<VoiceChannelProperties> func, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task ModifyAsync(Action<GuildChannelProperties> func, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task RemovePermissionOverwriteAsync(IRole role, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task RemovePermissionOverwriteAsync(IUser user, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task SyncPermissionsAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        Task<IUser> IChannel.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
        {
            throw new NotImplementedException();
        }

        IAsyncEnumerable<IReadOnlyCollection<IUser>> IChannel.GetUsersAsync(CacheMode mode, RequestOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
