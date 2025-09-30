using Core.Catalog.Taxonomies;

namespace Core.Promotions;

public sealed class PromotionRuleTaxon
{
    public Guid Id { get; set; }
    public PromotionRule? PromotionRule { get; set; }

    public Guid TaxonId { get; set; }
    public Taxon? Taxon { get; set; }
}