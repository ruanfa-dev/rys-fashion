using Core.Todos;

namespace UseCases.Todos.Items.Common;

public record TodoItemParam
{
    public int ListId { get; set; }
    public string? Title { get; set; }
    public string? Note { get; set; }
    public PriorityLevel? Priority { get; set; }
    public DateTimeOffset? Reminder { get; set; }
}
