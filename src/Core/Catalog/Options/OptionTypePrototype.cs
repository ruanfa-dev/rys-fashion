using Core.Catalog.Prototypes;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Options;

public sealed class OptionTypePrototype : AuditableEntity
{
    #region Properties
    public Guid OptionTypeId { get; set; }
    public Guid PrototypeId { get; set; }

    #endregion

    #region Relationships
    public OptionType? OptionType { get; set; }
    public Prototype? Prototype { get; set; }
    #endregion

    #region Contructors
    public OptionTypePrototype() { }

    public OptionTypePrototype(Guid optionTypeId, Guid prototypeId)
    {
        OptionTypeId = optionTypeId;
        PrototypeId = prototypeId;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        // Validation:
        // ID: required, non-empty
        public static Error OptionTypeRequired => Error.Validation("OptionTypePrototype.OptionTypeRequired", "Option type is required.");
        public static Error PrototypeRequired => Error.Validation("OptionTypePrototype.PrototypeRequired", "Prototype is required.");

        // Conflicts
        public static Error AlreadyExists => Error.Conflict("OptionTypePrototype.AlreadyExists", "The OptionType is already assigned to this Prototype.");
    }
    #endregion
    public static ErrorOr<OptionTypePrototype> Create(Guid optionTypeId, Guid prototypeId)
    {
        if (optionTypeId == Guid.Empty)
            return Errors.OptionTypeRequired;
        if (prototypeId == Guid.Empty)
            return Errors.PrototypeRequired;

        OptionTypePrototype otp = new OptionTypePrototype(optionTypeId, prototypeId);

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
