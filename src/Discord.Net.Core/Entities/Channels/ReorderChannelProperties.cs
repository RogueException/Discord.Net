﻿namespace Discord
{
    public class ReorderChannelProperties
    {
        public ReorderChannelProperties(ulong id, int position)
        {
            Id = id;
            Position = position;
        }

        /// <summary>The id of the channel to apply this position to.</summary>
        public ulong Id { get; }

        /// <summary>The new zero-based position of this channel. </summary>
        public int Position { get; }
    }
}
