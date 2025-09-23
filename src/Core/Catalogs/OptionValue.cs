using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Catalogs;

/// <summary>
/// Domain model converted from Spree::OptionValue (simplified).
/// - Represents a selectable option value (e.g. "Red") that belongs to an OptionType (e.g. "Color").
/// - Persistence concerns (uniqueness, translations, nested-list behavior, attached files) belong to infra layer.
/// </summary>
public sealed class OptionValue : AuditableEntity
{
    #region Properties

    public string Name { get; set; } = default!;
    public string Presentation { get; set; } = default!;
    public int Position { get; set; }

    public Guid OptionTypeId { get; set; }
    public OptionType OptionType { get; set; } = default!;

    #endregion

    #region Relationships

    public ICollection<VariantOptionValue> VariantOptionValues { get; set; } = new List<VariantOptionValue>();
    public IEnumerable<Variant> Variants => VariantOptionValues.Select(ovv => ovv.Variant);
    public IEnumerable<Product> Products => Variants.Select(v => v.Product);

    #endregion

    #region Factory / Update

    private OptionValue() { }

    public static OptionValue Create(Guid optionTypeId, string name, string presentation, int position)
        => new OptionValue
        {
            OptionTypeId = optionTypeId,
            Name = name,
            Presentation = presentation,
            Position = position
        };

    public void Update(string? name = null, string? presentation = null, int? position = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name!;
        if (!string.IsNullOrWhiteSpace(presentation)) Presentation = presentation!;
        if (position.HasValue) Position = position.Value;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Returns combined presentation: "OptionTypePresentation: Presentation" (e.g. "Color: Red").
    /// Falls back to the value's presentation when OptionType is not available.
    /// </summary>
    public string DisplayPresentation =>
        (OptionType != null && !string.IsNullOrWhiteSpace(OptionType.Presentation))
            ? $"{OptionType.Presentation}: {Presentation}"
            : Presentation;

    /// <summary>
    /// Touch all related variants (mark updated). Persistence must be done by application layer.
    /// </summary>
    public void TouchAllVariants()
    {
        foreach (var ovv in VariantOptionValues)
        {
            try { ovv.Variant?.MarkAsUpdated(); } catch { }
        }
        this.MarkAsUpdated();
    }

    /// <summary>
    /// Touch all related products (via variants). Persistence must be done by application layer.
    /// </summary>
    public void TouchAllProducts()
    {
        foreach (var product in Products)
        {
            try { product?.MarkAsUpdated(); } catch { }
        }
        this.MarkAsUpdated();
    }

    /// <summary>
    /// Legacy compatibility: a parameterized filter key derived from the name.
    /// </summary>
    public string FilterParam() => Parameterize(Name);

    private static string Parameterize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

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
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9\-_]", string.Empty);
        return cleaned.Trim('-', '_');
    }

    #endregion

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        // Name: required, unique, 1-100 chars, alphanumeric, hyphens, underscores
        public const int NameMinLength = 1;
        public const int NameMaxLength = 100;
        public const string NameRegex = @"^[a-zA-Z0-9_-]+$";

        // Presentation: required, 1-150 chars, alphanumeric, spaces, hyphens, underscores
        public const int PresentationMinLength = 1;
        public const int PresentationMaxLength = 150;
        public const string PresentationRegex = @"^[a-zA-Z0-9 _-]+$";

        // Position: Index-0ed, non-negative integer
        public const int PositionMinValue = 0;
    }

    public static class Errors
    {
        public static Error IdRequired => Error.Validation("OptionValue.IdRequired", "OptionValue ID is required.");
        public static Error OptionTypeRequired => Error.Validation("OptionValue.OptionTypeRequired", "OptionType is required.");

        public static Error NameRequired => Error.Validation("OptionValue.NameRequired", "Option value name is required.");
        public static Error InvalidNameLength => Error.Validation("OptionValue.InvalidNameLength", $"Name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters.");
        public static Error InvalidNameFormat => Error.Validation("OptionValue.InvalidNameFormat", $"Name can only contain alphanumeric characters, hyphens, and underscores.");

        public static Error PresentationRequired => Error.Validation("OptionValue.PresentationRequired", "Option value presentation is required.");
        public static Error InvalidPresentationLength => Error.Validation("OptionValue.InvalidPresentationLength", $"Presentation must be between {Constraints.PresentationMinLength} and {Constraints.PresentationMaxLength} characters.");
        public static Error InvalidPresentationFormat => Error.Validation("OptionValue.InvalidPresentationFormat", $"Presentation can only contain alphanumeric characters, spaces, hyphens, and underscores.");

        public static Error InvalidPosition => Error.Validation("OptionValue.InvalidPosition", $"Position must be a non-negative integer.");

        public static Error NotFound(Guid id) => Error.NotFound("OptionValue.NotFound", $"OptionValue with ID '{id}' was not found.");
        public static Error OptionValueUnexpected(string operation, string reason) => Error.Unexpected($"OptionValue.{operation}Unexpected", $"Unexpected error during '{operation}' operation: {reason}");

        public static Error DuplicateForOptionType(string optionValue, string optionType) => Error.Conflict("OptionValue.Duplicate", $"OptionValue '{optionValue}' already exists for OptionType '{optionType}'.");
        public static Error InUseByVariants(int variantCount) => Error.Conflict("OptionValue.InUseByVariants", $"OptionValue is in use by {variantCount} variant(s) and cannot be deleted.");

        public static Error CannotChangeOptionTypeIfInUse => Error.Conflict("OptionValue.CannotChangeOptionTypeIfInUse", "OptionType cannot be changed because the OptionValue is associated with existing Variants.");
    }

    public static List<Error> ValidateModel(string? name, string? presentation)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add(Errors.NameRequired);
        else if (name!.Length < Constraints.NameMinLength || name.Length > Constraints.NameMaxLength)
            errors.Add(Errors.InvalidNameLength);

        if (string.IsNullOrWhiteSpace(presentation))
            errors.Add(Errors.PresentationRequired);
        else if (presentation!.Length > Constraints.PresentationMaxLength)
            errors.Add(Errors.InvalidPresentationLength);

        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid OptionValueId) : DomainEvent;
        public record Updated(Guid OptionValueId) : DomainEvent;
        public record Deleted(Guid OptionValueId) : DomainEvent;
    }

    #endregion
}
