using Core.Catalog.Products;
using Core.Catalog.Prototypes;

using ErrorOr;

using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.Parameterizable;
using SharedKernel.Domain.Attributes.Positionable;
using SharedKernel.Domain.Attributes.TranslatableResource;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Properties;

/// <summary>
/// Domain model representing a product Property.
/// Mirrors Spree::Property: has name, presentation, kind, filterable flag,
/// display_on target, and ordering (position).
/// </summary>
public sealed class Property :
    AuditableEntity,
    IParameterizableName,
    IMetadataSupport,
    IPositionable,
    ITranslatable<PropertyTranslation>
{
    #region Properties

    /// <summary>
    /// Internal identifier name (required, unique).
    /// Example: "material".
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// User-facing label (required).
    /// Example: "Material".
    /// </summary>
    public string Presentation { get; set; } = null!;

    /// <summary>
    /// Type of property value (short text, long text, number, rich text).
    /// Mirrors Spree::Property#kind.
    /// </summary>
    public PropertyKind Kind { get; set; } = PropertyKind.ShortText;

    /// <summary>
    /// Whether this property is filterable (used in storefront filtering).
    /// </summary>
    public bool Filterable { get; set; }

    /// <summary>
    /// Where the property should be displayed (frontend, backend, both, or none).
    /// Mirrors Spree::Property#display_on.
    /// </summary>
    public DisplayOn DisplayOn { get; set; } = DisplayOn.Both;

    /// <summary>
    /// Position for ordering in lists.
    /// </summary>
    public int Position { get; set; }
    #endregion

    #region Relationships
    public ICollection<PropertyPrototype> PrototypeProperties { get; set; } = new List<PropertyPrototype>();
    public ICollection<ProductProperty> ProductProperties { get; set; } = new List<ProductProperty>();
    public IEnumerable<Product> Products => ProductProperties.Select(pp => pp.Product);
    public IEnumerable<Prototype> Prototypes => PrototypeProperties.Select(pp => pp.Prototype).Where(p => p != null).Cast<Prototype>();

    // Translations
    public ICollection<PropertyTranslation> Translations { get; set; } = new List<PropertyTranslation>();
    #endregion

    #region Metadata
    public IDictionary<string, string?>? PublicMetadata { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?>? PrivateMetadata { get; set; } = new Dictionary<string, string?>();

    public IReadOnlyCollection<string> TranslatableFields => [nameof(Presentation)];

    #endregion

    #region Errors

    public static class Errors
    {
        // Validations:
        // ID: required, non-empty
        public static Error IdRequired => Error.Validation("Property.InvalidId", "Property ID is required.");

        // DisplayOn: valid enum
        public static Error InvalidDisplayOn => Error.Validation(
            "Property.InvalidDisplayOn",
            "Invalid display target. Must be one of: None, Frontend, Backend, Both."
        );

        // Kind: valid enum
        public static Error InvalidKind => Error.Validation(
            "Property.InvalidKind",
            "Invalid property kind. Must be one of: ShortText, LongText, Number, RichText."
        );

        // NotFound:
        public static Error NotFound(Guid id) => Error.NotFound(
            "Property.NotFound",
            $"Property with ID '{id}' was not found."
        );

        // Conflict:
        public static Error NameAlreadyExists(string name) => Error.Conflict(
            "Property.NameAlreadyExists",
            $"A property with the name '{name}' already exists."
        );

        // Delete:
        // In use by product properties
        public static Error HasDependentProductProperties => Error.Validation(
            "Property.HasDependentProductProperties",
            "Cannot delete property while it has associated product properties. Remove associations first."
        );

        public static Error UnexpectedError(string operationName, Exception? ex = null) => Error.Unexpected(
          code: $"Property.{operationName}UnexpectedError",
          description: $"An unexpected error occurred during execution of {operationName} operation on Property. {ex?.Message}");
    }

    #endregion

    #region Constructors
    private Property() { }
    #endregion

    #region Factory

    public static ErrorOr<Property> Create(
        string name,
        string presentation,
        PropertyKind kind = PropertyKind.ShortText,
        bool filterable = false,
        DisplayOn displayOn = DisplayOn.Both,
        int position = 0,
        IDictionary<string, string?>? publicMetadata = null,
        IDictionary<string, string?>? privateMetadata = null)
    {
        // Validation: already in fluent validation
        // Create: new instance
        Property property = new()
        {
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Kind = kind,
            Filterable = filterable,
            DisplayOn = displayOn,
            Position = Math.Max(position, PositionableConstraints.PositionMin)
        };

        // Assign: optional metadata if provided
        if (publicMetadata != null)
            property.PublicMetadata = new Dictionary<string, string?>(publicMetadata);
        if (privateMetadata != null)
            property.PrivateMetadata = new Dictionary<string, string?>(privateMetadata);

        // Raise: create events
        property.AddDomainEvent(new Events.Created(property.Id));
        return property;
    }

    #endregion

    #region Behavior

    public ErrorOr<Property> Update(
        string? name = null,
        string? presentation = null,
        PropertyKind? kind = null,
        bool? filterable = null,
        DisplayOn? displayOn = null,
        int? position = null,
        IDictionary<string, string?>? publicMetadata = null,
        IDictionary<string, string?>? privateMetadata = null)
    {
        var changedFields = new HashSet<string>();

        if (!string.IsNullOrWhiteSpace(name) && name != Name)
        {
            Name = name.Trim();
            changedFields.Add(nameof(Name));
        }

        if (!string.IsNullOrWhiteSpace(presentation) && presentation != Presentation)
        {
            Presentation = presentation.Trim();
            changedFields.Add(nameof(Presentation));
        }

        if (kind.HasValue && kind.Value != Kind)
        {
            Kind = kind.Value;
            changedFields.Add(nameof(Kind));
        }

        if (filterable.HasValue && filterable.Value != Filterable)
        {
            bool old = Filterable;
            Filterable = filterable.Value;
            changedFields.Add(nameof(Filterable));
        }

        if (displayOn.HasValue && displayOn.Value != DisplayOn)
        {
            DisplayOn = displayOn.Value;
            changedFields.Add(nameof(DisplayOn));
        }

        if (position.HasValue && position.Value != Position)
        {
            Position = Math.Clamp(position.Value, PositionableConstraints.PositionMin, PositionableConstraints.PositionMax);
            changedFields.Add(nameof(Position));
        }

        if (publicMetadata != null)
        {
            PublicMetadata = new Dictionary<string, string?>(publicMetadata);
        }

        if (privateMetadata != null)
        {
            PrivateMetadata = new Dictionary<string, string?>(privateMetadata);
        }

        // Update: all product if DEPENDENCY fields changed
        if (changedFields.Overlaps([nameof(Name), nameof(Presentation), nameof(Kind), nameof(Filterable), nameof(DisplayOn), nameof(Position)]))
        {
            TouchAllProducts();
        }

        // Raise: update event if any changes
        if (Filterable)
        {
            EnsureProductPropertiesHaveFilterParams();
        }

        return this;
    }

    // Ported helpers to mimic Rails scopes/defaults
    public static IQueryable<Property> ApplyDefaultOrdering(IQueryable<Property> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return query.OrderBy(p => p.Position).ThenBy(p => p.CreatedAt);
    }

    public static IQueryable<Property> ApplySorted(IQueryable<Property> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return query.OrderBy(p => p.Name);
    }

    public static IQueryable<Property> FilterableScope(IQueryable<Property> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return query.Where(p => p.Filterable);
    }

    /// <summary>
    /// Returns unique pairs of (filter_param, value) from the associated product properties.
    /// If productPropertiesScope is provided, filters by those ProductProperty Ids.
    /// Note: this operates on the loaded ProductProperties collection; for large datasets prefer a repository query.
    /// </summary>
    public List<(string? FilterParam, string Value)> UniqValues(IEnumerable<Guid>? productPropertiesScope = null)
    {
        IEnumerable<ProductProperty> props = ProductProperties.AsEnumerable();
        if (productPropertiesScope != null)
        {
            HashSet<Guid> ids = productPropertiesScope.ToHashSet();
            props = props.Where(pp => ids.Contains(pp.Id));
        }

        List<(string? FilterParam, string Value)> pairs = props
            .Where(pp => !string.IsNullOrWhiteSpace(pp.Value))
            .Select(pp => (pp.FilterParam, pp.Value))
            .Distinct()
            .ToList();

        return pairs;
    }


    public ErrorOr<Deleted> Delete()
    {
        if (ProductProperties.Any())
            return Errors.HasDependentProductProperties;

        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }

    public List<ProductProperty>? EnsureProductPropertiesHaveFilterParams()
    {
        if (!Filterable) return null;

        if (ProductProperties.Count == 0) return null;

        List<ProductProperty> missingProducts = ProductProperties
            .Where(pp => string.IsNullOrWhiteSpace(pp.FilterParam) && !string.IsNullOrWhiteSpace(pp.Value))
            .Distinct()
            .ToList();

        if (!missingProducts.Any()) return null;

        // Generate: missing filter params for existing product properties
        foreach (ProductProperty pp in ProductProperties.Where(pp =>
                     string.IsNullOrWhiteSpace(pp.FilterParam) &&
                     !string.IsNullOrWhiteSpace(pp.Value)))
        {
            pp.EnsureFilterParam();
        }

        return missingProducts;
    }

    public void TouchAllProducts()
    {
        //foreach (var prod in Products)
        //    prod.Touch();
    }

    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid PropertyId) : DomainEvent;
        public record Updated(Guid PropertyId) : DomainEvent;
        public record Deleted(Guid PropertyId) : DomainEvent;
        public record FilterableChanged(Guid PropertyId, bool OldValue, bool NewValue) : DomainEvent;
    }

    #endregion
}

/// <summary>
/// Type of property values (Spree::Property#kind).
/// </summary>
public enum PropertyKind
{
    ShortText = 0,
    LongText = 1,
    Number = 2,
    RichText = 3
}

/// <summary>
/// Where a property should be displayed (Spree::Property#display_on).
/// </summary>
public enum DisplayOn
{
    None = 0,
    Frontend = 1,
    Backend = 2,
    Both = 3
}
