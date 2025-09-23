using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Catalogs;

/// <summary>
/// Simplified domain model port of Spree::Variant.
/// Focuses on core properties, relationships and light domain helpers used by application logic.
/// Persistence behaviour, callbacks, background jobs and advanced stock/price logic belong to application/infrastructure layers.
/// </summary>
public sealed class Variant : AuditableEntity
{
    #region Properties

    public string? Sku { get; set; }
    public decimal? CostPrice { get; set; }
    public string? CostCurrency { get; set; }

    // Dimension & weight
    public decimal? Weight { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Depth { get; set; }
    public string? WeightUnit { get; set; }     // e.g. "g", "kg", "lb", "oz"
    public string? DimensionsUnit { get; set; } // e.g. "mm", "cm", "in", "ft"

    public bool TrackInventory { get; set; } = true;
    public bool IsMaster { get; set; } = false;

    public DateTimeOffset? DiscontinueOn { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    #endregion

    #region Relationships

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();
    public ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();

    public ICollection<VariantOptionValue> OptionValueVariants { get; set; } = new List<VariantOptionValue>();
    public IEnumerable<OptionValue> OptionValues => OptionValueVariants.Select(v => v.OptionValue);

    public ICollection<Image> Images { get; set; } = new List<Image>();

    public ICollection<Price> Prices { get; set; } = new List<Price>();

    public ICollection<Digital> Digitals { get; set; } = new List<Digital>();

    #endregion

    #region Constructors / Factory

    private Variant() { }

    public static Variant Create(Guid productId, string? sku = null, bool isMaster = false)
    {
        return new Variant
        {
            ProductId = productId,
            Sku = sku,
            IsMaster = isMaster
        };
    }

    public void Update(string? sku = null, decimal? costPrice = null, string? costCurrency = null)
    {
        if (!string.IsNullOrWhiteSpace(sku)) Sku = sku;
        if (costPrice.HasValue) CostPrice = costPrice;
        if (!string.IsNullOrWhiteSpace(costCurrency)) CostCurrency = costCurrency;
    }

    #endregion

    #region Stock / availability helpers

    /// <summary>
    /// Total on hand across stock items. If stock items not loaded, returns 0 (infrastructure should provide repository query).
    /// </summary>
    public int TotalOnHand => StockItems.Sum(s => s.CountOnHand);

    public bool InStock => TotalOnHand > 0;

    public bool Backorderable => StockItems.Any(s => s.Backorderable);

    public bool Purchasable => InStock || Backorderable;

    public bool Discontinued => DiscontinueOn.HasValue && DiscontinueOn.Value <= DateTimeOffset.UtcNow;

    #endregion

    #region Price helpers

    /// <summary>
    /// Returns Price for a currency if exists in the Prices collection; otherwise returns a new Price with VariantId set.
    /// Application layer should prefer repository queries for performance.
    /// </summary>
    public Price PriceIn(string? currency)
    {
        currency = currency?.ToUpperInvariant();

        var price = Prices.FirstOrDefault(p => string.Equals(p.Currency, currency, StringComparison.OrdinalIgnoreCase));
        if (price != null) return price;

        return new Price { Currency = currency, VariantId = Id };
    }

    public decimal? AmountIn(string? currency) => PriceIn(currency).Amount;

    public decimal? CompareAtAmountIn(string? currency) => PriceIn(currency).CompareAtAmount;

    public decimal? CompareAtPrice => PriceIn(CostCurrency).CompareAtAmount;

    #endregion

    #region Presentation helpers

    public string NameAndSku() => $"{(Product?.Name ?? string.Empty)} - {Sku}";

    public string SkuAndOptionsText()
    {
        var optText = OptionsText();
        return string.IsNullOrWhiteSpace(optText) ? (Sku ?? string.Empty) : $"{Sku} {optText}".Trim();
    }

    public string OptionsText()
    {
        // Simple join of option presentations. If OptionValues not loaded, return empty.
        var presentations = OptionValues.Select(ov => ov.Presentation).Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join("/", presentations);
    }

    public Image? DefaultImage()
    {
        return Images.FirstOrDefault() ?? Product?.ProductImages.FirstOrDefault();
    }

    public Image? SecondaryImage()
    {
        return Images.ElementAtOrDefault(1) ?? Product?.ProductImages.ElementAtOrDefault(1);
    }

    public IEnumerable<Image> AdditionalImages()
    {
        var defaultImage = DefaultImage();
        var combined = Images.Concat(Product?.ProductImages ?? Enumerable.Empty<ProductImage>()).DistinctBy(i => i.Id);
        return combined.Where(i => defaultImage == null || i.Id != defaultImage.Id);
    }

    #endregion

    #region Misc helpers

    public string FilterParam() => Parameterize(Sku);

    private static string Parameterize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", "-");
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9\-_]", string.Empty);
        return cleaned.Trim('-', '_');
    }

    #endregion

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int SkuMaxLength = 100;
        public const int WeightPrecision = 18;
    }

    public static class Errors
    {
        public static Error SkuRequired => Error.Validation("Variant.SkuRequired", "SKU is required.");
        public static Error InvalidPrice => Error.Validation("Variant.InvalidPrice", "Price is invalid.");
        public static Error CannotDeleteIfAttachedToCompletedOrders => Error.Conflict("Variant.CannotDelete", "Variant cannot be deleted because it is attached to completed orders.");
        public static Error NotFound(Guid id) => Error.NotFound("Variant.NotFound", $"Variant with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? sku, decimal? costPrice)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(sku))
            errors.Add(Errors.SkuRequired);

        if (costPrice.HasValue && costPrice < 0)
            errors.Add(Errors.InvalidPrice);

        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid VariantId) : DomainEvent;
        public record Updated(Guid VariantId) : DomainEvent;
        public record Deleted(Guid VariantId) : DomainEvent;
    }

    #endregion
}

