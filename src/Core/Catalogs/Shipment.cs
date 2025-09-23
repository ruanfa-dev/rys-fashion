using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model port of Spree::Shipment (simplified).
/// - Represents a shipment tied to an order/stock location/address and composed of inventory units.
/// - Contains core behaviors used by application/services (state transitions, manifest, rate selection).
/// Note: persistence, business policies and complex side-effects should remain in application/services or handlers.
/// </summary>
public sealed class Shipment : AuditableEntity
{
    #region Identity & basic fields

    public string? Number { get; private set; } // port of spree number generator
    public string State { get; private set; } = "pending";
    public decimal Cost { get; private set; } = 0m;
    public decimal AdjustmentTotal { get; private set; } = 0m;
    public decimal PromoTotal { get; private set; } = 0m;
    public string? Tracking { get; set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public string? SpecialInstructions { get; set; }

    #endregion

    #region Associations

    public Guid? AddressId { get; set; }
    public Address? Address { get; set; }

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid StockLocationId { get; set; }
    public StockLocation StockLocation { get; set; } = default!;

    public ICollection<Adjustment> Adjustments { get; set; } = new List<Adjustment>();
    public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();
    public ICollection<ShippingRate> ShippingRates { get; set; } = new List<ShippingRate>();

    #endregion

    #region Computed & convenience

    public ShippingRate? SelectedShippingRate => ShippingRates.FirstOrDefault(r => r.Selected) ?? ShippingRates.OrderBy(r => r.Cost).FirstOrDefault();

    public ShippingMethod? ShippingMethod => SelectedShippingRate?.ShippingMethod ?? ShippingRates.FirstOrDefault()?.ShippingMethod;

    public decimal ItemCost => ComputeItemCost();

    public decimal FinalPrice => Cost + AdjustmentTotal;

    public IEnumerable<LineItem> LineItems =>
        InventoryUnits
            .Where(iu => iu.LineItem is not null)
            .Select(iu => iu.LineItem!)
            .Distinct();

    public bool Backordered => InventoryUnits.Any(iu => iu.Backordered);

    public bool Tracked => !string.IsNullOrWhiteSpace(Tracking) || !string.IsNullOrWhiteSpace(TrackingUrl);

    public bool Digital => ShippingMethod?.Digital ?? false;

    public string? TrackingUrl => ShippingMethod?.BuildTrackingUrl(Tracking);

    #endregion

    #region Manifest (grouping inventory units into manifest items)

    public sealed record ManifestItem(LineItem LineItem, Variant? Variant, int Quantity, IReadOnlyDictionary<string, int> States);

    public IEnumerable<ManifestItem> Manifest()
    {
        // Group by variant_id then by line_item_id and build states map (like Spree manifest)
        return InventoryUnits
            .GroupBy(iu => iu.VariantId)
            .SelectMany(groupByVariant =>
                groupByVariant
                    .GroupBy(iu => iu.LineItemId)
                    .Select(g =>
                    {
                        var states = g
                            .GroupBy(iu => iu.State)
                            .ToDictionary(k => k.Key ?? "unknown", v => v.Sum(iu => iu is { } ? 1 : 0) * 1); // quantity stored on IU in this model is implicit per unit

                        var first = g.First();
                        var lineItem = first.LineItem ?? throw new InvalidOperationException("InventoryUnit must carry LineItem to build manifest");
                        var variant = first.Variant;
                        var qty = g.Sum(iu => 1); // InventoryUnit model treats each unit as single quantity
                        return new ManifestItem(lineItem, variant, qty, states);
                    })
            );
    }

    public int ItemQuantity() => Manifest().Sum(m => m.Quantity);

    #endregion

    #region Creation / factories

    private Shipment() { }

    public static Shipment Create(Guid stockLocationId, Guid? orderId = null, Guid? addressId = null, decimal cost = 0m)
    {
        return new Shipment
        {
            StockLocationId = stockLocationId,
            OrderId = orderId,
            AddressId = addressId,
            Cost = cost
        };
    }

    #endregion

    #region Business behaviors (state transitions & helpers)

    public void SetNumber(string number)
    {
        Number = number;
        MarkAsUpdated();
    }

    public void SetCost(decimal cost)
    {
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        var old = Cost;
        Cost = cost;
        MarkAsUpdated();

        if (old != cost && State != "shipped")
        {
            RecalculateAdjustments();
        }
    }

    public void MarkShipped()
    {
        if (ShippedAt != null) return;
        ShippedAt = DateTimeOffset.UtcNow;
        State = "shipped";
        AddDomainEvent(new Events.Shipped(Id));
        MarkAsUpdated();
    }

    public void Ship() => MarkShipped();

    public void Cancel()
    {
        if (State == "canceled") return;
        State = "canceled";
        AddDomainEvent(new Events.Canceled(Id));
        MarkAsUpdated();
        // restock manifest items - domain service should call StockLocation via application service
    }

    public void Resume()
    {
        if (State != "canceled") return;
        State = DetermineState(Order);
        AddDomainEvent(new Events.Resumed(Id));
        MarkAsUpdated();
    }

    public void ReadyIfAppropriate()
    {
        if (State != "pending") return;
        if (DetermineState(Order) == "ready")
        {
            State = "ready";
            AddDomainEvent(new Events.Ready(Id));
            MarkAsUpdated();
        }
    }

    /// <summary>
    /// Determine shipment state using simplified rules from Spree:
    /// - canceled if either shipment or order canceled
    /// - pending if order cannot ship or any inventory is backordered
    /// - shipped if already shipped
    /// - ready otherwise when order is paid or configured auto-capture on dispatch (handled at application level)
    /// </summary>
    public string DetermineState(Order? order)
    {
        if (State == "canceled" || order?.State == "canceled") return "canceled";
        if (order == null) return "pending";

        // Note: these calls assume Order exposes these members; application layer may need to map as appropriate.
        // Fallback to conservative defaults when members are not available.
        bool orderCanShip = order.CanShip(); // expected on Order
        bool orderPaid = order.Paid();       // expected on Order

        if (!orderCanShip) return "pending";
        if (InventoryUnits.Any(iu => iu.Backordered)) return "pending";
        if (State == "shipped" || ShippedAt != null) return "shipped";

        return orderPaid ? "ready" : "pending";
    }

    #endregion

    #region Shipping rates / selection helpers

    public ShippingRate? AddShippingMethod(ShippingMethod method, bool selected = false)
    {
        var rate = ShippingRate.Create(method, Cost, selected);
        ShippingRates.Add(rate);
        MarkAsUpdated();
        return rate;
    }

    public Guid? SelectedShippingRateId => SelectedShippingRate?.Id;

    public void SelectShippingRate(Guid rateId)
    {
        foreach (var r in ShippingRates) r.Selected = false;
        var toSelect = ShippingRates.FirstOrDefault(r => r.Id == rateId);
        if (toSelect != null)
        {
            toSelect.Selected = true;
            Cost = toSelect.Cost;
            MarkAsUpdated();
        }
    }

    public void RefreshRates(Func<Shipment, IEnumerable<ShippingRate>> estimator)
    {
        if (State == "shipped") return;
        var rates = estimator(this) ?? Enumerable.Empty<ShippingRate>();
        ShippingRates.Clear();
        foreach (var r in rates) ShippingRates.Add(r);
        MarkAsUpdated();
    }

    #endregion

    #region Inventory helpers

    public void SetUpInventory(string state, Variant variant, Order order, LineItem lineItem, int quantity = 1)
    {
        if (quantity <= 0) return;
        for (var i = 0; i < quantity; i++)
        {
            var iu = InventoryUnit.Create(variant.Id, lineItem.Id, order.Id, this.Id);
            if (state == "backordered") iu.MarkBackordered();
            InventoryUnits.Add(iu);
        }
        MarkAsUpdated();
    }

    public bool Include(Variant variant)
    {
        return InventoryUnits.Any(iu => iu.VariantId == variant.Id);
    }

    public IEnumerable<InventoryUnit> InventoryUnitsFor(Variant variant)
        => InventoryUnits.Where(iu => iu.VariantId == variant.Id);

    public IEnumerable<InventoryUnit> InventoryUnitsForItem(LineItem lineItem, Variant? variant = null)
        => InventoryUnits.Where(iu => iu.LineItemId == lineItem.Id && (variant == null || iu.VariantId == variant.Id));

    #endregion

    #region Money & totals helpers

    private decimal ComputeItemCost()
    {
        // Attempt compute similar to Spree but simplified: sum of line_item price * quantity for manifest
        try
        {
            return Manifest()
                .Select(m =>
                {
                    var price = m.LineItem.Price;
                    var quantity = m.Quantity;
                    // line_item.adjustment_total / line_item.quantity is behavior that belongs to line item domain
                    return price * quantity;
                })
                .Sum();
        }
        catch
        {
            return 0m;
        }
    }

    public bool Free()
    {
        if (FinalPrice == 0m) return true;
        return WithFreeShippingPromotion();
    }

    private bool WithFreeShippingPromotion()
    {
        return Adjustments?.OfType<Adjustment>()
                .Any(a => a.Source != null && a.Source.FreeShipping()) ?? false;
    }

    #endregion

    #region Adjustments

    private void RecalculateAdjustments()
    {
        // Hook for adjustments recalculation. Implementation lives in application/services.
        AddDomainEvent(new Events.AdjustmentsRecalculationRequested(Id));
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid ShipmentId) : DomainEvent;
        public record Shipped(Guid ShipmentId) : DomainEvent;
        public record Canceled(Guid ShipmentId) : DomainEvent;
        public record Resumed(Guid ShipmentId) : DomainEvent;
        public record Ready(Guid ShipmentId) : DomainEvent;
        public record AdjustmentsRecalculationRequested(Guid ShipmentId) : DomainEvent;
        public record Updated(Guid ShipmentId) : DomainEvent;
    }

    #endregion
}
