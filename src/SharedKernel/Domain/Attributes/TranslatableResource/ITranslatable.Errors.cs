using ErrorOr;

using SharedKernel.Domain.Extensions;

namespace SharedKernel.Domain.Attributes.TranslatableResource;

public static class TranslatableErrors
{
    private const string Prefix = "TRANSLATABLE";

    public static Error CultureRequired(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.CultureRequired", GetCultureRequiredMessage(prefix));

    public static Error InvalidCultureLength(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.InvalidCultureLength", GetInvalidCultureLengthMessage(prefix));

    public static Error FieldsTooManyEntries(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.FieldsTooManyEntries", GetFieldsTooManyEntriesMessage(prefix));

    public static Error FieldKeyInvalidLength(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.FieldKeyInvalidLength", GetFieldKeyInvalidLengthMessage(prefix));

    public static Error FieldKeyInvalidChars(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.FieldKeyInvalidChars", GetFieldKeyInvalidCharsMessage(prefix));

    public static Error FieldValueInvalidLength(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.FieldValueInvalidLength", GetFieldValueInvalidLengthMessage(prefix));

    private static string GetCodePrefix(string? prefix) =>
        !string.IsNullOrWhiteSpace(prefix) ? prefix! : Prefix;

    private static string GetCultureRequiredMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "Translation culture is required.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} translation culture is required.";
    }

    private static string GetInvalidCultureLengthMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Translation culture must be at most {TranslatableConstraints.CultureMaxLength} characters.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} translation culture must be at most {TranslatableConstraints.CultureMaxLength} characters.";
    }

    private static string GetFieldsTooManyEntriesMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Translation fields dictionary must contain at most {TranslatableConstraints.MaxFields} entries.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} translation fields dictionary must contain at most {TranslatableConstraints.MaxFields} entries.";
    }

    private static string GetFieldKeyInvalidLengthMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Field key length must be between {TranslatableConstraints.FieldKeyMinLength} and {TranslatableConstraints.FieldKeyMaxLength} characters.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} translation field key length must be between {TranslatableConstraints.FieldKeyMinLength} and {TranslatableConstraints.FieldKeyMaxLength} characters.";
    }

    private static string GetFieldKeyInvalidCharsMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Field key contains invalid characters. Allowed pattern: {TranslatableConstraints.FieldKeyAllowedPattern}";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} translation field key contains invalid characters. Allowed pattern: {TranslatableConstraints.FieldKeyAllowedPattern}";
    }

    private static string GetFieldValueInvalidLengthMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Field value length must be between {TranslatableConstraints.FieldValueMinLength} and {TranslatableConstraints.FieldValueMaxLength} characters.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} translation field value length must be between {TranslatableConstraints.FieldValueMinLength} and {TranslatableConstraints.FieldValueMaxLength} characters.";
    }
}