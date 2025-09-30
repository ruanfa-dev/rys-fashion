using System.Linq.Expressions;

using SharedKernel.Domain.Attributes.TranslatableResource;
using SharedKernel.Extensions.Text;
using SharedKernel.Models.Filter;

namespace SharedKernel.Domain.Attributes.Parameterizable;

/// <summary>
/// Lightweight instance extensions that replace an injectable service.
/// Null-safe and avoids assigning null to non-nullable properties.
/// Also provides query helpers (search_by_name scope equivalents).
/// </summary>
public static class ParameterizableNameExtensions
{
    /// <summary>
    /// Trim whitespace from name and presentation and convert empty -> empty string.
    /// Coalesce to empty string to avoid null-assignment warnings.
    /// </summary>
    public static void AutoStrip(this IParameterizableName entity)
    {
        if (entity is null) return;

        entity.Name = TrimToNull(entity.Name) ?? string.Empty;
        entity.Presentation = TrimToNull(entity.Presentation) ?? string.Empty;
    }

    /// <summary>
    /// If Name is null/empty, set it from Presentation.
    /// </summary>
    public static void SetNameFromPresentationIfBlank(this IParameterizableName entity)
    {
        if (entity is null) return;

        if (string.IsNullOrWhiteSpace(entity.Name))
            entity.Name = entity.Presentation;
    }

    /// <summary>
    /// Normalize the name using the project's <see cref="Slugifier"/> (parameterize + trim).
    /// Uses fallbacks to avoid null references and preserves string-typed properties.
    /// </summary>
    public static void NormalizeName(this IParameterizableName entity)
    {
        if (entity is null) return;

        // Use Name, otherwise Presentation, otherwise empty string as source for slugification.
        string source = entity.Name ?? entity.Presentation ?? string.Empty;
        string normalized = Slugifier.Parameterize(source).Trim();

        // Keep the property non-nullable by using empty string when result is empty.
        entity.Name = string.IsNullOrEmpty(normalized) ? string.Empty : normalized;
    }

    /// <summary>
    /// Full flow equivalent to the Spree concern callbacks:
    /// AutoStrip -> SetNameFromPresentationIfBlank -> NormalizeName
    /// </summary>
    public static void ApplyBeforeValidation(this IParameterizableName entity)
    {
        if (entity is null) return;

        entity.AutoStrip();
        entity.SetNameFromPresentationIfBlank();
        entity.NormalizeName();
    }

    private static string? TrimToNull(string? value)
    {
        if (value is null) return null;
        string t = value.Trim();
        return t.Length == 0 ? null : t;
    }

    #region Query helpers (search_by_name)

    /// <summary>
    /// Search by Name or Presentation on entities implementing IParameterizableName.
    /// Uses SQL-compatible LIKE via EF.Functions.Like and LOWER for case-insensitive match.
    /// This implementation uses the shared QueryFilterBuilder to construct filters.
    /// </summary>
    public static IQueryable<T> SearchByName<T>(this IQueryable<T> source, string? query)
        where T : class, IParameterizableName
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (string.IsNullOrWhiteSpace(query)) return source;

        string q = query.Trim();

        QueryFilterBuilder builder = QueryFilterBuilder.Create()
            .Or("Name", FilterOperator.Contains, q)
            .Or("Presentation", FilterOperator.Contains, q);

        return builder.ApplyTo(source);
    }

    /// <summary>
    /// Translation-aware search: joins translations navigation and searches translated fields (Presentation/Name).
    /// Uses QueryFilterBuilder with nested property paths (Translations.Presentation, Translations.Name).
    /// Note: searching JSON Fields requires provider-specific support; prefer mapping common fields on translation type.
    /// </summary>
    public static IQueryable<T> SearchByName<T, TTranslation>(this IQueryable<T> source,
        Expression<Func<T, IEnumerable<TTranslation>>> translationsNav,
        string? query)
        where T : class, IParameterizableName
        where TTranslation : class, ITranslation
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (translationsNav == null) throw new ArgumentNullException(nameof(translationsNav));
        if (string.IsNullOrWhiteSpace(query)) return source;

        string q = query.Trim();

        QueryFilterBuilder builder = QueryFilterBuilder.Create()
            .Or("Name", FilterOperator.Contains, q)
            .Or("Presentation", FilterOperator.Contains, q)
            .Or("Translations.Presentation", FilterOperator.Contains, q)
            .Or("Translations.Name", FilterOperator.Contains, q);

        return builder.ApplyTo(source);
    }

    #endregion
}