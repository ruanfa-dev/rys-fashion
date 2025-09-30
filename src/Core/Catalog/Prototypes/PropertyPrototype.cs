using Core.Catalog.Properties;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Prototypes;

/// <summary>
/// Join entity between Prototype and Property (ports Spree::PropertyPrototype)
/// </summary>
public sealed class PropertyPrototype : AuditableEntity
{
    #region Properties

    public Guid PrototypeId { get; set; }
    public Prototype? Prototype { get; set; }

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error PrototypeRequired => Error.Validation("PropertyPrototype.PrototypeRequired", "Prototype is required.");
        public static Error PropertyRequired => Error.Validation("PropertyPrototype.PropertyRequired", "Property is required.");
        public static Error DuplicateAssociation => Error.Conflict("PropertyPrototype.Duplicate", "A prototype-property association already exists for the given prototype and property.");
    }

    #endregion

    #region Constructors

    private PropertyPrototype() { }

    #endregion

    #region Factory

    public static ErrorOr<PropertyPrototype> Create(Guid prototypeId, Guid propertyId)
    {
        if (prototypeId == Guid.Empty) return Errors.PrototypeRequired;
        if (propertyId == Guid.Empty) return Errors.PropertyRequired;

        var pp = new PropertyPrototype
        {
            PrototypeId = prototypeId,
            PropertyId = propertyId
        };

        pp.AddDomainEvent(new Events.Created(pp.Id));
        return pp;
    }

    #endregion

    #region Behavior

    public ErrorOr<PropertyPrototype> Update(Guid? prototypeId = null, Guid? propertyId = null)
    {
        var changed = false;

        if (prototypeId.HasValue && prototypeId.Value != PrototypeId)
        {
            if (prototypeId.Value == Guid.Empty) return Errors.PrototypeRequired;
            PrototypeId = prototypeId.Value;
            changed = true;
        }

        if (propertyId.HasValue && propertyId.Value != PropertyId)
        {
            if (propertyId.Value == Guid.Empty) return Errors.PropertyRequired;
            PropertyId = propertyId.Value;
            changed = true;
        }

        if (changed)
        {
            MarkAsUpdated();
            AddDomainEvent(new Events.Updated(Id));
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }

    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid PropertyPrototypeId) : DomainEvent;
        public record Updated(Guid PropertyPrototypeId) : DomainEvent;
        public record Deleted(Guid PropertyPrototypeId) : DomainEvent;
    }

    #endregion
}
