namespace UseCases.Admin.Catalogs.OptionValues.Commons;

public record OptionValueParam
{
    public required string Name { get; set; }
    public required string Presentation { get; set; }
    public int Position { get; set; }

    public Guid OptionTypeId { get; set; }
}
