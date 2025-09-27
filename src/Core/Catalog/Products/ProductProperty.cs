using Core.Catalog.Properties;
using Core.Commons.Extensions;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Products;

/// <summary>
/// Represents the association between a Product and a Property with a concrete value.
/// Ported from Spree's ProductProperty model (simplified).
/// </summary>
public sealed class ProductProperty : AuditableEntity
{
    #region Properties

    /// <summary>
    /// The actual value for the property on this product (translatable in Spree).
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// A parameterized version used for filtering (e.g. "cotton-100").
    /// </summary>
    public string? FilterParam { get; private set; }

    /// <summary>
    /// Position used for ordering (acts_as_list scope: product)
    /// </summary>
    public int Position { get; private set; }

    #endregion

    #region Relationships

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public Guid PropertyId { get; private set; }
    public Property Property { get; private set; } = null!;


    #endregion

    #region Constructors

    private ProductProperty() { }

    public ProductProperty(Guid productId, Guid propertyId, string value, int position = 0)
    {
        ProductId = productId;
        PropertyId = propertyId;
        Value = value?.Trim() ?? string.Empty;
        Position = Math.Max(0, position);
        EnsureFilterParam();
    }

    #endregion

    #region Factory

    public static ErrorOr<ProductProperty> Create(
        Guid productId,
        Guid propertyId,
        string value,
        int position = 0)
    {
        // Validate: ProductId
        if (productId == Guid.Empty)
            return Errors.ProductIdRequired;

        // Validate: PropertyId
        if (propertyId == Guid.Empty)
            return Errors.PropertyIdRequired;

        // Validate: Value (required, max length, allowed characters)
        if (string.IsNullOrWhiteSpace(value))
            return Errors.ValueRequired;
        value = value.Trim();
        if (value.Length > Constraints.MaxValueLength)
            return Errors.ValueTooLong;
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, Constraints.ValueAllowedPattern))
            return Errors.InvalidValue;
        var filterParamCandidate = value.ComputeFilterParam();

        // Validate: FilterParam (max length, allowed characters)
        if (filterParamCandidate.Length > Constraints.MaxFilterParamLength)
            return Errors.FilterParamTooLong;
        if (!System.Text.RegularExpressions.Regex.IsMatch(filterParamCandidate, Constraints.FilterParamAllowedPattern))
            return Errors.InvalidFilterParam;
       
        var productProperty = new ProductProperty(
            productId: productId,
            propertyId: propertyId,
            value: value,
            position: position
        );

        productProperty.MarkAsCreated();
        productProperty.AddDomainEvent(new Events.Created(productProperty.Id));
        return productProperty;
    }

    #endregion

    #region Behavior

    public ErrorOr<Success> Update(string? value = null, int? position = null)
    {
        var changed = false;

        if (value != null && value.Trim() != Value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Errors.ValueRequired;

            Value = value.Trim();
            changed = true;
        }

        if (position.HasValue && position.Value != Position)
        {
            Position = Math.Max(0, position.Value);
            changed = true;
        }

        if (changed)
        {
            EnsureFilterParam();
            MarkAsUpdated();
            AddDomainEvent(new Events.Updated(Id));
        }

        return Result.Success;
    }

    /// <summary>
    /// Ensure the FilterParam is present when appropriate (property is filterable).
    /// Generates a parameter candidate from the value when missing.
    /// </summary>
    public void EnsureFilterParam()
    {
        if (!Property.Filterable)
            return;

        if (!string.IsNullOrWhiteSpace(FilterParam))
            return;

        var candidate = Value;
        if (string.IsNullOrWhiteSpace(candidate))
            return;

        FilterParam = candidate.ComputeFilterParam();
    }
    #endregion

    #region Contraints
    public static class Constraints
    {
        public const int MaxValueLength = 1000;
        public const string ValueAllowedPattern = @"^[\w\s\p{P}\p{S}]{1,1000}$"; // Allow letters, digits, whitespace, punctuation, symbols
        public const int MaxFilterParamLength = 100;
        public const string FilterParamAllowedPattern = @"^[a-z0-9-]{1,100}$"; // Lowercase alphanumeric and hyphens
    }
    #endregion

    #region Errors

    public static class Errors
    {
        // Value: required, max length, allowed characters
        public static Error ValueRequired => Error.Validation(
            "ProductProperty.ValueRequired",
            "Value is required."
        );

        public static Error ValueTooLong => Error.Validation(
            "ProductProperty.ValueTooLong",
            $"Value must be at most {Constraints.MaxValueLength} characters long."
        );

        public static Error InvalidValue => Error.Validation(
            "ProductProperty.InvalidValue",
            $"Value must match the pattern: {Constraints.ValueAllowedPattern}"
        );

        // FilterParam: max length, allowed characters
        public static Error FilterParamTooLong => Error.Validation(
            "ProductProperty.FilterParamTooLong",
            $"FilterParam must be at most {Constraints.MaxFilterParamLength} characters long."
        );
        public static Error InvalidFilterParam => Error.Validation(
            "ProductProperty.InvalidFilterParam",
            $"FilterParam must match the pattern: {Constraints.FilterParamAllowedPattern}"
        );

        // ProductId: required
        public static Error ProductIdRequired => Error.Validation(
            "ProductProperty.ProductIdRequired",
            "ProductId is required."
        );

        // PropertyId: required
        public static Error PropertyIdRequired => Error.Validation(
            "ProductProperty.PropertyIdRequired",
            "PropertyId is required."
        );

        // Uniqueness: ProductId + PropertyId must be unique
        public static Error ProductPropertyAlreadyExists(Guid productId, Guid propertyId) => Error.Conflict(
            "ProductProperty.AlreadyExists",
            $"ProductProperty for ProductId '{productId}' and PropertyId '{propertyId}' already exists."
        );
    }

    #endregion
    #region Events

    public static class Events
    {
        public record Created(Guid ProductPropertyId) : DomainEvent;
        public record Updated(Guid ProductPropertyId) : DomainEvent;
        public record Deleted(Guid ProductPropertyId) : DomainEvent;
    }

    #endregion
}
