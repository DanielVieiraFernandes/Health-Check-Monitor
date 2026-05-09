using HealthCheck.Framework.Enums;

namespace HealthCheck.Framework.Models;

public class MonitoredSystem
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

    #region Atributos utilitários para validação de campos e construção de queries dinâmicas

    private List<string> _ignoreAttributes = [nameof(MonitoredSystem.UpdatedAt),
                                             nameof(MonitoredSystem.Id),
                                             nameof(MonitoredSystem.UserId),
                                             nameof(MonitoredSystem.LastCheckedAt),
                                             nameof(MonitoredSystem.LastStatus)];

    public List<string> GetIgnoreAttributes() => _ignoreAttributes;

    public void AddIgnoreAttribute(string attributeName)
    {
        if (!_ignoreAttributes.Contains(attributeName))
            _ignoreAttributes.Add(attributeName);
    }
    public void RemoveIgnoreAttribute(string attributeName)
    {
        if (_ignoreAttributes.Contains(attributeName))
            _ignoreAttributes.Remove(attributeName);
    }

    #endregion Atributos utilitários para validação de campos e construção de queries dinâmicas
}
