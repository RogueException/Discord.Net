﻿#pragma warning disable CS1591
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Discord.API.Gateway
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class IdentifyParams
    {
        [JsonProperty("token")] public string Token { get; set; }

        [JsonProperty("properties")] public IDictionary<string, string> Properties { get; set; }

        [JsonProperty("large_threshold")] public int LargeThreshold { get; set; }

        [JsonProperty("shard")] public Optional<int[]> ShardingParams { get; set; }
    }
}
