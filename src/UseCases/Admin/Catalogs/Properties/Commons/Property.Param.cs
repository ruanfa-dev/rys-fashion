using Core.Catalog.Properties;

using SharedKernel.Domain.Attributes.Metadata;

namespace UseCases.Admin.Catalogs.Properties.Commons;
public record PropertyParam : MetadataParam
{
    public required string Name { get; set; }
    public required string Presentation { get; set; }
    public PropertyKind Kind { get; set; }
    public DisplayOn DisplayOn { get; set; }
    public bool Filterable { get; set; }
    public int Position { get; set; }
}
