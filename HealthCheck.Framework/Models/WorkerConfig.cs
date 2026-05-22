namespace HealthCheck.Framework.Models;

public class WorkerConfig
{
    public int MonitoringIntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxConcurrentChecks { get; set; } = 10;
    public int MaxRetries { get; set; } = 0;
    public int DelayBetweenRetriesMs { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public Guid UserUUIDLastModified { get; set; }
}
