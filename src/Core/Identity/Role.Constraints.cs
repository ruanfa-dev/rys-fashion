namespace Core.Identity;

public partial class Role
{
    public static class Constraints
    {
        // Name: Alphanumeric, spaces, hyphens, underscores allowed for role names
        public const int MinNameLength = 3;
        public const int MaxNameLength = 256;
        public const string NameAllowedPattern = @"^[a-zA-Z0-9 _-]{3,256}$"; // Alphanumeric, spaces, underscores, hyphens

        // Display Name: Allow most characters but restrict length
        public const int MaxDisplayNameLength = 256;
        public const string DisplayNameAllowedPattern = @"^[\w\s\p{P}\p{S}]{0,256}$"; // Allow letters, digits, whitespace, punctuation, symbols

        // Description: Allow most characters but restrict length
        public const int MaxDescriptionLength = 1000;

        // Priority: 0 to 100, lower is higher priority
        public const int MinPriority = 0;
        public const int MaxPriority = 1;
    }
}
