using System.Reflection;

namespace HealthCheck.Framework.Helpers;

public class QueryBuilder
{
    public static string BuildInsertQuery<T>(T entity,
                                             List<string> ignoreThisAttr,
                                             string tableName,
                                             bool returnResult = false)
    {
        Dictionary<string, string> columnsAndValues = GetColumnsAndValues<T>(ignoreThisAttr);

        string sql = $"INSERT INTO {tableName} ({string.Join(", ", columnsAndValues.Keys)}) VALUES ({string.Join(", ", columnsAndValues.Values)})";

        if (returnResult)
            sql += " RETURNING *";

        return sql;
    }

    public static string BuildUpdateQuery<T>(string tableName, string whereClause, List<string> ignoreThisAttr)
    {
        Dictionary<string, string> columnsAndValues = GetColumnsAndValues<T>(ignoreThisAttr);
        return $"UPDATE {tableName} SET {string.Join(", ", columnsAndValues.Select(kv => $"{kv.Key} = {kv.Value}"))} WHERE {whereClause}";
    }

    public static string BuildDeleteQuery(string tableName, string whereClause)
    {
        return $"DELETE FROM {tableName} WHERE {whereClause}";
    }

    public static string BuildSelectQuery(string tableName, string whereClause = "")
    {
        return $"SELECT * FROM {tableName}" + (string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}");
    }

    private static Dictionary<string, string> GetColumnsAndValues<T>(List<string> ignoreThisAttr)
    {
        PropertyInfo[] properties = typeof(T).GetProperties();

        Dictionary<string, string> columnsAndValues = new();

        foreach (PropertyInfo property in properties)
        {
            //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
            // Caso a propriedade esteja na lista de atributos a serem ignorados,
            // pule para a próxima iteração
            //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
            if (ignoreThisAttr.Contains(property.Name))
                continue;

            columnsAndValues.Add(property.Name.ToSnakeCase(), $"@{property.Name}");
        }

        return columnsAndValues;
    }
}
