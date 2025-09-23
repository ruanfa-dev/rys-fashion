using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Domain port of Spree::StoreCreditCategory (simplified).
/// - Lightweight behaviour: non-expiring detection, safe-delete helpers and basic validation.
/// - Repository/infra is responsible for persistence, uniqueness and callbacks.
/// </summary>
public sealed class StoreCreditCategory : AuditableEntity
{
    public const string GIFT_CARD_CATEGORY_NAME = "Gift Card";
    private static readonly string[] DEFAULT_NON_EXPIRING_TYPES = new[] { GIFT_CARD_CATEGORY_NAME };

    public string Name { get; set; } = default!;

    private StoreCreditCategory() { }

    public static StoreCreditCategory Create(string name)
        => new StoreCreditCategory { Name = name?.Trim() ?? string.Empty };

    public void Update(string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name!.Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Returns whether this category is considered non-expiring.
    /// Merges the framework defaults with a runtime configuration list provided by the caller (infra/app).
    /// </summary>
    public bool NonExpiring(IEnumerable<string>? configuredNonExpiringTypes = null)
    {
        var set = NonExpiringCategoryTypes(configuredNonExpiringTypes);
        return set.Contains(Name);
    }

    /// <summary>
    /// Returns the set of category type names that are treated as non-expiring.
    /// Caller should provide application configuration values when available.
    /// </summary>
    public ISet<string> NonExpiringCategoryTypes(IEnumerable<string>? configuredNonExpiringTypes = null)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in DEFAULT_NON_EXPIRING_TYPES) set.Add(t);
        if (configuredNonExpiringTypes != null)
        {
            foreach (var t in configuredNonExpiringTypes.Where(s => !string.IsNullOrWhiteSpace(s))) set.Add(t.Trim());
        }
        return set;
    }

    /// <summary>
    /// Best-effort check if any StoreCredit references this category using provided collection.
    /// Repository/DB should be used for large datasets.
    /// </summary>
    public bool StoreCreditCategoryUsed(IEnumerable<StoreCredit>? storeCredits)
    {
        if (storeCredits == null) return false;
        return storeCredits.Any(sc => sc.CategoryId == this.Id);
    }

    /// <summary>
    /// Lightweight validation to be used before deletion. Returns errors when category is in use.
    /// </summary>
    public List<Error> ValidateNotUsedForDelete(IEnumerable<StoreCredit>? storeCredits)
    {
        var errors = new List<Error>();
        if (StoreCreditCategoryUsed(storeCredits))
            errors.Add(Errors.CannotDeleteWhenUsed);
        return errors;
    }

    /// <summary>
    /// Convenience: whether this category can be deleted given existing store credits.
    /// </summary>
    public bool CanBeDeleted(IEnumerable<StoreCredit>? storeCredits) => !StoreCreditCategoryUsed(storeCredits);

    /// <summary>
    /// Class-level helper to pick a default reimbursement category from an in-memory sequence.
    /// Application/infra should provide DB-backed lookup when necessary.
    /// </summary>
    public static StoreCreditCategory? DefaultReimbursementCategory(IEnumerable<StoreCreditCategory>? categories)
        => categories?.FirstOrDefault();

    #region Validation / Errors

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("StoreCreditCategory.NameRequired", "Name is required.");
        public static Error CannotDeleteWhenUsed => Error.Validation("StoreCreditCategory.CannotDeleteWhenUsed", "Cannot delete category while it is used by store credits.");
        public static Error NotFound(Guid id) => Error.NotFound("StoreCreditCategory.NotFound", $"StoreCreditCategory with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid StoreCreditCategoryId) : DomainEvent;
        public record Updated(Guid StoreCreditCategoryId) : DomainEvent;
        public record Deleted(Guid StoreCreditCategoryId) : DomainEvent;
    }

    #endregion
}