using static Core.Catalogs.Property;

namespace UseCases.Admin.Catalogs.Properties.Commons;

public record PropertyResult : PropertyParam
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
public record PropertyListItemResult : PropertyParam
{
    public Guid Id { get; init; }
    public int ProductsCount { get; init; }
    public int PrototypesCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public record PropertyOptionItemResult
{
    public Guid Id { get; init; }
    public required string Name { get; set; }
    public required string Presentation { get; set; }
    public PropertyKind Kind { get; set; }
    public DisplayOnKind DisplayOn { get; set; }
    public bool Filterable { get; set; }
    public int Position { get; set; }
}