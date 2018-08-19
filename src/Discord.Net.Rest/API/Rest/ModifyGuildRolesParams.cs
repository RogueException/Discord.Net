﻿#pragma warning disable CS1591
using Newtonsoft.Json;

namespace Discord.API.Rest
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class ModifyGuildRolesParams : ModifyGuildRoleParams
    {
        public ModifyGuildRolesParams(ulong id, int position)
        {
            Id = id;
            Position = position;
        }

        [JsonProperty("id")] public ulong Id { get; }

        [JsonProperty("position")] public int Position { get; }
    }
}
