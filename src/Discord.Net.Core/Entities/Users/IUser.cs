using System.Threading.Tasks;

namespace Discord
{
    public interface IUser : ISnowflakeEntity, IMentionable, IPresence
    {
        /// <summary> Gets the id of this user's avatar. </summary>
        string AvatarId { get; }
        /// <summary> Gets the url to this user's avatar. </summary>
        string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128);
        /// <summary> Gets the per-username unique id for this user. </summary>
        string Discriminator { get; }
        /// <summary> Gets the per-username unique id for this user. </summary>
        ushort DiscriminatorValue { get; }
        /// <summary> Returns true if this user is a bot user. </summary>
        bool IsBot { get; }
        /// <summary> Returns true if this user is a webhook user. </summary>
        bool IsWebhook { get; }
        /// <summary> Gets the username for this user. </summary>
        string Username { get; }
        
        /// <summary> Adds this user as a friend, this will remove a block </summary>
        Task AddFriendAsync(RequestOptions options = null);
        /// <summary> Blocks this user, and removes the user as a friend </summary>
        Task BlockUserAsync(RequestOptions options = null);
        /// <summary> Removes the relationship of this user </summary>
        Task RemoveRelationshipAsync(RequestOptions options = null);
        /// <summary> Returns a private message channel to this user, creating one if it does not already exist. </summary>
        Task<IDMChannel> GetOrCreateDMChannelAsync(RequestOptions options = null);
    }
}
