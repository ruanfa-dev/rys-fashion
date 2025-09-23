using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Port of Spree::OrderInventory (simplified).
/// Responsibilities:
/// - ensure line item has the correct number of inventory units allocated across shipments
/// - add inventory units to a shipment (respecting stock location fill status)
/// - remove inventory units from shipments and restock stock locations when appropriate
///
/// Note: persistence, transactions and some application-level behaviours (creating proposed shipments,
/// touching DB, background jobs) are left to the application/infrastructure layer.
/// The implementation below assumes InventoryUnit represents a single physical unit (quantity = 1).
/// If your domain stores quantity on InventoryUnit, adapt usages accordingly.
/// </summary>
public sealed class OrderInventory
{
    private readonly Order _order;
    private readonly LineItem _lineItem;
    private readonly Variant _variant;

    public OrderInventory(Order order, LineItem lineItem)
    {
        _order = order ?? throw new ArgumentNullException(nameof(order));
        _lineItem = lineItem ?? throw new ArgumentNullException(nameof(lineItem));
        _variant = lineItem.Variant ?? throw new ArgumentException("LineItem must have a Variant", nameof(lineItem));
    }

    private IEnumerable<InventoryUnit> InventoryUnits => _lineItem.InventoryUnits ?? Enumerable.Empty<InventoryUnit>();

    /// <summary>
    /// Ensure inventory units match the line item quantity.
    /// If a shipment is supplied, only operate on that shipment where appropriate.
    /// </summary>
    public void Verify(Shipment? shipment = null, bool isUpdated = false)
    {
        // Only run when order is completed or a shipment was explicitly passed in
        if (!(_order != null && (_order.IsCompleted() || shipment != null)))
            return;

        // Current units count (treat each InventoryUnit as 1 quantity)
        var unitsCount = InventoryUnits.Count();

        // We don't have AR-style dirty tracking here; callers can pass isUpdated to influence behavior.
        // For simplicity treat line_item_changed as false (conservative) when caller doesn't indicate update.
        var lineItemChanged = false;

        if (unitsCount < _lineItem.Quantity)
        {
            var quantityToAdd = _lineItem.Quantity - unitsCount;
            shipment ??= DetermineTargetShipment();
            AddToShipment(shipment, quantityToAdd);
        }
        else if (unitsCount > _lineItem.Quantity || (unitsCount == _lineItem.Quantity && lineItemChanged))
        {
            Remove(unitsCount, shipment);
        }
    }

    private void Remove(int unitsCount, Shipment? targetShipment = null)
    {
        var quantity = SetQuantityToRemove(unitsCount);

        if (targetShipment != null)
        {
            RemoveFromShipment(targetShipment, quantity);
            return;
        }

        // iterate shipments until we've removed required quantity
        foreach (var shipment in _order.Shipments ?? Enumerable.Empty<Shipment>())
        {
            if (quantity <= 0) break;
            var removed = RemoveFromShipment(shipment, quantity);
            quantity -= removed;
        }
    }

    private int SetQuantityToRemove(int unitsCount)
    {
        // mirror Ruby logic:
        // if (units_count - line_item.quantity).zero? then remove line_item.quantity else remove difference
        var diff = unitsCount - _lineItem.Quantity;
        return diff == 0 ? _lineItem.Quantity : diff;
    }

    /// <summary>
    /// Determine best target shipment:
    /// - first unshipped (ready or pending) that already includes this variant
    /// - otherwise first unshipped whose stock location stocks the variant
    /// </summary>
    private Shipment? DetermineTargetShipment()
    {
        var shipments = _order.Shipments ?? Enumerable.Empty<Shipment>();

        var target = shipments.FirstOrDefault(s =>
            (s.State == "ready" || s.State == "pending") && s.Include(_variant));

        if (target != null) return target;

        return shipments.FirstOrDefault(s =>
            (s.State == "ready" || s.State == "pending") &&
            (_variant is Variant v && v != null && v.Product != null) &&
            // best-effort: check stock location id membership on variant (StockLocationIds not modelled in domain)
            // we try to match by stock location id presence on variant via a potential property StockLocationIds
            (s.StockLocation != null && VariantStocksAt(s.StockLocation, _variant))
        );
    }

    private static bool VariantStocksAt(StockLocation stockLocation, Variant variant)
    {
        // Domain-level best-effort: if stockLocation has StockItems for variant
        return stockLocation.StockItems.Any(si => si.VariantId == variant.Id);
    }

    /// <summary>
    /// Add specified quantity of inventory to the given shipment, respecting stock location fill status.
    /// If shipment is null a best-effort attempt is made to select/ create a shipment (application should provide create_proposed_shipments).
    /// </summary>
    private int AddToShipment(Shipment? shipment, int quantity)
    {
        if (quantity <= 0) return 0;

        // If no shipment supplied, try to pick first existing shipment; application should supply proper creation.
        if (shipment == null)
        {
            shipment = _order.Shipments?.FirstOrDefault() ?? Shipment.Create(Guid.Empty, _order.Id, addressId: null, cost: 0m);
            // Note: Shipment.Create with Guid.Empty StockLocation is a placeholder — infra should create a proper proposed shipment.
        }

        if (_variant.TrackInventory)
        {
            var (onHand, backOrder) = shipment.StockLocation != null
                ? shipment.StockLocation.FillStatus(_variant, quantity)
                : (quantity, 0);

            if (onHand > 0)
                shipment.SetUpInventory("on_hand", _variant, _order, _lineItem, onHand);

            if (backOrder > 0)
                shipment.SetUpInventory("backordered", _variant, _order, _lineItem, backOrder);
        }
        else
        {
            shipment.SetUpInventory("on_hand", _variant, _order, _lineItem, quantity);
        }

        // When order already completed and variant tracks inventory, remove from stock location immediately
        if (_order.IsCompleted() && _variant.TrackInventory && shipment.StockLocation != null)
        {
            shipment.StockLocation.Unstock(_variant, quantity, shipment, persist: true);
        }

        return quantity;
    }

    /// <summary>
    /// Remove up to <paramref name="quantity"/> inventory units from the shipment.
    /// Returns number of removed units.
    /// </summary>
    private int RemoveFromShipment(Shipment shipment, int quantity)
    {
        if (quantity == 0 || shipment.State == "shipped") return 0;

        // Get shipment units for this line item and variant, excluding already shipped ones
        var shipmentUnits = shipment.InventoryUnitsForItem(_lineItem, _variant)
                                    .Where(iu => !string.Equals(iu.State, "shipped", StringComparison.OrdinalIgnoreCase))
                                    .ToList();

        var removedQuantity = 0;
        var removedBackordered = 0;

        // Each InventoryUnit represents one unit in this simplified model
        foreach (var iu in shipmentUnits)
        {
            if (removedQuantity >= quantity) break;

            if (iu.Backordered)
            {
                removedBackordered++;
            }

            // remove the inventory unit from shipment
            shipment.InventoryUnits.Remove(iu);
            removedQuantity++;
        }

        // If shipment has no inventory units left, remove it from order shipments (best-effort)
        if ((shipment.InventoryUnits?.Count ?? 0) == 0)
        {
            try
            {
                _order.Shipments?.Remove(shipment);
            }
            catch
            {
                // best-effort: ignore if order.Shipments not modifiable
            }
        }

        // If order completed we need to return stock to stock_location
        if (_order.IsCompleted() && shipment.StockLocation != null)
        {
            var currentOnHand = shipment.StockLocation.CountOnHand(_variant) ?? 0;

            if (currentOnHand < 0 && Math.Abs(currentOnHand) < removedBackordered)
            {
                // if current_on_hand negative and smaller than removed backordered, restock that amount first
                shipment.StockLocation.RestockBackordered(_variant, Math.Abs(currentOnHand));
                var remainingBackordered = removedBackordered - Math.Abs(currentOnHand);
                if (remainingBackordered > 0)
                    shipment.StockLocation.RestockBackordered(_variant, remainingBackordered);
            }
            else
            {
                if (removedBackordered > 0)
                    shipment.StockLocation.RestockBackordered(_variant, removedBackordered);
            }

            var toRestockOnHand = removedQuantity - removedBackordered;
            if (toRestockOnHand > 0)
            {
                shipment.StockLocation.Restock(_variant, toRestockOnHand, shipment, persist: true);
            }
        }

        return removedQuantity;
    }
}