using static Core.Catalogs.Property;

namespace UseCases.Admin.Catalogs.Properties.Commons;

public record PropertyParam
{
    public required string Name { get; set; }
    public required string Presentation { get; set; }
    public PropertyKind Kind { get; set; }
    public DisplayOnKind DisplayOn { get; set; }
    public bool Filterable { get; set; }
    public int Position { get; set; }
}
