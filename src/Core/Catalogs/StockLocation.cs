using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model port of Spree::StockLocation (simplified).
/// - Tracks address fields, propagation and convenience helpers to work with StockItem/Variant.
/// - Persistence/cascades/jobs/backgound tasks belong to application/infrastructure layers.
/// </summary>
public sealed class StockLocation : AuditableEntity
{
    #region Properties

    public string Name { get; set; } = default!;
    public string? AdminName { get; set; }

    public bool Default { get; set; }
    public bool Active { get; set; } = true;

    // Address fields (mirrors Spree attributes used by Address() helper)
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Company { get; set; }
    public string? City { get; set; }
    public Guid? StateId { get; set; }
    public string? StateName { get; set; }
    public Guid CountryId { get; set; }
    public string? Zipcode { get; set; }
    public string? Phone { get; set; }

    // Propagation / defaults
    public bool PropagateAllVariants { get; set; }
    public bool BackorderableDefault { get; set; }

    #endregion

    #region Navigations

    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public IEnumerable<Variant> Variants => StockItems.Select(si => si.Variant).Where(v => v != null).ToList();
    public IEnumerable<StockMovement> StockMovements => StockItems.SelectMany(si => si.StockMovements).ToList();

    public Guid? StateRefId { get; set; }
    public State? State { get; set; }
    public Country Country { get; set; } = default!;

    #endregion

    #region Constructors / Factory

    private StockLocation() { }

    public static StockLocation Create(string name, Guid countryId, bool propagateAllVariants = false, bool backorderableDefault = false)
    {
        return new StockLocation
        {
            Name = name?.Trim() ?? string.Empty,
            CountryId = countryId,
            PropagateAllVariants = propagateAllVariants,
            BackorderableDefault = backorderableDefault
        };
    }

    public void Update(string? name = null, bool? active = null, bool? propagateAll = null, bool? backorderableDefault = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name!.Trim();
        if (active.HasValue) Active = active.Value;
        if (propagateAll.HasValue) PropagateAllVariants = propagateAll.Value;
        if (backorderableDefault.HasValue) BackorderableDefault = backorderableDefault.Value;
        MarkAsUpdated();
    }

    #endregion

    #region Stock item helpers

    /// <summary>
    /// Wrapper for creating a new stock item respecting the backorderable config.
    /// Application should persist returned StockItem.
    /// </summary>
    public StockItem PropagateVariant(Variant variant)
    {
        var existing = StockItemFor(variant.Id);
        if (existing != null) return existing;

        var created = StockItem.Create(this.Id, variant.Id, initialCount: 0, backorderable: BackorderableDefault);
        StockItems.Add(created);
        MarkAsUpdated();
        return created;
    }

    /// <summary>
    /// Return a stock item for the given variant id or null if none.
    /// </summary>
    public StockItem? StockItemFor(Guid variantId)
        => StockItems.Where(si => si.VariantId == variantId).OrderBy(si => si.Id).FirstOrDefault();

    /// <summary>
    /// Return an existing stock item or create one when missing (by Variant instance).
    /// </summary>
    public StockItem StockItemOrCreate(Variant variant) => StockItemFor(variant.Id) ?? PropagateVariant(variant);

    /// <summary>
    /// Return an existing stock item or create one when missing (by variant id).
    /// </summary>
    public StockItem StockItemOrCreate(Guid variantId)
    {
        var existing = StockItemFor(variantId);
        if (existing != null) return existing;

        // If we don't have a Variant entity available, create a stock item for the id.
        var created = StockItem.Create(this.Id, variantId, initialCount: 0, backorderable: BackorderableDefault);
        StockItems.Add(created);
        MarkAsUpdated();
        return created;
    }

    /// <summary>
    /// Alias for stock_item_or_create in Ruby.
    /// </summary>
    public StockItem SetUpStockItem(Variant variant) => StockItemOrCreate(variant);

    /// <summary>
    /// Checks whether this stock location tracks the given variant.
    /// </summary>
    public bool Stocks(Variant variant) => StockItems.Any(si => si.VariantId == variant.Id);

    /// <summary>
    /// Returns count on hand for a variant (null when missing).
    /// </summary>
    public int? CountOnHand(Variant variant) => StockItemFor(variant.Id)?.CountOnHand;

    /// <summary>
    /// Whether the variant at this location is backorderable.
    /// </summary>
    public bool Backorderable(Variant variant) => StockItemFor(variant.Id)?.Backorderable ?? false;

    /// <summary>
    /// Restock a variant at this location (positive quantity), or unstock (negative).
    /// If persist==true the domain will also record a StockMovement and adjust count on hand.
    /// </summary>
    public void Move(Variant variant, int quantity, object? originator = null, bool persist = true)
    {
        var item = StockItemOrCreate(variant);

        if (persist)
        {
            var movement = StockMovement.Create(item.Id, quantity, originator);
            item.StockMovements.Add(movement);
            item.AdjustCountOnHand(quantity);
        }
        else
        {
            var movement = StockMovement.Create(item.Id, quantity, originator);
            item.StockMovements.Add(movement);
        }

        MarkAsUpdated();
    }

    public void Restock(Variant variant, int quantity, object? originator = null, bool persist = true) => Move(variant, quantity, originator, persist);

    public void RestockBackordered(Variant variant, int quantity)
    {
        var item = StockItemOrCreate(variant);
        item.SetCountOnHand(item.CountOnHand + quantity);
        MarkAsUpdated();
    }

    public void Unstock(Variant variant, int quantity, object? originator = null, bool persist = true) => Move(variant, -quantity, originator, persist);

    /// <summary>
    /// Compute how many units can be taken on-hand and how many will be backordered for the requested quantity.
    /// Returns tuple (onHand, backordered).
    /// </summary>
    public (int onHand, int backordered) FillStatus(Variant variant, int quantity)
    {
        var item = StockItemOrCreate(variant);
        if (item == null) return (0, 0);

        var available = Math.Max(0, item.CountOnHand);
        if (available >= quantity) return (quantity, 0);

        var onHand = available;
        var backordered = item.Backorderable ? Math.Max(0, quantity - onHand) : 0;
        return (onHand, backordered);
    }

    #endregion

    #region Maintenance helpers (to be called by application layer)

    /// <summary>
    /// Called after creation when PropagateAllVariants == true; application should schedule background job.
    /// </summary>
    public void CreateStockItemsIfNeeded()
    {
        if (!PropagateAllVariants) return;
        // infra should schedule a job to create stock items for all known variants.
        AddDomainEvent(new Events.PropagateAllVariantsRequested(Id));
    }

    /// <summary>
    /// Ensure only one stock location has Default == true. Application/repository should enforce via a transaction.
    /// </summary>
    public void EnsureOneDefault(IEnumerable<StockLocation> otherLocations)
    {
        if (!Default) return;

        foreach (var loc in otherLocations.Where(l => l.Default && l.Id != this.Id))
        {
            loc.Default = false;
            loc.MarkAsUpdated();
        }
    }

    /// <summary>
    /// Called when Active flag changes to conditionally touch related records.
    /// Infra should persist timestamps efficiently (bulk update).
    /// </summary>
    public void ConditionalTouchRecords(bool activeChanged)
    {
        if (!activeChanged) return;

        foreach (var si in StockItems) si.MarkAsUpdated();
        foreach (var v in Variants) v.MarkAsUpdated();
    }

    #endregion

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int NameMaxLength = 255;
    }

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("StockLocation.NameRequired", "Stock location name is required.");
        public static Error CountryRequired => Error.Validation("StockLocation.CountryRequired", "Country is required.");
        public static Error NotFound(Guid id) => Error.NotFound("StockLocation.NotFound", $"StockLocation with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name, Guid countryId)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        if (countryId == Guid.Empty) errors.Add(Errors.CountryRequired);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid StockLocationId) : DomainEvent;
        public record Updated(Guid StockLocationId) : DomainEvent;
        public record Deleted(Guid StockLocationId) : DomainEvent;
        public record PropagateAllVariantsRequested(Guid StockLocationId) : DomainEvent;
    }

    #endregion
}
