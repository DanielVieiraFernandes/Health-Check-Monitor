using HealthCheck.Framework.Enums;

namespace HealthCheck.Framework.Models;

public class SystemCheck : UtilsForModels
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SystemId { get; set; }
    public HealthStatus Status { get; set; }
    public long LatencyMs { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.Now;
    public string? Message { get; set; }
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }

    protected override List<string> ignoreAttributes { get; } =
    [
        nameof(SystemCheck.Id),
        nameof(SystemCheck.CheckedAt)
    ];
}