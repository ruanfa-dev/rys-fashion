using System.Reflection;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace SharedKernel.Models.ModelBinding;

public static class MinimalApiBindHelper
{
    // Create T from query collection by matching camelCase or snake_case keys against property names
    public static T BindFromQuery<T>(IQueryCollection query) where T : new()
    {
        T? instance = new T();
        Type type = typeof(T);

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite) continue;

            // try multiple candidate keys
            string[] candidates = new[]
            {
                prop.Name,                          // PageIndex
                ToCamelCase(prop.Name),             // pageIndex (redundant when prop.Name is pascal)
                ToSnakeCase(prop.Name)              // page_index
            };

            foreach (string key in candidates)
            {
                if (query.TryGetValue(key, out StringValues value) && value.Count > 0)
                {
                    try
                    {
                        object? converted = ConvertValue(value, prop.PropertyType);
                        if (converted != null)
                        {
                            prop.SetValue(instance, converted);
                            break;
                        }
                    }
                    catch
                    {
                        // ignore conversion errors, try next candidate
                    }
                }
            }
        }

        return instance;
    }

    private static object? ConvertValue(StringValues value, Type targetType)
    {
        string s = value.ToString();
        if (string.IsNullOrWhiteSpace(s)) return null;

        // handle arrays
        if (targetType == typeof(string[]))
        {
            string?[] arr = s.Contains(',') ? s.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray()
                                      : value.ToArray();
            return arr;
        }

        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(string)) return s;
        if (underlying == typeof(int) && int.TryParse(s, out int i)) return i;
        if (underlying == typeof(long) && long.TryParse(s, out long l)) return l;
        if (underlying == typeof(bool) && bool.TryParse(s, out bool b)) return b;
        if (underlying == typeof(DateTime) && DateTime.TryParse(s, out DateTime dt)) return dt;
        if (underlying == typeof(Guid) && Guid.TryParse(s, out Guid g)) return g;

        // fallback to ChangeType
        return Convert.ChangeType(s, underlying);
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (char.IsLower(name[0])) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}