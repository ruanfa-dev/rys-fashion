using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Simplified Taxonomy domain model (corresponds to Spree::Taxonomy).
/// Added domain helpers to mirror Spree callbacks: ensure root taxon and sync root name when taxonomy name changes.
/// Application layer / repositories must call <see cref="EnsureRootExists"/> after creating a taxonomy
/// and call <see cref="SyncRootNameIfChanged(string)"/> when persisting updates (passing prior name).
/// </summary>
public partial class Taxonomy : AuditableEntity
{
    public string Name { get; set; } = default!;
    public Guid StoreId { get; set; }

    // acts_as_list -> position on taxonomy list
    public int? Position { get; set; }

    public ICollection<Taxon> Taxons { get; set; } = new List<Taxon>();

    /// <summary>
    /// Root taxon is the taxon without a parent for this taxonomy (best-effort computed).
    /// </summary>
    public Taxon? Root => Taxons.FirstOrDefault(t => t.ParentId == null);

    private Taxonomy() { }

    public static Taxonomy Create(string name, Guid storeId)
        => new() { Name = name?.Trim() ?? string.Empty, StoreId = storeId };

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var previous = Name;
        Name = name.Trim();
        MarkAsUpdated();

        // Caller (application/repository) may call SyncRootNameIfChanged(previous) to mirror Spree after_update callback.
    }

    /// <summary>
    /// Ensure a root taxon exists for this taxonomy.
    /// Call this after a taxonomy has been created/persisted by application layer.
    /// If root is missing a new Taxon will be created (in-memory) and added to Taxons.
    /// The repository is responsible for persisting the created Taxon.
    /// </summary>
    public void EnsureRootExists()
    {
        if (Root != null) return;

        var root = Taxon.Create(Id, Name, parentId: null);
        Taxons.Add(root);
        MarkAsUpdated();
    }

    /// <summary>
    /// If taxonomy name changed, ensure the root taxon name remains in sync.
    /// Call from application layer after persisting taxonomy updates and passing the previous name.
    /// </summary>
    public void SyncRootNameIfChanged(string? previousName)
    {
        if (string.Equals(previousName, Name, StringComparison.Ordinal)) return;

        var root = Root;
        if (root == null) return;

        if (!string.Equals(root.Name, Name, StringComparison.Ordinal))
        {
            root.Update(Name);
            MarkAsUpdated();
        }
    }

    #region Validation / Errors / Constraints

    public static class Constraints
    {
        public const int NameMaxLength = 255;
    }

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("Taxonomy.NameRequired", "Taxonomy name is required.");
        public static Error StoreRequired => Error.Validation("Taxonomy.StoreRequired", "Store is required.");
        public static Error NameTooLong => Error.Validation("Taxonomy.NameTooLong", $"Name must be at most {Constraints.NameMaxLength} characters.");
        public static Error NotFound(Guid id) => Error.NotFound("Taxonomy.NotFound", $"Taxonomy with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name, Guid storeId)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        else if (name!.Length > Constraints.NameMaxLength) errors.Add(Errors.NameTooLong);
        if (storeId == Guid.Empty) errors.Add(Errors.StoreRequired);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid TaxonomyId) : DomainEvent;
        public record Updated(Guid TaxonomyId) : DomainEvent;
        public record Deleted(Guid TaxonomyId) : DomainEvent;
    }

    #endregion
}
