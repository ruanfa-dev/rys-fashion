using SharedKernel.Domain.Primitives;

namespace Core.Catalog.Products;
public class Product : AuditableEntity
{
    // Placeholder for the Product entity
    #region Relationships
    public ICollection<ProductProperty> ProductProperties { get; set; } = default!;
    #endregion
}
