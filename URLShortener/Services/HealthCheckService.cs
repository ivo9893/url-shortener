using Microsoft.EntityFrameworkCore;
using URLShortener.Data;
using URLShortener.Infrastructure;
using URLShortener.Models.DTOs;
using URLShortener.Services.Interfaces;

namespace URLShortener.Services
{
    public class HealthCheckService
    {
        private readonly IRedisCacheService _redis;
        private readonly ShardedDbContextFactory _dbContextFactory;
        private readonly ILogger<HealthCheckService> _logger;
        private readonly IConfiguration _configuration;

        public HealthCheckService(IRedisCacheService redis, ShardedDbContextFactory dbContextFactory, ILogger<HealthCheckService> logger, IConfiguration configuration)
        {
            _redis = redis;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<HealthCheckResponse> CheckHealthAsync()
        {
            var response = new HealthCheckResponse();

            response.Components["redis"] = await CheckRedisAsync();

            response.Components["database_shard_0"] = await CheckDatabaseShardAsync(0);
            response.Components["database_shard_1"] = await CheckDatabaseShardAsync(1);
            response.Components["database_shard_2"] = await CheckDatabaseShardAsync(2);
            response.Components["database_shard_3"] = await CheckDatabaseShardAsync(3);

            if (response.Components.Values.Any(c => c.Status == "Unhealthy"))
            {
                response.Status = "Unhealthy";
            }

            return response;
        }

        private async Task<ComponentHealth> CheckRedisAsync()
        {
            try
            {
                var testKey = $"health:check:{Guid.NewGuid()}";
                await _redis.SetAsync(testKey, "test", TimeSpan.FromSeconds(5));
                var value = await _redis.GetAsync(testKey);
                await _redis.DeleteAsync(testKey);

                return new ComponentHealth { Status = "Healthy" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    Message = ex.Message
                };
            }
        }

        private async Task<ComponentHealth> CheckDatabaseShardAsync(int shardIndex)
        {
            try
            {
                var connectionString = _configuration[$"SHARD_{shardIndex}_CONNECTION"];

                if (string.IsNullOrEmpty(connectionString))
                {
                    return new ComponentHealth
                    {
                        Status = "Unhealthy",
                        Message = "Connection string not configured"
                    };
                }

                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(connectionString);

                using var context = new ApplicationDbContext(optionsBuilder.Options);

                // Actually query the database, not just check if we can connect
                var count = await context.ShortUrls.CountAsync();

                return new ComponentHealth
                {
                    Status = "Healthy",
                    Message = $"{count} URLs in shard"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database shard {ShardIndex} health check failed", shardIndex);
                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    Message = ex.Message
                };
            }
        }

    }
}
