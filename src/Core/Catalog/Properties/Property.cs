using Core.Catalog.Products;
using Core.Catalog.Prototypes;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Properties;

/// <summary>
/// Domain model representing a product Property.
/// Mirrors Spree::Property: has name, presentation, kind, filterable flag,
/// display_on target, and ordering (position).
/// </summary>
public sealed class Property : AuditableEntity
{
    #region Properties

    /// <summary>
    /// Internal identifier name (required, unique).
    /// Example: "material".
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// User-facing label (required).
    /// Example: "Material".
    /// </summary>
    public string Presentation { get; private set; } = null!;

    /// <summary>
    /// Type of property value (short text, long text, number, rich text).
    /// Mirrors Spree::Property#kind.
    /// </summary>
    public PropertyKind Kind { get; private set; } = PropertyKind.ShortText;

    /// <summary>
    /// Whether this property is filterable (used in storefront filtering).
    /// </summary>
    public bool Filterable { get; private set; }

    /// <summary>
    /// Where the property should be displayed (frontend, backend, both, or none).
    /// Mirrors Spree::Property#display_on.
    /// </summary>
    public DisplayOn DisplayOn { get; private set; } = DisplayOn.Both;

    /// <summary>
    /// Position for ordering in lists.
    /// </summary>
    public int Position { get; private set; }

    public ICollection<PrototypeProperty> PrototypeProperties { get; set; } = new List<PrototypeProperty>();
    public ICollection<ProductProperty> ProductProperties { get; set; } = new List<ProductProperty>();
    public IEnumerable<Product> Products => ProductProperties.Select(pp => pp.Product);
    public IEnumerable<Prototype> Prototypes => PrototypeProperties.Select(pp => pp.Prototype);
    #endregion

    #region Constraints

    public static class Constraints
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 100;

        public const int PresentationMinLength = 2;
        public const int PresentationMaxLength = 255;

        public const int PositionMin = 0;
        public const int PositionMax = 100000;
    }

    #endregion

    #region Errors

    public static class Errors
    {
        #region Validations
        // ID: required, non-empty
        public static Error IdRequired => Error.Validation("Property.InvalidId", "Property ID is required.");

        // Name: required, length
        public static Error NameRequired => Error.Validation("Property.NameRequired", "Property name is required.");
        public static Error InvalidNameLength => Error.Validation(
            "Property.InvalidNameLength",
            $"Property name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters long."
        );

        // Presentation: required, length
        public static Error PresentationRequired => Error.Validation("Property.PresentationRequired", "Property presentation is required.");
        public static Error InvalidPresentationLength => Error.Validation(
            "Property.InvalidPresentationLength",
            $"Property presentation must be between {Constraints.PresentationMinLength} and {Constraints.PresentationMaxLength} characters long."
        );

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

        // Position: non-negative
        public static Error InvalidPosition => Error.Validation(
            "Property.InvalidPosition",
            $"Position must be between {Constraints.PositionMin} and {Constraints.PositionMax}."
        );

        #endregion

        public static Error NotFound(Guid id) => Error.NotFound(
            "Property.NotFound",
            $"Property with ID '{id}' was not found."
        );

        public static Error NameAlreadyExists(string name) => Error.Conflict(
            "Property.NameAlreadyExists",
            $"A property with the name '{name}' already exists."
        );

        // Cannot delete if product is using this property
        public static Error CannotDeleteInUse(Guid id, int usageCount) => Error.Validation(
            "Property.CannotDeleteInUse",
            $"Cannot delete property '{id}' because it is used by {usageCount} product(s)."
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
        int position = 0)
    {
        var property = new Property
        {
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Kind = kind,
            Filterable = filterable,
            DisplayOn = displayOn,
            Position = Math.Max(position, Constraints.PositionMin)
        };

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
        int? position = null)
    {
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(name) && name != Name)
        {
            var result = SetName(name);
            if (result.IsError) return result.Errors;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(presentation) && presentation != Presentation)
        {
            var result = SetPresentation(presentation);
            if (result.IsError) return result.Errors;
            changed = true;
        }

        if (kind.HasValue && kind.Value != Kind)
        {
            Kind = kind.Value;
            changed = true;
        }

        if (filterable.HasValue && filterable.Value != Filterable)
        {
            SetFilterable(filterable.Value);
            changed = true;
        }

        if (displayOn.HasValue && displayOn.Value != DisplayOn)
        {
            DisplayOn = displayOn.Value;
            changed = true;
        }

        if (position.HasValue && position.Value != Position)
        {
            SetPosition(position.Value);
            changed = true;
        }

        if (changed)
        {
            MarkAsUpdated();
            AddDomainEvent(new Events.Updated(Id));
        }

        return this;
    }

    public ErrorOr<Success> SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Errors.NameRequired;
        if (name.Length < Constraints.NameMinLength || name.Length > Constraints.NameMaxLength)
            return Errors.InvalidNameLength;

        Name = name.Trim();
        MarkAsUpdated();
        AddDomainEvent(new Events.Updated(Id));
        return Result.Success;
    }

    public ErrorOr<Success> SetPresentation(string presentation)
    {
        if (string.IsNullOrWhiteSpace(presentation)) return Errors.PresentationRequired;
        if (presentation.Length > Constraints.PresentationMaxLength)
            return Errors.InvalidPresentationLength;

        Presentation = presentation.Trim();
        MarkAsUpdated();
        AddDomainEvent(new Events.Updated(Id));
        return Result.Success;
    }

    public void SetKind(PropertyKind kind)
    {
        if (kind != Kind)
        {
            Kind = kind;
            MarkAsUpdated();
            AddDomainEvent(new Events.Updated(Id));
        }
    }

    public void SetFilterable(bool filterable)
    {
        if (filterable != Filterable)
        {
            var old = Filterable;
            Filterable = filterable;
            MarkAsUpdated();
            AddDomainEvent(new Events.FilterableChanged(Id, old, filterable));

            if (Filterable)
                EnsureProductPropertiesHaveFilterParams();
        }
    }

    public void SetPosition(int position)
    {
        Position = Math.Clamp(position, Constraints.PositionMin, Constraints.PositionMax);
        MarkAsUpdated();
        AddDomainEvent(new Events.Updated(Id));
    }

    public ErrorOr<Deleted> Delete()
    {
        if (ProductProperties.Any())
            return Errors.CannotDeleteInUse(Id, ProductProperties.Count);



        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }

    public List<ProductProperty>? EnsureProductPropertiesHaveFilterParams()
    {
        if (!Filterable) return null;

        if (ProductProperties.Count == 0) return null;

        var missingProducts = ProductProperties
            .Where(pp => string.IsNullOrWhiteSpace(pp.FilterParam) && !string.IsNullOrWhiteSpace(pp.Value))
            .Distinct()
            .ToList();

        if (!missingProducts.Any()) return null;

        // Generate: missing filter params for existing product properties
        foreach (var pp in ProductProperties.Where(pp =>
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
