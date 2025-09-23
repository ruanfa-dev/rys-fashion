using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Catalogs;

/// <summary>
/// Domain representation of Spree::Taxon / Spree::Taxonomy concepts (simplified).
/// Focus: names, permalink (slug), parent/children nesting, taxonomy relationship,
/// product & prototype joins, basic helpers and domain errors/events.
/// Persistence-specific behavior (attachments, translations, nested-set mechanics,
/// background re-generation, touch/async deletes, search integration) belong to
/// application/infrastructure layers and are intentionally omitted here.
/// </summary>
public partial class Taxon : AuditableEntity
{
    #region Properties

    public string Name { get; set; } = default!;
    public string? PrettyName { get; set; }
    public string? Description { get; set; }
    public string? Permalink { get; set; } // slug
    public bool HideFromNav { get; set; }
    public bool Automatic { get; set; }
    public string? RulesMatchPolicy { get; set; } // e.g. "all" or "any"
    public string? SortOrder { get; set; } // e.g. "manual", "name-a-z", etc.

    // Nested-set placeholders (left/right) - kept for possible EF mapping if needed
    public int? Lft { get; set; }
    public int? Rgt { get; set; }

    #endregion

    #region Relationships

    public Guid? ParentId { get; set; }
    public Taxon? Parent { get; set; }
    public ICollection<Taxon> Children { get; set; } = new List<Taxon>();

    public Guid TaxonomyId { get; set; }
    public Taxonomy Taxonomy { get; set; } = default!;

    // Joins
    public ICollection<PrototypeTaxon> PrototypeTaxons { get; set; } = new List<PrototypeTaxon>();
    public IEnumerable<Prototype> Prototypes => PrototypeTaxons.Select(pt => pt.Prototype);

    public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
    public IEnumerable<Product> Products => Classifications.Select(c => c.Product);

    #endregion

    #region Constructors / Factory

    public Taxon() { }

    public static Taxon Create(Guid taxonomyId, string name, Guid? parentId = null)
    {
        return new Taxon
        {
            TaxonomyId = taxonomyId,
            Name = name,
            ParentId = parentId,
            HideFromNav = false,
            Automatic = false,
            RulesMatchPolicy = "all",
            SortOrder = "manual"
        };
    }

    public void Update(string? name = null, string? description = null, bool? hideFromNav = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name!;
        if (description != null) Description = description;
        if (hideFromNav.HasValue) HideFromNav = hideFromNav.Value;
    }

    #endregion

    #region Utilities (slug / pretty name / regeneration helpers)

    public bool IsRoot() => ParentId == null;

    public void SetPrettyName() => PrettyName = GeneratePrettyName();

    public string GeneratePrettyName()
    {
        if (Parent != null && !string.IsNullOrWhiteSpace(Parent.PrettyName))
            return string.Join(" -> ", Parent.PrettyName, Name);
        return Name;
    }

    public void SetPermalink()
    {
        Permalink = GenerateSlug();
    }

    public string GenerateSlug()
    {
        var slug = Slugify(Name);

        if (Parent != null && !string.IsNullOrWhiteSpace(Parent.Permalink))
        {
            // Join parent permalink and last segment
            var last = slug;
            return $"{Parent.Permalink.TrimEnd('/')}/{last}";
        }

        return slug;
    }

    private static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        // Normalize (remove diacritics), lowercase, remove invalid chars, replace spaces w/ dash
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", "-");
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9\-_\/]", string.Empty); // allow slash for nested permalink
        cleaned = cleaned.Trim('-', '_', '/');
        return cleaned;
    }

    #endregion

    #region Caching helpers

    // Simple in-memory process-level cache to approximate Rails.cache.fetch behavior used in Spree.
    private static readonly ConcurrentDictionary<string, object> _cache = new();

    /// <summary>
    /// Returns cached list of this taxon and descendant ids.
    /// Cache key includes UpdatedAt to invalidate when changed.
    /// Application layer should call SaveChanges that will update UpdatedAt and therefore key.
    /// </summary>
    public IReadOnlyList<Guid> CachedSelfAndDescendantsIds()
    {
        var version = UpdatedAt?.UtcTicks ?? CreatedAt.UtcTicks;
        var key = $"taxon-descendants:{Id}:{version}";

        if (_cache.TryGetValue(key, out var obj) && obj is IReadOnlyList<Guid> ids)
            return ids;

        var idsList = GetSelfAndDescendantsIds();
        _cache[key] = idsList;
        return idsList;
    }

    private IReadOnlyList<Guid> GetSelfAndDescendantsIds()
    {
        // If children not loaded, this returns only self.
        var collected = new List<Guid> { Id };
        foreach (var child in Children)
        {
            collected.AddRange(child.GetSelfAndDescendantsIds());
        }
        return collected;
    }

    #endregion

    #region Touch / sync helpers

    /// <summary>
    /// Mark ancestors and taxonomy as updated. Application layer should persist changes.
    /// </summary>
    public void TouchAncestorsAndTaxonomy()
    {
        // Mark ancestors UpdatedAt
        var ancestor = Parent;
        var updatedAt = DateTimeOffset.UtcNow;
        while (ancestor != null)
        {
            ancestor.UpdatedAt = updatedAt;
            ancestor = ancestor.Parent;
        }

        try
        {
            Taxonomy.UpdatedAt = updatedAt;
        }
        catch
        {
            // Taxonomy may be null in some usage scenarios in tests; swallow to avoid domain errors.
        }
    }

    #endregion

    #region Validation constraints & errors

    public static class Constraints
    {
        public const int NameMaxLength = 255;
        public const int NameMinLength = 1;
        public const int MetaMaxLength = 255;
        public const int PermalinkMaxLength = 255;
    }

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("Taxon.NameRequired", "Taxon name is required.");
        public static Error InvalidNameLength => Error.Validation("Taxon.InvalidNameLength", $"Taxon name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters.");
        public static Error InvalidMetaLength => Error.Validation("Taxon.InvalidMetaLength", $"Meta fields must be at most {Constraints.MetaMaxLength} characters.");
        public static Error TaxonomyRequired => Error.Validation("Taxon.TaxonomyRequired", "Taxonomy is required.");
        public static Error ParentMustBelongToSameTaxonomy => Error.Validation("Taxon.ParentTaxonomyMismatch", "Parent taxon must belong to the same taxonomy.");
        public static Error RootConflict => Error.Validation("Taxon.RootConflict", "This taxonomy already has a root taxon.");
        public static Error NotFound(Guid id) => Error.NotFound("Taxon.NotFound", $"Taxon with ID '{id}' was not found.");
    }

    /// <summary>
    /// Lightweight validation useful for handlers or tests.
    /// </summary>
    public static List<Error> ValidateModel(string? name, string? metaTitle, string? metaDescription)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add(Errors.NameRequired);
        else
        {
            if (name.Length < Constraints.NameMinLength || name.Length > Constraints.NameMaxLength)
                errors.Add(Errors.InvalidNameLength);
        }

        if (!string.IsNullOrEmpty(metaTitle) && metaTitle!.Length > Constraints.MetaMaxLength)
            errors.Add(Errors.InvalidMetaLength);
        if (!string.IsNullOrEmpty(metaDescription) && metaDescription!.Length > Constraints.MetaMaxLength)
            errors.Add(Errors.InvalidMetaLength);

        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid TaxonId) : DomainEvent;
        public record Updated(Guid TaxonId) : DomainEvent;
        public record Deleted(Guid TaxonId) : DomainEvent;
    }

    #endregion
}

