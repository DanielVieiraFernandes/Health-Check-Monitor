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
            char prevC = i > 0 ? str[i - 1] : ' ';
            char nextC = i + 1 < str.Length ? str[i + 1] : ' ';
            if (char.IsUpper(c) && (!char.IsUpper(prevC) || !char.IsUpper(nextC)))
            {
                // Adiciona um underscore antes de letras maiúsculas, exceto para a primeira letra
                if (i > 0)
                    stringBuilder.Append('_');
                stringBuilder.Append(char.ToLower(c));
            }
            else
            {
                stringBuilder.Append(char.ToLower(c));
            }
        }
        return stringBuilder.ToString();
    }

    public static string NormalizeWhiteSpaces(this string? str, bool removeAccent = false, bool removeMultipleSpaces = false)
    {
        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        var trimmed = str.Trim();

        //****************************************************************************************************************************
        //Caso seja necessário remover os acentos
        //****************************************************************************************************************************
        if (removeAccent)
        {
            //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
            //Remove acentos usando NormalizationForm
            //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
            trimmed = trimmed.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (char c in trimmed)
            {
                if (char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            trimmed = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        //****************************************************************************************************************************
        //Caso seja necessário remover os múltiplos espaços
        //****************************************************************************************************************************
        if (removeMultipleSpaces)
        {
            //Retorna o valor com os espaços normalizados (múltiplos espaços são reduzidos a um único espaço)
            trimmed = Regex.Replace(trimmed, @"\s+", " ");
        }

        return trimmed;
    }
}
