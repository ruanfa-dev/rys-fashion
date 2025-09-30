using ErrorOr;

namespace SharedKernel.Domain.Attributes.Parameterizable;

public static class ParameterizableErrors
{
    private const string Prefix = "PARAMETERIZABLE";

    public static Error NameRequired(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.NameRequired", "Name is required and cannot be empty.");

    public static Error InvalidNameLength(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.InvalidNameLength", $"Name length must be between {ParameterizableConstraints.NameMinLength} and {ParameterizableConstraints.NameMaxLength} characters.");

    public static Error NameInvalidChars(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.NameInvalidChars", $"Name contains invalid characters. Allowed pattern: {ParameterizableConstraints.NameAllowedPattern}");

    public static Error PresentationRequired(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.PresentationRequired", "Presentation is required and cannot be empty.");

    public static Error InvalidPresentationLength(string? prefix = null) =>
        Error.Validation($"{(prefix ?? Prefix)}.InvalidPresentationLength", $"Presentation length must be between {ParameterizableConstraints.PresentationMinLength} and {ParameterizableConstraints.PresentationMaxLength} characters.");
}
