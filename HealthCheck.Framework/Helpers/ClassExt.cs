using System.Reflection;

namespace HealthCheck.Framework.Helpers;

public static class ClassExt
{
    public static T Clone<T>(this T obj) where T : class
    {
        return (T)obj.GetType().GetMethod(
            "MemberwiseClone",
            BindingFlags.NonPublic | BindingFlags.Instance
        )!.Invoke(obj!, null)!;
    }
}
