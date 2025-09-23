namespace UseCases.Admin.Catalogs.OptionTypes.Commons;

public record OptionTypeResult : OptionTypeParam
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public record OptionTypeListItemResult : OptionTypeParam
{
    public Guid Id { get; init; }
    public int ProductsCount { get; init; }
    public int PrototypesCount { get; init; }
    public int OptionValuesCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public record OptionTypeSelectItemResult
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Presentation { get; init; } = default!;
    public int? Position { get; init; }
}