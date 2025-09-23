namespace UseCases.Admin.Catalogs.OptionTypes.Commons;

public record OptionTypeParam
{
    public required string Name { get; set; }
    public required string Presentation { get; set; }
    public bool Filterable { get; set; }
    public int Position { get; set; }
}
