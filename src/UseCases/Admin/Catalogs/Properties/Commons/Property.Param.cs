using Core.Catalog.Properties;

using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.Parameterizable;
using SharedKernel.Domain.Attributes.Positionable;

namespace UseCases.Admin.Catalogs.Properties.Commons;
public record PropertyParam : IMetadataSupport, IParameterizableName, IPositionable
{
    public required string Name { get; set; }
    public required string Presentation { get; set; }
    public PropertyKind Kind { get; set; }
    public DisplayOn DisplayOn { get; set; }
    public bool Filterable { get; set; }
    public int Position { get; set; }
    public IDictionary<string, string?>? PublicMetadata { get; set; }
    public IDictionary<string, string?>? PrivateMetadata { get; set; }
}
