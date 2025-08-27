using SharedKernel.Domain.Primitives;

namespace Core.Todos;

public sealed partial class TodoList : AuditableEntity<int>
{
    #region Properties
    public string Title { get; set; } = string.Empty;
    public Colour? Colour { get; set; }
    #endregion

    #region Relationship
    public ICollection<TodoItem> Items { get; set; } = new List<TodoItem>();
    #endregion

    #region Computed properties
    public bool IsCompleted => Items.All(m => m.Done);
    #endregion

    #region 
    private TodoList()
    {
    }
    private TodoList(string title, Colour? colour)
    {
        Title = title;
        Colour = colour;
        Items = [];
    }
    #endregion

    #region Factory Method
    public static TodoList Create(string title, Colour? colour)
    {
        return new TodoList(title.Trim(), colour);
    }

    #endregion
}
