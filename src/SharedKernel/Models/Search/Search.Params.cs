namespace SharedKernel.Models.Search;
/// <summary>
/// Enhanced search parameters with integrated options.
/// </summary>
public record SearchParameter(
    string? SearchTerm = null,
    string[]? SearchFields = null,
    SearchOptions? Options = null)
{
    public static explicit operator SearchParameter(SearchParams p) =>
        new SearchParameter(
            p.SearchTerm,
            p.SearchFields,
            new SearchOptions
            {
                CaseSensitive = p.CaseSensitive ?? false,
                ExactMatch = p.ExactMatch ?? false,
                StartsWith = p.StartsWith
            });

    public static explicit operator SearchParams(SearchParameter p) =>
        new SearchParams(
            p.SearchTerm,
            p.SearchFields,
            p.Options?.CaseSensitive ?? false,
            p.Options?.ExactMatch ?? false,
            p.Options?.StartsWith ?? false);
}

/// <summary>
/// Search parameters with explicit fields, equivalent to SearchParameter.
/// </summary>
public record SearchParams(
    string? SearchTerm = null,
    string[]? SearchFields = null,
    bool? CaseSensitive = null,
    bool? ExactMatch = null,
    bool? StartsWith = null)
{
    public static explicit operator SearchParameter(SearchParams p) =>
        new SearchParameter(
            p.SearchTerm,
            p.SearchFields,
            new SearchOptions
            {
                CaseSensitive = p.CaseSensitive ?? true,
                ExactMatch = p.ExactMatch ?? true,
                StartsWith = p.StartsWith ?? true
            });

    public static explicit operator SearchParams(SearchParameter p) =>
        new SearchParams(
            p.SearchTerm,
            p.SearchFields,
            p.Options?.CaseSensitive ?? true,
            p.Options?.ExactMatch ?? true,
            p.Options?.StartsWith ?? true);
}
