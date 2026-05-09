using URLShortener.Models.DTOs;

namespace URLShortener.Services.Interfaces
{
    public interface IRabbitPublisher
    {
        Task PublishClickEvent(UrlClickedEvent clickEvent);
    }
}
