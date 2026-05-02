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

        var propertiesObj1 = type1.GetProperties();
        var propertiesObj2 = type2.GetProperties();

        List<string> differences = new();

        foreach (var prop1 in propertiesObj1)
        {
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Verifica se a propriedade atual está na lista de propriedades a serem ignoradas.
            // Se estiver, pula para a próxima propriedade.
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (ignoreAttr != null && ignoreAttr.Contains(prop1.Name))
                continue;

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Tento encontrar a propriedade correspondente no segundo objeto com base no nome da propriedade do primeiro objeto.
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            var prop2 = propertiesObj2.FirstOrDefault(p => p.Name == prop1.Name);

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Se não encontrar a propriedade, pula para a próxima.
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (prop2 == null)
                continue;

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Recupero o valor das duas propriedades
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            var value1 = prop1.GetValue(obj1);
            var value2 = prop2.GetValue(obj2);

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Comparo os valores. Se forem diferentes, adiciono uma string formatada à lista de diferenças.
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (!Equals(value1, value2))
                differences.Add($"Propriedade '{prop1.Name}' alterada de '{value2 ?? "null"}' para '{value1 ?? "null"}'.");
        }

        return string.Join(Environment.NewLine, differences);
    }


}
