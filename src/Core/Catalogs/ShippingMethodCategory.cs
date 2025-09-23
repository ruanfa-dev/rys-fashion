using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Join entity between ShippingMethod and ShippingCategory.
/// DB-level uniqueness (ShippingMethodId, ShippingCategoryId) should be enforced in EF configuration.
/// </summary>
public sealed class ShippingMethodCategory : AuditableEntity
{
    public Guid ShippingMethodId { get; set; }
    public ShippingMethod ShippingMethod { get; set; } = default!;

    public Guid ShippingCategoryId { get; set; }
    public ShippingCategory ShippingCategory { get; set; } = default!;

    private ShippingMethodCategory() { }

    public static ShippingMethodCategory Create(Guid shippingMethodId, Guid shippingCategoryId)
        => new ShippingMethodCategory
        {
            ShippingMethodId = shippingMethodId,
            ShippingCategoryId = shippingCategoryId
        };

    public static List<Error> ValidateModel(Guid shippingMethodId, Guid shippingCategoryId)
    {
        var errors = new List<Error>();
        if (shippingMethodId == Guid.Empty) errors.Add(Errors.ShippingMethodRequired);
        if (shippingCategoryId == Guid.Empty) errors.Add(Errors.ShippingCategoryRequired);
        return errors;
    }

    public static class Errors
    {
        public static Error ShippingMethodRequired =>
            Error.Validation("ShippingMethodCategory.ShippingMethodRequired", "ShippingMethod is required.");

        public static Error ShippingCategoryRequired =>
            Error.Validation("ShippingMethodCategory.ShippingCategoryRequired", "ShippingCategory is required.");

        public static Error DuplicateAssignment(Guid shippingMethodId, Guid shippingCategoryId) =>
            Error.Conflict(
                "ShippingMethodCategory.DuplicateAssignment",
                $"ShippingMethod '{shippingMethodId}' is already assigned to ShippingCategory '{shippingCategoryId}'.");
    }

    public static class Events
    {
        public record Created(Guid ShippingMethodCategoryId) : DomainEvent;
        public record Deleted(Guid ShippingMethodCategoryId) : DomainEvent;
    }
}
