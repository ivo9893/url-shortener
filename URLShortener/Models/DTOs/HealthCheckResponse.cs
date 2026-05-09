namespace URLShortener.Models.DTOs
{
    public class HealthCheckResponse
    {
        public string Status { get; set; } = "Healthy";
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    }

    public class ComponentHealth
    {
        public string Status { get; set; } = "Healthy";
        public string? Message { get; set; }
    }
}
