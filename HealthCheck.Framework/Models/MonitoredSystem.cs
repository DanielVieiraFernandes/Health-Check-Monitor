using HealthCheck.Framework.Enums;
using System.Net;

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

    public SystemType SystemType { get; set; } = SystemType.WebApi;

    /// <summary>Código HTTP esperado. Null = usa default do tipo.</summary>
    public HttpStatusCode? ExpectedHttpStatus { get; set; }

    /// <summary>Texto esperado no body (Front-end). Null = não verifica.</summary>
    public string? ExpectedBodyText { get; set; }

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
