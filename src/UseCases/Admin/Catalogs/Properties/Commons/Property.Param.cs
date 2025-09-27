using Core.Catalog.Properties;

namespace UseCases.Admin.Catalogs.Properties.Commons;
public record PropertyParam
{
    public required string Name { get; set; }
    public required string Presentation { get; set; }
    public PropertyKind Kind { get; set; }
    public DisplayOn DisplayOn { get; set; }
    public bool Filterable { get; set; }
    public int Position { get; set; }
}
