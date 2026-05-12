namespace HealthCheck.Web.Components.Shared.UIModels;

public class DashboardInfoUI
{
    /// <summary>
    /// Quantidade de sistemas ativos
    /// </summary>
    public int ActiveSystems { get; set; }

    /// <summary>
    /// Alertas críticos
    /// </summary>
    public int CriticalAlerts { get; set; }

    /// <summary>
    /// Latência média das requisições
    /// </summary>
    public int MediumLatency { get; set; }

    /// <summary>
    /// Percentual de disponibilidade dos sistemas monitorados
    /// </summary>
    public decimal Availability { get; set; }
}
