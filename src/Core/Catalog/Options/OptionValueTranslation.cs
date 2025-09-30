using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Core.Catalog.Options;

public sealed class OptionValueTranslation : BaseTranslation
{
    #region Properties
    public Guid Id { get; set; }
    public Guid OptionValueId { get; set; }
    #endregion

    #region Relationships
    public OptionValue? OptionValue { get; set; }
    #endregion
}
