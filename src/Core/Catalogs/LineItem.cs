using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Simplified domain model port of Spree::LineItem.
/// Keeps core fields and lightweight domain helpers; persistence and complex side-effects belong to infra layer.
/// </summary>
public sealed class LineItem : AuditableEntity
{
    // Core attributes
    public int Quantity { get; private set; } = 1;
    public decimal Price { get; private set; }
    public decimal? CostPrice { get; private set; }
    public string? CostCurrency { get; private set; }
    public string? Currency { get; private set; }

    // Associations
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid VariantId { get; set; }
    public Variant Variant { get; set; } = default!;

    public Guid? TaxCategoryId { get; set; }
    public TaxCategory? TaxCategory { get; set; }

    // Relations used by domain helpers
    public ICollection<Adjustment> Adjustments { get; set; } = new List<Adjustment>();
    public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<DigitalLink> DigitalLinks { get; set; } = new List<DigitalLink>();

    // Lightweight ctor
    private LineItem() { }

    public static LineItem Create(Guid variantId, Guid orderId, int quantity = 1)
    {
        var li = new LineItem
        {
            VariantId = variantId,
            OrderId = orderId
        };
        li.SetQuantity(quantity);
        return li;
    }

    // Mutators
    public void SetQuantity(int q)
    {
        Quantity = q < 0 ? 0 : q;
        MarkAsUpdated();
    }

    public void AssignCurrency(string currency) => Currency = currency;
    public void SetCostPrice(decimal? costPrice, string? costCurrency)
    {
        CostPrice = costPrice;
        CostCurrency = costCurrency;
    }

    public void SetPrice(decimal price)
    {
        Price = price;
        MarkAsUpdated();
    }

    // Domain helpers (mirrors Spree logic, simplified)

    public void CopyPrice()
    {
        // Copy price/cost/currency from variant when available
        if (Variant == null) return;

        if (Price == 0m)
        {
            var currency = Order?.Currency ?? Currency;
            var p = Variant.PriceIn(currency);
            Price = p?.Amount ?? 0m;
        }

        if (CostPrice == null)
            CostPrice = Variant.CostPrice;

        if (string.IsNullOrWhiteSpace(Currency))
            Currency = Variant.Prices.FirstOrDefault()?.Currency;
    }

    public void CopyTaxCategory()
    {
        if (Variant == null) return;
        TaxCategory = Variant.TaxCategory ?? TaxCategory;
        TaxCategoryId = TaxCategory?.Id ?? TaxCategoryId;
    }

    public decimal Amount() => Price * Quantity;
    public decimal Subtotal() => Amount();
    public decimal TaxableAmount() => Amount() + TaxableAdjustmentTotal;
    public decimal TaxTotal() => IncludedTaxTotal + AdditionalTaxTotal;
    public decimal FinalAmount() => Amount() + AdjustmentTotal;
    public decimal ItemWeight() => (Variant?.Weight ?? 0m) * Quantity;

    // Adjustment totals - simple aggregations (in real app these are persisted fields)
    public decimal AdjustmentTotal => Adjustments?.Where(a => !a.IsTax).Sum(a => a.Amount) ?? 0m;
    public decimal AdditionalTaxTotal => Adjustments?.Where(a => a.IsTax && !a.IsIncluded).Sum(a => a.Amount) ?? 0m;
    public decimal IncludedTaxTotal => Adjustments?.Where(a => a.IsTax && a.IsIncluded).Sum(a => a.Amount) ?? 0m;
    public decimal PreTaxAmount => Amount() - IncludedTaxTotal;
    public decimal DiscountedAmount => PreTaxAmount - (Adjustments?.Where(a => a.IsPromotion).Sum(a => a.Amount) ?? 0m);

    // Stock helpers
    public bool SufficientStock()
    {
        if (Variant == null) return false;
        if (!Variant.TrackInventory) return true;
        return Variant.TotalOnHand >= Quantity;
    }

    public bool InsufficientStock() => !SufficientStock();

    // Shipping / inventory helpers
    public bool AnyShipped() => InventoryUnits?.Any(u => string.Equals(u.State, "shipped", StringComparison.OrdinalIgnoreCase)) == true;
    public bool FullyShipped() => InventoryUnits != null && InventoryUnits.Any() && InventoryUnits.All(u => string.Equals(u.State, "shipped", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Compute proportional shipping cost across shipments that include inventory units for this line item.
    /// This is a best-effort computation — infra should supply accurate shipment data.
    /// </summary>
    public decimal ShippingCost()
    {
        if (Shipments == null || !Shipments.Any()) return 0m;

        decimal total = 0m;
        foreach (var shipment in Shipments)
        {
            if (shipment.IsCancelled) return 0m;

            var lineItemUnits = shipment.InventoryUnits?.Count(u => u.LineItemId == Id) ?? 0;
            var totalUnits = shipment.InventoryUnits?.Count() ?? 0;
            if (totalUnits == 0 || lineItemUnits == 0 || shipment.Cost == 0m) return 0m;

            total += shipment.Cost * ((decimal)lineItemUnits / (decimal)totalUnits);
        }

        return total;
    }

    public bool WithDigitalAssets() => Variant?.Digitals?.Any() == true;

    // Lightweight validation used by handlers
    public static List<Error> ValidateModel(int quantity, Guid variantId, Guid orderId)
    {
        var errors = new List<Error>();
        if (quantity < 0) errors.Add(Errors.InvalidQuantity);
        if (variantId == Guid.Empty) errors.Add(Errors.VariantRequired);
        if (orderId == Guid.Empty) errors.Add(Errors.OrderRequired);
        return errors;
    }

    public static class Errors
    {
        public static Error InvalidQuantity => Error.Validation("LineItem.InvalidQuantity", "Quantity must be a non-negative integer.");
        public static Error VariantRequired => Error.Validation("LineItem.VariantRequired", "Variant is required.");
        public static Error OrderRequired => Error.Validation("LineItem.OrderRequired", "Order is required.");
    }
}

