﻿#pragma warning disable CS1591
using Newtonsoft.Json;

namespace Discord.API.Rest
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class ModifyGuildChannelsParams
    {
        public ModifyGuildChannelsParams(ulong id, int position)
        {
            Id = id;
            Position = position;
        }

        [JsonProperty("id")] public ulong Id { get; }

        [JsonProperty("position")] public int Position { get; }
    }
}
