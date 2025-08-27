namespace UseCases.Todos.Lists.Common;

public record TodoListParam
{
    public required string Title { get; init; }
    public required string Colour { get; init; }
}
