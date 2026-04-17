using System.ComponentModel;

namespace HealthCheck.Framework.Helpers;

public static class EnumsExt
{
    /// <summary>
    /// Recupera a descrição de um valor de enumeração, caso exista. Se a descrição não estiver presente, retorna o nome do valor do enum.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetDescription(this Enum value)
    {
        //Recuperando o tipo do enum
        var enumType = value.GetType();

        //Recuperando o nome do item do enum
        var field = enumType.GetField(value.ToString());

        //Recuperando o texto do enum
        var attributes = field!.GetCustomAttributes(typeof(DescriptionAttribute), false);

        //Senão tiver declarado o atributo Description é retornado o nome do item do enum
        return attributes.Length == 0 ? value.ToString() : ((DescriptionAttribute)attributes[0]).Description;
    }
}
