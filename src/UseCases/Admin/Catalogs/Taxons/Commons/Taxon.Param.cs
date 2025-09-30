using System.ComponentModel.DataAnnotations;

using SharedKernel.Domain.Attributes.Metadata;

namespace UseCases.Admin.Catalogs.Taxons.Commons;

public record TaxonParam : MetadataParam
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
    public string? Name { get; init; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; init; }

    public bool? Automatic { get; init; }

    [RegularExpression("^(all|any)$", ErrorMessage = "RulesMatchPolicy must be 'all' or 'any'")]
    public string? RulesMatchPolicy { get; init; }

    [RegularExpression("^(manual|best-selling|name-a-z|name-z-a|price-high-to-low|price-low-to-high|newest-first|oldest-first)$",
        ErrorMessage = "SortOrder must be one of: manual, best-selling, name-a-z, name-z-a, price-high-to-low, price-low-to-high, newest-first, oldest-first")]
    public string? SortOrder { get; init; }

    public bool? HideFromNav { get; init; }

    [Url(ErrorMessage = "ImageUrl must be a valid URL")]
    public string? ImageUrl { get; init; }

    [Url(ErrorMessage = "SquareImageUrl must be a valid URL")]
    public string? SquareImageUrl { get; init; }

    public Guid? ParentId { get; init; }

    [Required(ErrorMessage = "TaxonomyId is required")]
    public Guid TaxonomyId { get; init; }

    [StringLength(255, ErrorMessage = "MetaTitle cannot exceed 255 characters")]
    public string? MetaTitle { get; init; }

    [StringLength(255, ErrorMessage = "MetaDescription cannot exceed 255 characters")]
    public string? MetaDescription { get; init; }

    [StringLength(255, ErrorMessage = "MetaKeywords cannot exceed 255 characters")]
    public string? MetaKeywords { get; init; }
}
