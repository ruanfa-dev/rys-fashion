using SharedKernel.Domain.Primitives;

namespace Core.Catalog.Products;
public class Product : AuditableEntity
{
    // Placeholder for the Product entity
    #region Relationships
    public ICollection<ProductProperty> ProductProperties { get; set; } = default!;
    #endregion

    // Domain flags used by taxon/product queries
    public bool IsActive { get; set; } = true;

    // Keep archive helper as a method for backward compatibility
    public bool IsArchived() => false;

    // Placeholder helper for storefront currency availability checks
    public bool IsAvailableInCurrency(string? currency)
    {
        // Real implementations will look up pricing/stock per currency.
        // For now return true to keep domain behavior predictable in tests.
        return true;
    }
}
