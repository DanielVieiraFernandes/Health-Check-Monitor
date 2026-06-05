using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;

namespace HealthCheck.Worker.Services.SystemCheckers;

public interface ISystemChecker
{
    SystemType SupportedType { get; }
    Task<CheckResult> CheckAsync(MonitoredSystem system, CancellationToken ct);
}

public record CheckResult
{
    public HealthStatus Status { get; init; }
    public long LatencyMs { get; init; }
    public string? Response { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ExceptionType { get; init; }
    public string? StackTrace { get; init; }
}
