using Core.Catalog.Properties;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Prototypes;

/// <summary>
/// Represents the association between a Prototype and a Property.
/// Mirrors Spree::PropertyPrototype: belongs_to :prototype, :property
/// </summary>
public sealed class PrototypeProperty : AuditableEntity
{
    #region Properties

    public Guid PrototypeId { get; private set; }
    public Prototype Prototype { get; private set; } = null!;

    public Guid PropertyId { get; private set; }
    public Property Property { get; private set; } = null!;

    #endregion

    #region Constructors

    private PrototypeProperty() { }

    public PrototypeProperty(Guid prototypeId, Guid propertyId)
    {
        PrototypeId = prototypeId;
        PropertyId = propertyId;
    }

    #endregion

    #region Validation

    public static List<Error> Validate(Guid prototypeId, Guid propertyId)
    {
        var errors = new List<Error>();
        if (prototypeId == Guid.Empty) errors.Add(Error.Validation("PrototypeProperty.PrototypeRequired", "PrototypeId is required."));
        if (propertyId == Guid.Empty) errors.Add(Error.Validation("PrototypeProperty.PropertyRequired", "PropertyId is required."));
        return errors;
    }

    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid PrototypePropertyId) : DomainEvent;
        public record Deleted(Guid PrototypePropertyId) : DomainEvent;
    }

    #endregion
}
