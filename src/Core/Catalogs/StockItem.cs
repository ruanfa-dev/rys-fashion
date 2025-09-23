using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model port of Spree::StockItem (simplified).
/// - Tracks inventory count for a variant at a stock location.
/// - Provides helpers to adjust count and process backorders (best-effort; heavy work belongs to infra/services).
/// - DB-level constraints (unique variant+stock_location) and cascade behaviours belong in EF configurations.
/// </summary>
public sealed class StockItem : AuditableEntity
{
    private readonly object _sync = new();

    public Guid StockLocationId { get; set; }
    public StockLocation? StockLocation { get; set; }

    public Guid VariantId { get; set; }
    public Variant Variant { get; set; } = default!;

    /// <summary>
    /// Current available count on hand for this stock item.
    /// </summary>
    public int CountOnHand { get; private set; }

    /// <summary>
    /// Whether backorders are allowed for this stock item.
    /// </summary>
    public bool Backorderable { get; set; }

    /// <summary>
    /// Soft-delete placeholder (used by EF/infra).
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    private StockItem() { }

    public static StockItem Create(Guid stockLocationId, Guid variantId, int initialCount = 0, bool backorderable = false)
    {
        return new StockItem
        {
            StockLocationId = stockLocationId,
            VariantId = variantId,
            CountOnHand = initialCount,
            Backorderable = backorderable
        };
    }

    /// <summary>
    /// Adjust the on-hand count by value (positive or negative) in a thread-safe manner.
    /// </summary>
    public void AdjustCountOnHand(int value)
    {
        lock (_sync)
        {
            SetCountOnHand(CheckedAdd(CountOnHand, value));
        }
    }

    /// <summary>
    /// Set the on-hand count, process backorders for any increase and mark entity updated.
    /// </summary>
    public void SetCountOnHand(int value)
    {
        // Normalize lower bound
        var previous = CountOnHand;
        CountOnHand = Math.Max(0, value);

        var delta = CountOnHand - previous;
        if (delta > 0)
        {
            ProcessBackorders(delta);
        }

        // Domain signal — infra should persist
        MarkAsUpdated();

        // Touch variant so caches/denormalizations can be invalidated by infra
        ConditionalVariantTouch(previous);
    }

    /// <summary>
    /// True when there is at least one unit in stock.
    /// </summary>
    public bool InStock() => CountOnHand > 0;

    /// <summary>
    /// Available to include in shipment: in stock or backorderable.
    /// </summary>
    public bool Available() => InStock() || Backorderable;

    /// <summary>
    /// Reduce count_on_hand to zero if positive.
    /// </summary>
    public void ReduceCountOnHandToZero()
    {
        if (CountOnHand > 0)
            SetCountOnHand(0);
    }

    /// <summary>
    /// Returns backordered inventory units associated with this stock item.
    /// This uses the InventoryUnits collection on the variant or those linked by infra.
    /// Note: infra may provide optimized queries; this is an in-memory fallback.
    /// </summary>
    public IEnumerable<InventoryUnit> BackorderedInventoryUnits()
    {
        // Prefer inventory units that reference this stock item (if such relation exists)
        // Fallback to variant-level/backordered units if not.
        if (Variant != null && Variant.InventoryUnits != null)
        {
            return Variant.InventoryUnits
                          .Where(iu => iu.Backordered && iu.ShipmentId == null) // best-effort filter
                          .ToList();
        }

        // If we don't have variant inventory units loaded, return empty - infra should implement query.
        return Enumerable.Empty<InventoryUnit>();
    }

    /// <summary>
    /// Try to fill backordered inventory units when new stock arrives.
    /// Simplified: mark available backordered units as on-hand until 'number' is exhausted.
    /// Real implementation should support splitting units and background jobs.
    /// </summary>
    private void ProcessBackorders(int number)
    {
        if (number <= 0) return;

        var units = BackorderedInventoryUnits().Take(number).ToList();
        foreach (var unit in units)
        {
            if (number <= 0) break;

            // Simplified behavior:
            // - If unit.Quantity > number we mark the unit as on-hand (best-effort).
            // - Real systems may split units; that belongs to infra/services.
            unit.MarkOnHand();
            number -= unit.Quantity;
        }
    }

    /// <summary>
    /// Touch variant when inventory changes in ways that matter.
    /// Infra-specific config (binary inventory cache) is not modelled here; we always touch for simplicity.
    /// </summary>
    private void ConditionalVariantTouch(int previousCountOnHand)
    {
        try
        {
            // touch variant if stock moved from/to zero or if variant exists
            if (Variant != null)
            {
                var wentToOrFromZero = (previousCountOnHand == 0 && CountOnHand > 0) || (previousCountOnHand > 0 && CountOnHand == 0);
                if (wentToOrFromZero || true)
                    Variant.MarkAsUpdated();
            }
        }
        catch
        {
            // Guard: variant may be a proxy or not loaded — swallow to avoid domain errors.
        }
    }

    private static int CheckedAdd(int a, int b)
    {
        try
        {
            return checked(a + b);
        }
        catch (OverflowException)
        {
            return b > 0 ? int.MaxValue : int.MinValue;
        }
    }

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int MaxCountOnHand = int.MaxValue;
    }

    public static class Errors
    {
        public static Error StockLocationRequired => Error.Validation("StockItem.StockLocationRequired", "Stock location is required.");
        public static Error VariantRequired => Error.Validation("StockItem.VariantRequired", "Variant is required.");
        public static Error InvalidCountOnHand => Error.Validation("StockItem.InvalidCountOnHand", $"Count on hand must be between 0 and {Constraints.MaxCountOnHand}.");
        public static Error NotFound(Guid id) => Error.NotFound("StockItem.NotFound", $"StockItem with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(Guid stockLocationId, Guid variantId, int? countOnHand)
    {
        var errors = new List<Error>();
        if (stockLocationId == Guid.Empty) errors.Add(Errors.StockLocationRequired);
        if (variantId == Guid.Empty) errors.Add(Errors.VariantRequired);
        if (countOnHand.HasValue && (countOnHand < 0 || countOnHand > Constraints.MaxCountOnHand)) errors.Add(Errors.InvalidCountOnHand);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid StockItemId) : DomainEvent;
        public record Updated(Guid StockItemId) : DomainEvent;
        public record Deleted(Guid StockItemId) : DomainEvent;
    }

    #endregion
}
