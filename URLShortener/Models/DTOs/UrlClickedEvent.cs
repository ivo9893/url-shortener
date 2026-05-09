namespace URLShortener.Models.DTOs
{
    public class UrlClickedEvent
    {
        public string ShortCode { get; set; } = string.Empty;
        public DateTime ClickedAt { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
    }
}
