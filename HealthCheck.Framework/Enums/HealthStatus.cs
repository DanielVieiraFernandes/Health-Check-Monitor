using System.ComponentModel;

namespace HealthCheck.Framework.Enums;

public enum HealthStatus
{
    [Description("1 - Saudável")]
    Healthy = 1,
    [Description("2 - Não saudável")]
    Unhealthy = 2,
    [Description("3 - Desconhecido")]
    Unknown = 3,
    [Description("4 - Todos")]
    All = 4
}
