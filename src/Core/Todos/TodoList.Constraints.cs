namespace Core.Todos;

public sealed partial class TodoList
{
    public static class Constraints
    {
        public const string EntityName = "TodoList";

        // Name: Alphanumeric, spaces, hyphens, underscores allowed for todo list titles
        public const int MaxTitleLength = 100;
        public const int MinTitleLength = 1;
        public const string TitleAllowedPattern = @"^[a-zA-Z0-9 _-]{1,100}$"; // Alphanumeric, spaces, underscores, hyphens

        // Colour: Hexadecimal format (e.g., #RRGGBB or #RGB)
        public const string ColourRegex = @"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$";
    }
}
