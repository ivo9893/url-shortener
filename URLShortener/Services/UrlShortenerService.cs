using Microsoft.EntityFrameworkCore;
using URLShortener.Infrastructure;
using URLShortener.Models;
using URLShortener.Models.DTOs;
using URLShortener.Services.Interfaces;

namespace URLShortener.Services
{
    public class UrlShortenerService : IUrlShortenerService
    {
        private readonly IdGeneratorService _idGenerator;
        private readonly ShardedDbContextFactory _dbContextFactory;
        private readonly ILogger<UrlShortenerService> _logger;
        private readonly IRedisCacheService _redisCacheService;

        public UrlShortenerService(
            IdGeneratorService idGenerator,
            ShardedDbContextFactory dbContextFactory,
            ILogger<UrlShortenerService> logger,
            IRedisCacheService redisCacheService)
        {
            _idGenerator = idGenerator;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _redisCacheService = redisCacheService;
        }

        public async Task<ShortUrl> GetUrlByShortCodeAsync(string shortCode)
        {
            var cacheUrl = await _redisCacheService.GetAsync($"url:{shortCode}");
            if(cacheUrl != null)
            {
                _logger.LogInformation("Cache hit for short code {ShortCode}", shortCode);
                return new ShortUrl
                {
                    ShortCode = shortCode,
                    OriginalUrl = cacheUrl
                };
            }

            _logger.LogInformation("Cache MISS for {ShortCode}", shortCode);

            using var dbContext = _dbContextFactory.CreateDbContext(shortCode);

            var shortUrl = await dbContext.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);

            if (shortUrl != null && shortUrl.ExpireAt.HasValue && shortUrl.ExpireAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Short code {ShortCode} has expired!", shortCode);
                return null;
            }

            var cacheExpire = shortUrl?.ExpireAt.HasValue == true ? shortUrl.ExpireAt.Value - DateTime.UtcNow : TimeSpan.FromDays(24);

            await _redisCacheService.SetAsync($"url:{shortCode}", shortUrl?.OriginalUrl, cacheExpire);

            return shortUrl;
        }

        public async Task<UrlStatsResponse> GetUrlStatsAsync(string shortCode)
        {
            using var dbContext = _dbContextFactory.CreateDbContext(shortCode);

            var shortUrl = await dbContext.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);

            if(shortUrl == null)
            {
                return null;
            }

            return new UrlStatsResponse
            {
                ShortCode = shortUrl.ShortCode,
                OriginalUrl = shortUrl.OriginalUrl,
                ClickCount = shortUrl.ClickCount,
                CreatedAt = shortUrl.Created,
                ExpiresAt = shortUrl.ExpireAt,
                IsExpired = shortUrl.ExpireAt.HasValue && shortUrl.ExpireAt < DateTime.UtcNow
            };
        }

        public async Task<ShortUrl> ShortenUrlAsync(string originalUrl, DateTime? expiresAt = null)
        {
            var (id, shortCode) = _idGenerator.GenerateShortCode();

            using var dbContext = _dbContextFactory.CreateDbContext(shortCode);

            var shortUrl = new ShortUrl
            {
                Id = id,
                ShortCode = shortCode,
                OriginalUrl = originalUrl,
                Created = DateTime.UtcNow,
                ExpireAt = expiresAt
            };

            dbContext.ShortUrls.Add(shortUrl);
            await dbContext.SaveChangesAsync();

            var cacheExpire = expiresAt.HasValue ? expiresAt.Value - DateTime.UtcNow : TimeSpan.FromDays(24);

            await _redisCacheService.SetAsync($"url:{shortCode}", originalUrl, cacheExpire);

            _logger.LogInformation("Created short URL {ShortCode} for {OriginalUrl}", shortCode, originalUrl);

            return shortUrl;
        }
    }
}
