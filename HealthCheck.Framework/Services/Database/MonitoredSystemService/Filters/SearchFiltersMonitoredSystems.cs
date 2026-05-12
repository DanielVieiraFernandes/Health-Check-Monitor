using HealthCheck.Framework.Enums;

namespace HealthCheck.Framework.Services.Database.MonitoredSystemService.Filters;

public class SearchFiltersMonitoredSystems
{
    public Guid? UserId { get; set; }
    public HealthStatus? Status { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
}
