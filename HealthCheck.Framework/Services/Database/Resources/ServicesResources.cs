namespace HealthCheck.Framework.Services.Database.Resources;

public class ServicesResources
{
    /// <summary>
    /// Compara objetos do mesmo tipo e retorna uma string formatada com as diferenças entre eles.
    /// </summary>
    /// <param name="obj1">Objeto mais recente usado como referência para o valor novo.</param>
    /// <param name="obj2">Objeto mais antigo usado como referência para o valor anterior.</param>
    /// <param name="ignoreAttr">Lista de nomes de propriedades que devem ser ignoradas na comparação.</param>
    /// <returns>Descrição das diferenças encontradas entre os objetos.</returns>
    public static string CompareObjects(object obj1, object obj2, List<string>? ignoreAttr = null)
    {

        // Recupero os tipos dos objetos usando o reflection do C#.
        var type1 = obj1.GetType();
        var type2 = obj2.GetType();

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Verifico se os tipos dos objetos são diferentes. Caso seja, lanço uma exceção, pois a comparação só faz sentido para
        // objetos do mesmo tipo.
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (type1 != type2)
            throw new ArgumentException("Os objetos devem ser do mesmo tipo.");

        var propertiesObj = type1.GetProperties()
            .Where(p => ignoreAttr != null ? !ignoreAttr.Contains(p.Name) || p.Name.Equals("History", StringComparison.InvariantCultureIgnoreCase) : true);

        List<string> differences = new();

        foreach (var prop in propertiesObj)
        {
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Recupero o valor das duas propriedades
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            var value1 = prop.GetValue(obj1);
            var value2 = prop.GetValue(obj2);

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Comparo os valores. Se forem diferentes, adiciono uma string formatada à lista de diferenças.
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (!Equals(value1, value2))
                differences.Add($"Propriedade '{prop.Name}' alterada de '{value2 ?? "null"}' para '{value1 ?? "null"}'.");
        }

        return string.Join(Environment.NewLine, differences);
    }


}
