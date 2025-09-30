using System;
using System.Linq;
using System.Text.Json;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Converters;

internal static class DictionaryJsonConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static ValueConverter<IDictionary<string, string?>?, string?> GetConverter() =>
        new ValueConverter<IDictionary<string, string?>?, string?>(
            dict => dict == null ? null : JsonSerializer.Serialize(dict, JsonOptions),
            json => string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions) as IDictionary<string, string?>);

    public static ValueComparer<IDictionary<string, string?>?> GetComparer() =>
        new ValueComparer<IDictionary<string, string?>?>( 
            (d1, d2) => DictionaryEquals(d1, d2),
            d => d == null ? 0 : GetDictionaryHashCode(d),
            d => d == null ? null : new Dictionary<string, string?>(d));

    private static bool DictionaryEquals(IDictionary<string, string?>? a, IDictionary<string, string?>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var bv)) return false;
            if (!string.Equals(kv.Value, bv, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static int GetDictionaryHashCode(IDictionary<string, string?> d)
    {
        unchecked
        {
            int hash = 17;
            foreach (var kv in d.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                hash = hash * 23 + StringComparer.Ordinal.GetHashCode(kv.Key);
                hash = hash * 23 + (kv.Value?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}
