using HealthCheck.Framework.Enums;

namespace HealthCheck.Framework.Models;

public sealed class MonitoredSystem : UtilsForModels
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string History { get; set; } = string.Empty;
    public HealthStatus LastStatus { get; set; } = HealthStatus.Unknown;
    public DateTime? LastCheckedAt { get; set; } = null;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // ATRIBUTOS UTILITÁRIOS PARA VALIDAÇÕES E CONSTRUIR QUERIES DINÂMICAS
    //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    protected override List<string> ignoreAttributes { get; } =
    [
        nameof(MonitoredSystem.UpdatedAt),
        nameof(MonitoredSystem.Id),
        nameof(MonitoredSystem.UserId),
        nameof(MonitoredSystem.LastCheckedAt),
        nameof(MonitoredSystem.LastStatus)
    ];
}
