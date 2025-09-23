using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Join entity between Store and PaymentMethod (port of Spree::StorePaymentMethod).
/// DB-level uniqueness (StoreId, PaymentMethodId) should be enforced in EF configuration/migrations.
/// </summary>
public sealed class StorePaymentMethod : AuditableEntity
{
    public Guid StoreId { get; set; }
    public Store? Store { get; set; }

    public Guid PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    private StorePaymentMethod() { }

    public static StorePaymentMethod Create(Guid storeId, Guid paymentMethodId)
        => new StorePaymentMethod
        {
            StoreId = storeId,
            PaymentMethodId = paymentMethodId
        };

    /// <summary>
    /// Lightweight validation used by handlers before persistence.
    /// Uniqueness must be enforced/checked by repository layer.
    /// </summary>
    public static List<Error> ValidateModel(Guid storeId, Guid paymentMethodId)
    {
        var errors = new List<Error>();
        if (storeId == Guid.Empty) errors.Add(Errors.StoreRequired);
        if (paymentMethodId == Guid.Empty) errors.Add(Errors.PaymentMethodRequired);
        return errors;
    }

    public static class Errors
    {
        public static Error StoreRequired =>
            Error.Validation("StorePaymentMethod.StoreRequired", "Store is required.");

        public static Error PaymentMethodRequired =>
            Error.Validation("StorePaymentMethod.PaymentMethodRequired", "Payment method is required.");

        public static Error DuplicateAssignment(Guid storeId, Guid paymentMethodId) =>
            Error.Conflict("StorePaymentMethod.DuplicateAssignment", $"Payment method '{paymentMethodId}' is already assigned to store '{storeId}'.");

        public static Error NotFound(Guid id) =>
            Error.NotFound("StorePaymentMethod.NotFound", $"StorePaymentMethod with ID '{id}' was not found.");
    }

    public static class Events
    {
        public record Created(Guid StorePaymentMethodId) : DomainEvent;
        public record Deleted(Guid StorePaymentMethodId) : DomainEvent;
    }
}
