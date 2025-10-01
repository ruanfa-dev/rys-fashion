using ErrorOr;

using SharedKernel.Domain.Extensions;

namespace SharedKernel.Domain.Attributes.Seo;

public static class SlugSupportErrors
{
    private const string Prefix = "SLUG";

    public static Error SlugInvalidChars(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.SlugInvalidChars", GetSlugInvalidCharsMessage(prefix));

    public static Error SlugInvalidLength(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.SlugInvalidLength", GetSlugInvalidLengthMessage(prefix));

    private static string GetCodePrefix(string? prefix) =>
        !string.IsNullOrWhiteSpace(prefix) ? prefix! : Prefix;

    private static string GetSlugInvalidCharsMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Slug contains invalid characters. Allowed pattern: {SlugSupportConstraints.SlugAllowedPattern}";
        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} slug contains invalid characters. Allowed pattern: {SlugSupportConstraints.SlugAllowedPattern}";
    }

    private static string GetSlugInvalidLengthMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Slug length must be between {SlugSupportConstraints.SlugMinLength} and {SlugSupportConstraints.SlugMaxLength} characters.";
        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} slug length must be between {SlugSupportConstraints.SlugMinLength} and {SlugSupportConstraints.SlugMaxLength} characters.";
    }
}