using System.ComponentModel.DataAnnotations;

namespace URLShortener.Models
{
    public class ShortUrl
    {
        [Key]
        public long Id { get; set; }
        [Required]
        [MaxLength(2048)]
        public string OriginalUrl { get; set; }

        [Required]
        [MaxLength(12)]
        public string ShortCode { get; set; }
        public DateTime Created {  get; set; }
        public DateTime? ExpireAt { get; set; }
        public long ClickCount { get; set; } = 0;

    }
}
