using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Core.Catalog.Taxonomies;

/// <summary>
/// Simple translation entity for Taxonomy. Mimics Spree's translations table structure.
/// Stored as related entity with columns: TaxonomyId, Locale (mapped via EF configuration), Name
/// </summary>
public sealed class TaxonomyTranslation : ITranslation
{
    public Guid Id { get; set; }
    public Guid TaxonomyId { get; set; }

    // Culture (eg. "en", "en-US"). Column name mapping is handled in EF configuration.
    public string Culture { get; set; } = null!;

    public bool IsDefault { get; set; }

    // Optional fields dictionary for flexible storage; mapped to JSON in EF config
    public IDictionary<string, string?>? Fields { get; set; }

    public string? Name { get; set; }

    // Navigation property
    public Taxonomy? Taxonomy { get; set; }
}
