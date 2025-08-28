namespace Core.Identity;

public partial class Role
{
    public static class Constraints
    {
        // Name: Alphanumeric, spaces, hyphens, underscores allowed for role names
        public const int MinNameLength = 3;
        public const int MaxNameLength = 256;
        public const string NameAllowedPattern = @"^[a-zA-Z0-9 _-]{3,256}$"; // Alphanumeric, spaces, underscores, hyphens

        // Description: Allow most characters but restrict length
        public const int MaxDescriptionLength = 1000;
    }
}
