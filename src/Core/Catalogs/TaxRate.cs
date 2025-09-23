using ErrorOr;

using SharedKernel.Domain.Primitives;

namespace Core.Catalogs;

/// <summary>
/// Domain model inspired by Spree::TaxRate (simplified).
/// - Amount is a fractional value (e.g. 0.1 for 10%).
/// - Includes helpers to read/write percentage form and to compute tax amount for an item.
/// Persistence, repository lookup and adjustment creation are responsibilities of the application layer.
/// </summary>
public sealed class TaxRate : AuditableEntity
{
    public Guid? ZoneId { get; set; }
    public Zone? Zone { get; set; }

    public Guid TaxCategoryId { get; set; }
    public TaxCategory TaxCategory { get; set; } = default!;

    /// <summary>
    /// Fractional rate: 0.10 = 10%
    /// </summary>
    public decimal? Amount { get; set; }

    public bool IncludedInPrice { get; set; }
    public string? Name { get; set; }

    private TaxRate() { }

    public static TaxRate Create(Guid taxCategoryId, decimal? amount, string? name = null, bool includedInPrice = false, Guid? zoneId = null)
    {
        return new TaxRate
        {
            TaxCategoryId = taxCategoryId,
            Amount = amount,
            Name = name,
            IncludedInPrice = includedInPrice,
            ZoneId = zoneId
        };
    }

    /// <summary>
    /// Percentage view for forms/UI: 10.0 for 10%
    /// </summary>
    public decimal? AmountPercentage
    {
        get => Amount.HasValue ? Math.Round(Amount.Value * 100m, 2) : null;
        set => Amount = value.HasValue ? Math.Round(value.Value / 100m, 6) : null;
    }

    /// <summary>
    /// Compute the tax amount for a target item.
    /// The method expects the target to expose a numeric taxable base:
    /// - If target is LineItem, use target.TaxableAmount (or Amount when TaxableAmount not present)
    /// - If target is Shipment, use target.DiscountedCost (if present)
    /// - Otherwise caller should compute base and pass that value via the optional baseAmount parameter.
    /// This is intentionally lightweight — application layer should provide robust wiring.
    /// </summary>
    public decimal ComputeAmount(object? item, decimal? baseAmount = null)
    {
        if (Amount == null || Amount == 0m)
            return 0m;

        decimal taxableBase = 0m;

        if (baseAmount.HasValue)
        {
            taxableBase = baseAmount.Value;
        }
        else if (item is LineItem li)
        {
            // Prefer taxable amount if available
            try { taxableBase = li.TaxableAmount(); }
            catch { taxableBase = li.Amount(); }
        }
        else if (item is Shipment sh)
        {
            // Shipment domain model in this simplified domain exposes Cost and DiscountedCost in infra. Try Cost as fallback.
            var prop = sh.GetType().GetProperty("DiscountedCost");
            if (prop != null)
            {
                var val = prop.GetValue(sh);
                if (val is decimal d) taxableBase = d;
            }
            if (taxableBase == 0m)
            {
                var costProp = sh.GetType().GetProperty("Cost");
                if (costProp != null && costProp.GetValue(sh) is decimal cd) taxableBase = cd;
            }
        }
        else
        {
            // unknown item - no base provided
            return 0m;
        }

        return Math.Round(taxableBase * (Amount ?? 0m), 2);
    }

    #region Validation / Errors

    public static List<Error> ValidateModel(decimal? amount, Guid taxCategoryId, string? name)
    {
        var errors = new List<Error>();
        if (taxCategoryId == Guid.Empty) errors.Add(Errors.TaxCategoryRequired);
        if (amount.HasValue && (amount < 0m || amount > 10m)) // sanity upper bound (1000% unlikely)
            errors.Add(Errors.InvalidAmount);
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        return errors;
    }

    public static class Errors
    {
        public static Error TaxCategoryRequired => Error.Validation("TaxRate.TaxCategoryRequired", "Tax category is required.");
        public static Error InvalidAmount => Error.Validation("TaxRate.InvalidAmount", "Tax rate amount is invalid.");
        public static Error NameRequired => Error.Validation("TaxRate.NameRequired", "Tax rate name is required.");
        public static Error NotFound(Guid id) => Error.NotFound("TaxRate.NotFound", $"TaxRate with ID '{id}' was not found.");
    }

    #endregion
}
