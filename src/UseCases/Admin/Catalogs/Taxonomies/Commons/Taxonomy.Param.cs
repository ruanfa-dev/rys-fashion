using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.Positionable;

namespace UseCases.Admin.Catalogs.Taxonomies.Commons;
public record TaxonomyParam : IMetadataSupport, IPositionable
{
    public required string Name { get; set; }
    public int Position { get; set; }
    public Guid? StoreId { get; set; }
    public IDictionary<string, string?>? PublicMetadata { get; set; }
    public IDictionary<string, string?>? PrivateMetadata { get; set; }
}
