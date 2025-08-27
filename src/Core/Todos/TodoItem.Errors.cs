using ErrorOr;

using SharedKernel.Domain.Extensions;

namespace Core.Todos;
public sealed partial class TodoItem
{
    public static class Errors
    {
        // Title errors
        public static Error TitleTooShort => Error.Validation($"TodoItem.TitleTooShort", $"Title must be at least {Constraints.MinTitleLength} character long.");
        public static Error TitleTooLong => Error.Validation($"TodoItem.TitleTooLong", $"Title must be at most {Constraints.MaxTitleLength} characters long.");
        public static Error TitleInvalidFormat => Error.Validation($"TodoItem.TitleInvalidFormat", "Title contains invalid characters. Only alphanumeric characters, spaces, underscores, and hyphens are allowed.");

        // Note errors
        public static Error NoteTooLong => Error.Validation($"TodoItem.NoteTooLong", $"Note must be at most {Constraints.MaxNoteLength} characters long.");

        // Priority errors
        public static Error PriorityLevelInvalid => Error.Validation($"TodoItem.PriorityLevelInvalid", $"Priority level must be in [{EnumDescriptionExtensions.GetEnumContextDescription<PriorityLevel>()}].");

        // Reminder errors
        public static Error ReminderMustBeInFuture => Error.Validation($"TodoItem.ReminderMustBeInFuture", "Reminder must be set to a future date and time.");

        // Entity errors
        public static Error TodoItemNotFound => Error.NotFound($"TodoItem.TodoItemNotFound", "Todo item not found.");
        public static Error TodoItemAlreadyExists(string title = "") => Error.Conflict($"TodoItem.TodoItemAlreadyExists", $"A todo item with the title '{title}' already exists.");
        public static Error TodoItemAlreadyCompleted => Error.Conflict($"TodoItem.TodoItemAlreadyCompleted", "This todo item is already completed.");
    }
}
