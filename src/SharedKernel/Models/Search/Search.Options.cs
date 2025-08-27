namespace SharedKernel.Models.Search;
/// <summary>
/// Configuration options for search behavior.
/// </summary>
public record SearchOptions
{
    public SearchOptions()
    {
    }

    public bool? CaseSensitive { get; init; }
    public bool? ExactMatch { get; init; }
    public bool? StartsWith { get; init; }
}