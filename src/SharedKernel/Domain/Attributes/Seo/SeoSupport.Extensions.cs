namespace SharedKernel.Domain.Attributes.Seo;

/// <summary>
/// Null-safe helpers for <see cref="ISeoSupport"/>.
/// </summary>
public static class ISeoSupportExtensions
{
    /// <summary>
    /// Sets the meta title safely, trimming and enforcing max length.
    /// </summary>
    public static void SetMetaTitle(this ISeoSupport? target, string? title)
    {
        if (target is null) return;
        if (string.IsNullOrWhiteSpace(title))
        {
            target.MetaTitle = null;
            return;
        }
        target.MetaTitle = title.Trim().Length > SeoSupportConstraints.MetaTitleMaxLength
            ? title.Trim()[..SeoSupportConstraints.MetaTitleMaxLength]
            : title.Trim();
    }

    /// <summary>
    /// Sets the meta description safely, trimming and enforcing max length.
    /// </summary>
    public static void SetMetaDescription(this ISeoSupport? target, string? description)
    {
        if (target is null) return;
        if (string.IsNullOrWhiteSpace(description))
        {
            target.MetaDescription = null;
            return;
        }
        target.MetaDescription = description.Trim().Length > SeoSupportConstraints.MetaDescriptionMaxLength
            ? description.Trim()[..SeoSupportConstraints.MetaDescriptionMaxLength]
            : description.Trim();
    }

    /// <summary>
    /// Sets the meta keywords safely, trimming and enforcing max length.
    /// </summary>
    public static void SetMetaKeywords(this ISeoSupport? target, string? keywords)
    {
        if (target is null) return;
        if (string.IsNullOrWhiteSpace(keywords))
        {
            target.MetaKeywords = null;
            return;
        }
        target.MetaKeywords = keywords.Trim().Length > SeoSupportConstraints.MetaFieldMaxLength
            ? keywords.Trim()[..SeoSupportConstraints.MetaFieldMaxLength]
            : keywords.Trim();
    }

}