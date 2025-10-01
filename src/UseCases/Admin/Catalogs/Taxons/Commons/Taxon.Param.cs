using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.Positionable;
using SharedKernel.Domain.Attributes.Seo;

namespace UseCases.Admin.Catalogs.Taxons.Commons;

public record TaxonParam : IMetadataSupport, IPositionable, ISeoSupport
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? Automatic { get; set; }
    public string? RulesMatchPolicy { get; set; }
    public string? SortOrder { get; set; }
    public bool? HideFromNav { get; set; }
    public string? ImageUrl { get; set; }
    public string? SquareImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public Guid TaxonomyId { get; set; }

    // SEO properties
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    // Positionable property
    public int Position { get; set; } = 0;

    // Metadata property
    public IDictionary<string, string?>? PublicMetadata { get; set; }
    public IDictionary<string, string?>? PrivateMetadata { get; set; }
}
