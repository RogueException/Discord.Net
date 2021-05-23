using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Discord.WebSocket
{
    public class MessageCache : IMessageCache
    {
        private readonly ConcurrentDictionary<ulong, SocketMessage> _messages;
        private readonly ConcurrentQueue<ulong> _orderedMessages;
        private readonly int _size;

        public IReadOnlyCollection<SocketMessage> Messages => _messages.ToReadOnlyCollection();

        public IMessageCache CreateMessageCache(int size) => new MessageCache(size);

        public MessageCache(int size)
        {
            _size = size;
            _messages = new ConcurrentDictionary<ulong, SocketMessage>(ConcurrentHashSet.DefaultConcurrencyLevel, (int)(_size * 1.05));
            _orderedMessages = new ConcurrentQueue<ulong>();
        }

        public void Add(SocketMessage message)
        {
            if (_messages.TryAdd(message.Id, message))
            {
                _orderedMessages.Enqueue(message.Id);

                while (_orderedMessages.Count > _size && _orderedMessages.TryDequeue(out ulong msgId))
                    _messages.TryRemove(msgId, out _);
            }
        }

        public SocketMessage Remove(ulong id)
        {
            _messages.TryRemove(id, out SocketMessage msg);
            return msg;
        }

        public SocketMessage Get(ulong id)
        {
            if (_messages.TryGetValue(id, out SocketMessage result))
                return result;
            return null;
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is less than 0.</exception>
        public IReadOnlyCollection<SocketMessage> GetMany(ulong? fromMessageId, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch)
        {
            if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
            if (limit == 0) return ImmutableArray<SocketMessage>.Empty;

            IEnumerable<ulong> cachedMessageIds;
            if (fromMessageId == null)
                cachedMessageIds = _orderedMessages;
            else if (dir == Direction.Before)
                cachedMessageIds = _orderedMessages.Where(x => x < fromMessageId.Value);
            else if (dir == Direction.After)
                cachedMessageIds = _orderedMessages.Where(x => x > fromMessageId.Value);
            else //Direction.Around
            {
                if (!_messages.TryGetValue(fromMessageId.Value, out SocketMessage msg))
                    return ImmutableArray<SocketMessage>.Empty;
                int around = limit / 2;
                var before = GetMany(fromMessageId, Direction.Before, around);
                var after = GetMany(fromMessageId, Direction.After, around).Reverse();

                return after.Concat(new SocketMessage[] { msg }).Concat(before).ToImmutableArray();
            }

            if (dir == Direction.Before)
                cachedMessageIds = cachedMessageIds.Reverse();
            if (dir == Direction.Around) //Only happens if fromMessageId is null, should only get "around" and itself (+1)
                limit = limit / 2 + 1;

            return cachedMessageIds
                .Select(x =>
                {
                    if (_messages.TryGetValue(x, out SocketMessage msg))
                        return msg;
                    return null;
                })
                .Where(x => x != null)
                .Take(limit)
                .ToImmutableArray();
        }
    }
}
