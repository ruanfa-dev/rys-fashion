using ErrorOr;

namespace SharedKernel.Domain.Attributes.Metadata;

public static class MetadataErrors
{
    public static Error PublicMetadataTooManyEntries =>
        Error.Validation("METADATA_PUBLIC_TOO_MANY_ENTRIES", $"Public metadata must contain at most {MetadataConstraints.MaxEntries} entries.");

    public static Error PrivateMetadataTooManyEntries =>
        Error.Validation("METADATA_PRIVATE_TOO_MANY_ENTRIES", $"Private metadata must contain at most {MetadataConstraints.MaxEntries} entries.");

    public static Error MetadataKeyRequired =>
        Error.Validation("METADATA_KEY_REQUIRED", "Metadata key is required and cannot be empty.");

    public static Error MetadataKeyInvalidChars =>
        Error.Validation("METADATA_KEY_INVALID_CHARS", $"Metadata key contains invalid characters. Allowed pattern: {MetadataConstraints.KeyAllowedPattern}");

    public static Error MetadataKeyInvalidLength =
        Error.Validation("METADATA_KEY_INVALID_LENGTH", $"Metadata key length must be between {MetadataConstraints.KeyMinLength} and {MetadataConstraints.KeyMaxLength} characters.");

    public static Error MetadataValueInvalidLength =>
        Error.Validation("METADATA_VALUE_INVALID_LENGTH", $"Metadata value length must be between {MetadataConstraints.ValueMinLength} and {MetadataConstraints.ValueMaxLength} characters.");
}