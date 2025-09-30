using SharedKernel.Domain.Primitives;

namespace Core.Catalog.Products;

public sealed class ProductOptionType : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid OptionTypeId { get; set; }
    public Options.OptionType? OptionType { get; set; }
}
