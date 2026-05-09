using StackExchange.Redis;
using URLShortener.Services.Interfaces;

namespace URLShortener.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
            _logger = logger;
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                return value.HasValue ? value.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value from Redis for key {Key}", key);
                return null;
            }
        }

        public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                await _db.StringSetAsync(key, value, expiry.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value in Redis for key {Key}", key);
            }
        }

        public async Task<bool> DeleteAsync(string key)
        {
            try
            {
                return await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting key {Key} from Redis", key);
                return false;
            }
        }
    }
}
