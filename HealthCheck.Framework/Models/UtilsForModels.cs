namespace HealthCheck.Framework.Models;

public abstract class UtilsForModels
{
    protected abstract List<string> ignoreAttributes { get; }

    #region Atributos utilitários para validação de campos e construção de queries dinâmicas

    public List<string> GetIgnoreAttributes() => ignoreAttributes;

    public void AddIgnoreAttribute(string attributeName)
    {
        if (!ignoreAttributes.Contains(attributeName))
            ignoreAttributes.Add(attributeName);
    }
    public void RemoveIgnoreAttribute(string attributeName)
    {
        if (ignoreAttributes.Contains(attributeName))
            ignoreAttributes.Remove(attributeName);
    }

    #endregion Atributos utilitários para validação de campos e construção de queries dinâmicas
}
