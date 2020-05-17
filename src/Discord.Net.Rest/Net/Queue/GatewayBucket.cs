using System.Collections.Immutable;

namespace Discord.Net.Queue
{
    public enum GatewayBucketType
    {
        Unbucketed = 0,
        Identify = 1
    }
    internal struct GatewayBucket
    {
        private static readonly ImmutableDictionary<GatewayBucketType, GatewayBucket> DefsByType;
        private static readonly ImmutableDictionary<string, GatewayBucket> DefsById;

        static GatewayBucket()
        {
            var buckets = new[]
            {
                new GatewayBucket(GatewayBucketType.Unbucketed, "<unbucketed>", 120, 60),
                new GatewayBucket(GatewayBucketType.Identify, "<identify>", 1, 5)
            };

            var builder = ImmutableDictionary.CreateBuilder<GatewayBucketType, GatewayBucket>();
            foreach (var bucket in buckets)
                builder.Add(bucket.Type, bucket);
            DefsByType = builder.ToImmutable();

            var builder2 = ImmutableDictionary.CreateBuilder<string, GatewayBucket>();
            foreach (var bucket in buckets)
                builder2.Add(bucket.Id, bucket);
            DefsById = builder2.ToImmutable();
        }

        public static GatewayBucket Get(GatewayBucketType type) => DefsByType[type];
        public static GatewayBucket Get(string id) => DefsById[id];

        public GatewayBucketType Type { get; }
        public string Id { get; }
        public int WindowCount { get; }
        public int WindowSeconds { get; }

        public GatewayBucket(GatewayBucketType type, string id, int count, int seconds)
        {
            Type = type;
            Id = id;
            WindowCount = count;
            WindowSeconds = seconds;
        }
    }
}
