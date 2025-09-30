using ErrorOr;

namespace SharedKernel.Domain.Attributes.TranslatableResource;

public static class TranslatableErrors
{
    private const string Prefix = "TRANSLATABLE";

    public static Error CultureRequired(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.CultureRequired", "Translation culture is required.");

    public static Error InvalidCultureLength(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.InvalidCultureLength", $"Culture value must be at most {TranslatableConstraints.CultureMaxLength} characters.");

    public static Error FieldsTooManyEntries(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.FieldsTooManyEntries", $"Translation fields dictionary must contain at most {TranslatableConstraints.MaxFields} entries.");

    public static Error FieldKeyInvalidLength(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.FieldKeyInvalidLength", $"Field key length must be between {TranslatableConstraints.FieldKeyMinLength} and {TranslatableConstraints.FieldKeyMaxLength} characters.");

    public static Error FieldKeyInvalidChars(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.FieldKeyInvalidChars", $"Field key contains invalid characters. Allowed pattern: {TranslatableConstraints.FieldKeyAllowedPattern}");

    public static Error FieldValueInvalidLength(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.FieldValueInvalidLength", $"Field value length must be between {TranslatableConstraints.FieldValueMinLength} and {TranslatableConstraints.FieldValueMaxLength} characters.");
}
