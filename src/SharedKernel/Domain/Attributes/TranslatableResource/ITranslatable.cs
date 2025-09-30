using System.Reflection;

namespace SharedKernel.Domain.Attributes.TranslatableResource;

/// <summary>
/// Generic contract for resources that expose typed translations.
/// Provides a default implementation of GetField to avoid per-entity boilerplate.
/// </summary>
/// <typeparam name="TTranslation">Translation type implementing <see cref="ITranslation"/>.</typeparam>
public interface ITranslatable<TTranslation>
    where TTranslation : class, ITranslation
{
    ICollection<TTranslation> Translations { get; }

    IReadOnlyCollection<string> TranslatableFields { get; }

    /// <summary>
    /// Get translated value for a named field using translation lookup rules.
    /// Default implementation uses reflection on the translation type to read the named field.
    /// </summary>
    public string? GetField(string fieldName, string culture, bool fallback = false)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return null;

        // Delegate to typed collection helper. The selector uses reflection on TTranslation.
        return TranslatableExtensions.GetFieldWithCulture(Translations, culture, (TTranslation t) =>
        {
            PropertyInfo? pi = typeof(TTranslation).GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            return pi?.GetValue(t)?.ToString();
        }, fallback);
    }
}