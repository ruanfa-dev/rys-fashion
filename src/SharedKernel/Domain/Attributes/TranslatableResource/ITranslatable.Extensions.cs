using System.Reflection;

namespace SharedKernel.Domain.Attributes.TranslatableResource;

/// <summary>
/// Strongly-typed extensions for translatable resources and translation collections.
/// Combines instance helpers and collection fallback helpers for culture lookup, neutral fallback,
/// default translation and convenience dictionary builders.
/// </summary>
public static class TranslatableExtensions
{
    #region Instance extensions

    /// <summary>
    /// Get translated field value from an ITranslatable resource. Uses translation collection lookup rules.
    /// </summary>
    public static string? GetField<TTranslation>(this ITranslatable<TTranslation> resource, string fieldName, string culture, bool fallback = false)
        where TTranslation : class, ITranslation
    {
        if (resource == null) throw new ArgumentNullException(nameof(resource));
        return resource.Translations.GetFieldWithCulture(culture, t => GetStringField(t, fieldName), fallback);
    }

    /// <summary>
    /// Try to get the translated value for <paramref name="fieldName"/> for the requested <paramref name="culture"/>.
    /// Falls back to neutral culture, default translation or first available when <paramref name="fallback"/> is true.
    /// </summary>
    public static bool TryGetField<TTranslation>(this ITranslatable<TTranslation>? resource, string fieldName, string culture, bool fallback, out string? value)
        where TTranslation : class, ITranslation
    {
        value = null;
        if (resource is null) return false;

        ICollection<TTranslation>? translations = resource.Translations;
        if (translations is null || translations.Count == 0) return false;

        TTranslation? t = translations.GetTranslationForCulture(culture, fallback);
        if (t is null) return false;

        value = GetStringField(t, fieldName);
        return !string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Gets the translated value or returns the provided <paramref name="defaultValue"/> when missing.
    /// </summary>
    public static string? GetFieldOrDefault<TTranslation>(this ITranslatable<TTranslation>? resource, string fieldName, string culture, bool fallback = true, string? defaultValue = null)
        where TTranslation : class, ITranslation
    {
        if (resource.TryGetField(fieldName, culture, fallback, out string? v))
            return v;

        return defaultValue;
    }

    /// <summary>
    /// Builds a dictionary of localized values for the given locales. Preserves order of locales.
    /// </summary>
    public static IDictionary<string, string?> LocalizedFieldForLocales<TTranslation>(this ITranslatable<TTranslation>? resource, IEnumerable<string>? locales, string fieldName = "Slug")
        where TTranslation : class, ITranslation
    {
        Dictionary<string, string?> result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (resource is null || locales is null) return result;

        foreach (string locale in locales)
        {
            string? value = resource.GetFieldOrDefault(fieldName, locale, fallback: true, defaultValue: null);
            result[locale] = value;
        }

        return result;
    }

    #endregion

    #region Collection helpers

    /// <summary>
    /// Get a translated value from a typed translations collection using a field selector.
    /// Supports exact culture, neutral culture and default/first fallbacks.
    /// </summary>
    public static string? GetFieldWithCulture<TTranslation>(this IEnumerable<TTranslation>? translations, string culture, Func<TTranslation, string?> fieldSelector, bool fallback = false)
        where TTranslation : class, ITranslation
    {
        if (translations is null) return null;
        if (fieldSelector is null) throw new ArgumentNullException(nameof(fieldSelector));
        if (string.IsNullOrWhiteSpace(culture))
            return fallback ? translations.Select(fieldSelector).FirstOrDefault(v => !string.IsNullOrEmpty(v)) : null;

        // exact culture
        TTranslation? exact = translations.FirstOrDefault(t => string.Equals(t.Culture, culture, StringComparison.OrdinalIgnoreCase));
        string? val = exact is null ? null : fieldSelector(exact);
        if (val is not null || !fallback) return val;

        // neutral language (e.g. "en" from "en-US")
        string neutral = GetNeutralCulture(culture);
        if (!string.Equals(neutral, culture, StringComparison.OrdinalIgnoreCase))
        {
            TTranslation? neutralMatch = translations.FirstOrDefault(t => string.Equals(t.Culture, neutral, StringComparison.OrdinalIgnoreCase));
            val = neutralMatch is null ? null : fieldSelector(neutralMatch);
            if (val is not null) return val;
        }

        // fallback to default translation
        TTranslation? defaultTrans = translations.FirstOrDefault(t => t.IsDefault);
        if (defaultTrans != null) return fieldSelector(defaultTrans);

        // fallback to first available translation value
        return translations.Select(fieldSelector).FirstOrDefault(v => !string.IsNullOrEmpty(v));
    }

    /// <summary>
    /// Returns translation according to culture matching rules.
    /// </summary>
    public static TTranslation? GetTranslationForCulture<TTranslation>(this IEnumerable<TTranslation>? translations, string culture, bool fallback = false)
        where TTranslation : class, ITranslation
    {
        if (translations is null) return null;
        if (string.IsNullOrWhiteSpace(culture))
            return fallback ? translations.FirstOrDefault() : null;

        // exact
        TTranslation? exact = translations.FirstOrDefault(t => string.Equals(t.Culture, culture, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        if (!fallback) return null;

        // neutral
        string neutral = GetNeutralCulture(culture);
        if (!string.Equals(neutral, culture, StringComparison.OrdinalIgnoreCase))
        {
            TTranslation? neutralMatch = translations.FirstOrDefault(t => string.Equals(t.Culture, neutral, StringComparison.OrdinalIgnoreCase));
            if (neutralMatch != null) return neutralMatch;
        }

        // default marked translation
        TTranslation? defaultTrans = translations.FirstOrDefault(t => t.IsDefault);
        if (defaultTrans != null) return defaultTrans;

        // fallback to first
        return translations.FirstOrDefault();
    }

    /// <summary>
    /// Returns available cultures present in translations (ordered, distinct).
    /// </summary>
    public static IEnumerable<string> GetAvailableCultures<TTranslation>(this ITranslatable<TTranslation>? resource)
        where TTranslation : class, ITranslation
    {
        if (resource is null) return [];
        return resource.Translations?.Select(t => t.Culture).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase) ?? [];
    }

    #endregion

    #region Private helpers

    private static string GetNeutralCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) return string.Empty;
        string[] parts = culture.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : culture;
    }

    private static string? GetStringField<TTranslation>(TTranslation translation, string fieldName)
        where TTranslation : class, ITranslation
    {
        if (translation is null) return null;
        if (string.IsNullOrWhiteSpace(fieldName)) return null;

        // First try Fields dictionary (case-insensitive key)
        IDictionary<string, string?>? fields = translation.Fields;
        if (fields != null)
        {
            if (fields.TryGetValue(fieldName, out string? v) && !string.IsNullOrEmpty(v))
                return v;

            // case-insensitive lookup
            KeyValuePair<string, string?> matched = fields.FirstOrDefault(kv => string.Equals(kv.Key, fieldName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(matched.Value))
                return matched.Value;
        }

        // Fall back to reflection property on translation type
        PropertyInfo? pi = translation.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (pi is null) return null;
        object? val = pi.GetValue(translation);
        return val?.ToString();
    }

    #endregion
}
