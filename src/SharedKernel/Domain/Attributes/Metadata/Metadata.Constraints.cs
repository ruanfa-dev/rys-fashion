namespace SharedKernel.Domain.Attributes.Metadata;
public static class MetadataConstraints
{
    // Maximum number of metadata entries allowed per dictionary
    public const int MaxEntries = 50;

    // Key constraints
    public const int KeyMinLength = 1;
    public const int KeyMaxLength = 64;

    // Value constraints
    public const int ValueMinLength = 0;
    public const int ValueMaxLength = 2048;

    // Allowed key pattern (alphanumeric, underscore, hyphen, dot)
    // Use this pattern to prevent problematic characters in JSON keys and DB column indexing.
    public const string KeyAllowedPattern = "^[A-Za-z0-9_.-]+$";
}