using System;
using System.Globalization;
using System.Linq;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model converted from Spree::ShippingRate (simplified).
/// </summary>
public sealed class ShippingRate : AuditableEntity
{
    public Guid ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }

    public Guid ShippingMethodId { get; set; }
    public ShippingMethod? ShippingMethod { get; set; }

    public Guid? TaxRateId { get; set; }
    public TaxRate? TaxRate { get; set; }

    /// <summary>
    /// Base cost for this shipping rate.
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// When true this shipping rate is the currently selected rate for the shipment.
    /// </summary>
    public bool Selected { get; set; }

    private decimal? _taxAmountCache;

    public ShippingRate() { }

    // Delegates (ruby: delegate :order, :currency, :free?, :with_free_shipping_promotion?, to: :shipment)
    public string? Name => ShippingMethod?.Name;
    public string? ShippingMethodCode =>
        ShippingMethod?.GetType().GetProperty("Code")?.GetValue(ShippingMethod) as string;

    public string DisplayBasePrice() => FormatMoney(Cost, Shipment?.Order?.Currency ?? "");
    public string DisplayTaxAmount() => FormatMoney(TaxAmount(), Shipment?.Order?.Currency ?? "");
    public string DisplayPrice()
    {
        var price = DisplayBasePrice();
        var tax = TaxAmount();

        // Respect tax rate label visibility if the TaxRate exposes such a flag (mirrors Ruby: tax_rate.show_rate_in_label)
        var showRateLabel = true;
        if (TaxRate != null)
        {
            var prop = TaxRate.GetType().GetProperty("ShowRateInLabel");
            if (prop != null && prop.GetValue(TaxRate) is bool b) showRateLabel = b;
        }

        if (TaxRate == null || tax == 0m || !showRateLabel) return price;

        var label = TaxRate.IncludedInPrice ? "including" : "excluding";
        var taxName = string.IsNullOrWhiteSpace(TaxRate.Name) ? "tax" : TaxRate.Name;
        return $"{price} ({label} tax {DisplayTaxAmount()} - {taxName})";
    }
    public string DisplayCost() => DisplayPrice();

    public decimal TaxAmount()
    {
        if (_taxAmountCache.HasValue) return _taxAmountCache.Value;
        // Ruby calls tax_rate&.calculator&.compute_shipping_rate(self)
        _taxAmountCache = TaxRate?.ComputeAmount(this) ?? 0m;
        return _taxAmountCache.Value;
    }

    public decimal FinalPrice()
    {
        var discount = DiscountAmount();

        // Prefer shipment.Free() semantic (mirrors Ruby with_free_shipping_promotion? / free?)
        var shipmentFree = false;
        if (Shipment != null)
        {
            var method = Shipment.GetType().GetMethod("Free");
            if (method != null && method.Invoke(Shipment, Array.Empty<object>()) is bool rf) shipmentFree = rf;
        }

        if (shipmentFree || Cost < -discount) return 0m;
        return Cost + discount;
    }

    public string? DeliveryRange() => ShippingMethod?.DeliveryRange();
    public string? DisplayDeliveryRange()
    {
        var dr = DeliveryRange();
        if (string.IsNullOrWhiteSpace(dr)) return null;
        // Localization/translation should be provided by infra; keep a simple placeholder.
        return $"Delivery: {dr}";
    }

    private decimal DiscountAmount()
    {
        if (Shipment == null) return 0m;
        return Shipment.Adjustments?
            .Where(a => a.IsPromotion)
            .Sum(a => a.Amount) ?? 0m;
    }

    private static string FormatMoney(decimal amount, string? currency)
    {
        var formatted = amount.ToString("N2", CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(currency) ? formatted : $"{formatted} {currency.ToUpperInvariant()}";
    }

    // Repository-level / business constraints for ShippingRate values
    public static class Constraints
    {
        public const decimal MaxCost = 99_999_999.99m;
    }

    // Reusable Error definitions for validation/handlers
    public static class Errors
    {
        public static Error CostRequired => Error.Validation("ShippingRate.CostRequired", "Shipping rate cost is required.");
        public static Error InvalidCost => Error.Validation("ShippingRate.InvalidCost", $"Cost must be between 0 and {Constraints.MaxCost:N2}.");
        public static Error NotFound(Guid id) => Error.NotFound("ShippingRate.NotFound", $"ShippingRate with ID '{id}' was not found.");
    }

    public static class Events
    {
        public record Created(Guid ShippingRateId) : DomainEvent;
        public record Updated(Guid ShippingRateId) : DomainEvent;
        public record Deleted(Guid ShippingRateId) : DomainEvent;
    }

    // Lightweight validation helper consumed by handlers
    public static List<Error> ValidateModel(decimal? cost)
    {
        var errors = new List<Error>();
        if (!cost.HasValue) errors.Add(Errors.CostRequired);
        else if (cost < 0m || cost > Constraints.MaxCost) errors.Add(Errors.InvalidCost);
        return errors;
    }
}
