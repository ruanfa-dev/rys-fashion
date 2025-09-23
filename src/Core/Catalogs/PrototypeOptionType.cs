using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

public sealed class PrototypeOptionType : AuditableEntity
{
    // Composite uniqueness (OptionTypeId, PrototypeId) should be enforced via EF configuration.
    public Guid OptionTypeId { get; set; }
    public OptionType OptionType { get; set; } = default!;

    public Guid PrototypeId { get; set; }
    public Prototype Prototype { get; set; } = default!;

    private PrototypeOptionType() { }

    public static PrototypeOptionType Create(Guid optionTypeId, Guid prototypeId)
    {
        return new PrototypeOptionType
        {
            OptionTypeId = optionTypeId,
            PrototypeId = prototypeId
        };
    }

    /// <summary>
    /// Lightweight validation used by application/handlers before persistence.
    /// Note: uniqueness must be checked against the database.
    /// </summary>
    public static List<Error> ValidateModel(Guid optionTypeId, Guid prototypeId)
    {
        var errors = new List<Error>();

        if (optionTypeId == Guid.Empty)
            errors.Add(Errors.OptionTypeRequired);

        if (prototypeId == Guid.Empty)
            errors.Add(Errors.PrototypeRequired);

        return errors;
    }

    public static class Errors
    {
        public static Error OptionTypeRequired =>
            Error.Validation("OptionTypePrototype.OptionTypeRequired", "OptionType is required.");

        public static Error PrototypeRequired =>
            Error.Validation("OptionTypePrototype.PrototypeRequired", "Prototype is required.");

        public static Error DuplicateAssignment(Guid optionTypeId, Guid prototypeId) =>
            Error.Conflict(
                "OptionTypePrototype.DuplicateAssignment",
                $"OptionType '{optionTypeId}' is already assigned to Prototype '{prototypeId}'.");
    }

    public static class Events
    {
        public record Created(Guid OptionTypePrototypeId) : DomainEvent;
        public record Deleted(Guid OptionTypePrototypeId) : DomainEvent;
    }
}
