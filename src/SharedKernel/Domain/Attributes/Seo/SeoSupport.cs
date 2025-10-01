namespace SharedKernel.Domain.Attributes.Seo;

/// <summary>
/// Contract for types that carry SEO metadata.
/// </summary>
public interface ISeoSupport
{
    /// <summary>
    /// Optional meta/title for SEO listing (usually shown in <title>).
    /// </summary>
    string? MetaTitle { get; set; }

    /// <summary>
    /// Optional meta/description for SEO listing.
    /// </summary>
    string? MetaDescription { get; set; }

    /// <summary>
    /// Optional meta/keywords (comma separated).
    /// </summary>
    string? MetaKeywords { get; set; }

}