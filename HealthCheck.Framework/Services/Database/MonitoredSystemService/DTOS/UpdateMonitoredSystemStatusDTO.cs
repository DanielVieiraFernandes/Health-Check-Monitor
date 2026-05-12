using HealthCheck.Framework.Enums;

namespace HealthCheck.Framework.Services.Database.MonitoredSystemService.DTOS;

public class UpdateMonitoredSystemStatusDTO
{
    public Guid Id { get; set; }
    public HealthStatus Status { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public string History { get; set; } = string.Empty;
}
