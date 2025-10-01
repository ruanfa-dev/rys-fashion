using ErrorOr;

using SharedKernel.Domain.Extensions;

namespace SharedKernel.Domain.Attributes.Metadata;

public static class MetadataErrors
{
    private const string Prefix = "METADATA";

    public static Error PublicMetadataTooManyEntries(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.PublicMetadataTooManyEntries", GetPublicMetadataTooManyEntriesMessage(prefix));

    public static Error PrivateMetadataTooManyEntries(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.PrivateMetadataTooManyEntries", GetPrivateMetadataTooManyEntriesMessage(prefix));

    public static Error MetadataKeyRequired(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.MetadataKeyRequired", GetMetadataKeyRequiredMessage(prefix));

    public static Error MetadataKeyInvalidChars(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.MetadataKeyInvalidChars", GetMetadataKeyInvalidCharsMessage(prefix));

    public static Error MetadataKeyInvalidLength(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.MetadataKeyInvalidLength", GetMetadataKeyInvalidLengthMessage(prefix));

    public static Error MetadataValueInvalidLength(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.MetadataValueInvalidLength", GetMetadataValueInvalidLengthMessage(prefix));

    private static string GetCodePrefix(string? prefix) =>
        !string.IsNullOrWhiteSpace(prefix) ? prefix! : Prefix;

    private static string GetPublicMetadataTooManyEntriesMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Public metadata must contain at most {MetadataConstraints.MaxEntries} entries.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} public metadata must contain at most {MetadataConstraints.MaxEntries} entries.";
    }

    private static string GetPrivateMetadataTooManyEntriesMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Private metadata must contain at most {MetadataConstraints.MaxEntries} entries.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} private metadata must contain at most {MetadataConstraints.MaxEntries} entries.";
    }

    private static string GetMetadataKeyRequiredMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "Metadata key is required and cannot be empty.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} metadata key is required and cannot be empty.";
    }

    private static string GetMetadataKeyInvalidCharsMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Metadata key contains invalid characters. Allowed pattern: {MetadataConstraints.KeyAllowedPattern}";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} metadata key contains invalid characters. Allowed pattern: {MetadataConstraints.KeyAllowedPattern}";
    }

    private static string GetMetadataKeyInvalidLengthMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Metadata key length must be between {MetadataConstraints.KeyMinLength} and {MetadataConstraints.KeyMaxLength} characters.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} metadata key length must be between {MetadataConstraints.KeyMinLength} and {MetadataConstraints.KeyMaxLength} characters.";
    }

    private static string GetMetadataValueInvalidLengthMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Metadata value length must be between {MetadataConstraints.ValueMinLength} and {MetadataConstraints.ValueMaxLength} characters.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} metadata value length must be between {MetadataConstraints.ValueMinLength} and {MetadataConstraints.ValueMaxLength} characters.";
    }
}