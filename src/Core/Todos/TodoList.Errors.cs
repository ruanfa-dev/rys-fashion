using ErrorOr;

namespace Core.Todos;

public sealed partial class TodoList
{
    public static class Errors
    {
        // Title errors
        public static Error TitleRequired => Error.Validation($"TodoList.TitleRequired", "Title is required.");
        public static Error TitleTooShort => Error.Validation($"TodoList.TitleTooShort", $"Title must be at least {Constraints.MinTitleLength} character long.");
        public static Error TitleTooLong => Error.Validation($"TodoList.TitleTooLong", $"Title must be at most {Constraints.MaxTitleLength} characters long.");
        public static Error TitleInvalidFormat => Error.Validation($"TodoList.TitleInvalidFormat", "Title contains invalid characters. Only alphanumeric characters, spaces, underscores, and hyphens are allowed.");

        // Colour errors
        public static Error ColourRequired => Error.Validation($"TodoList.ColourRequired", "Colour is required.");
        public static Error ColourInvalidFormat => Error.Validation($"TodoList.ColourInvalidFormat", "Colour must be a valid hex color code (e.g., #RRGGBB or #RGB).");

        // Entity errors
        public static Error TodoListNotFound => Error.NotFound($"TodoList.TodoListNotFound", "Todo list not found.");
        public static Error TodoListAlreadyExists(string title = "") => Error.Conflict($"TodoList.TodoListAlreadyExists", $"A todo list with the title '{title}' already exists.");
    }
}
