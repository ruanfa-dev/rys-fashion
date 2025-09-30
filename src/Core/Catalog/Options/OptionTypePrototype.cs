using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;
using Core.Catalog.Prototypes;

namespace Core.Catalog.Options;

public sealed class OptionTypePrototype : AuditableEntity
{
    public Guid OptionTypeId { get; set; }
    public OptionType? OptionType { get; set; }

    public Guid PrototypeId { get; set; }
    public Prototype? Prototype { get; set; }

    private OptionTypePrototype() { }

    public static ErrorOr<OptionTypePrototype> Create(Guid optionTypeId, Guid prototypeId)
    {
        if (optionTypeId == Guid.Empty) return Error.Validation("OptionTypePrototype.OptionTypeRequired", "OptionType is required.");
        if (prototypeId == Guid.Empty) return Error.Validation("OptionTypePrototype.PrototypeRequired", "Prototype is required.");

        OptionTypePrototype otp = new OptionTypePrototype
        {
            OptionTypeId = optionTypeId,
            PrototypeId = prototypeId
        };

        otp.AddDomainEvent(new Events.Created(otp.Id));
        return otp;
    }

    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }

    public static class Events
    {
        public record Created(Guid OptionTypePrototypeId) : DomainEvent;
        public record Deleted(Guid OptionTypePrototypeId) : DomainEvent;
    }
}
