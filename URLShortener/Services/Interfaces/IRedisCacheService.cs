namespace URLShortener.Services.Interfaces
{
    public interface IRedisCacheService
    {
        Task<string?> GetAsync(string key);
        Task<bool> DeleteAsync(string key);
        Task SetAsync(string key, string value, TimeSpan? expiry = null);
    }
}
