using HealthCheck.Framework.Enums;

namespace HealthCheck.Framework.Services.Database.SystemChecksService.Filters;

public class SearchSystemChecksFilter
{
    public Guid UserId { get; set; }

    public string? SearchTerm { get; set; }

    //***************************************************************************************************************************************
    //Sempre que o usuário não informar o período de consulta das checagens, o sistema deve considerar as últimas 24 horas
    //como período padrão para a consulta. Nunca irei retornar todas as checagens do usuário sem um parâmetro para o período definido.
    //***************************************************************************************************************************************
    public bool Last24Hours => FromDate == null && ToDate == null;
    public List<HealthStatus>? HealthStatusSelected { get; set; }
    private DateTime? _fromDate { get; set; }
    private DateTime? _toDate { get; set; }
    public DateTime? FromDate { get => _fromDate?.Date; set => _fromDate = value; }
    public DateTime? ToDate { get => _toDate?.Date.AddDays(1).AddSeconds(-1); set => _toDate = value; }
    public LatencyPreference? LatencyPreference { get; set; }
}
