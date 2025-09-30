using Core.Catalog.Variants;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Options;

public sealed class OptionValueVariant : AuditableEntity
{
    #region Properties
    public Guid OptionValueId { get; set; }
    public Guid VariantId { get; set; }
    #endregion

    #region Relationships
    public OptionValue OptionValue { get; set; } = null!;
    public Variant Variant { get; set; } = null!;
    #endregion

    #region Contructors
    public OptionValueVariant() { }
    public OptionValueVariant(Guid optionValueId, Guid variantId)
    {
        OptionValueId = optionValueId;
        VariantId = variantId;
    }
    #endregion


    #region Factory
    public static ErrorOr<OptionValueVariant> Create(Guid optionValueId, Guid variantId)
    {
        if (optionValueId == Guid.Empty) return Error.Validation("OptionValueVariant.OptionValueRequired", "OptionValue is required.");
        if (variantId == Guid.Empty) return Error.Validation("OptionValueVariant.VariantRequired", "Variant is required.");

        OptionValueVariant ovv = new OptionValueVariant(optionValueId, variantId);
        ovv.AddDomainEvent(new Events.Created(ovv.Id));
        return ovv;
    }

    #endregion

    #region Methods

    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }


    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid OptionValueVariantId) : DomainEvent;
        public record Deleted(Guid OptionValueVariantId) : DomainEvent;
    }

    #endregion
}
