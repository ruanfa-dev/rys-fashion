using UseCases.Todos.Lists.Common;

namespace UseCases.Todos.Items.Common;

public record TodoItemListResult : TodoItemParam
{
    public int Id { get; set; }

    // Complete events
    public bool Done { get; set; }
    public DateTimeOffset? DoneAt { get; set; }

    // Audit fields
    public DateTimeOffset? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed record TodoItemResult : TodoItemListResult
{
    public TodoListSelectItemResult? List { get; set; }
}
