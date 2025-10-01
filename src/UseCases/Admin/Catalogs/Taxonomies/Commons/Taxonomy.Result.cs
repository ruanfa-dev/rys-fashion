namespace UseCases.Admin.Catalogs.Taxonomies.Commons;
public static class TaxonomyResult
{
    public record ListItem 
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Position { get; init; }

        // Audit
        public DateTimeOffset? CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }

        // Statistic analysis

        public int? TaxonsCount { get; init; }
    
    }

    public record Details : ListItem
    {
        public string? CreatedBy { get; init; }
        public string? UpdatedBy { get; init; }
        public IDictionary<string, string?>? PublicMetadata { get; init; }
        public IDictionary<string, string?>? PrivateMetadata { get; init; }
    }
    public record ComboItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
