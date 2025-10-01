using System.ComponentModel.DataAnnotations.Schema;

using Core.Catalog.Products;
using Core.Catalog.Prototypes;
using Core.Promotions;

using ErrorOr;

using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.TranslatableResource;
using SharedKernel.Domain.Primitives;
using SharedKernel.Extensions.Text;
using SharedKernel.Messaging;

namespace Core.Catalog.Taxonomies;

/// <summary>
/// Domain model for a Taxon (category node) following DDD principles.
/// Combines hierarchical structure, automatic rule processing, and rich business behavior.
/// </summary>
public sealed class Taxon : AuditableEntity, IMetadataSupport, ITranslatable<TaxonTranslation>
{
    #region Constraints

    public static class Constraints
    {
        public const int NameMinLength = 1;
        public const int NameMaxLength = 255;
        public const int PrettyNameMaxLength = 500;
        public const int DescriptionMaxLength = 2000;
        public const int PermalinkMaxLength = 500;
        public const int PositionMin = 0;
        public const int PositionMax = 999999;
        public const int DepthMin = 0;
        public const int DepthMax = 20;
        public static readonly string[] RulesMatchPolicies = { "all", "any" };
        public static readonly string[] SortOrders = {
            "manual", "best-selling", "name-a-z", "name-z-a",
            "price-high-to-low", "price-low-to-high", "newest-first", "oldest-first"
        };
        public static readonly string[] ValidImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
    }

    #endregion

    #region Errors

    public static class Errors
    {
        // ID validations
        public static Error IdRequired => Error.Validation("Taxon.IdRequired", "Taxon ID is required.");
        public static Error InvalidId => Error.Validation("Taxon.InvalidId", "Taxon ID must be a valid GUID.");

        // Name validations
        public static Error NameRequired => Error.Validation("Taxon.NameRequired", "Taxon name is required.");
        public static Error InvalidNameLength => Error.Validation(
            "Taxon.InvalidNameLength",
            $"Taxon name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters."
        );

        // Taxonomy validations
        public static Error TaxonomyRequired => Error.Validation(
            "Taxon.TaxonomyRequired",
            "Taxonomy is required.");
        public static Error InvalidTaxonomyId => Error.Validation("Taxon.InvalidTaxonomyId", "Taxonomy ID must be a valid GUID.");

        // Rules match policy validations
        public static Error InvalidRulesMatchPolicy => Error.Validation(
            "Taxon.InvalidRulesMatchPolicy",
            $"Rules match policy must be one of: {string.Join(", ", Constraints.RulesMatchPolicies)}."
        );

        // Sort order validations
        public static Error InvalidSortOrder => Error.Validation(
            "Taxon.InvalidSortOrder",
            $"Sort order must be one of: {string.Join(", ", Constraints.SortOrders)}."
        );
        public static Error DescriptionTooLong => Error.Validation(
            "Taxon.DescriptionTooLong",
            $"Description cannot exceed {Constraints.DescriptionMaxLength} characters."
        );
        public static Error InvalidPosition => Error.Validation(
            "Taxon.InvalidPosition",
            $"Position must be between {Constraints.PositionMin} and {Constraints.PositionMax}."
        );

        // Hierarchy validations
        public static Error SelfParent => Error.Validation("Taxon.SelfParent", "Taxon cannot be its own parent.");
        public static Error ParentTaxonomyMismatch => Error.Validation(
            "Taxon.ParentTaxonomyMismatch",
            "Parent must belong to the same taxonomy."
        );
        public static Error CircularReference => Error.Validation(
            "Taxon.CircularReference",
            "Cannot set parent that would create a circular reference."
        );
        public static Error RootConflict => Error.Validation(
            "Taxon.RootConflict",
            "This taxonomy already has a root taxon."
        );
        public static Error InvalidDepth => Error.Validation(
            "Taxon.InvalidDepth",
            $"Taxon depth must be between {Constraints.DepthMin} and {Constraints.DepthMax}."
        );
        public static Error InvalidNestedSetValues => Error.Validation(
            "Taxon.InvalidNestedSetValues",
            "Left value must be less than right value in nested set model."
        );
        public static Error InvalidImageContentType => Error.Validation(
            "Taxon.InvalidImageContentType",
            $"Image must be one of the following types: {string.Join(", ", Constraints.ValidImageExtensions)}."
        );

        public static Error InvalidSquareImageContentType => Error.Validation(
            "Taxon.InvalidSquareImageContentType",
            $"Square image must be one of the following types: {string.Join(", ", Constraints.ValidImageExtensions)}."
        );

        // Business rule validations
        public static Error HasChildren => Error.Validation(
            "Taxon.HasChildren",
            "Cannot delete taxon with children. Remove children first."
        );
        public static Error HasClassifications => Error.Validation(
            "Taxon.HasClassifications",
            "Cannot delete taxon with product classifications. Remove classifications first."
        );
        public static Error HasRules => Error.Validation(
            "Taxon.HasRules",
            "Cannot delete taxon with associated rules. Remove rules first."
        );
        public static Error HasPromotionRules => Error.Validation(
            "Taxon.HasPromotionRules",
            "Cannot delete taxon with associated promotion rules. Remove promotion rules first."
        );
        public static Error NullRule => Error.Validation("Taxon.NullRule", "Cannot add null rule.");
        public static Error AutomaticOnly => Error.Validation(
            "Taxon.AutomaticOnly",
            "Rules can only be added to automatic taxons."
        );
        public static Error NullProduct => Error.Validation("Taxon.NullProduct", "Cannot classify null product.");
        public static Error ProductAlreadyClassified => Error.Validation(
            "Taxon.ProductAlreadyClassified",
            "Product is already classified under this taxon."
        );

        // Not Found Errors
        public static Error NotFound(Guid id) => Error.NotFound(
            "Taxon.NotFound",
            $"Taxon with ID '{id}' was not found."
        );
        public static Error RuleNotFound => Error.NotFound("Taxon.RuleNotFound", "Rule not found.");
        public static Error ProductNotClassified => Error.NotFound(
            "Taxon.ProductNotClassified",
            "Product is not classified under this taxon."
        );

        // Conflict Errors
        public static Error NameAlreadyExists(string name, Guid taxonomyId) => Error.Conflict(
            "Taxon.NameAlreadyExists",
            $"A taxon with the name '{name}' already exists in this taxonomy."
        );
        public static Error PermalinkAlreadyExists(string permalink, Guid taxonomyId) => Error.Conflict(
            "Taxon.PermalinkAlreadyExists",
            $"A taxon with the permalink '{permalink}' already exists in this taxonomy."
        );

        // Unexpected Errors
        public static Error UnexpectedError(string operationName, Exception? ex = null) => Error.Unexpected(
            code: $"Taxon.{operationName}UnexpectedError",
            description: $"An unexpected error occurred during execution of {operationName} operation on Taxon. {ex?.Message}"
        );
    }

    #endregion

    #region Core Properties

    public string Name { get; set; } = null!;
    public string? PrettyName { get; set; }
    public string? Description { get; set; }
    public string Permalink { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public bool Automatic { get; set; }
    public string RulesMatchPolicy { get; set; } = "all";
    public string SortOrder { get; set; } = "manual";
    public bool HideFromNav { get; set; }
    public bool MarkedForRegenerateTaxonProducts { get; set; } = true;
    public string? ImageUrl { get; set; }
    public string? SquareImageUrl { get; set; }

    #endregion

    #region Hierarchy Properties

    public int Lft { get; set; }
    public int Rgt { get; set; }
    public int Depth { get; set; }
    public Guid? ParentId { get; set; }
    public Taxon? Parent { get; set; }
    public ICollection<Taxon> Children { get; set; } = new List<Taxon>();
    public int ChildIndex { get; set; }
    public Guid TaxonomyId { get; set; }
    public Taxonomy? Taxonomy { get; set; }

    #endregion

    #region Domain Associations

    public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
    public ICollection<TaxonRule> TaxonRules { get; set; } = new List<TaxonRule>();
    public ICollection<TaxonTranslation> Translations { get; set; } = new List<TaxonTranslation>();
    public ICollection<PrototypeTaxon> PrototypeTaxons { get; set; } = new List<PrototypeTaxon>();
    public ICollection<PromotionRuleTaxon> PromotionRuleTaxons { get; set; } = new List<PromotionRuleTaxon>();

    #endregion

    #region Metadata & Translation Support

    public IDictionary<string, string?>? PublicMetadata { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?>? PrivateMetadata { get; set; } = new Dictionary<string, string?>();

    [NotMapped]
    public IReadOnlyCollection<string> TranslatableFields =>
    [
        nameof(Name),
        nameof(PrettyName),
        nameof(Description),
        nameof(Permalink)
    ];

    #endregion

    #region Computed Properties

    public bool IsRoot => ParentId == null;
    public bool IsManual => !Automatic;
    public bool IsManualSortOrder => SortOrder == "manual";
    public string PageBuilderImageUrl => SquareImageUrl ?? ImageUrl ?? string.Empty;
    public string SeoTitle => string.IsNullOrWhiteSpace(MetaTitle) ? Name : MetaTitle;
    public string Slug
    {
        get => Permalink;
        private set => Permalink = value;
    }

    #endregion

    #region Caching

    private readonly object _cacheLock = new();
    private List<Guid>? _cachedSelfAndDescendantsIds;

    public IReadOnlyList<Guid> CachedSelfAndDescendantsIds
    {
        get
        {
            lock (_cacheLock)
            {
                if (_cachedSelfAndDescendantsIds != null)
                    return _cachedSelfAndDescendantsIds.AsReadOnly();

                List<Guid> ids = new List<Guid> { Id };
                CollectDescendantIds(this, ids);
                _cachedSelfAndDescendantsIds = ids;
                return _cachedSelfAndDescendantsIds.AsReadOnly();
            }
        }
    }

    private static void CollectDescendantIds(Taxon node, List<Guid> ids)
    {
        foreach (Taxon child in node.Children)
        {
            ids.Add(child.Id);
            CollectDescendantIds(child, ids);
        }
    }

    public void InvalidateDescendantsCache()
    {
        lock (_cacheLock)
        {
            _cachedSelfAndDescendantsIds = null;
        }
        foreach (Taxon child in Children)
            child.InvalidateDescendantsCache();
    }

    #endregion

    #region Constructors & Factory

    private Taxon() { }

    public static ErrorOr<Taxon> Create(
        string name,
        Guid taxonomyId,
        Guid? parentId = null,
        bool automatic = false,
        string? rulesMatchPolicy = null,
        string? sortOrder = null,
        bool hideFromNav = false,
        string? description = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        string? imageUrl = null,
        string? squareImageUrl = null,
        IDictionary<string, string?>? publicMetadata = null,
        IDictionary<string, string?>? privateMetadata = null)
    {
        rulesMatchPolicy ??= "all";
        sortOrder ??= "manual";

        string trimmedName = name.Trim();
        Taxon taxon = new Taxon
        {
            Name = trimmedName,
            Description = description?.Trim(),
            Automatic = automatic,
            RulesMatchPolicy = rulesMatchPolicy,
            SortOrder = sortOrder,
            HideFromNav = hideFromNav,
            TaxonomyId = taxonomyId,
            ParentId = parentId,
            MetaTitle = metaTitle?.Trim(),
            MetaDescription = metaDescription?.Trim(),
            MetaKeywords = metaKeywords?.Trim(),
            ImageUrl = imageUrl,
            SquareImageUrl = squareImageUrl,
            PublicMetadata = publicMetadata != null ? new Dictionary<string, string?>(publicMetadata) : new Dictionary<string, string?>(),
            PrivateMetadata = privateMetadata != null ? new Dictionary<string, string?>(privateMetadata) : new Dictionary<string, string?>()
        };

        taxon.SetPrettyName();
        taxon.SetPermalink(includeParentIfAvailable: false);
        taxon.AddDomainEvent(new Events.Created(taxon.Id, taxon));
        return taxon;
    }

    #endregion

    #region Business Operations

    public ErrorOr<Taxon> Update(
        string? name = null,
        Guid? parentId = null,
        string? description = null,
        bool? automatic = null,
        string? rulesMatchPolicy = null,
        string? sortOrder = null,
        bool? hideFromNav = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        string? imageUrl = null,
        string? squareImageUrl = null,
        IDictionary<string, string?>? publicMetadata = null,
        IDictionary<string, string?>? privateMetadata = null)
    {
        bool hasChanges = false;
        bool rulesPolicyChanged = false;
        bool parentChanged = false;

        string? prevImageUrl = ImageUrl;
        string? prevSquareImageUrl = SquareImageUrl;

        // Handle parent change (only update ParentId here, SetParent called from handler)
        if (parentId != ParentId)
        {
            if (parentId == Id)
                return Errors.SelfParent;

            ParentId = parentId;
            parentChanged = true;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != Name)
        {
            Name = name.Trim();
            hasChanges = true;
        }

        if (description != null && description.Trim() != Description)
        {
            Description = description.Trim();
            hasChanges = true;
        }

        if (automatic.HasValue && automatic.Value != Automatic)
        {
            Automatic = automatic.Value;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(rulesMatchPolicy) && rulesMatchPolicy != RulesMatchPolicy)
        {
            RulesMatchPolicy = rulesMatchPolicy;
            rulesPolicyChanged = true;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(sortOrder) && sortOrder != SortOrder)
        {
            SortOrder = sortOrder;
            hasChanges = true;
        }

        if (hideFromNav.HasValue && hideFromNav.Value != HideFromNav)
        {
            HideFromNav = hideFromNav.Value;
            hasChanges = true;
        }

        if (metaTitle != null && metaTitle != MetaTitle)
        {
            MetaTitle = metaTitle.Trim();
            hasChanges = true;
        }

        if (metaDescription != null && metaDescription != MetaDescription)
        {
            MetaDescription = metaDescription.Trim();
            hasChanges = true;
        }

        if (metaKeywords != null && metaKeywords != MetaKeywords)
        {
            MetaKeywords = metaKeywords.Trim();
            hasChanges = true;
        }

        if (imageUrl != ImageUrl)
        {
            ImageUrl = imageUrl;
            hasChanges = true;
        }

        if (squareImageUrl != SquareImageUrl)
        {
            SquareImageUrl = squareImageUrl;
            hasChanges = true;
        }

        if (publicMetadata != null)
        {
            PublicMetadata = new Dictionary<string, string?>(publicMetadata);
            hasChanges = true;
        }

        if (privateMetadata != null)
        {
            PrivateMetadata = new Dictionary<string, string?>(privateMetadata);
            hasChanges = true;
        }

        if (hasChanges)
        {
            AddDomainEvent(new Events.Updated(Id, this, parentChanged));

            bool imagesChanged = prevImageUrl != ImageUrl || prevSquareImageUrl != SquareImageUrl;
            if (imagesChanged)
            {
                AddDomainEvent(new Events.TouchFeaturedSections(Id));
                if (string.IsNullOrWhiteSpace(ImageUrl) && string.IsNullOrWhiteSpace(SquareImageUrl) &&
                    (!string.IsNullOrWhiteSpace(prevImageUrl) || !string.IsNullOrWhiteSpace(prevSquareImageUrl)))
                {
                    AddDomainEvent(new Events.RemoveFeaturedSections(Id));
                }
            }

            if (rulesPolicyChanged && Automatic)
            {
                RegenerateTaxonProducts(onlyOnce: true);
            }
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        if (Children.Any())
            return Errors.HasChildren;

        if (Classifications.Any())
            return Errors.HasClassifications;

        if (TaxonRules.Any())
            return Errors.HasRules;

        if (PromotionRuleTaxons.Any())
            return Errors.HasPromotionRules;

        AddDomainEvent(new Events.Deleted(Id, this));
        return Result.Deleted;
    }

    public ErrorOr<Taxon> SetParent(Taxon newParent)
    {
        if (newParent.Id == Id)
            return Errors.SelfParent;

        if (newParent.TaxonomyId != TaxonomyId)
            return Errors.ParentTaxonomyMismatch;

        if (IsAncestorOf(newParent))
            return Errors.CircularReference;

        if (Parent != null)
        {
            Parent.Children.Remove(this);
            Parent.InvalidateDescendantsCache();
        }

        ParentId = newParent.Id;
        Parent = newParent;

        if (!newParent.Children.Contains(this))
            newParent.Children.Add(this);

        SetPrettyName();
        SetPermalink();
        InvalidateDescendantsCache();
        newParent.InvalidateDescendantsCache();

        return this;
    }

    public ErrorOr<Taxon> AddRule(TaxonRule rule)
    {
        if (rule == null)
            return Errors.NullRule;

        if (IsManual)
            return Errors.AutomaticOnly;

        if (TaxonRules.Any(r => r.Id == rule.Id))
            return this;

        rule.SetTaxon(Id);
        TaxonRules.Add(rule);

        if (Automatic)
            RegenerateTaxonProducts(onlyOnce: false);

        AddDomainEvent(new Events.RuleAdded(Id, rule.Id));
        return this;
    }

    public ErrorOr<Taxon> RemoveRule(Guid ruleId)
    {
        TaxonRule? rule = TaxonRules.FirstOrDefault(r => r.Id == ruleId);
        if (rule == null)
            return Errors.RuleNotFound;

        TaxonRules.Remove(rule);

        if (Automatic)
            RegenerateTaxonProducts(onlyOnce: false);

        AddDomainEvent(new Events.RuleRemoved(Id, ruleId));
        return this;
    }

    public ErrorOr<Taxon> ClassifyProduct(Guid productId, int position = 0)
    {
        if (productId == Guid.Empty)
            return Errors.NullProduct;

        if (Classifications.Any(c => c.ProductId == productId))
            return Errors.ProductAlreadyClassified;

        ErrorOr<Classification> classification = Classification.Create(productId, Id, position);
        if (classification.IsError) return classification.Errors;

        Classifications.Add(classification.Value);
        AddDomainEvent(new Events.ProductClassified(Id, productId));

        return this;
    }

    public ErrorOr<Taxon> UnclassifyProduct(Guid productId)
    {
        Classification? classification = Classifications.FirstOrDefault(c => c.ProductId == productId);
        if (classification == null)
            return Errors.ProductNotClassified;

        Classifications.Remove(classification);
        AddDomainEvent(new Events.ProductUnclassified(Id, productId));

        return this;
    }

    public IEnumerable<Product> GetActiveProductsWithDescendants()
    {
        List<Product> products = new List<Product>();
        foreach (Classification c in Classifications)
        {
            if (c.Product != null && c.Product.IsActive)
                products.Add(c.Product);
        }

        foreach (Taxon child in Children)
        {
            products.AddRange(child.GetActiveProductsWithDescendants());
        }

        return products.Distinct().ToList();
    }

    #endregion

    #region Collection Management

    public void AddChild(Taxon child)
    {
        if (child == null || Children.Contains(child)) return;

        if (child.Parent != null && child.Parent != this)
        {
            child.Parent.Children.Remove(child);
            child.Parent.InvalidateDescendantsCache();
        }

        Children.Add(child);
        child.Parent = this;
        child.ParentId = Id;

        InvalidateDescendantsCache();
        child.InvalidateDescendantsCache();
    }

    internal void RemoveChild(Taxon child)
    {
        if (child == null || !Children.Contains(child)) return;

        Children.Remove(child);
        child.Parent = null;
        child.ParentId = null;

        InvalidateDescendantsCache();
        child.InvalidateDescendantsCache();
    }

    #endregion

    #region Hierarchy Operations

    public IEnumerable<Taxon> GetAllDescendants()
    {
        List<Taxon> descendants = new List<Taxon>();
        CollectAllDescendants(this, descendants);
        return descendants;
    }

    private static void CollectAllDescendants(Taxon node, List<Taxon> descendants)
    {
        foreach (Taxon child in node.Children)
        {
            descendants.Add(child);
            CollectAllDescendants(child, descendants);
        }
    }

    public IEnumerable<Taxon> GetAncestors()
    {
        List<Taxon> ancestors = new List<Taxon>();
        Taxon? current = Parent;
        while (current != null)
        {
            ancestors.Add(current);
            current = current.Parent;
        }
        return ancestors.AsEnumerable().Reverse();
    }

    public bool IsAncestorOf(Taxon other, int maxDepth = Constraints.DepthMax)
    {
        int depth = 0;
        Taxon? current = other.Parent;
        while (current != null && depth++ < maxDepth)
        {
            if (current.Id == Id) return true;
            current = current.Parent;
        }
        return false;
    }

    public bool IsDescendantOf(Taxon other)
    {
        return other.IsAncestorOf(this);
    }

    public int GetLevel()
    {
        int level = 0;
        Taxon? current = Parent;
        while (current != null)
        {
            level++;
            current = current.Parent;
        }
        return level;
    }

    #endregion

    #region Product Operations

    public void RegenerateTaxonProducts(bool onlyOnce = false)
    {
        if (!MarkedForRegenerateTaxonProducts) return;
        AddDomainEvent(new Events.RegenerateProducts(Id, onlyOnce));
        if (onlyOnce)
            MarkedForRegenerateTaxonProducts = false;
    }

    #endregion

    #region Slug & Pretty Name Generation

    public void SetPrettyName()
    {
        PrettyName = GeneratePrettyName();
    }

    public string GeneratePrettyName()
    {
        if (Parent?.PrettyName != null)
            return $"{Parent.PrettyName} -> {Name}";
        return Name;
    }

    public void SetPermalink()
    {
        Permalink = GenerateSlug();
    }

    public string GenerateSlug()
    {
        if (Parent != null)
        {
            string slugPart = Name.Parameterize();
            if (!string.IsNullOrWhiteSpace(Parent.Permalink))
                return string.Join('/', new[] { Parent.Permalink.TrimEnd('/'), slugPart }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return slugPart;
        }

        return Name.Parameterize();
    }

    public void RegeneratePrettyNameAndPermalink()
    {
        SetPrettyName();
        SetPermalink();

        foreach (TaxonTranslation t in Translations)
        {
            try { t.UpdatePrettyNameAndPermalink(this); } catch { }
        }

        foreach (Taxon child in Children)
        {
            try { child.RegeneratePrettyNameAndPermalinkRecursive(); } catch { }
        }
    }

    private void RegeneratePrettyNameAndPermalinkRecursive()
    {
        SetPrettyName();
        SetPermalink();

        foreach (TaxonTranslation t in Translations)
        {
            try { t.UpdatePrettyNameAndPermalink(this); } catch { }
        }

        foreach (Taxon child in Children)
        {
            try { child.RegeneratePrettyNameAndPermalinkRecursive(); } catch { }
        }
    }

    public ErrorOr<Success> ValidateForCreateAgainst(Taxonomy? taxonomy)
    {
        if (taxonomy == null) return Errors.TaxonomyRequired;
        if (ParentId == null && taxonomy.Taxons.Any(t => t.ParentId == null))
            return Errors.RootConflict;
        return Result.Success;
    }

    public ErrorOr<Success> UpdateNestedSetValues(int lft, int rgt, int depth)
    {
        if (lft >= rgt)
            return Errors.InvalidNestedSetValues;

        if (depth < Constraints.DepthMin || depth > Constraints.DepthMax)
            return Errors.InvalidDepth;

        Lft = lft;
        Rgt = rgt;
        Depth = depth;

        AddDomainEvent(new Events.Updated(Id, this));
        return Result.Success;
    }

    public ErrorOr<Success> UpdateChildIndex(int index)
    {
        if (index < 0)
            return Error.Validation("Taxon.InvalidChildIndex", "Child index must be non-negative.");

        ChildIndex = index;
        return Result.Success;
    }

    public void TouchAncestorsAndTaxonomy()
    {
        Taxon? current = Parent;
        while (current != null)
        {
            current.MarkAsUpdated();
            current.AddDomainEvent(new Events.Updated(current.Id, current));
            current = current.Parent;
        }

        if (Taxonomy != null)
        {
            Taxonomy.MarkAsUpdated();
            Taxonomy.AddDomainEvent(new Taxonomy.Events.Updated(Taxonomy.Id));
        }
    }

    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid TaxonId, Taxon? Taxon = null) : DomainEvent;
        public record Updated(Guid TaxonId, Taxon? Taxon = null, bool ParentChanged = false) : DomainEvent;
        public record Deleted(Guid TaxonId, Taxon? Taxon = null) : DomainEvent;
        public record RuleAdded(Guid TaxonId, Guid RuleId) : DomainEvent;
        public record RuleRemoved(Guid TaxonId, Guid RuleId) : DomainEvent;
        public record ProductClassified(Guid TaxonId, Guid ProductId) : DomainEvent;
        public record ProductUnclassified(Guid TaxonId, Guid ProductId) : DomainEvent;
        public record RegenerateProducts(Guid TaxonId, bool OnlyOnce) : DomainEvent;
        public record TouchFeaturedSections(Guid TaxonId) : DomainEvent;
        public record RemoveFeaturedSections(Guid TaxonId) : DomainEvent;
        public record Moved(Guid TaxonId, Guid? ParentId, int NewIndex) : DomainEvent;
    }

    #endregion
}