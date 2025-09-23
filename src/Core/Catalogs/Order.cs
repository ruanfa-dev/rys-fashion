using Core.Identity;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Simplified domain port of Spree::Order (behavioral surface only).
/// Persistence, complex workflow (checkout), payments, promotions and other
/// application concerns are left to application/infra services.
/// </summary>
public sealed class Order : AuditableEntity
{
    // Constants mirroring Spree lists (kept as guidance for validations / UI)
    public static readonly string[] PAYMENT_STATES = { "balance_due", "credit_owed", "failed", "paid", "void" };
    public static readonly string[] SHIPMENT_STATES = { "backorder", "canceled", "partial", "pending", "ready", "shipped" };

    #region Enums
    public enum OrderState
    {
        None = 0,
        Cart,
        Address,
        Delivery,
        Payment,
        Confirm,
        Complete
    }
    public enum PaymentState
    {
        None = 0,
        BalanceDue,
        CreditOwed,
        Failed,
        Paid,
        Void
    }
    public enum ShipmentState
    {
        None = 0,
        Backorder,
        Canceled,
        Partial,
        Pending,
        Ready,
        Shipped
    }
    #endregion

    #region Identity & basic fields

    public string? Number { get; set; }
    public OrderState State { get; set; } = OrderState.None;
    public PaymentState PaymentState { get; set; } = PaymentState.None;
    public ShipmentState ShipmentState { get; set; } = ShipmentState.None;
    public string? Currency { get; set; }
    public string? Email { get; set; }

    public decimal? ItemTotal { get; set; }
    public decimal? AdjustmentTotal { get; set; }
    public decimal? IncludedTaxTotal { get; set; }
    public decimal? AdditionalTaxTotal { get; set; }
    public decimal? ShipmentTotal { get; set; }
    public decimal? PromoTotal { get; set; }
    public decimal? Total { get; set; }
    public decimal? PaymentTotal { get; set; }

    public int ItemCount { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
    public bool Canceled { get; set; }

    #endregion

    #region Associations / navigations

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid? CreatedById { get; set; }
    public Guid? ApproverId { get; set; }
    public Guid? CancelerId { get; set; }

    public Guid? BillAddressId { get; set; }
    public Address? BillAddress { get; set; }

    public Guid? ShipAddressId { get; set; }
    public Address? ShipAddress { get; set; }

    public Guid StoreId { get; set; }
    public Store? Store { get; set; }

    public ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Adjustment> Adjustments { get; set; } = new List<Adjustment>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();

    public ICollection<OrderPromotion> OrderPromotions { get; set; } = new List<OrderPromotion>();
    public IEnumerable<Promotion> Promotions => OrderPromotions.Select(op => op.Promotion).Where(p => p != null).ToList();

    #endregion

    #region Construction / factory

    private Order() { }

    public static Order Create(Guid storeId, string? currency, string? email = null)
    {
        return new Order
        {
            StoreId = storeId,
            Currency = currency,
            Email = email
        };
    }

    #endregion

    #region High-level helpers

    public bool Completed() => CompletedAt.HasValue;
    public bool CanceledOrDeleted() => Canceled;

    public int Quantity() => LineItems?.Sum(li => li.Quantity) ?? 0;

    public decimal OutstandingBalance()
    {
        if (Canceled) return -(PaymentTotal ?? 0m);
        return (Total ?? 0m) - ((PaymentTotal ?? 0m) + ReimbursementPaidTotal());
    }

    public bool OutstandingBalanceIsNonZero() => OutstandingBalance() != 0m;

    public decimal ReimbursementPaidTotal()
        => 0m; // infra: reimbursements sum

    public bool Backordered() => Shipments?.Any(s => s.Backordered) == true;

    public decimal ItemTotalComputed() => LineItems?.Sum(li => li.Amount()) ?? 0m;

    public IEnumerable<LineItem> AllLineItems() => LineItems;

    #endregion

    #region Line item helpers

    public LineItem? FindLineItemByVariant(Variant variant, IDictionary<string, object>? options = null)
    {
        if (variant == null) return null;
        return LineItems.FirstOrDefault(li =>
            li.VariantId == variant.Id &&
            // extension point: application should supply compare logic; default to true
            (options == null || options.Count == 0));
    }

    public int QuantityOf(Variant variant, IDictionary<string, object>? options = null)
    {
        var li = FindLineItemByVariant(variant, options);
        return li?.Quantity ?? 0;
    }

    #endregion

    #region Shipment / payment helpers

    public void CreateProposedShipments(Func<Order, IEnumerable<Shipment>> coordinator)
    {
        if (coordinator == null) return;
        // remove existing shipment-related adjustments in infra then create new shipments
        var proposed = coordinator(this);
        Shipments.Clear();
        foreach (var s in proposed) Shipments.Add(s);
        MarkAsUpdated();
    }

    public void FinalizeOrder(IOrderUpdater updater)
    {
        // Lock adjustments (infra) then update payments / shipments
        foreach (var a in Adjustments) { /* infra: a.Close() */ }

        updater.UpdatePaymentState(this);
        foreach (var s in Shipments.Where(s => s.Persisted)) { s.UpdateForOrder(this); s.Finalize(); }

        updater.UpdateShipmentState(this);
        // infra persistence and notifications follow
    }

    #endregion

    #region Delegation to application services

    // Domain delegates that application should wire (set before use)
    public IOrderPersistence? Persistence { get; set; }
    public IOrderUpdater? Updater { get; set; }

    public void UpdateWithUpdater()
    {
        Updater?.Update();
    }

    public void PersistTotals()
    {
        Persistence?.UpdateTotals(this);
    }

    #endregion

    #region Validation / Errors

    public static class Errors
    {
        public static Error StoreRequired => Error.Validation("Order.StoreRequired", "Store is required.");
        public static Error CurrencyRequired => Error.Validation("Order.CurrencyRequired", "Currency is required.");
        public static Error NotFound(Guid id) => Error.NotFound("Order.NotFound", $"Order with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(Guid storeId, string? currency)
    {
        var errors = new List<Error>();
        if (storeId == Guid.Empty) errors.Add(Errors.StoreRequired);
        if (string.IsNullOrWhiteSpace(currency)) errors.Add(Errors.CurrencyRequired);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid OrderId) : DomainEvent;
        public record Updated(Guid OrderId) : DomainEvent;
        public record Completed(Guid OrderId) : DomainEvent;
        public record Canceled(Guid OrderId) : DomainEvent;
    }

    #endregion
}

/// <summary>
/// Infrastructure/application contracts referenced by the Order domain (implement in application/infra).
/// Kept here as lightweight interfaces so domain code compiles and callers know required surface.
/// </summary>
public interface IOrderPersistence
{
    void UpdateTotals(Order order);
}

public interface IOrderUpdater
{
    void Update();
    void UpdatePaymentState(Order order);
    void UpdateShipmentState(Order order);
}
