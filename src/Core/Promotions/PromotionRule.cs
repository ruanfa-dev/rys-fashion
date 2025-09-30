using Core.Catalog.Products;

namespace Core.Promotions;

public sealed class PromotionRule
{
    public Guid Id { get; set; }

    // Accepts a queryable of products and returns a filtered IQueryable. Simple pass-through default.
    public IQueryable<Product> Apply(IQueryable<Product> products)
    {
        return products;
    }
}