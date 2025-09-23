using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model port of Spree::StockMovement (simplified).
/// - Records a change in inventory for a StockItem.
/// - The application/infrastructure layer is responsible for persisting movements and calling <see cref="ApplyToStockItem"/> after create.
/// </summary>
public sealed class StockMovement : AuditableEntity
{
    // Mirrors Ruby QUANTITY_LIMITS
    public static class QUANTITY_LIMITS
    {
        public const int Max = int.MaxValue;      // 2**31 - 1
        public const int Min = int.MinValue;      // -2**31
    }

    public Guid StockItemId { get; set; }
    public StockItem? StockItem { get; set; }

    // Polymorphic originator (type name + id). Infra may map this to concrete FK relations.
    public Guid? OriginatorId { get; set; }
    public string? OriginatorType { get; set; }

    // Quantity delta (positive = restock, negative = unstock)
    public int Quantity { get; set; }

    private StockMovement() { }

    public static StockMovement Create(Guid stockItemId, int quantity, object? originator = null)
    {
        var sm = new StockMovement
        {
            StockItemId = stockItemId,
            Quantity = quantity
        };

        if (originator is Guid guid) sm.OriginatorId = guid;
        else if (originator != null)
        {
            // best-effort: try to read Id property from originator
            var idProp = originator.GetType().GetProperty("Id");
            if (idProp != null && idProp.GetValue(originator) is Guid id) sm.OriginatorId = id;
            sm.OriginatorType = originator.GetType().Name;
        }

        return sm;
    }

    /// <summary>
    /// Apply the movement to the related stock item.
    /// Should be invoked by application code after the movement is persisted (mirrors after_create callback).
    /// Will only change stock when the variant/stock item indicates inventory should be tracked.
    /// </summary>
    public void ApplyToStockItem()
    {
        if (StockItem == null) return;

        // Prefer variant-level tracking flag if available.
        var variantTracks = StockItem.Variant?.TrackInventory ?? true;

        if (!variantTracks)
            return;

        StockItem.AdjustCountOnHand(Quantity);
    }

    /// <summary>
    /// Computes the minimum allowed quantity for this movement depending on backorderability.
    /// Mirrors Ruby's min_quantity logic.
    /// </summary>
    public int MinQuantity()
    {
        if (StockItem == null || StockItem.Backorderable) return QUANTITY_LIMITS.Min;

        // cannot decrease more than current on-hand
        return -StockItem.CountOnHand;
    }

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int MaxQuantity = QUANTITY_LIMITS.Max;
        public const int MinQuantity = QUANTITY_LIMITS.Min;
    }

    public static class Errors
    {
        public static Error StockItemRequired => Error.Validation("StockMovement.StockItemRequired", "StockItem is required.");
        public static Error QuantityOutOfRange => Error.Validation("StockMovement.QuantityOutOfRange", $"Quantity must be between {Constraints.MinQuantity} and {Constraints.MaxQuantity}.");
        public static Error NotFound(Guid id) => Error.NotFound("StockMovement.NotFound", $"StockMovement with ID '{id}' was not found.");
    }

    /// <summary>
    /// Lightweight validation helper. If stockItem is available, min/max will respect its state (backorderable/count_on_hand).
    /// </summary>
    public static List<Error> ValidateModel(Guid stockItemId, int quantity, StockItem? stockItem = null)
    {
        var errors = new List<Error>();

        if (stockItemId == Guid.Empty) errors.Add(Errors.StockItemRequired);

        var min = stockItem == null || stockItem.Backorderable ? Constraints.MinQuantity : -stockItem.CountOnHand;
        var max = Constraints.MaxQuantity;

        if (quantity < min || quantity > max) errors.Add(Errors.QuantityOutOfRange);

        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid StockMovementId) : DomainEvent;
        public record Deleted(Guid StockMovementId) : DomainEvent;
    }

    #endregion
}
