namespace Core.Todos;
public sealed partial class TodoItem
{
    public static class Constraints
    {
        // Title: Alphanumeric, spaces, hyphens, underscores allowed for todo item titles
        public const int MinTitleLength = 1;
        public const int MaxTitleLength = 100;
        public const string TitleAllowedPattern = @"^[a-zA-Z0-9 _-]{1,100}$"; // Alphanumeric, spaces, underscores, hyphens

        // Note: Allow most characters but restrict length
        public const int MaxNoteLength = 500;
    }
}
