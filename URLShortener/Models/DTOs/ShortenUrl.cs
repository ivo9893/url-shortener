namespace URLShortener.Models.DTOs
{
    public class ShortenUrlRequest
    {
        public string OriginalUrl { get; set; }
        public DateTime? ExpireAt { get; set; }
    }

    public class ShortenUrlResponse
    {
        public string ShortCode { get; set; }
        public string ShortUrl { get; set; }
        public string OrigiralUrl { get; set; }
    }
}
