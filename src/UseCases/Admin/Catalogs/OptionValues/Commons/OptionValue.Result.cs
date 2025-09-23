namespace UseCases.Admin.Catalogs.OptionValues.Commons;

public record OptionValueResult : OptionValueParam
{
    public Guid Id { get; init; }
    public string? OptionTypeName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public record OptionValueSelectItemResult 
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Presentation { get; init; }
    public int? Position { get; init; }
}