using System.ComponentModel;

namespace HealthCheck.Framework.Enums;

public enum LatencyPreference
{
    [Description("Maior latência")]
    Highest = 1,
    [Description("Menor latência")]
    Lowest = 2
}
