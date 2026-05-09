using Microsoft.AspNetCore.Mvc;
using URLShortener.Models.DTOs;
using URLShortener.Services;

namespace URLShortener.Controllers
{
    [ApiController]
    [Route("")]  
    public class HealthController : ControllerBase
    {
        [HttpGet("health")]
        public async Task<ActionResult<HealthCheckResponse>> GetHealth(
            [FromServices] HealthCheckService healthCheck)
        {
            var health = await healthCheck.CheckHealthAsync();
            var statusCode = health.Status == "Healthy" ? 200 : 503;
            return StatusCode(statusCode, health);
        }
    }
}
