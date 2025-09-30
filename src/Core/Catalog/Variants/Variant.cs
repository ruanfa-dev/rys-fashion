using SharedKernel.Domain.Primitives;
using Core.Catalog.Options;
using Core.Catalog.Products;

namespace Core.Catalog.Variants;

public class Variant : AuditableEntity
{
    // Minimal placeholder for Variant referenced by option value variants
    public ICollection<OptionValueVariant>? OptionValueVariants { get; set; }
    public ICollection<Product>? Products { get; set; }
}
