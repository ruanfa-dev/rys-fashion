using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model for an inventory unit (simplified port of Spree::InventoryUnit).
/// An InventoryUnit represents a physical unit of a Variant allocated to an order/line item/shipment.
/// Persistence, complex state transitions and business rules should be implemented in application/services.
/// </summary>
public sealed class InventoryUnit : AuditableEntity
{
    // Associations
    public Guid VariantId { get; set; }
    public Variant Variant { get; set; } = default!;

    public Guid? LineItemId { get; set; }
    public LineItem? LineItem { get; set; }

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid? ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }

    // State (e.g. "on_hand", "backordered", "shipped", "returned")
    public string State { get; private set; } = "on_hand";

    // Whether this unit was backordered when allocated
    public bool Backordered { get; private set; }

    private InventoryUnit() { }

    public static InventoryUnit Create(Guid variantId, Guid? lineItemId = null, Guid? orderId = null, Guid? shipmentId = null)
    {
        return new InventoryUnit
        {
            VariantId = variantId,
            LineItemId = lineItemId,
            OrderId = orderId,
            ShipmentId = shipmentId,
            State = "on_hand",
            Backordered = false
        };
    }

    public void MarkBackordered()
    {
        Backordered = true;
        State = "backordered";
        this.MarkAsUpdated();
    }

    public void MarkOnHand()
    {
        Backordered = false;
        State = "on_hand";
        this.MarkAsUpdated();
    }

    public void MarkShipped(Guid shipmentId)
    {
        ShipmentId = shipmentId;
        State = "shipped";
        this.MarkAsUpdated();
    }

    public void MarkReturned()
    {
        State = "returned";
        this.MarkAsUpdated();
    }

    public void AssociateLineItem(Guid lineItemId)
    {
        LineItemId = lineItemId;
        this.MarkAsUpdated();
    }

    public void AssociateOrder(Guid orderId)
    {
        OrderId = orderId;
        this.MarkAsUpdated();
    }

    public static List<Error> ValidateModel(Guid variantId)
    {
        var errors = new List<Error>();
        if (variantId == Guid.Empty)
            errors.Add(Errors.VariantRequired);
        return errors;
    }

    public static class Errors
    {
        public static Error VariantRequired => Error.Validation("InventoryUnit.VariantRequired", "Variant is required for an inventory unit.");
        public static Error NotFound(Guid id) => Error.NotFound("InventoryUnit.NotFound", $"InventoryUnit with ID '{id}' was not found.");
    }

    public static class Events
    {
        public record Created(Guid InventoryUnitId) : DomainEvent;
        public record Shipped(Guid InventoryUnitId) : DomainEvent;
        public record Backordered(Guid InventoryUnitId) : DomainEvent;
        public record Returned(Guid InventoryUnitId) : DomainEvent;
        public record Deleted(Guid InventoryUnitId) : DomainEvent;
    }
}
