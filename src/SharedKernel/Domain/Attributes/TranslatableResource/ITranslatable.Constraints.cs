namespace SharedKernel.Domain.Attributes.TranslatableResource;

public static class TranslatableConstraints
{
    // Maximum number of entries in a translation Fields dictionary
    public const int MaxFields = 50;

    // Maximum number of translation rows per resource
    public const int MaxTranslations = 20;

    // Field key constraints (e.g. "Presentation", "Slug", "Name")
    public const int FieldKeyMinLength = 1;
    public const int FieldKeyMaxLength = 64;

    // Field value constraints
    public const int FieldValueMinLength = 0;
    public const int FieldValueMaxLength = 2048;

    // Culture string constraints (e.g., "en", "en-US")
    public const int CultureMaxLength = 16;

    // Allowed key pattern (alphanumeric, underscore, hyphen, dot)
    public const string FieldKeyAllowedPattern = "^[A-Za-z0-9_.-]+$";
}