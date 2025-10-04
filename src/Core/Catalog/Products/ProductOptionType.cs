using Core.Catalog.Options;

using SharedKernel.Domain.Primitives;

namespace Core.Catalog.Products;

public sealed class ProductOptionType : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid OptionTypeId { get; set; }
    public OptionType OptionType { get; set; } = null!;
}
