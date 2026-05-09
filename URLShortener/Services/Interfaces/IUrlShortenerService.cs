using URLShortener.Models;
using URLShortener.Models.DTOs;

namespace URLShortener.Services.Interfaces
{
    public interface IUrlShortenerService
    {
        Task<ShortUrl> ShortenUrlAsync(string originalUrl, DateTime? expiresAt = null);
        Task<ShortUrl> GetUrlByShortCodeAsync(string shortCode);
        Task<UrlStatsResponse> GetUrlStatsAsync(string shortCode);
    }
}
