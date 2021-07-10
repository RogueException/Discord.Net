using Discord.Audio;
using System;
using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    ///     Represents a generic audio channel.
    /// </summary>
    public interface IAudioChannel : IChannel
    {
        /// <summary>
        ///     Connects to this audio channel.
        /// </summary>
        /// <param name="selfDeaf">Determines whether the client should deaf itself upon connection.</param>
        /// <param name="selfMute">Determines whether the client should mute itself upon connection.</param>
        /// <param name="external">Determines whether the audio client is an external one or not.</param>
        /// <returns>
        ///     A task representing the asynchronous connection operation. The task result contains the
        ///     <see cref="IAudioClient"/> responsible for the connection.
        /// </returns>
        Task<IAudioClient> ConnectAsync(bool selfDeaf = false, bool selfMute = false, bool external = false);

        /// <summary>
        ///     Disconnects from this audio channel.
        /// </summary>
        /// <returns>
        ///     A task representing the asynchronous operation for disconnecting from the audio channel.
        /// </returns>
        Task DisconnectAsync();

        /// <summary>
        ///     Modifies this audio channel.
        /// </summary>
        /// <param name="func">The properties to modify the channel with.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous modification operation.
        /// </returns>
        /// <seealso cref="AudioChannelProperties"/>
        Task ModifyAsync(Action<AudioChannelProperties> func, RequestOptions options = null);
    }
}
