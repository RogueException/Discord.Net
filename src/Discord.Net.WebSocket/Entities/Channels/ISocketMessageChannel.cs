using Discord.Rest;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Discord.WebSocket
{
    /// <summary>
    ///     Represents a generic WebSocket-based channel that can send and receive messages.
    /// </summary>
    public interface ISocketMessageChannel : IMessageChannel
    {
        /// <summary>
        ///     Gets all messages in this channel's cache.
        /// </summary>
        /// <returns>
        ///     A read-only collection of WebSocket-based messages.
        /// </returns>
        IReadOnlyCollection<SocketMessage> CachedMessages { get; }
        IMessageCache MessageCache { get; }

        /// <summary>
        ///     Sends a message to this message channel.
        /// </summary>
        /// <remarks>
        ///     This method follows the same behavior as described in <see cref="IMessageChannel.SendMessageAsync"/>.
        ///     Please visit its documentation for more details on this method.
        /// </remarks>
        /// <param name="text">The message to be sent.</param>
        /// <param name="isTTS">Determines whether the message should be read aloud by Discord or not.</param>
        /// <param name="embed">The <see cref="Discord.EmbedType.Rich"/> <see cref="Embed"/> to be sent.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <param name="allowedMentions">
        ///     Specifies if notifications are sent for mentioned users and roles in the message <paramref name="text"/>.
        ///     If <c>null</c>, all mentioned roles and users will be notified.
        /// </param>
        /// <param name="messageReference">The message references to be included. Used to reply to specific messages.</param>
        /// <returns>
        ///     A task that represents an asynchronous send operation for delivering the message. The task result
        ///     contains the sent message.
        /// </returns>
        new Task<RestUserMessage> SendMessageAsync(string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null);
        /// <summary>
        ///     Sends a file to this message channel with an optional caption.
        /// </summary>
        /// <remarks>
        ///     This method follows the same behavior as described in <see cref="IMessageChannel.SendFileAsync(string, string, bool, Embed, RequestOptions, bool, AllowedMentions, MessageReference)"/>.
        ///     Please visit its documentation for more details on this method.
        /// </remarks>
        /// <param name="filePath">The file path of the file.</param>
        /// <param name="text">The message to be sent.</param>
        /// <param name="isTTS">Whether the message should be read aloud by Discord or not.</param>
        /// <param name="embed">The <see cref="Discord.EmbedType.Rich" /> <see cref="Embed" /> to be sent.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <param name="isSpoiler">Whether the message attachment should be hidden as a spoiler.</param>
        /// <param name="allowedMentions">
        ///     Specifies if notifications are sent for mentioned users and roles in the message <paramref name="text"/>.
        ///     If <c>null</c>, all mentioned roles and users will be notified.
        /// </param>
        /// <param name="messageReference">The message references to be included. Used to reply to specific messages.</param>
        /// <returns>
        ///     A task that represents an asynchronous send operation for delivering the message. The task result
        ///     contains the sent message.
        /// </returns>
        new Task<RestUserMessage> SendFileAsync(string filePath, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null);
        /// <summary>
        ///     Sends a file to this message channel with an optional caption.
        /// </summary>
        /// <remarks>
        ///     This method follows the same behavior as described in <see cref="IMessageChannel.SendFileAsync(Stream, string, string, bool, Embed, RequestOptions, bool, AllowedMentions, MessageReference)"/>.
        ///     Please visit its documentation for more details on this method.
        /// </remarks>
        /// <param name="stream">The <see cref="Stream" /> of the file to be sent.</param>
        /// <param name="filename">The name of the attachment.</param>
        /// <param name="text">The message to be sent.</param>
        /// <param name="isTTS">Whether the message should be read aloud by Discord or not.</param>
        /// <param name="embed">The <see cref="Discord.EmbedType.Rich"/> <see cref="Embed"/> to be sent.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <param name="isSpoiler">Whether the message attachment should be hidden as a spoiler.</param>
        /// <param name="allowedMentions">
        ///     Specifies if notifications are sent for mentioned users and roles in the message <paramref name="text"/>.
        ///     If <c>null</c>, all mentioned roles and users will be notified.
        /// </param>
        /// <param name="messageReference">The message references to be included. Used to reply to specific messages.</param>
        /// <returns>
        ///     A task that represents an asynchronous send operation for delivering the message. The task result
        ///     contains the sent message.
        /// </returns>
        new Task<RestUserMessage> SendFileAsync(Stream stream, string filename, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null);

        /// <summary>
        ///     Gets a cached message from this channel.
        /// </summary>
        /// <remarks>
        ///     <note type="warning">
        ///         This method requires the use of cache, which is not enabled by default; if caching is not enabled,
        ///         this method will always return <c>null</c>. Please refer to
        ///         <see cref="Discord.WebSocket.DiscordSocketConfig.MessageCacheSize" /> for more details.
        ///     </note>
        ///     <para>
        ///         This method retrieves the message from the local WebSocket cache and does not send any additional
        ///         request to Discord. This message may be a message that has been deleted.
        ///     </para>
        /// </remarks>
        /// <param name="id">The snowflake identifier of the message.</param>
        /// <returns>
        ///     A WebSocket-based message object; <c>null</c> if it does not exist in the cache or if caching is not
        ///     enabled.
        /// </returns>
        SocketMessage GetCachedMessage(ulong id);
        /// <summary>
        ///     Gets the last N cached messages from this message channel.
        /// </summary>
        /// <remarks>
        ///     <note type="warning">
        ///         This method requires the use of cache, which is not enabled by default; if caching is not enabled,
        ///         this method will always return an empty collection. Please refer to
        ///         <see cref="Discord.WebSocket.DiscordSocketConfig.MessageCacheSize" /> for more details.
        ///     </note>
        ///     <para>
        ///         This method retrieves the message(s) from the local WebSocket cache and does not send any additional
        ///         request to Discord. This read-only collection may include messages that have been deleted. The
        ///         maximum number of messages that can be retrieved from this method depends on the
        ///         <see cref="Discord.WebSocket.DiscordSocketConfig.MessageCacheSize" /> set.
        ///     </para>
        /// </remarks>
        /// <param name="limit">The number of messages to get.</param>
        /// <returns>
        ///     A read-only collection of WebSocket-based messages.
        /// </returns>
        IReadOnlyCollection<SocketMessage> GetCachedMessages(int limit = DiscordConfig.MaxMessagesPerBatch);

        /// <summary>
        ///     Gets the last N cached messages starting from a certain message in this message channel.
        /// </summary>
        /// <remarks>
        ///     <note type="warning">
        ///         This method requires the use of cache, which is not enabled by default; if caching is not enabled,
        ///         this method will always return an empty collection. Please refer to
        ///         <see cref="Discord.WebSocket.DiscordSocketConfig.MessageCacheSize" /> for more details.
        ///     </note>
        ///     <para>
        ///         This method retrieves the message(s) from the local WebSocket cache and does not send any additional
        ///         request to Discord. This read-only collection may include messages that have been deleted. The
        ///         maximum number of messages that can be retrieved from this method depends on the
        ///         <see cref="Discord.WebSocket.DiscordSocketConfig.MessageCacheSize" /> set.
        ///     </para>
        /// </remarks>
        /// <param name="fromMessageId">The message ID to start the fetching from.</param>
        /// <param name="dir">The direction of which the message should be gotten from.</param>
        /// <param name="limit">The number of messages to get.</param>
        /// <returns>
        ///     A read-only collection of WebSocket-based messages.
        /// </returns>
        IReadOnlyCollection<SocketMessage> GetCachedMessages(ulong fromMessageId, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch);
        /// <summary>
        ///     Gets the last N cached messages starting from a certain message in this message channel.
        /// </summary>
        /// <remarks>
        ///     <note type="warning">
        ///         This method requires the use of cache, which is not enabled by default; if caching is not enabled,
        ///         this method will always return an empty collection. Please refer to
        ///         <see cref="Discord.WebSocket.DiscordSocketConfig.MessageCacheSize" /> for more details.
        ///     </note>
        ///     <para>
        ///         This method retrieves the message(s) from the local WebSocket cache and does not send any additional
        ///         request to Discord. This read-only collection may include messages that have been deleted. The
        ///         maximum number of messages that can be retrieved from this method depends on the
        ///         <see cref="Discord.WebSocket.DiscordSocketConfig.MessageCacheSize" /> set.
        ///     </para>
        /// </remarks>
        /// <param name="fromMessage">The message to start the fetching from.</param>
        /// <param name="dir">The direction of which the message should be gotten from.</param>
        /// <param name="limit">The number of messages to get.</param>
        /// <returns>
        ///     A read-only collection of WebSocket-based messages.
        /// </returns>
        IReadOnlyCollection<SocketMessage> GetCachedMessages(IMessage fromMessage, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch);
        /// <summary>
        ///     Gets a read-only collection of pinned messages in this channel.
        /// </summary>
        /// <remarks>
        ///     This method follows the same behavior as described in <see cref="IMessageChannel.GetPinnedMessagesAsync"/>.
        ///     Please visit its documentation for more details on this method.
        /// </remarks>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous get operation for retrieving pinned messages in this channel.
        ///     The task result contains a read-only collection of messages found in the pinned messages.
        /// </returns>
        new Task<IReadOnlyCollection<RestMessage>> GetPinnedMessagesAsync(RequestOptions options = null);
    }
}
