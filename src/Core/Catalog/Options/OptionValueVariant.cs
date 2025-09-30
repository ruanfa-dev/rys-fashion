using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;
using Core.Catalog.Variants;

namespace Core.Catalog.Options;

public sealed class OptionValueVariant : AuditableEntity
{
    public Guid OptionValueId { get; set; }
    public OptionValue OptionValue { get; set; } = null!;

    public Guid VariantId { get; set; }
    public Variant Variant { get; set; } = null!;

    private OptionValueVariant() { }

    public OptionValueVariant(Guid optionValueId, Guid variantId)
    {
        OptionValueId = optionValueId;
        VariantId = variantId;
    }

    public static ErrorOr<OptionValueVariant> Create(Guid optionValueId, Guid variantId)
    {
        if (optionValueId == Guid.Empty) return Error.Validation("OptionValueVariant.OptionValueRequired", "OptionValue is required.");
        if (variantId == Guid.Empty) return Error.Validation("OptionValueVariant.VariantRequired", "Variant is required.");

        OptionValueVariant ovv = new OptionValueVariant(optionValueId, variantId);
        ovv.AddDomainEvent(new Events.Created(ovv.Id));
        return ovv;
    }

    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }

    public static class Events
    {
        public record Created(Guid OptionValueVariantId) : DomainEvent;
        public record Deleted(Guid OptionValueVariantId) : DomainEvent;
    }
}
