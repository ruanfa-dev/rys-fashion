using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model converted from Spree::OptionType (simplified).
/// Contains option values, product associations and helpers such as filter param and color detection.
/// Persistence behaviours (nested attributes, callbacks wiring, scopes) belong to application/infrastructure layers.
/// </summary>
public partial class OptionType : AuditableEntity
{
    #region Properties

    public string Name { get; set; } = default!;
    public string Presentation { get; set; } = default!;
    public bool Filterable { get; set; }
    public int Position { get; set; }

    #endregion

    #region Relationships

    public ICollection<OptionValue> OptionValues { get; set; } = new List<OptionValue>();
    public ICollection<ProductOptionType> ProductOptionTypes { get; set; } = new List<ProductOptionType>();
    public IEnumerable<Product> Products => ProductOptionTypes.Select(pot => pot.Product);

    public ICollection<PrototypeOptionType> PrototypeOptionTypes { get; set; } = new List<PrototypeOptionType>();
    public IEnumerable<Prototype> Prototypes => PrototypeOptionTypes.Select(op => op.Prototype);

    #endregion

    #region Constructors / Factory

    private OptionType() { }

    public static OptionType Create(string name, string presentation, bool filterable = false, int position = 0)
    {
        return new OptionType
        {
            Name = name,
            Presentation = presentation,
            Filterable = filterable,
            Position = position
        };
    }

    public void Update(string? name = null, string? presentation = null, bool? filterable = null, int? position = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name!;
        if (!string.IsNullOrWhiteSpace(presentation))
            Presentation = presentation!;
        if (filterable.HasValue)
            Filterable = filterable.Value;
        if (position.HasValue)
            Position = position.Value;
    }

    #endregion

    #region Domain helpers

    /// <summary>
    /// Parameterized form of the option type name — used by legacy code as filter_param.
    /// </summary>
    public string FilterParam() => Parameterize(Name);


    /// <summary>
    /// Touch (mark updated) all products associated with this option type.
    /// Application layer should persist changes (SaveChanges) after calling this.
    /// </summary>
    public void TouchAllProducts()
    {
        foreach (var pot in ProductOptionTypes)
        {
            try
            {
                pot.Product?.MarkAsUpdated();
            }
            catch
            {
                // swallow - product may be proxy/not loaded in some scenarios
            }
        }

        // Also bump this OptionType's updated timestamp so caches based on it will invalidate.
        this.MarkAsUpdated();
    }

    private static string Parameterize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        // Normalize, remove diacritics, lowercase, replace whitespace with '-' and remove invalid chars.
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
        cleaned = cleaned.Trim('-', '_');
        return cleaned;
    }

    #endregion

    #region Validation / Constraints / Errors 
    public static class Constraints
    {
        // Name: required, unique, 1-100 chars, alphanumeric, spaces, hyphens, underscores
        public const int NameMinLength = 1;
        public const int NameMaxLength = 100;
        public const string NameRegex = @"^[a-zA-Z0-9 _-]+$";

        // Presentation: required, max length 150, alphanumeric, spaces, hyphens, underscores
        public const int PresentationMaxLength = 150;
        public const int PresentationMinLength = 1;
        public const string PresentationRegex = @"^[a-zA-Z0-9 _-]+$";

        // Position: non-negative
        public const int PositionMinValue = 0;
    }

    public static class Errors
    {
        public static Error IdRequired => Error.Validation("OptionType.IdRequired", "Option type ID is required.");

        public static Error NameRequired => Error.Validation("OptionType.NameRequired", "Option type name is required.");
        public static Error InvalidNameLength => Error.Validation("OptionType.InvalidNameLength", $"Option type name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters.");
        public static Error InvalidNameFormat => Error.Validation("OptionType.InvalidNameFormat", "Option type name contains invalid characters. Only alphanumeric characters, spaces, hyphens and underscores are allowed.");

        public static Error PresentationRequired => Error.Validation("OptionType.PresentationRequired", "Option type presentation is required.");
        public static Error InvalidPresentationLength => Error.Validation("OptionType.InvalidPresentationLength", $"Presentation must be at most {Constraints.PresentationMaxLength} characters.");
        public static Error InvalidPresentationFormat => Error.Validation("OptionType.InvalidPresentationFormat", "Option type presentation contains invalid characters. Only alphanumeric characters, spaces, hyphens and underscores are allowed.");

        public static Error InvalidPosition => Error.Validation("OptionType.InvalidPosition", $"Position must be at least {Constraints.PositionMinValue}.");

        public static Error OptionTypeUnexpected(string operation, string reason) => Error.Unexpected($"OptionType.{operation}Unexpected", $"Unexpected error during '{operation}' operation: {reason}");
        public static Error NotFound(Guid id) => Error.NotFound("OptionType.NotFound", $"OptionType with ID '{id}' was not found.");
        public static Error DuplicateName(string name) => Error.Conflict("OptionType.DuplicateName", $"OptionType with name '{name}' already exists.");
        public static Error DuplicateForTaxon(Guid optionTypeId, Guid taxonId) => Error.Conflict("OptionType.Duplicate", $"OptionType '{optionTypeId}' is already assigned to Taxon '{taxonId}'.");
        
        public static Error HasProductAssociations(Guid optionTypeId) => Error.Validation("OptionType.HasProductAssociations", $"OptionType '{optionTypeId}' is associated with Products and cannot be deleted.");
        public static Error HasPrototypeAssociations(Guid optionTypeId) => Error.Validation("OptionType.HasPrototypeAssociations", $"OptionType '{optionTypeId}' is associated with Prototypes and cannot be deleted.");
       
        public static Error CannotDeleteDefault => Error.Validation("OptionType.CannotDeleteDefault", "The default OptionType cannot be deleted.");
        public static Error CannotDeleteLastOptionType => Error.Validation("OptionType.CannotDeleteLastOptionType", "At least one OptionType must exist.");
        public static Error CannotUnassignFromProducts(Guid optionTypeId) => Error.Validation("OptionType.CannotUnassignFromProducts", $"OptionType '{optionTypeId}' cannot be unassigned from Products.");

        public static Error CannotRemoveOptionValue(Guid optionValueId) => Error.Validation("OptionType.CannotRemoveOptionValue", $"OptionValue '{optionValueId}' cannot be removed as it is associated with Variants.");
        public static Error OptionValueNotFound(Guid optionValueId) => Error.NotFound("OptionType.OptionValueNotFound", $"OptionValue with ID '{optionValueId}' was not found.");

    }

    /// <summary>
    /// Lightweight validation helpers for application/handlers.
    /// </summary>
    public static List<Error> ValidateModel(string? name, string? presentation)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add(Errors.NameRequired);
        else
        {
            if (name!.Length < Constraints.NameMinLength || name.Length > Constraints.NameMaxLength)
                errors.Add(Errors.InvalidNameLength);
        }

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
        public record Created(Guid OptionTypeId) : DomainEvent;
        public record Updated(Guid OptionTypeId) : DomainEvent;
        public record Deleted(Guid OptionTypeId) : DomainEvent;
    }

    #endregion
}