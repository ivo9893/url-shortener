using URLShortener.Models.DTOs;

namespace URLShortener.Services.Interfaces
{
    public interface IClickAnalyticsService
    {
        Task ProcessClickAsync(UrlClickedEvent clickEvent);
    }
}
