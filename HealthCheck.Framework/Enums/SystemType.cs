using System.ComponentModel;

namespace HealthCheck.Framework.Enums;

public enum SystemType
{
    [Description("Web API")]
    WebApi = 1,

    [Description("Front-end")]
    Frontend = 2
}
