namespace SharedKernel.Models.Sort;
// <Summary>
// Represents sorting parameters for queries.
// </Summary>
public record SortParams(string? SortBy = null, string SortOrder = "asc")
{
    public bool IsValid => !string.IsNullOrWhiteSpace(SortBy);
    public bool IsDescending => string.Equals(SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
}