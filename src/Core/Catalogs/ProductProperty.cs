using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Catalogs;

/// <summary>
/// Domain model for ProductProperty (join between Product and Property).
/// Mirrors Spree::ProductProperty behaviour: holds value, computed filter param and position (acts_as_list).
/// Persistence-level uniqueness (ProductId + PropertyId) must be enforced by DB/EF configuration.
/// </summary>
public sealed class ProductProperty : AuditableEntity
{
    #region Properties

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = default!;

    // The user-provided value for the property (translatable in Spree).
    public string? Value { get; set; }

    // Computed parameterized representation used for filtering (Spree: filter_param).
    public string? FilterParam { get; set; }

    // Acts-as-list scope column in Spree. Nullable to match Rails allow_blank/allow_nil behaviour.
    public int? Position { get; set; }

    #endregion

    #region Constructors / Factory

    private ProductProperty() { }

    public static ProductProperty Create(Guid productId, Guid propertyId, string value, int? position = null)
    {
        var pp = new ProductProperty
        {
            ProductId = productId,
            PropertyId = propertyId,
            Position = position
        };
        pp.SetValue(value);
        return pp;
    }

    #endregion

    #region Behaviour

    /// <summary>
    /// Sets value and ensures the filter param is computed.
    /// Trims the value (mirrors auto_strip_attributes :value).
    /// </summary>
    public void SetValue(string? value)
    {
        Value = value?.Trim();
        EnsureFilterParam();
        // mark updated for caching/audit purposes
        this.MarkAsUpdated();
        Product?.MarkAsUpdated();
        Property?.MarkAsUpdated();
    }

    /// <summary>
    /// Ensure FilterParam is present when Value exists and property is filterable.
    /// </summary>
    public void EnsureFilterParam()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            FilterParam = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(FilterParam))
        {
            FilterParam = ComputeFilterParam(Value);
        }
    }

    #endregion

    #region Utilities (compute filter param)

    private static string ComputeFilterParam(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Normalize (remove diacritics), lowercase, replace whitespace with '-', remove invalid chars.
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();

        // Replace whitespace sequences with single dash
        cleaned = Regex.Replace(cleaned, @"\s+", "-");

        // Keep alphanumeric, dash and underscore
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9\-_]", string.Empty);

        // Trim edge dashes/underscores
        cleaned = cleaned.Trim('-', '_');

        return cleaned;
    }

    #endregion

    #region Validation helpers & errors

    public static List<Error> ValidateModel(Guid productId, Guid propertyId, string? value)
    {
        var errors = new List<Error>();

        if (productId == Guid.Empty)
            errors.Add(Errors.ProductRequired);

        if (propertyId == Guid.Empty)
            errors.Add(Errors.PropertyRequired);

        if (string.IsNullOrWhiteSpace(value))
            errors.Add(Errors.ValueRequired);

        return errors;
    }

    public static class Errors
    {
        public static Error ProductRequired => Error.Validation("ProductProperty.ProductRequired", "Product is required.");
        public static Error PropertyRequired => Error.Validation("ProductProperty.PropertyRequired", "Property is required.");
        public static Error ValueRequired => Error.Validation("ProductProperty.ValueRequired", "Property value is required.");
        public static Error AlreadyAssigned(Guid productId, Guid propertyId) =>
            Error.Conflict("ProductProperty.AlreadyAssigned", $"Property '{propertyId}' is already assigned to Product '{productId}'.");
        public static Error NotFound(Guid id) => Error.NotFound("ProductProperty.NotFound", $"ProductProperty with ID '{id}' was not found.");
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid ProductPropertyId) : DomainEvent;
        public record Updated(Guid ProductPropertyId) : DomainEvent;
        public record Deleted(Guid ProductPropertyId) : DomainEvent;
    }

    #endregion
}
