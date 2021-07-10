using System;
using System.Collections.Generic;

namespace Discord
{
    /// <summary>
    ///     Represents a collection of <see cref="IEqualityComparer{T}"/> for various Discord objects.
    /// </summary>
    public static class DiscordComparers
    {
        /// <summary>
        ///     Gets an <see cref="IEqualityComparer{T}"/> to be used to compare users.
        /// </summary>
        public static IEqualityComparer<IUser> UserComparer { get; } = new EntityEqualityComparer<IUser, ulong>();
        /// <summary>
        ///     Gets an <see cref="IEqualityComparer{T}"/> to be used to compare guilds.
        /// </summary>
        public static IEqualityComparer<IGuild> GuildComparer { get; } = new EntityEqualityComparer<IGuild, ulong>();
        /// <summary>
        ///     Gets an <see cref="IEqualityComparer{T}"/> to be used to compare channels.
        /// </summary>
        public static IEqualityComparer<IChannel> ChannelComparer { get; } = new EntityEqualityComparer<IChannel, ulong>();
        /// <summary>
        ///     Gets an <see cref="IEqualityComparer{T}"/> to be used to compare roles.
        /// </summary>
        public static IEqualityComparer<IRole> RoleComparer { get; } = new EntityEqualityComparer<IRole, ulong>();
        /// <summary>
        ///     Gets an <see cref="IEqualityComparer{T}"/> to be used to compare messages.
        /// </summary>
        public static IEqualityComparer<IMessage> MessageComparer { get; } = new EntityEqualityComparer<IMessage, ulong>();

        private sealed class EntityEqualityComparer<TEntity, TId> : EqualityComparer<TEntity>
            where TEntity : IEntity<TId>
            where TId : IEquatable<TId>
        {
            public override bool Equals(TEntity x, TEntity y)
            {
                return (x, y) switch
                {
                    (null, null) => true,
                    (null, _)    => false,
                    (_, null)    => false,
                    _            => x.Id.Equals(y.Id)
                };
            }

            public override int GetHashCode(TEntity obj)
            {
                return obj?.Id.GetHashCode() ?? 0;
            }
        }
    }
}
