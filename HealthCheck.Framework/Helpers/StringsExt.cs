using System.Text;
using System.Text.RegularExpressions;

namespace HealthCheck.Framework.Helpers;

public static class StringsExt
{
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var stringBuilder = new StringBuilder();

        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (char.IsUpper(c))
            {
                // Adiciona um underscore antes de letras maiúsculas, exceto para a primeira letra
                if (i > 0)
                    stringBuilder.Append('_');
                stringBuilder.Append(char.ToLower(c));
            }
            else
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString();
    }

    public static string NormalizeWhiteSpaces(this string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        var trimmed = str.Trim();

        return Regex.Replace(trimmed, @"\s+", " ");
    }
}
