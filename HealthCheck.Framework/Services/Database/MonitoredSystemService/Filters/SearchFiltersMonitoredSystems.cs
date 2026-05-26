using HealthCheck.Framework.Enums;

namespace HealthCheck.Framework.Services.Database.MonitoredSystemService.Filters;

public class SearchFiltersMonitoredSystems
{
    public Guid? UserId { get; set; }
    public List<HealthStatus>? StatusSelected { get; set; }
    public string SearchTerm { get; set; } = string.Empty;

    //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
    //PARÂMETROS PARA ORDENAR PELA DATA DE CRIAÇÃO!!!
    //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

    public DateTime? FromDate { get => field?.Date; set; }
    public DateTime? ToDate { get => field?.Date.AddDays(1).AddMicroseconds(-1); set; }

    public bool SearchToDate { get => FromDate.HasValue && ToDate.HasValue; }
}
