using System.ComponentModel;

namespace HealthCheck.Framework.Enums;

public enum HealthStatus
{
    [Description("Saudável")]
    Healthy = 1,
    [Description("Não saudável")]
    Unhealthy = 2,
    [Description("Desconhecido")]
    Unknown = 3,
    [Description("Todos")]
    All = 4
}
