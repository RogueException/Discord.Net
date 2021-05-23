using System.Collections.Generic;
using Discord.WebSocket;

namespace Discord
{
    public interface IMessageCache
    {
        public IMessageCache CreateMessageCache(int size);

        public IReadOnlyCollection<SocketMessage> Messages { get; }

        public void Add(SocketMessage message);

        public SocketMessage Remove(ulong id);

        public SocketMessage Get(ulong id);

        public IReadOnlyCollection<SocketMessage> GetMany(ulong? fromMessageId, Direction dir, int limit);
    }
}
