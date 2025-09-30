using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Core.Catalog.Options;

public sealed class OptionTypeTranslation : BaseTranslation
{
    #region Properties
    public Guid Id { get; set; }
    public Guid OptionTypeId { get; set; }
    #endregion

    #region Relationships
    public OptionType? OptionType { get; set; }
    #endregion
}
