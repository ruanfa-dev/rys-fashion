namespace UseCases.Todos.Items.Complete
{
    public static partial class CompleteTodoItem
    {
        public const string Name = nameof(CompleteTodoItem);
        public const string Route = "{id:guid}/complete";
        public const string Description = "Marks a todo item as completed by its unique ID.";
        public const string Summary = "Mark a todo item as complete";
    }
}
