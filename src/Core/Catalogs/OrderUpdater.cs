using System;
using System.Linq;

using Core.Catalogs;

namespace Core.Cart;

/// <summary>
/// Port of Spree::OrderUpdater (simplified).
/// - Updates counts/totals and triggers recalculation hooks.
/// - Persistence is delegated to an injected persistence implementation (application/infra responsibility).
/// </summary>
public sealed class OrderUpdater
{
    private readonly Order _order;
    private readonly IOrderPersistence _persistence;
    private readonly IAdjustmentsUpdater? _adjustmentsUpdater;

    public OrderUpdater(Order order, IOrderPersistence persistence, IAdjustmentsUpdater? adjustmentsUpdater = null)
    {
        _order = order ?? throw new ArgumentNullException(nameof(order));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _adjustmentsUpdater = adjustmentsUpdater;
    }

    /// <summary>
    /// Main entry point — updates item counts, totals, shipments and persists totals.
    /// Callers should ensure this does not cause recursive save callbacks.
    /// </summary>
    public void Update()
    {
        UpdateItemCount();
        UpdateTotals();

        if (_order.IsCompleted())
        {
            UpdatePaymentState();
            UpdateShipments();
            UpdateShipmentState();
            UpdateShipmentTotal();
        }

        RunHooks();
        PersistTotals();
    }

    public void RunHooks()
    {
        // order.UpdateHooks is expected to be an enumerable of parameterless action names to invoke on order.
        if (_order.UpdateHooks == null) return;

        foreach (var hook in _order.UpdateHooks)
        {
            try { _order.InvokeHook(hook); } catch { /* ignore hook errors in domain layer */ }
        }
    }

    public void RecalculateAdjustments()
    {
        if (_adjustmentsUpdater == null) return;

        var allAdjustables = _order.AllAdjustments?
            .Select(a => a.Adjustable)
            .Where(a => a != null)
            .Distinct()
            .ToList();

        if (allAdjustables == null) return;

        foreach (var adjustable in allAdjustables)
        {
            _adjustmentsUpdater.Update(adjustable!);
        }
    }

    public void UpdateTotals()
    {
        UpdatePaymentTotal();
        UpdateItemTotal();
        UpdateShipmentTotal();
        UpdateAdjustmentTotal();
    }

    public void UpdateShipments()
    {
        var shippingMethodFilter = _order.IsCompleted() ? ShippingMethod.DISPLAY_ON_BACK_END : ShippingMethod.DISPLAY_ON_FRONT_END;

        foreach (var shipment in _order.Shipments ?? Enumerable.Empty<Shipment>())
        {
            if (!shipment.Persisted) continue;

            try
            {
                shipment.UpdateForOrder(_order);
                shipment.RefreshRates(s => s, shippingMethodFilter); // infra should provide proper estimator; this is a placeholder call-signature
                shipment.UpdateAmounts();
            }
            catch
            {
                // best-effort: ignore per- shipment errors in domain updater
            }
        }
    }

    public void UpdatePaymentTotal()
    {
        // Sum completed payments less refunds
        _order.PaymentTotal = (_order.Payments ?? Enumerable.Empty<Payment>())
            .Where(p => p.Completed)
            .Sum(p => p.Amount - (p.Refunds?.Sum(r => r.Amount) ?? 0m));
    }

    public void UpdateShipmentTotal()
    {
        _order.ShipmentTotal = (_order.Shipments ?? Enumerable.Empty<Shipment>()).Sum(s => s.Cost);
        UpdateOrderTotal();
    }

    public void UpdateOrderTotal()
    {
        _order.Total = (_order.ItemTotal ?? 0m) + (_order.ShipmentTotal ?? 0m) + (_order.AdjustmentTotal ?? 0m);
    }

    public void UpdateAdjustmentTotal()
    {
        // Recalculate adjustments before aggregating so values are fresh
        RecalculateAdjustments();

        var lineItemAdjustments = (_order.LineItems ?? Enumerable.Empty<LineItem>()).Sum(li => li.AdjustmentTotal);
        var shipmentAdjustments = (_order.Shipments ?? Enumerable.Empty<Shipment>()).Sum(s => s.AdjustmentTotal);
        var eligibleAdjustments = (_order.Adjustments ?? Enumerable.Empty<Adjustment>()).Where(a => a.Eligible).Sum(a => a.Amount);

        _order.AdjustmentTotal = lineItemAdjustments + shipmentAdjustments + eligibleAdjustments;

        _order.IncludedTaxTotal = (_order.LineItems ?? Enumerable.Empty<LineItem>()).Sum(li => li.IncludedTaxTotal)
                                 + (_order.Shipments ?? Enumerable.Empty<Shipment>()).Sum(s => s.IncludedTaxTotal);

        _order.AdditionalTaxTotal = (_order.LineItems ?? Enumerable.Empty<LineItem>()).Sum(li => li.AdditionalTaxTotal)
                                  + (_order.Shipments ?? Enumerable.Empty<Shipment>()).Sum(s => s.AdditionalTaxTotal);

        _order.PromoTotal = (_order.LineItems ?? Enumerable.Empty<LineItem>()).Sum(li => li.PromoTotal)
                         + (_order.Shipments ?? Enumerable.Empty<Shipment>()).Sum(s => s.PromoTotal)
                         + ((_order.Adjustments ?? Enumerable.Empty<Adjustment>()).Where(a => IsPromotion(a) && a.Eligible).Sum(a => a.Amount));

        UpdateOrderTotal();
    }

    public void UpdateItemCount()
    {
        _order.ItemCount = _order.Quantity;
    }

    public void UpdateItemTotal()
    {
        _order.ItemTotal = (_order.LineItems ?? Enumerable.Empty<LineItem>()).Sum(li => li.Amount);
        UpdateOrderTotal();
    }

    public void PersistTotals()
    {
        // Delegate persistence to infra. Implementation decides how to persist (bulk update, snapshot, etc).
        _persistence.UpdateTotals(_order);
    }

    public string UpdateShipmentState()
    {
        if (_order.Backordered)
        {
            _order.ShipmentState = "backorder";
        }
        else
        {
            var shipmentStates = (_order.Shipments ?? Enumerable.Empty<Shipment>()).Select(s => s.State).Where(s => s != null).Distinct().ToList();

            if (shipmentStates.Count > 1)
            {
                if (shipmentStates.Contains("shipped")) _order.ShipmentState = "partial";
                else if (shipmentStates.Contains("pending")) _order.ShipmentState = "pending";
                else _order.ShipmentState = "ready";
            }
            else
            {
                _order.ShipmentState = shipmentStates.FirstOrDefault();
            }
        }

        _order.StateChanged("shipment");
        return _order.ShipmentState ?? string.Empty;
    }

    public string UpdatePaymentState()
    {
        var lastState = _order.PaymentState;

        if ((_order.Payments ?? Enumerable.Empty<Payment>()).Any() && (_order.Payments ?? Enumerable.Empty<Payment>()).All(p => p.Valid == false))
        {
            _order.PaymentState =  P;
        }
        else if (_order.Canceled && _order.PaymentTotal == 0m)
        {
            _order.PaymentState = "void";
        }
        else
        {
            if (_order.OutstandingBalance() > 0m) _order.PaymentState = "balance_due";
            if (_order.OutstandingBalance() < 0m) _order.PaymentState = "credit_owed";
            if (_order.OutstandingBalance() == 0m) _order.PaymentState = "paid";
        }

        if (lastState != _order.PaymentState) _order.StateChanged("payment");

        return _order.PaymentState ?? string.Empty;
    }

    private static bool IsPromotion(Adjustment a)
    {
        // best-effort detection; infra should provide reliable flagging
        return a.SourceId != null || (a.GetType().GetProperty("SourceType")?.GetValue(a) as string)?.Contains("Promotion", StringComparison.OrdinalIgnoreCase) == true;
    }
}

/// <summary>
/// Persistence contract used by OrderUpdater to persist totals.
/// Implement this in the application/infrastructure layer to perform efficient updates (bulk SQL, EF Core UpdateColumns, etc).
/// </summary>
public interface IOrderPersistence
{
    void UpdateTotals(Order order);
}

/// <summary>
/// Small contract to allow OrderUpdater to ask infra to recalculate adjustments per adjustable.
/// Implementations should call the AdjustmentsUpdater logic in infra (this mirrors Spree::Adjustable::AdjustmentsUpdater.update).
/// </summary>
public interface IAdjustmentsUpdater
{
    void Update(object adjustable);
}