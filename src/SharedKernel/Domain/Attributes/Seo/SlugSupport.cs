namespace SharedKernel.Domain.Attributes.Seo;
/// <summary>
/// Contract for types that carry a url-friendly slug.
/// </summary>
public interface ISlugSupport
{
    /// <summary>
    /// Optional url-friendly slug for the resource.
    /// </summary>
    string? UrlSlug { get; set; }
}