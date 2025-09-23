using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Join entity between OptionValue and Variant.
/// DB-level constraints (unique composite index on OptionValueId + VariantId) should be configured in EF.
/// </summary>
public sealed class VariantOptionValue : AuditableEntity
{
    public Guid OptionValueId { get; set; }
    public OptionValue OptionValue { get; set; } = default!;

    public Guid VariantId { get; set; }
    public Variant Variant { get; set; } = default!;

    private VariantOptionValue() { }

    public static VariantOptionValue Create(Guid optionValueId, Guid variantId)
    {
        return new VariantOptionValue
        {
            OptionValueId = optionValueId,
            VariantId = variantId
        };
    }

    /// <summary>
    /// Lightweight validation used by application/handlers before persistence.
    /// Uniqueness must be enforced/checked at the database level.
    /// </summary>
    public static List<Error> ValidateModel(Guid optionValueId, Guid variantId)
    {
        var errors = new List<Error>();

        if (optionValueId == Guid.Empty)
            errors.Add(Errors.OptionValueRequired);

        if (variantId == Guid.Empty)
            errors.Add(Errors.VariantRequired);

        return errors;
    }

    /// <summary>
    /// Helper to filter an IQueryable of OptionValueVariant by a set of option types.
    /// Mirrors the Rails scope :for_option_types.
    /// </summary>
    public static IQueryable<VariantOptionValue> ForOptionTypes(IQueryable<VariantOptionValue> query, IEnumerable<OptionType> optionTypes)
    {
        var optionTypeIds = optionTypes.Select(ot => ot.Id).ToList();
        return query.Where(ovv => optionTypeIds.Contains(ovv.OptionValue.OptionTypeId));
    }

    public static class Errors
    {
        public static Error OptionValueRequired =>
            Error.Validation("VariantOptionValue.OptionValueRequired", "OptionValue is required.");

        public static Error VariantRequired =>
            Error.Validation("VariantOptionValue.VariantRequired", "Variant is required.");

        public static Error DuplicateAssignment(Guid optionValueId, Guid variantId) =>
            Error.Conflict(
                "OptionValueVariant.DuplicateAssignment",
                $"OptionValue '{optionValueId}' is already assigned to Variant '{variantId}'.");
    }

    public static class Events
    {
        public record Created(Guid OptionValueVariantId) : DomainEvent;
        public record Deleted(Guid OptionValueVariantId) : DomainEvent;
    }
}
