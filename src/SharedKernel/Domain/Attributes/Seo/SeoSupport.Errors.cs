using ErrorOr;

using SharedKernel.Domain.Extensions;

namespace SharedKernel.Domain.Attributes.Seo;

public static class SeoErrors
{
    private const string Prefix = "SEO";

    public static Error MetaTitleTooLong(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.MetaTitleTooLong", GetMetaTitleTooLongMessage(prefix));

    public static Error MetaDescriptionTooLong(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.MetaDescriptionTooLong", GetMetaDescriptionTooLongMessage(prefix));

    public static Error MetaKeywordsTooLong(string? prefix = null) =>
        Error.Validation($"{GetCodePrefix(prefix)}.MetaKeywordsTooLong", GetMetaKeywordsTooLongMessage(prefix));

    private static string GetCodePrefix(string? prefix) =>
        !string.IsNullOrWhiteSpace(prefix) ? prefix! : Prefix;

    private static string GetMetaTitleTooLongMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Meta title must be at most {SeoSupportConstraints.MetaTitleMaxLength} characters.";
        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} meta title must be at most {SeoSupportConstraints.MetaTitleMaxLength} characters.";
    }

    private static string GetMetaDescriptionTooLongMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Meta description must be at most {SeoSupportConstraints.MetaDescriptionMaxLength} characters.";
        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} meta description must be at most {SeoSupportConstraints.MetaDescriptionMaxLength} characters.";
    }

    private static string GetMetaKeywordsTooLongMessage(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return $"Meta keywords must be at most {SeoSupportConstraints.MetaFieldMaxLength} characters.";
        string label = StringHumanize.ToEntityLabel(prefix!);
        return $"{label} meta keywords must be at most {SeoSupportConstraints.MetaFieldMaxLength} characters.";
    }
}