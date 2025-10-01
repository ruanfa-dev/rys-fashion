using System.Text.RegularExpressions;

namespace SharedKernel.Domain.Attributes.Seo;

/// <summary>
/// Null-safe helpers for <see cref="ISlugSupport"/>.
/// </summary>
public static class ISlugSupportExtensions
{
    /// <summary>
    /// Sets the slug safely, trimming, lowercasing, and enforcing allowed pattern and length.
    /// </summary>
    public static void SetSlug(this ISlugSupport? target, string? slug)
    {
        if (target is null)
            return;
        if (string.IsNullOrWhiteSpace(slug))
        {
            target.UrlSlug = string.Empty;
            return;
        }
        var trimmed = slug.Trim().ToLowerInvariant();
        if (trimmed.Length < SlugSupportConstraints.SlugMinLength ||
            trimmed.Length > SlugSupportConstraints.SlugMaxLength ||
            !Regex.IsMatch(trimmed, SlugSupportConstraints.SlugAllowedPattern))
        {
            target.UrlSlug = string.Empty;
            return;
        }
        target.UrlSlug = trimmed;
    }
}