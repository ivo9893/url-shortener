using Microsoft.EntityFrameworkCore;
using URLShortener.Infrastructure;
using URLShortener.Models.DTOs;
using URLShortener.Services.Interfaces;

namespace URLShortener.Services
{
    public class ClickAnalyticsService : IClickAnalyticsService
    {
        private readonly ShardedDbContextFactory _dbContextFactory;
        private readonly ILogger<ClickAnalyticsService> _logger;
    
        public ClickAnalyticsService(ShardedDbContextFactory dbContextFactory, ILogger<ClickAnalyticsService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task ProcessClickAsync(UrlClickedEvent clickEvent)
        {
            using var dbContext = _dbContextFactory.CreateDbContext(clickEvent.ShortCode);

            var shortUrl = await dbContext.ShortUrls.FirstOrDefaultAsync(u => u.ShortCode == clickEvent.ShortCode);

            if(shortUrl == null)
            {
                _logger.LogWarning("ShortCode {ShortCode} not found for click tracking", clickEvent.ShortCode);
                return;
            }

            shortUrl.ClickCount++;

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Incremented click count for {ShortCode} to {Count}",
           clickEvent.ShortCode, shortUrl.ClickCount);
        }
    }
}
