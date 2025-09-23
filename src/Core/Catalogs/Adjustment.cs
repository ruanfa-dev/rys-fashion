using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model for an order/item adjustment (port of Spree::Adjustment, simplified).
/// Persistence behaviours, promotion/tax source implementations and automatic recalculation
/// must be orchestrated by the application/infrastructure layer.
/// </summary>
public sealed class Adjustment : AuditableEntity
{
    public enum AdjustmentState { Open, Closed }

    // Polymorphic association: adjustable (e.g. Order, LineItem, Shipment)
    public Guid? AdjustableId { get; set; }
    public string? AdjustableType { get; set; }

    // Polymorphic association: source (e.g. PromotionAction, TaxRate)
    public Guid? SourceId { get; set; }
    public string? SourceType { get; set; }

    // Back-reference to order (for reporting/queries)
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    // Core adjustment fields
    public string Label { get; set; } = default!;
    public decimal Amount { get; private set; }
    public bool Mandatory { get; set; } = false;
    public bool Eligible { get; private set; } = true;
    public bool Included { get; set; } = false;

    public AdjustmentState State { get; private set; } = AdjustmentState.Open;

    private Adjustment() { }

    public static Adjustment Create(Guid orderId, string label, decimal amount = 0m, bool mandatory = false, bool included = false)
    {
        return new Adjustment
        {
            OrderId = orderId,
            Label = label?.Trim() ?? string.Empty,
            Amount = amount,
            Mandatory = mandatory,
            Included = included,
            Eligible = true,
            State = AdjustmentState.Open
        };
    }

    public void SetAmount(decimal amount)
    {
        Amount = amount;
        MarkAsUpdated();
    }

    public void SetEligible(bool eligible)
    {
        Eligible = eligible;
        MarkAsUpdated();
    }

    public void Close()
    {
        State = AdjustmentState.Closed;
        MarkAsUpdated();
    }

    public void Open()
    {
        State = AdjustmentState.Open;
        MarkAsUpdated();
    }

    /// <summary>
    /// Update amount using a source compute function. Mirrors Spree's update!(target = adjustable).
    /// - If adjustment is closed or computeAmount is null, returns current Amount.
    /// - computeAmount receives the target (typically the adjustable) and should return computed decimal amount.
    /// - promotionEligible (optional) informs whether the promotion remains eligible for the given target.
    /// Application layer should pass appropriate target and functions.
    /// </summary>
    public decimal UpdateFromSource(Func<object?, decimal>? computeAmount, Func<object?, bool>? promotionEligible = null, object? target = null)
    {
        if (State == AdjustmentState.Closed || computeAmount == null)
            return Amount;

        var amount = computeAmount(target);
        Amount = amount;

        if (promotionEligible != null)
            Eligible = promotionEligible(target);

        UpdatedAt = DateTimeOffset.UtcNow;
        return Amount;
    }

    /// <summary>
    /// Lightweight validation used by handlers before persistence.
    /// Uniqueness / competing promos logic must be enforced by repositories or services when required.
    /// </summary>
    public static System.Collections.Generic.List<Error> ValidateModel(string? label, decimal? amount)
    {
        var errors = new System.Collections.Generic.List<Error>();

        if (string.IsNullOrWhiteSpace(label))
            errors.Add(Errors.LabelRequired);

        if (amount.HasValue && decimal.IsNegative(amount.Value))
            errors.Add(Errors.InvalidAmount);

        return errors;
    }

    public static class Errors
    {
        public static Error LabelRequired => Error.Validation("Adjustment.LabelRequired", "Adjustment label is required.");
        public static Error InvalidAmount => Error.Validation("Adjustment.InvalidAmount", "Adjustment amount is invalid.");
        public static Error NotFound(Guid id) => Error.NotFound("Adjustment.NotFound", $"Adjustment with ID '{id}' was not found.");
    }

    public static class Events
    {
        public record Created(Guid AdjustmentId) : DomainEvent;
        public record Updated(Guid AdjustmentId) : DomainEvent;
        public record Closed(Guid AdjustmentId) : DomainEvent;
        public record Opened(Guid AdjustmentId) : DomainEvent;
        public record Deleted(Guid AdjustmentId) : DomainEvent;
    }
}
