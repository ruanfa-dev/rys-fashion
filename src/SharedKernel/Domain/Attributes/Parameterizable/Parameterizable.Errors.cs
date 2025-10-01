using ErrorOr;
using SharedKernel.Domain.Extensions;

namespace SharedKernel.Domain.Attributes.Parameterizable;

public static class ParameterizableErrors
{
    private const string Prefix = "PARAMETERIZABLE";

    public static Error NameRequired(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.NameRequired", GetNameRequiredMessage(prefix));

    public static Error InvalidNameLength(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.InvalidNameLength", GetInvalidNameLengthMessage(prefix));

    public static Error NameInvalidChars(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.NameInvalidChars", GetNameInvalidCharsMessage(prefix));

    public static Error PresentationRequired(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.PresentationRequired", GetPresentationRequiredMessage(prefix));

    public static Error InvalidPresentationLength(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.InvalidPresentationLength", GetInvalidPresentationLengthMessage(prefix));

    private static string GetCodePrefix(string? prefix) =>
        !string.IsNullOrWhiteSpace(prefix) ? prefix! : Prefix;

    private static string GetNameRequiredMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "Name is required and cannot be empty.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} name is required and cannot be empty.";
    }

    private static string GetInvalidNameLengthMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Name length must be between {ParameterizableConstraints.NameMinLength} and {ParameterizableConstraints.NameMaxLength} characters.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} name length must be between {ParameterizableConstraints.NameMinLength} and {ParameterizableConstraints.NameMaxLength} characters.";
    }

    private static string GetNameInvalidCharsMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Name contains invalid characters. Allowed pattern: {ParameterizableConstraints.NameAllowedPattern}";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} name contains invalid characters. Allowed pattern: {ParameterizableConstraints.NameAllowedPattern}";
    }

    private static string GetPresentationRequiredMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "Presentation is required and cannot be empty.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} presentation is required and cannot be empty.";
    }

    private static string GetInvalidPresentationLengthMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Presentation length must be between {ParameterizableConstraints.PresentationMinLength} and {ParameterizableConstraints.PresentationMaxLength} characters.";

        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} presentation length must be between {ParameterizableConstraints.PresentationMinLength} and {ParameterizableConstraints.PresentationMaxLength} characters.";
    }
}
