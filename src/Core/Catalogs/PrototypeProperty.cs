using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Join entity between Prototype and Property (port of Spree::PropertyPrototype).
/// DB-level uniqueness (PrototypeId, PropertyId) should be enforced in EF configuration/migrations.
/// </summary>
public sealed class PrototypeProperty : AuditableEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = default!;

    public Guid PrototypeId { get; set; }
    public Prototype Prototype { get; set; } = default!;

    private PrototypeProperty() { }

    public static PrototypeProperty Create(Guid prototypeId, Guid propertyId)
    {
        return new PrototypeProperty
        {
            PrototypeId = prototypeId,
            PropertyId = propertyId
        };
    }

    /// <summary>
    /// Lightweight validation used by handlers before persistence.
    /// Uniqueness must be enforced/checked by repository layer.
    /// </summary>
    public static List<Error> ValidateModel(Guid prototypeId, Guid propertyId)
    {
        var errors = new List<Error>();
        if (prototypeId == Guid.Empty) errors.Add(Errors.PrototypeRequired);
        if (propertyId == Guid.Empty) errors.Add(Errors.PropertyRequired);
        return errors;
    }

    public static class Errors
    {
        public static Error PrototypeRequired => Error.Validation("PrototypeProperty.PrototypeRequired", "Prototype is required.");
        public static Error PropertyRequired => Error.Validation("PrototypeProperty.PropertyRequired", "Property is required.");
        public static Error DuplicateAssignment(Guid prototypeId, Guid propertyId) =>
            Error.Conflict("PrototypeProperty.DuplicateAssignment", $"Prototype '{prototypeId}' is already assigned to Property '{propertyId}'.");
        public static Error NotFound(Guid id) => Error.NotFound("PrototypeProperty.NotFound", $"PrototypeProperty with ID '{id}' was not found.");
    }

    public static class Events
    {
        public record Created(Guid PrototypePropertyId) : DomainEvent;
        public record Deleted(Guid PrototypePropertyId) : DomainEvent;
    }
}