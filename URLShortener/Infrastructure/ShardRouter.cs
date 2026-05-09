using URLShortener.Services;

namespace URLShortener.Infrastructure
{
    public class ShardRouter
    {

        private readonly IConfiguration _configuration;

        private readonly Dictionary<int, int> _virtualToPhysicalMap;
        private readonly int _virtualShardCount;

        private readonly ILogger<ShardRouter> _logger;

        public ShardRouter(IConfiguration configuration, ILogger<ShardRouter> logger)
        {
            _configuration = configuration;
            _virtualShardCount = configuration.GetValue<int>("Sharding:VirtualShards");
            _virtualToPhysicalMap = configuration
                .GetSection("Sharding:VirtualToPhysicalMap")
                .Get<Dictionary<string, int>>()
                ?.ToDictionary(kvp => int.Parse(kvp.Key), kvp => kvp.Value)
                ?? throw new InvalidOperationException("Shard mapping not configured");

            _logger = logger;
        }

        public string GetShardConnectionString(string shortCode)
        {
            int hash = GetDeterministicHash(shortCode);
            int virtualShard = Math.Abs(hash) % _virtualShardCount;
            int physicalShard = _virtualToPhysicalMap[virtualShard];

            _logger.LogInformation("ShortCode: {ShortCode}, Hash: {Hash}, VirtualShard: {VirtualShard}, PhysicalShard: {PhysicalShard}",
    shortCode, hash, virtualShard, physicalShard);

            string connectionString = _configuration[$"SHARD_{physicalShard}_CONNECTION"]
                ?? throw new InvalidOperationException($"Connection string for shard {physicalShard} not found");

            return connectionString;
        }

        private int GetDeterministicHash(string input)
        {
            unchecked
            {
                int hash = 5381;
                foreach (char c in input)
                {
                    hash = ((hash << 5) + hash) + c; // hash * 33 + c
                }
                return hash;
            }
        }
    }
}
