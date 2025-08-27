using ErrorOr;

using SharedKernel.Domain.Primitives;

namespace Core.Todos;
public sealed partial class TodoItem : AuditableEntity<int>
{
    #region Properties
    public string? Title { get; set; }
    public string? Note { get; set; }
    public PriorityLevel Priority { get; set; } = PriorityLevel.None;
    public DateTimeOffset? Reminder { get; set; }
    public bool Done { get; set; }
    public DateTimeOffset? DoneAt { get; set; }
    #endregion

    #region Relationship
    public int ListId { get; set; }
    public TodoList List { get; set; } = null!;
    #endregion

    #region Contructor
    private TodoItem()
    {
    }

    private TodoItem(
       string? title,
       string? note,
       PriorityLevel priority,
       DateTimeOffset? reminder,
       int listId) : this()
    {
        Title = title;
        Note = note;
        Priority = priority;
        ListId = listId;
        Reminder = reminder;
    }
    #endregion

    #region Factory Methods
    public static TodoItem Create(
        string? title,
        string? note,
        PriorityLevel priority,
        DateTimeOffset? reminder,
        int listId)
    {
        var item = new TodoItem(title, note, priority, reminder, listId);
        return item;
    }

    #endregion

    #region Business Methods
    public ErrorOr<TodoItem> MarkAsDone()
    {
        if (Done)
        {
            return Errors.TodoItemAlreadyCompleted;
        }

        Done = true;
        DoneAt = DateTimeOffset.UtcNow;
        return this;
    }

    #endregion
}
