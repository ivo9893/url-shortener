using Microsoft.AspNetCore.Mvc;
using URLShortener.Infrastructure;
using URLShortener.Models;
using URLShortener.Models.DTOs;
using URLShortener.Services;
using URLShortener.Services.Interfaces;

namespace URLShortener.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class UrlController : ControllerBase
    {

        private readonly ILogger<UrlController> _logger;
        private readonly IUrlShortenerService _urlShortenerService;
        private readonly IRabbitPublisher _rabbitMq;

        public UrlController(ILogger<UrlController> logger, IUrlShortenerService urlShortenerService, IRabbitPublisher rabbitMq)
        {
            _logger = logger;
            _urlShortenerService = urlShortenerService;
            _rabbitMq = rabbitMq;
        }

        [HttpGet("{shortCode}")]
        public async Task<ActionResult> RedirectToUrl(string shortCode)
        {


            var shortUrl = await _urlShortenerService.GetUrlByShortCodeAsync(shortCode);

            if (shortUrl == null)
                return NotFound();

            _ = _rabbitMq.PublishClickEvent(new UrlClickedEvent
            {
                ShortCode = shortCode,
                ClickedAt = DateTime.UtcNow,
                UserAgent = Request.Headers.UserAgent.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            return Redirect(shortUrl.OriginalUrl);
        }

        [HttpGet("stats/{shortCode}")]
        public async Task<ActionResult<UrlStatsResponse>> GetUrlStats(string shortCode)
        {
            var stats = await _urlShortenerService.GetUrlStatsAsync(shortCode);
            if (stats == null)
                return NotFound();
            return Ok(stats);
        }

        [HttpPost("shorten")]
        public async Task<ActionResult<ShortenUrlResponse>> ShortenUrl([FromBody] ShortenUrlRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OriginalUrl))
                return BadRequest("URL is required");

            if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out _))
                return BadRequest("Invalid URL format");


            var shortUrl = await _urlShortenerService.ShortenUrlAsync(request.OriginalUrl, request.ExpireAt);


            var response = new ShortenUrlResponse
            {
                ShortCode = shortUrl.ShortCode,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/url/{shortUrl.ShortCode}",
                OrigiralUrl = request.OriginalUrl
            };

            return Ok(response);
        }
    }
}
