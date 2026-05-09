namespace HealthCheck.Framework.Services.Database.MonitoredSystemService.Filters;

public class SearchFiltersMonitoredSystems
{
    public Guid? UserId { get; set; } = null;
    public string SearchTerm { get; set; } = string.Empty;
}
