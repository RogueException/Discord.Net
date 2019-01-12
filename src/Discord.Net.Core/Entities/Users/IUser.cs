using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    ///     Represents a generic user.
    /// </summary>
    public interface IUser : ISnowflakeEntity, IMentionable, IPresence
    {
        /// <summary>
        ///     Gets the identifier of this user's avatar.
        /// </summary>
        string AvatarId { get; }
        /// <summary>
        ///     Gets the avatar URL for this user.
        /// </summary>
        /// <remarks>
        ///     This property retrieves a URL for this user's avatar. In event that the user does not have a valid avatar
        ///     (i.e. their avatar identifier is not set), this property will return <c>null</c>. If you wish to
        ///     retrieve the default avatar for this user, consider using <see cref="IUser.GetDefaultAvatarUrl"/> (see
        ///     example).
        /// </remarks>
        /// <example>
        ///     The following example attempts to retrieve the user's current avatar and send it to a channel; if one is
        ///     not set, a default avatar for this user will be returned instead.
        ///     <code language="cs">
        ///     var userAvatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
        ///     await textChannel.SendMessageAsync(userAvatarUrl);
        ///     </code>
        /// </example>
        /// <param name="format">The format to return.</param>
        /// <param name="size">The size of the image to return in. This can be any power of two between 16 and 2048.
        /// </param>
        /// <returns>
        ///     A string representing the user's avatar URL; <c>null</c> if the user does not have an avatar in place.
        /// </returns>
        string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128);
        /// <summary>
        ///     Gets the default avatar URL for this user.
        /// </summary>
        /// <remarks>
        ///     This property retrieves a URL for this user's default avatar generated by Discord (Discord logo followed
        ///     by a random color as its background). This property will always return a value as it is calculated based
        ///     on the user's <see cref="IUser.DiscriminatorValue"/> (<c>discriminator % 5</c>).
        /// </remarks>
        /// <returns>
        ///     A string representing the user's avatar URL.
        /// </returns>
        string GetDefaultAvatarUrl();
        /// <summary>
        ///     Gets the per-username unique ID for this user.
        /// </summary>
        string Discriminator { get; }
        /// <summary>
        ///     Gets the per-username unique ID for this user.
        /// </summary>
        ushort DiscriminatorValue { get; }
        /// <summary>
        ///     Gets a value that indicates whether this user is identified as a bot.
        /// </summary>
        /// <remarks>
        ///     This property retrieves a value that indicates whether this user is a registered bot application
        ///     (indicated by the blue BOT tag within the official chat client).
        /// </remarks>
        /// <returns>
        ///     <c>true</c> if the user is a bot application; otherwise <c>false</c>.
        /// </returns>
        bool IsBot { get; }
        /// <summary>
        ///     Gets a value that indicates whether this user is a webhook user.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the user is a webhook; otherwise <c>false</c>.
        /// </returns>
        bool IsWebhook { get; }
        /// <summary>
        ///     Gets the username for this user.
        /// </summary>
        string Username { get; }

        /// <summary>
        ///     Gets the direct message channel of this user, or create one if it does not already exist.
        /// </summary>
        /// <remarks>
        ///     This method is used to obtain or create a channel used to send a direct message.
        ///     <note type="warning">
        ///     In event that the current user cannot send a message to the target user, a channel can and will still be
        ///     created by Discord. However, attempting to send a message will yield a 
        ///     <see cref="Discord.Net.HttpException"/> with a 403 as its 
        ///     <see cref="Discord.Net.HttpException.HttpCode"/>. There are currently no official workarounds by
        ///     Discord.
        ///     </note>
        /// </remarks>
        /// <example>
        ///     The following example attempts to send a direct message to the target user and logs the incident should
        ///     it fail.
        ///     <code language="cs">
        ///     var channel = await user.GetOrCreateDMChannelAsync();
        ///     try
        ///     {
        ///         await channel.SendMessageAsync("Awesome stuff!");
        ///     }
        ///     catch (Discord.Net.HttpException ex) when (ex.HttpCode == 403)
        ///     {
        ///         Console.WriteLine($"Boo, I cannot message {user}");
        ///     }
        ///     </code>
        /// </example>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     A task that represents the asynchronous operation for getting or creating a DM channel. The task result
        ///     contains the DM channel associated with this user.
        /// </returns>
        Task<IDMChannel> GetOrCreateDMChannelAsync(RequestOptions options = null);
        /// <summary>
        ///     The flags that are applied to a user's account.
        /// </summary>
        /// <remarks>
        ///     This value is determined by bitwise OR-ing <see cref="UserFlag"/> values together.
        ///     Each flag's value can be checked using <see cref="UserExtensions.CheckUserFlag(IUser, UserFlag)"/>
        /// </remarks>
        /// <returns>
        ///     The value of flags for this user.
        /// </returns>
        int Flags { get; }
        /// <summary>
        ///     The type of Nitro subscription that is active on this user's account.
        /// </summary>
        /// <remarks>
        ///     This information may only be available with the identify OAuth scope,
        ///     meaning that users and bots will not have access to this information.
        /// </remarks>
        /// <returns>
        ///     The type of Nitro subscription the user subscribes to, or null if this value could not be determined.
        /// </returns>
        PremiumType? PremiumType { get; }
        /// <summary>
        ///     The user's chosen language option.
        /// </summary>
        /// <returns>
        ///     The value of the user's chosen language option, if provided.
        /// </returns>
        string Locale { get; }
    }
}
