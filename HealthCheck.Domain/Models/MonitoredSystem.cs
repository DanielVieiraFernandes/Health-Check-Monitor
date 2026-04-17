using HealthCheck.Framework.Enums;

namespace HealthCheck.Framework.Models;

public class MonitoredSystem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int IntervalInMinutes { get; set; } = 480;
    public HealthStatus LastStatus { get; set; } = HealthStatus.Unknown;
    public DateTime LastCheckedAt { get; set; }
}
