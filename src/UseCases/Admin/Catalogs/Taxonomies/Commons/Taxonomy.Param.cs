using SharedKernel.Domain.Attributes.Metadata;

namespace UseCases.Admin.Catalogs.Taxonomies.Commons;
public record TaxonomyParam : MetadataParam
{
    public required string Name { get; set; }
    public int Position { get; set; }
    public Guid? StoreId { get; set; }
}
