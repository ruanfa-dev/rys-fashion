using Core.Stores;

using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;
using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Core.Catalog.Taxonomies;

/// <summary>
/// Ported Taxonomy entity from Spree::Taxonomy
/// - supports translations via `Translations` collection
/// - holds taxons hierarchy and belongs to a store
/// - contains helper constants used by admin/search layers
/// </summary>
public sealed class Taxonomy : AuditableEntity, IMetadataSupport, ITranslatable<TaxonomyTranslation>
{
    #region Properties

    public string Name { get; set; } = null!;

    // acts_as_list ordering
    public int Position { get; set; }

    // Store association (scope for uniqueness)
    public Guid StoreId { get; set; }
    public Store? Store { get; set; }

    // Taxons (children nodes)
    public ICollection<Taxon> Taxons { get; set; } = new List<Taxon>();

    public ICollection<TaxonomyTranslation> Translations { get; set; } = new List<TaxonomyTranslation>();
    public IDictionary<string, string?>? PublicMetadata { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?>? PrivateMetadata { get; set; } = new Dictionary<string, string?>();

    public IReadOnlyCollection<string> TranslatableFields => new[] { nameof(Name) };

    #endregion

    #region Constraints

    public static class Constraints
    {
        public const int NameMinLength = 1;
        public const int NameMaxLength = 255;

        public const int PositionMin = 0;
        public const int PositionMax = 100000;
    }

    #endregion

    #region Errors

    public static class Errors
    {
        // Validation:
        public static Error NameRequired => Error.Validation("Taxonomy.NameRequired", "Taxonomy name is required.");
        public static Error InvalidNameLength => Error.Validation("Taxonomy.InvalidNameLength", $"Taxonomy name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters long.");
       
        public static Error InvalidPosition => Error.Validation("Taxonomy.InvalidPosition", $"Position must be between {Constraints.PositionMin} and {Constraints.PositionMax}.");

        public static Error StoreRequired => Error.Validation("Taxonomy.StoreRequired", "Store is required for a taxonomy.");
       
        public static Error RootCreationFailed => Error.Failure("Taxonomy.RootCreationFailed", "Unable to create root taxon for taxonomy.");
        public static Error NotFound(Guid id) => Error.NotFound("Taxonomy.NotFound", $"Taxonomy with ID '{id}' was not found.");
        public static Error NameAlreadyExists(string name) => Error.Conflict("Taxonomy.NameAlreadyExists", $"A taxonomy with the name '{name}' already exists.");
        public static Error HasDependentTaxons => Error.Validation("Taxonomy.HasDependentTaxons", "Cannot delete taxonomy while taxons have children. Remove or reassign children first.");
        public static Error HasClassifications => Error.Validation("Taxonomy.HasClassifications", "Cannot delete taxonomy while taxons have product classifications. Remove classifications before deleting the taxonomy.");
        public static Error UnexpectedError(string operationName, Exception? ex = null) => Error.Unexpected($"Taxonomy.Unexpected.{operationName}", ex?.Message ?? "An unexpected error occurred in taxonomy operation.");
    }

    #endregion

    #region Constructors

    Taxonomy() { }

    #endregion

    #region Factory

    public static ErrorOr<Taxonomy> Create(string name, Guid storeId, int position = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) return Errors.NameRequired;
        var trimmed = name.Trim();
        if (trimmed.Length < Constraints.NameMinLength || trimmed.Length > Constraints.NameMaxLength)
            return Errors.InvalidNameLength;

        // TODO: template uniqueness check requires repository access; move to service layer
        //if (storeId == Guid.Empty) return Errors.StoreRequired;

        var taxonomy = new Taxonomy
        {
            Name = trimmed,
            StoreId = storeId,
            Position = Math.Max(0, position)
        };

        // create root taxon immediately so callers have it available (mimics after_create :set_root)
        var rootResult = Taxon.Create(trimmed, taxonomy.Id, parentId: null);
        if (rootResult.IsError)
        {
            return Errors.InvalidNameLength;
        }

        taxonomy.Taxons.Add(rootResult.Value);
        taxonomy.AddDomainEvent(new Events.Created(taxonomy.Id));
        return taxonomy;
    }

    #endregion

    #region Behavior

    public ErrorOr<Taxonomy> Update(string? name = null, int? position = null)
    {
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != Name)
        {
            var trimmed = name.Trim();
            if (trimmed.Length < Constraints.NameMinLength || trimmed.Length > Constraints.NameMaxLength)
                return Errors.InvalidNameLength;

            Name = trimmed;
            changed = true;
        }

        if (position.HasValue && position.Value != Position)
        {
            Position = Math.Max(0, position.Value);
            changed = true;
        }

        if (changed)
        {
            MarkAsUpdated();
            AddDomainEvent(new Events.Updated(Id));
        }

        // If there's a root taxon in-memory, keep its name synchronized with taxonomy (mirrors Rails after_update behavior)
        var root = Root;
        if (root != null && root.Name != Name)
        {
            var _ = root.Update(Name);
            // root.Update will emit its own Updated event; persistence is left to the caller
        }

        return this;
    }

    /// <summary>
    /// Business validation before deleting a taxonomy.
    /// Prevent deletion if there are taxons (aside from root) or if root has classifications.
    /// </summary>
    public ErrorOr<Deleted> Delete()
    {
        // If there are any taxons beyond the root (child taxons), disallow deletion
        // Tests may add child taxons to the Taxons collection without setting ParentId, so also check count > 1
        if (Taxons.Count > 1 || Taxons.Any(t => t.ParentId != null))
            return Errors.HasDependentTaxons;

        // If any taxon (including root) has classifications, disallow
        if (Taxons.SelectMany(t => t.Classifications).Any())
            return Errors.HasClassifications;

        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }

    // Expose root taxon (has_one :root in Rails)
    public Taxon? Root => Taxons.FirstOrDefault(t => t.ParentId == null);

    /// <summary>
    /// Ensures the taxonomy has a root taxon. If missing, returns a new root taxon instance (not persisted).
    /// </summary>
    public ErrorOr<Taxon> EnsureRoot()
    {
        var root = Root;
        if (root != null) return root;

        var res = Taxon.Create(Name, Id);
        if (res.IsError) return Errors.RootCreationFailed;

        var newRoot = res.Value;
        Taxons.Add(newRoot);
        return newRoot;
    }

    /// <summary>
    /// Helper that mimics Rails' after_create :set_root behavior.
    /// Ensures a root taxon exists for this taxonomy. Safe to call multiple times.
    /// </summary>
    public ErrorOr<Taxon> SetRoot()
    {
        return EnsureRoot();
    }

    /// <summary>
    /// Applies the default ordering used by the Rails default_scope: position then created_at.
    /// Use in repositories to consistently order taxonomies.
    /// </summary>
    public static IQueryable<Taxonomy> ApplyDefaultOrdering(IQueryable<Taxonomy> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return query.OrderBy(t => t.Position).ThenBy(t => t.CreatedAt);
    }

    /// <summary>
    /// Helper to get translated value for a named field. Uses the ITranslatable<T> extension helpers.
    /// </summary>
    public string? GetTranslatedName(string? locale = null, bool fallback = true)
    {
        return this.GetField("Name", locale ?? string.Empty, fallback);
    }

    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid TaxonomyId) : DomainEvent;
        public record Updated(Guid TaxonomyId) : DomainEvent;
        public record Deleted(Guid TaxonomyId) : DomainEvent;
    }

    #endregion
}
